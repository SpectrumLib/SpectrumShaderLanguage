using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using SSLang.Generated;

namespace SSLang
{
	// Implementation of listener for parse error
	internal class SSLErrorListener : BaseErrorListener, IAntlrErrorListener<int>
	{
		#region Fields
		// The generated error
		public CompileError Error { get; private set; } = null;
		#endregion // Fields

		public override void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs)
		{
			base.ReportAmbiguity(recognizer, dfa, startIndex, stopIndex, exact, ambigAlts, configs);
		}

		public override void ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts, SimulatorState conflictState)
		{
			base.ReportAttemptingFullContext(recognizer, dfa, startIndex, stopIndex, conflictingAlts, conflictState);
		}

		public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState)
		{
			base.ReportContextSensitivity(recognizer, dfa, startIndex, stopIndex, prediction, acceptState);
		}

		public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			var context = e?.Context;
			var ridx = context?.RuleIndex ?? -1;
			var badText = offendingSymbol?.Text ?? e.OffendingToken?.Text ?? "";
			string errMsg = null;

			// TODO: Cause problems to see what errors come out, and fill this out with more intelligent error messages as we go
			if (ridx == SSLParser.RULE_file)
				errMsg = $"Unexpected input '{badText}' at top level; expected type block, uniform, or function.";
			else if (ridx == SSLParser.RULE_uniformQualifier)
				errMsg = $"Invalid or empty uniform qualifier.";
			else if (ridx == SSLParser.RULE_imageLayoutQualifier)
				errMsg = $"Invalid image format '{badText}'.";
			else
			{
				if (msg.Contains("missing ';' at"))
					errMsg = "Unexpected statement... are you missing a semicolon in the preceeding line?";
				else
					errMsg = $"(Rule '{((ridx == -1) ? "none" : SSLParser.ruleNames[ridx])}') ('{badText}') - {msg}";
			}

			var stack = ((SSLParser)recognizer).GetRuleInvocationStack().ToList();
			stack.Reverse();
			Error = new CompileError(ErrorSource.Parser, (uint)line, (uint)charPositionInLine, errMsg, stack.ToArray());
		}

		// Called from the lexer
		public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			
		}
	}
}
