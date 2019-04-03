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
		/// The source code line that the error occured on.
		/// </summary>
		public readonly uint Line;
		/// <summary>
		/// The character index into the line of the error.
		/// </summary>
		public readonly uint Index;
		/// <summary>
		/// A message explaining the nature of the error.
		/// </summary>
		public readonly string Message;
		#endregion // Fields

		internal CompileError(ErrorSource es, uint l, uint i, string m)
		{
			Source = es;
			Line = l;
			Index = i;
			Message = m;
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
		Compiler
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
