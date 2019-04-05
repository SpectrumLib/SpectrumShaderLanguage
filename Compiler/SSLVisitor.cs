using System;
using System.Collections.Generic;
using System.Globalization;
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
		#endregion // Fields

		public SSLVisitor(CommonTokenStream tokens)
		{
			_tokens = tokens;
			GLSL = new GLSLBuilder();
			Info = new ShaderInfo();
			Warnings = new List<(uint, string)>();
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
	}
}
