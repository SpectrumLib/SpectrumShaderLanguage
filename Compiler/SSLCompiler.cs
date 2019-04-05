﻿using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	/// <summary>
	/// Core type for managing the compilation of a Spectrum Shader Language file to SPIR-V bytecode. You need one
	/// instance of this type for each file that you want to compile.
	/// </summary>
	public sealed class SSLCompiler : IDisposable
	{
		#region Fields
		/// <summary>
		/// The SSL source code that will be compiled.
		/// </summary>
		public readonly string Source;
		/// <summary>
		/// If this compiler was created for a file, this will be the full path to the file. Otherwise, it will be null.
		/// </summary>
		public readonly string SourceFile;
		/// <summary>
		/// Reflection information about the shader. Will only be available after <see cref="Compile(CompileOptions, out CompileError)"/>
		/// is called and completes successfully. Reflection info will be available even if it was not requested in the compiler
		/// options.
		/// </summary>
		public ShaderInfo ShaderInfo { get; private set; } = null;

		private List<(uint, string)> _warnings;
		/// <summary>
		/// A list of warning messages, and their source lines, that were generated during compilation. Will only be
		/// populated after <see cref="Compile(CompileOptions, out CompileError)"/> is called.
		/// </summary>
		public IReadOnlyList<(uint Line, string Message)> Warnings => _warnings;

		private bool _isDisposed = false;
		#endregion // Fields

		private SSLCompiler(string src, string sf)
		{
			Source = src;
			SourceFile = sf;
		}
		~SSLCompiler()
		{
			dispose(false);
		}

		/// <summary>
		/// Creates a new compiler instance to manage the compilation of a single .ssl file.
		/// </summary>
		/// <param name="file">The path to the file to compile.</param>
		/// <returns>A new compiler instance.</returns>
		/// <exception cref="ArgumentException">The path is not a valid filesystem path.</exception>
		/// <exception cref="FileNotFoundException">The path does not point towards a file that exists.</exception>
		/// <exception cref="IOException">The program could not read the text from the file.</exception>
		public static SSLCompiler FromFile(string file)
		{
			if (String.IsNullOrWhiteSpace(file))
				throw new ArgumentException("Cannot pass a null or empty string for the file path.", nameof(file));
			if (!Uri.IsWellFormedUriString(file, UriKind.RelativeOrAbsolute))
				throw new ArgumentException($"The path '{file}' is not a valid filesystem path.", nameof(file));

			var inFile = new FileInfo(Path.GetFullPath(file));
			if (!inFile.Exists)
				throw new FileNotFoundException($"The input file '{inFile.FullName}' does not exist.", file);

			try
			{
				return new SSLCompiler(File.ReadAllText(inFile.FullName), inFile.FullName);
			}
			catch (Exception e)
			{
				throw new IOException($"Could not load input file: {e.Message}");
			}
		}

		/// <summary>
		/// Creates a new compiler instance to manage the compilation of the passed source code.
		/// </summary>
		/// <param name="source">The SSL source code to compile.</param>
		/// <returns>A new compiler instance.</returns>
		/// <exception cref="ArgumentException">The passed source code is null or empty.</exception>
		public static SSLCompiler FromSource(string source)
		{
			if (String.IsNullOrWhiteSpace(source))
				throw new ArgumentException("The source code cannot be null or empty.", nameof(source));
			return new SSLCompiler(source, null);
		}

		/// <summary>
		/// Performs the compilation of the input file, using the given options.
		/// </summary>
		/// <param name="options">The options to use when compiling the shader file.</param>
		/// <param name="error">The error generated by the compiler, if any. Will be <c>null</c> on success.</param>
		/// <returns><c>true</c> if the compilation completes successfully, <c>false</c> otherwise.</returns>
		public bool Compile(CompileOptions options, out CompileError error)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			error = null;

			// Validate the build options
			options.Validate();

			// Create the lexer and parser
			AntlrInputStream inStream = new AntlrInputStream(Source);
			SSLLexer lexer = new SSLLexer(inStream);
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			SSLParser parser = new SSLParser(tokenStream);

			// Register our custom error listener
			lexer.RemoveErrorListeners();
			parser.RemoveErrorListeners();
			var err = new SSLErrorListener();
			lexer.AddErrorListener(err);
			parser.AddErrorListener(err);

			// Perform the parsing, and report the error if there is one
			var fileCtx = parser.file();
			if (err.Error != null)
			{
				error = err.Error;
				return false;
			}

			// Visit the tree (this step actually generates the GLSL)
			SSLVisitor visitor = new SSLVisitor(tokenStream, options);
			try
			{
				visitor.Visit(fileCtx);
				ShaderInfo = visitor.Info;
				_warnings = visitor.Warnings;
			}
			catch (VisitException e)
			{
				error = e.Error;
				return false;
			}

			// Output the GLSL if requested
			if (options.OutputGLSL)
			{
				var glslPath = options.GLSLPath ?? CompileOptions.MakeDefaultGLSLPath(SourceFile)
					?? Path.Combine(Directory.GetCurrentDirectory(), $"{ShaderInfo.Name ?? "shader"}.glsl");

				try
				{
					if (File.Exists(glslPath))
						File.Delete(glslPath);
					File.WriteAllText(glslPath, visitor.GLSL.GetSource());
				}
				catch (Exception e)
				{
					throw new IOException($"Unable to write GLSL file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
				}
			}

			return true;
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
