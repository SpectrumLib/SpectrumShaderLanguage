using System;
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
		#endregion // Fields

		public SSLVisitor(CommonTokenStream tokens)
		{
			_tokens = tokens;
			GLSL = new GLSLBuilder();
			Info = new ShaderInfo();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void _THROW(RuleContext ctx, string msg)
		{
			var tk = _tokens.Get(ctx.SourceInterval.a);
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}
	}
}
