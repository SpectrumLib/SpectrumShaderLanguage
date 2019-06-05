using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Performs the AST stepping and GLSL generation
	internal class Translator : SSLBaseVisitor<Expression>
	{
		#region Fields
		// Object references from the compiler
		private readonly CommonTokenStream _tokens;
		public readonly Compiler Compiler;
		public readonly CompilerOptions Options;

		// The shader info
		public ShaderInfo ShaderInfo { get; private set; }
		#endregion // Fields

		public Translator(CommonTokenStream tokens, Compiler compiler, CompilerOptions options)
		{
			_tokens = tokens;
			Compiler = compiler;
			Options = options;
		}

		#region Messaging
		public void Info(string msg) => Options.VerboseCallback?.Invoke(Compiler, CompilerStage.Translator, msg);
		public void Warn(RuleContext ctx, string msg) => Options.WarnCallback?.Invoke(Compiler, CompilerStage.Translator, GetContextLine(ctx), msg);
		public void Error(RuleContext ctx, string msg)
		{
			var tk = _tokens.Get(ctx.SourceInterval.a);
			throw new VisitException(new CompilerError(CompilerStage.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}
		public void Error(IToken tk, string msg) => throw new VisitException(new CompilerError(CompilerStage.Translator, (uint)tk.Line, (uint)tk.Column, msg));

		private uint GetContextLine(RuleContext ctx) => (uint)_tokens.Get(ctx.SourceInterval.a).Line;
		#endregion // Messaging

		public override Expression VisitFile([NotNull] SSL.FileContext context)
		{
			ShaderInfo = new ShaderInfo(Compiler.LIBRARY_VERSION);

			// Visit the meta statements first
			var verCtx = context.versionMetaStatement();
			if (verCtx != null)
				Visit(verCtx);
			else
				ShaderInfo.SourceVersion = new Version(0, 1, 0);

			// Visit the non-function top level statements first
			foreach (var ch in context.children)
			{
				var cctx = ch as SSL.TopLevelStatementContext;
				if (cctx == null)
					continue;

				bool isFunc = false;// (cctx.stageFunction() != null) || (cctx.standardFunction() != null);
				if (!isFunc)
					Visit(cctx);
			}

			return null;
		}

		#region Meta Statements
		public override Expression VisitVersionMetaStatement([NotNull] SSL.VersionMetaStatementContext context)
		{
			// If there is a version statement, we know that it is already valid because it was parsed already
			ShaderInfo.SourceVersion = new Version(context.Version.Text);
			return null;
		}
		#endregion // Meta Statements

		#region Variable Statements
		public override Expression VisitUniformStatement([NotNull] SSL.UniformStatementContext context)
		{
			return null;
		}

		public override Expression VisitAttrStatement([NotNull] SSL.AttrStatementContext context)
		{
			return null;
		}

		public override Expression VisitOutStatement([NotNull] SSL.OutStatementContext context)
		{
			return null;
		}

		public override Expression VisitLocalStatement([NotNull] SSL.LocalStatementContext context)
		{
			return null;
		}

		public override Expression VisitConstStatement([NotNull] SSL.ConstStatementContext context)
		{
			return null;
		}
		#endregion // Variable Statements

		#region Public Helpers
		public static bool TryParseIntegerLiteral(string text, out long value, out bool isun, out string err)
		{
			value = 0;

			string orig = text;
			bool isNeg = text.StartsWith("-");
			isun = text.EndsWith("u") || text.EndsWith("U");
			text = isNeg ? text.Substring(1) : text;
			text = isun ? text.Substring(0, text.Length - 1) : text;
			bool isHex = text.StartsWith("0x");
			text = isHex ? text.Substring(2) : text;

			uint res = 0;
			try
			{
				res = Convert.ToUInt32(text, isHex ? 16 : 10);
			}
			catch
			{
				err = $"Could not convert '{orig}' to integer literal";
				return false;
			}

			if (isNeg && (res > (uint)Int32.MaxValue))
			{
				err = $"The value '{orig}' is too large for a signed integer";
				return false;
			}

			value = isNeg ? -res : res;
			err = null;
			return true;
		}

		public void ParseArrayIndexer(SSL.ArrayIndexerContext ctx, out (Expression I1, Expression I2) idx, (uint? L1, uint? L2) lims, bool allowSecond, bool requireLiteral)
		{
			idx = (null, null);

			// First index
			var e1 = Visit(ctx.Index1);
			if (!e1.IsInteger)
				Error(ctx.Index1, "Array indexers must be an integer type.");
			var l1 = e1.GetIntegerLiteral();
			if (l1.HasValue) // Check the literal
			{
				if (l1.Value < 0)
					Error(ctx.Index1, "Array indices cannot be negative.");
				if (lims.L1.HasValue && l1.Value > lims.L1.Value)
					Error(ctx.Index1, $"The array index {l1.Value} is too large for the expression.");
			}
			else if (requireLiteral)
				Error(ctx.Index1, "The array indexer requires a fixed size.");
			idx.I1 = e1;

			// Second index
			if (ctx.Index2 != null)
			{
				if (!allowSecond)
					Error(ctx, "Array indexer cannot have two values for this expression.");
				var e2 = Visit(ctx.Index2);
				if (!e2.IsInteger)
					Error(ctx.Index2, "Array indexers must be an integer type.");
				var l2 = e2.GetIntegerLiteral();
				if (l2.HasValue) // Check the literal
				{
					if (l2.Value < 0)
						Error(ctx.Index2, "Array indices cannot be negative.");
					if (lims.L2.HasValue && l2.Value > lims.L2.Value)
						Error(ctx.Index2, $"The array index {l2.Value} is too large for the expression.");
				}
				else if (requireLiteral)
					Error(ctx.Index2, "The array indexer requires a fixed size.");
				idx.I2 = e2;
			}
		}
		#endregion // Public Helpers
	}

	// Conveys a compiler error up the visit stack
	internal class VisitException : Exception
	{
		public readonly CompilerError Error;

		public VisitException(CompilerError err)
		{
			Error = err;
		}
	}
}
