﻿using System;
using System.IO;

namespace SSLang
{
	/// <summary>
	/// Represents the set of options to use when compiling an .ssl file into SPIR-V bytecode.
	/// </summary>
	public sealed class CompileOptions
	{
		#region Defaults
		/// <summary>
		/// Default timeout for the compiler (5 seconds).
		/// </summary>
		public const uint DEFAULT_TIMEOUT = 5000;
		/// <summary>
		/// The default limit on the number of binding slots occupied by vertex attributes.
		/// </summary>
		public const uint DEFAULT_LIMIT_ATTRIBUTES = 16;
		/// <summary>
		/// The default limit on the number of fragment shader outputs.
		/// </summary>
		public const uint DEFAULT_LIMIT_OUTPUTS = 4;
		/// <summary>
		/// The default limit on the number of binding slots occupied by internal values.
		/// </summary>
		public const uint DEFAULT_LIMIT_INTERNALS = 16;
		/// <summary>
		/// The default limit on the number of uniforms.
		/// </summary>
		public const uint DEFUALT_LIMIT_UNIFORMS = 16;
		/// <summary>
		/// The default limit on the number of subpass inputs.
		/// </summary>
		public const uint DEFAULT_LIMIT_SUBPASS_INPUTS = 4;
		#endregion // Defaults

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
		/// The number of milliseconds to wait for the Vulkan GLSL compiler to complete before timing out. Defaults to 5000
		/// (5 seconds).
		/// </summary>
		public uint CompilerTimeout = DEFAULT_TIMEOUT;
		/// <summary>
		/// Gets if the output bytecode is run through a secondary optimization step to optimize the bytecode for
		/// execution speed. Not required but strongly recommended. Defaults to true.
		/// </summary>
		public bool OptimizeBytecode = true;
		#endregion // Compiler Options

		#region Resource Limits
		/// <summary>
		/// The limit on the number of binding slots occupied by vertex attributes. Defaults to 16.
		/// </summary>
		public uint LimitAttributes = DEFAULT_LIMIT_ATTRIBUTES;
		/// <summary>
		/// The limit on the number of fragment shader outputs. Defaults to 4.
		/// </summary>
		public uint LimitOutputs = DEFAULT_LIMIT_OUTPUTS;
		/// <summary>
		/// The limit on the number of binding slots occupied by internals. Defaults to 16.
		/// </summary>
		public uint LimitInternals = DEFAULT_LIMIT_INTERNALS;
		/// <summary>
		/// The limit on the number of bound uniforms. Defaults to 16.
		/// </summary>
		public uint LimitUniforms = DEFUALT_LIMIT_UNIFORMS;
		/// <summary>
		/// The limit on the number of subpass inputs. Defaults to 4.
		/// </summary>
		public uint LimitSubpassInputs = DEFAULT_LIMIT_SUBPASS_INPUTS;
		#endregion // Resource Limits
		#endregion // Fields

		// Checks the options for validity, and throws exceptions if invalid
		internal void Validate()
		{
			if (Compile && (OutputPath != null) && !PathUtils.IsValid(OutputPath))
				throw new CompileOptionException(nameof(OutputPath), "invalid filesystem path.");
			if (OutputReflection && (ReflectionPath != null) && !PathUtils.IsValid(ReflectionPath))
				throw new CompileOptionException(nameof(ReflectionPath), "invalid filesystem path.");
			if (OutputGLSL && (GLSLPath != null) && !PathUtils.IsValidDirectory(GLSLPath))
				throw new CompileOptionException(nameof(GLSLPath), "invalid filesystem directory path.");

			OutputPath = (Compile && OutputPath != null) ? Path.GetFullPath(OutputPath) : null;
			ReflectionPath = (OutputReflection && ReflectionPath != null) ? Path.GetFullPath(ReflectionPath) : null;
			GLSLPath = (OutputGLSL && GLSLPath != null) ? Path.GetFullPath(GLSLPath) : null;
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
