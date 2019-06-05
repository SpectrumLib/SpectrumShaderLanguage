using System;

namespace SSLang
{
	/// <summary>
	/// Contains information about an error in a compilation process.
	/// </summary>
	public sealed class CompilerError
	{
		#region Fields
		/// <summary>
		/// The compiler stage the error was generated in.
		/// </summary>
		public readonly CompilerStage Stage;
		/// <summary>
		/// The source line the error occured on (only valid for non-<see cref="CompilerStage.Output"/>).
		/// </summary>
		public readonly uint Line;
		/// <summary>
		/// The character index the error occured on (only valid for non-<see cref="CompilerStage.Output"/>).
		/// </summary>
		public readonly uint CharIndex;
		/// <summary>
		/// The message explaining the nature of the error.
		/// </summary>
		public readonly string Message;
		/// <summary>
		/// The list of parser rule names, in the order they were entered before the error occured. Only valid for
		/// <see cref="CompilerStage.Parser"/>.
		/// </summary>
		public readonly string[] RuleStack;
		#endregion // Fields

		internal CompilerError(CompilerStage stage, uint l, uint c, string m, string[] rs = null)
		{
			Stage = stage;
			Line = l;
			CharIndex = c;
			Message = m;
			RuleStack = rs;
		}
	}
}
