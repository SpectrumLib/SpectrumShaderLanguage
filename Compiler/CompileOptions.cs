using System;
using System.IO;

namespace SSLang
{
	/// <summary>
	/// Represents the set of options to use when compiling an .ssl file into SPIR-V bytecode.
	/// </summary>
	public sealed class CompileOptions
	{
		#region Fields
		/// <summary>
		/// Controls if the source code is compiled into SPIR-V. If <c>false</c>, no SPIR-V bytecode will be generated.
		/// </summary>
		public bool Compile = true;

		/// <summary>
		/// The path to the output file for the compiled SPIR-V bytecode. A value of <c>null</c> will use the input 
		/// path with the extension `.spv`. Ignored if <see cref="Compile"/> is <c>false</c>.
		/// </summary>
		public string OutputPath = null;

		/// <summary>
		/// Controls if the compiler will output reflection info.
		/// </summary>
		public bool OutputReflection = false;
		
		/// <summary>
		/// The path to the output file for the reflection info. A value of <c>null</c> will use the input path with
		/// the extension `.refl`. Ignored if <see cref="OutputReflection"/> is <c>false</c>.
		/// </summary>
		public string ReflectionPath = null;

		/// <summary>
		/// Controls if the compiler will output the generated GLSL source code.
		/// </summary>
		public bool OutputGLSL = false;

		/// <summary>
		/// The path to the output file for the generated GLSL code. A value of <c>null</c> will use the input path with
		/// the extension `.glsl`. Ignored if <see cref="OutputGLSL"/> is <c>false</c>.
		/// </summary>
		public string GLSLPath = null;
		#endregion // Fields

		/// <summary>
		/// Creates the default SPIR-V bytecode output path using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default bytecode path.</returns>
		public static string MakeDefaultOutputPath(string inPath) => ReplaceExtension(inPath, ".spv");

		/// <summary>
		/// Creates the default reflection info output path using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default reflection info path.</returns>
		public static string MakeDefaultReflectionPath(string inPath) => ReplaceExtension(inPath, ".refl");

		/// <summary>
		/// Creates the default generated GLSL output path using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default generated GLSL path.</returns>
		public static string MakeDefaultGLSLPath(string inPath) => ReplaceExtension(inPath, ".glsl");

		private static string ReplaceExtension(string inPath, string ext)
		{
			if (String.IsNullOrEmpty(inPath))
				throw new ArgumentException("The input path cannot be null or empty.", nameof(inPath));
			if (!Uri.IsWellFormedUriString(inPath, UriKind.RelativeOrAbsolute))
				throw new IOException($"The path '{inPath}' is not a valid filesystem path.");

			inPath = Path.GetFullPath(inPath);
			return Path.GetFileNameWithoutExtension(inPath) + ext;
		}
	}
}
