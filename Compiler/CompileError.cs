using System;

namespace SSLang
{
	/// <summary>
	/// Contains information about an error that occurs in the compiler.
	/// </summary>
	public sealed class CompileError
	{
		#region Fields
		/// <summary>
		/// The compilation stage that generated this error.
		/// </summary>
		public readonly ErrorSource Source;
		/// <summary>
		/// The source code line that the error occured on. Only valid if <see cref="Source"/> is not
		/// <see cref="ErrorSource.Output"/>
		/// </summary>
		public readonly uint Line;
		/// <summary>
		/// The character index into the line of the error. Only valid if <see cref="Source"/> is not
		/// <see cref="ErrorSource.Output"/>
		/// </summary>
		public readonly uint CharIndex;
		/// <summary>
		/// A message explaining the nature of the error.
		/// </summary>
		public readonly string Message;
		/// <summary>
		/// A list of the parser rules names, in the order that they were entered before the error occured. Only
		/// valid for errors in the <see cref="ErrorSource.Parser"/> stage.
		/// </summary>
		public readonly string[] RuleStack;
		#endregion // Fields

		internal CompileError(ErrorSource es, uint l, uint i, string m, string[] rs = null)
		{
			Source = es;
			Line = l;
			CharIndex = i;
			Message = m;
			RuleStack = rs;
		}
	}

	/// <summary>
	/// Represents the different compiler stages that can generate errors.
	/// </summary>
	public enum ErrorSource
	{
		/// <summary>
		/// The error is encountered during the initial lexing and parsing phase.
		/// </summary>
		Parser,
		/// <summary>
		/// The error is encountered while translating the source into GLSL.
		/// </summary>
		Translator,
		/// <summary>
		/// The error is encountered while compiling the generated GLSL into SPIR-V bytecode.
		/// </summary>
		Compiler,
		/// <summary>
		/// An error is encountered while attempting to write any compiler output to a file.
		/// </summary>
		Output
	}

	// Used to communicate a compiler error while visiting
	internal class VisitException : Exception
	{
		public readonly CompileError Error;

		public VisitException(CompileError error)
		{
			Error = error;
		}
	}
}
