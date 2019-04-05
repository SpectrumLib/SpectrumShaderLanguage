﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// The root type that handles stepping through a parsed ssl file
	internal class SSLVisitor : SSLParserBaseVisitor<object>
	{
		#region Fields
		// Stream of tokens used to generate the visited tree
		private readonly CommonTokenStream _tokens;

		// The generated GLSL
		public readonly GLSLBuilder GLSL;

		// The reflection info built by the visitor
		public readonly ShaderInfo Info;

		// A list of warning messages generated during the translation process
		public readonly List<(uint, string)> Warnings;

		// The scope/variable manager
		public readonly ScopeManager ScopeManager;
		#endregion // Fields

		public SSLVisitor(CommonTokenStream tokens)
		{
			_tokens = tokens;
			GLSL = new GLSLBuilder();
			Info = new ShaderInfo();
			Warnings = new List<(uint, string)>();
			ScopeManager = new ScopeManager();
		}

		#region Utilities
		private void _WARN(uint line, string msg) => Warnings.Add((line, msg));
		private void _WARN(RuleContext ctx, string msg) => Warnings.Add((GetContextLine(ctx), msg));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void _THROW(RuleContext ctx, string msg)
		{
			var tk = _tokens.Get(ctx.SourceInterval.a);
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}

		private List<IToken> GetContextTokens(RuleContext ctx) => _tokens.Get(ctx.SourceInterval.a, ctx.SourceInterval.b) as List<IToken>;
		private IToken GetContextToken(RuleContext ctx, uint index) => _tokens.Get(ctx.SourceInterval.a + (int)index);
		private uint GetContextLine(RuleContext ctx) => (uint)_tokens.Get(ctx.SourceInterval.a).Line;
		#endregion // Utilities

		#region Public Helpers
		// Returns a long, but the value is guarenteed to be in the valid 32-bit range for the signedness
		public static long? ParseIntegerLiteral(string text, out bool isUnsigned, out string error)
		{
			string orig = text;
			bool isNeg = text.StartsWith("-");
			isUnsigned = text.EndsWith("u") || text.EndsWith("U");
			text = isNeg ? text.Substring(1) : text;
			text = isUnsigned ? text.Substring(0, text.Length - 1) : text;
			bool isHex = text.StartsWith("0x");
			text = isHex ? text.Substring(2) : text;

			uint res = 0;
			try
			{
				res = Convert.ToUInt32(text, isHex ? 16 : 10);
			}
			catch
			{
				error = $"Could not convert the text '{orig}' to an integer literal";
				return null;
			}

			if (isNeg && (res > ((uint)Int32.MaxValue + 1)))
			{
				error = $"The value {orig} is too large for a signed 32-bit integer";
				return null;
			}

			error = null;
			return isNeg ? -res : res;
		}
		#endregion // Public Helpers

		// This has to be overridden because we must manually visit all of the variable blocks before the functions
		public override object VisitFile([NotNull] SSLParser.FileContext context)
		{
			// Visit the meta statement first
			var meta = context.shaderMetaStatement();
			if (meta != null)
				Visit(meta);

			// Visit all of the blocks that create named variables first
			foreach (var ch in context.children)
			{ 
				var cctx = ch as SSLParser.TopLevelStatementContext;
				if (cctx == null)
					continue;

				bool isFunc = (cctx.stageFunction() != null) || (cctx.standardFunction() != null);
				if (!isFunc)
					Visit(cctx);
			}

			// Validate that the required blocks are present
			if (ScopeManager.Attributes.Count == 0)
				_THROW(context, "A shader is required to have an 'attributes' block to describe the vertex input.");
			if (ScopeManager.Outputs.Count == 0)
				_THROW(context, "A shader is required to have an 'output' block to describe the fragment stage output.");

			// Visit all functions
			foreach (var ch in context.children)
			{
				var cctx = ch as SSLParser.TopLevelStatementContext;
				if (cctx == null)
					continue;

				bool isFunc = (cctx.stageFunction() != null) || (cctx.standardFunction() != null);
				if (isFunc)
					Visit(cctx);
			}

			return null;
		}

		#region Top-Level
		public override object VisitShaderMetaStatement([NotNull] SSLParser.ShaderMetaStatementContext context)
		{
			var name = context.Name.Text;
			name = name.Substring(1, name.Length - 2);

			if (name.Length > 0)
			{
				GLSL.EmitCommentVar($"Shader name: \"{name}\"");
				GLSL.EmitBlankLineVar();
				Info.Name = name;
			}
			else
				_WARN(context, "Shader name is an empty string, ignoring.");

			return null;
		}

		public override object VisitAttributesStatement([NotNull] SSLParser.AttributesStatementContext context)
		{
			if (ScopeManager.Attributes.Count > 0) // Already encountered an attributes block
				_THROW(context, "A shader cannot have more than one 'attributes' block.");

			var block = context.typeBlock();
			if (block._Types.Count == 0)
				_THROW(context, "The 'attributes' block cannot be empty.");

			GLSL.EmitCommentVar("Vertex attributes");
			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				if (!ScopeManager.TryAddAttribute(tctx, out var vrbl, out var error))
					_THROW(tctx, error);
				if (!vrbl.Type.IsValueType())
					_THROW(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type.ToKeyword()}', only value types are allowed as attributes.");
				GLSL.EmitVertexAttribute(vrbl, loc);
				Info._attributes.Add((vrbl, loc));
				loc += vrbl.Type.GetSlotCount(vrbl.ArraySize);
			}
			GLSL.EmitBlankLineVar();

			return null;
		}

		public override object VisitOutputsStatement([NotNull] SSLParser.OutputsStatementContext context)
		{
			if (ScopeManager.Outputs.Count > 0) // Already encountered an outputs block
				_THROW(context, "A shader cannot have more than one 'outputs' block.");

			var block = context.typeBlock();
			if (block._Types.Count == 0)
				_THROW(context, "The 'output' block cannot be empty.");

			GLSL.EmitCommentVar("Fragment stage outputs");
			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				if (!ScopeManager.TryAddOutput(tctx, out var vrbl, out var error))
					_THROW(tctx, error);
				if (!vrbl.Type.IsValueType())
					_THROW(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type.ToKeyword()}', only value types are allowed as outputs.");
				if (vrbl.IsArray)
					_THROW(tctx, $"The output variable '{vrbl.Name}' cannot be an array.");
				GLSL.EmitFragmentOutput(vrbl, loc++);
				Info._outputs.Add(vrbl);
			}
			GLSL.EmitBlankLineVar();

			return null;
		}

		public override object VisitUniformStatement([NotNull] SSLParser.UniformStatementContext context)
		{
			var head = context.uniformHeader();
			var loc = ParseIntegerLiteral(head.Index.Text, out var isUnsigned, out var error);
			if (!loc.HasValue)
				_THROW(context, error);
			if (loc.Value < 0)
				_THROW(context, $"Uniforms cannot have a negative location.");

			GLSL.EmitCommentVar($"Uniform binding {loc.Value}");
			bool isHandle = context.variableDeclaration() != null;
			if (isHandle)
			{
				var vdec = context.variableDeclaration();
				if (!ScopeManager.TryAddUniform(vdec, out var vrbl, out error))
					_THROW(vdec, error);
				if (!vrbl.Type.IsHandleType())
					_THROW(context, $"The uniform '{vrbl.Name}' must be a handle type if declared outside of a block.");
				Info._uniforms.Add((vrbl, (uint)loc.Value, 0));
				GLSL.EmitUniform(vrbl, (uint)loc.Value);
			}
			else
			{

			}
			GLSL.EmitBlankLineVar();

			return null;
		}
		#endregion // Top-Level
	}
}
