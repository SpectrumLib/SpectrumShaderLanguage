using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using SSLang.Generated;

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

		public override object VisitShaderMetaStatement([NotNull] SSLParser.ShaderMetaStatementContext context)
		{
			var name = context.Name.Text;
			name = name.Substring(1, name.Length - 2);

			if (name.Length > 0)
			{
				GLSL.EmitComment($"Shader name: \"{name}\"");
				GLSL.EmitBlankLine();
				Info.Name = name;
			}
			else
				_WARN(context, "Shader name is an empty string, ignoring.");

			return null;
		}
	}
}
