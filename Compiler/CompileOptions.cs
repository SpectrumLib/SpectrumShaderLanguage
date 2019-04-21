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
		/// The optional callback to use to communicate warning messages to the client application.
		/// </summary>
		public CompilerMessageCallback WarnCallback = null;

		#region Output Options
		/// <summary>
		/// Controls if the source code is compiled into SPIR-V. If <c>false</c>, no SPIR-V bytecode will be generated.
		/// </summary>
		public bool Compile = true;

		/// <summary>
		/// The path to the output file for the compiled SPIR-V bytecode. A value of <c>null</c> will use the input 
		/// path with the extension `.spv`, or `[name].spv`. Ignored if <see cref="Compile"/> is <c>false</c>.
		/// </summary>
		public string OutputPath = null;

		/// <summary>
		/// Controls if the compiler will output reflection info.
		/// </summary>
		public bool OutputReflection = false;
		
		/// <summary>
		/// The path to the output file for the reflection info. A value of <c>null</c> will use the input path with
		/// the extension `.refl`, or `[name].refl`. Ignored if <see cref="OutputReflection"/> is <c>false</c>.
		/// </summary>
		public string ReflectionPath = null;

		/// <summary>
		/// If <c>false</c>, the reflection info output will be in a human readable format. If <c>true</c>, the
		/// reflection info output will be in a easily parseable binary format. Ignored if <see cref="OutputReflection"/>
		/// is <c>false</c>.
		/// </summary>
		public bool UseBinaryReflection = false;

		/// <summary>
		/// Controls if the compiler will output the generated GLSL source code. Each stage will be output into its own file.
		/// </summary>
		public bool OutputGLSL = false;

		/// <summary>
		/// The path to the output directory for the generated GLSL codes. A value of <c>null</c> will use the input directory 
		/// with the a stage extension. Ignored if <see cref="OutputGLSL"/> is <c>false</c>.
		/// <para>
		/// The possible stage extensions are 'vert', 'tesc', 'tese', 'geom', and 'frag'.
		/// </para>
		/// </summary>
		public string GLSLPath = null;
		#endregion // Output Options

		#region Compiler Options
		/// <summary>
		/// If <c>true</c>, all uniforms must be contiguous from zero, or a compiler error is thrown. Non-contiguous uniforms
		/// are valid, but result in sub-optimal performance.
		/// </summary>
		public bool ForceContiguousUniforms = false;
		/// <summary>
		/// The number of milliseconds to wait for the Vulkan GLSL compiler to complete before timing out. Defaults to 2000
		/// (5 seconds), and a value <= 0 will wait indefinitely.
		/// </summary>
		public int CompilerTimeout = 5000;
		#endregion // Compiler Options
		#endregion // Fields

		// Checks the options for validity, and throws exceptions if invalid
		internal void Validate()
		{
			if (Compile && (OutputPath != null) && !PathUtils.IsValid(OutputPath))
				throw new CompileOptionException(nameof(OutputPath), "invalid filesystem path.");
			if (OutputReflection && (ReflectionPath != null) && !PathUtils.IsValid(ReflectionPath))
				throw new CompileOptionException(nameof(ReflectionPath), "invalid filesystem path.");
			if (OutputGLSL && (GLSLPath != null) && !PathUtils.IsValidDirectory(GLSLPath))
				throw new CompileOptionException(nameof(GLSLPath), "invalid filesystem path to directory.");

			OutputPath = (Compile && OutputPath != null) ? Path.GetFullPath(OutputPath) : null;
			ReflectionPath = (OutputReflection && ReflectionPath != null) ? Path.GetFullPath(ReflectionPath) : null;
			GLSLPath = (OutputGLSL && GLSLPath != null) ? Path.GetFullPath(GLSLPath) : null;

			CompilerTimeout = Math.Max(CompilerTimeout, 0);
			CompilerTimeout = (CompilerTimeout == 0) ? Int32.MaxValue : CompilerTimeout;
		}

		/// <summary>
		/// Creates the default SPIR-V bytecode output path using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default bytecode path, or <c>null</c> if the input was null.</returns>
		public static string MakeDefaultOutputPath(string inPath) => (inPath != null) ? Path.GetFullPath(PathUtils.ReplaceExtension(inPath, ".spv")) : null;

		/// <summary>
		/// Creates the default reflection info output path using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default reflection info path, or <c>null</c> if the input was null.</returns>
		public static string MakeDefaultReflectionPath(string inPath) => (inPath != null) ? Path.GetFullPath(PathUtils.ReplaceExtension(inPath, ".refl")) : null;

		/// <summary>
		/// Creates the default generated GLSL output directory using the given SSL source path.
		/// </summary>
		/// <param name="inPath">The path to the SSL source file.</param>
		/// <returns>The default generated GLSL output directory, or <c>null</c> if the input was null.</returns>
		public static string MakeDefaultGLSLPath(string inPath) => (inPath != null) ? Path.GetDirectoryName(Path.GetFullPath(inPath)) : null;
	}

	/// <summary>
	/// Thrown when a member of a <see cref="CompileOptions"/> instance has an invalid value.
	/// </summary>
	public class CompileOptionException : Exception
	{
		#region Fields
		/// <summary>
		/// The name of the option that contained the invalid value.
		/// </summary>
		public readonly string OptionName;
		#endregion // Fields

		internal CompileOptionException(string name, string msg) :
			base($"Bad compiler option '{name}' - {msg}.")
		{
			OptionName = name;
		}
	}

	/// <summary>
	/// Callback type for communicating warning messages from the compiler to the user application.
	/// </summary>
	/// <param name="compiler">The compiler that generated the message.</param>
	/// <param name="source">The source compiler stage of the message.</param>
	/// <param name="line">If applicable, the line of the source code that generated the warning.</param>
	/// <param name="message">The text of the message.</param>
	public delegate void CompilerMessageCallback(SSLCompiler compiler, ErrorSource source, uint line, string message);
}
