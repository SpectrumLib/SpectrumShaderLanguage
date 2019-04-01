using System;
using System.IO;

namespace SSLang
{
	/// <summary>
	/// Core type for managing the compilation of a Spectrum Shader Language file to SPIR-V bytecode. You need one
	/// instance of this type for each file that you want to compile.
	/// </summary>
	public sealed class SSLCompiler : IDisposable
	{
		#region Fields

		// The input file info
		private readonly FileInfo _inputFile;
		/// <summary>
		/// The name of the input file.
		/// </summary>
		public string InputFile => _inputFile.Name;

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new compiler instance to manage the compilation of a single .ssl file.
		/// </summary>
		/// <param name="file">The path to the .ssl file to compile with this instance.</param>
		public SSLCompiler(string file)
		{
			if (String.IsNullOrWhiteSpace(file))
				throw new ArgumentException("Cannot pass a null or empty string for the file path.", nameof(file));
			
			try
			{
				_inputFile = new FileInfo(Path.GetFullPath(file));
			}
			catch
			{
				throw new ArgumentException($"The path '{file}' is not a valid filesystem path.", nameof(file));
			}
			if (!_inputFile.Exists)
				throw new FileNotFoundException("The input file was not found.", _inputFile.FullName);
		}
		~SSLCompiler()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{

			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
