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
		/// The source code line that the error occured on.
		/// </summary>
		public readonly uint Line;
		/// <summary>
		/// A message explaining the nature of the error.
		/// </summary>
		public readonly string Message;
		#endregion // Fields

		internal CompileError(uint l, string m)
		{
			Line = l;
			Message = m;
		}
	}
}
