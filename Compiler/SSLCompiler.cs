﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
		/// <summary>
		/// The version of the compiler library currently being used.
		/// </summary>
		public static readonly Version TOOL_VERSION;

		#region Fields
		/// <summary>
		/// The SSL source code that will be compiled.
		/// </summary>
		public readonly string Source;
		/// <summary>
		/// If this compiler was created for a file, this will be the name of the file. Otherwise, it will be null.
		/// </summary>
		public readonly string SourceFile;
		/// <summary>
		/// Reflection information about the shader. Will only be available after <see cref="Compile(CompileOptions, out CompileError)"/>
		/// is called and completes successfully. Reflection info will be available even if it was not requested in the compiler
		/// options.
		/// </summary>
		public ShaderInfo ShaderInfo { get; private set; } = null;

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
				return new SSLCompiler(File.ReadAllText(inFile.FullName), inFile.Name);
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

			// Check for the version statement first
			var versionCtx = parser.versionMetaStatement();
			if (err.Error != null)
			{
				error = err.Error;
				return false;
			}
			if (versionCtx != null && !checkVersion(versionCtx, out error))
				return false;

			// Perform the parsing, and report the error if there is one
			parser.Reset();
			var fileCtx = parser.file();
			if (err.Error != null)
			{
				error = err.Error;
				return false;
			}

			// Visit the tree (this step actually generates the GLSL)
			SSLVisitor visitor = new SSLVisitor(tokenStream, this, options);
			try
			{
				visitor.Visit(fileCtx);
				ShaderInfo = visitor.Info;
			}
			catch (VisitException e)
			{
				error = e.Error;
				return false;
			}

			// Output the reflection if requested
			if (options.OutputReflection && !outputRefl(options, visitor, out error))
				return false;

			// Output the GLSL if requested
			if (options.OutputGLSL && !outputGLSL(options, visitor, out error))
				return false;

			// Compile if requested
			if (options.Compile && !compile(options, visitor, out error))
				return false;

			return true;
		}

		private bool checkVersion(SSLParser.VersionMetaStatementContext ctx, out CompileError err)
		{
			var vstr = ctx.Version.Text.Split('.');
			uint maj = 0, min = 0, bld = 0;
			if (!UInt32.TryParse(vstr[0], out maj) || !UInt32.TryParse(vstr[1], out min) || !UInt32.TryParse(vstr[2], out bld))
			{
				err = new CompileError(ErrorSource.Parser, (uint)ctx.Start.Line, (uint)ctx.Start.StartIndex, $"Unable to parse version statement '{ctx.Version.Text}'.");
				return false;
			}
			if (maj > 255 || min > 255 || bld > 255)
			{
				err = new CompileError(ErrorSource.Parser, (uint)ctx.Start.Line, (uint)ctx.Start.StartIndex, $"Version integer components must be <= 255 ({ctx.Version.Text})'.");
				return false;
			}
			var version = new Version((int)maj, (int)min, (int)bld);

			if (version > TOOL_VERSION)
			{
				err = new CompileError(ErrorSource.Parser, (uint)ctx.Start.Line, (uint)ctx.Start.StartIndex,
					$"Version mismatch - requires version '{version}', but highest available is version '{TOOL_VERSION.Major}.{TOOL_VERSION.Minor}.{TOOL_VERSION.Build}'.");
				return false;
			}

			err = null;
			return true;
		}

		private bool outputRefl(CompileOptions options, SSLVisitor vis, out CompileError error)
		{
			error = null;
			var rPath = options.ReflectionPath ?? CompileOptions.MakeDefaultReflectionPath(SourceFile) ?? Path.Combine(Directory.GetCurrentDirectory(), "shader.refl");

			try
			{
				vis.Info.SaveToFile(rPath, options.UseBinaryReflection);
			}
			catch (Exception e)
			{
				error = new CompileError(ErrorSource.Output, 0, 0, $"Unable to generate reflection info: {e.Message}.");
			}

			return error == null;
		}

		private bool compile(CompileOptions options, SSLVisitor visitor, out CompileError error)
		{
			var finalPath = options.OutputPath ?? CompileOptions.MakeDefaultOutputPath(SourceFile) ?? Path.Combine(Directory.GetCurrentDirectory(), "shader.spv");

			bool hasVert = (ShaderInfo.Stages & ShaderStages.Vertex) > 0,
				 hasTesc = (ShaderInfo.Stages & ShaderStages.TessControl) > 0,
				 hasTese = (ShaderInfo.Stages & ShaderStages.TessEval) > 0,
				 hasGeom = (ShaderInfo.Stages & ShaderStages.Geometry) > 0,
				 hasFrag = (ShaderInfo.Stages & ShaderStages.Fragment) > 0;

			string vertPath = null, tescPath = null, tesePath = null, geomPath = null, fragPath = null;
			if (hasVert && !GLSLV.Compile(options, visitor.GLSL.GetGLSLOutput(ShaderStages.Vertex), ShaderStages.Vertex, out vertPath, out error))
				return false;
			if (hasTesc && !GLSLV.Compile(options, visitor.GLSL.GetGLSLOutput(ShaderStages.TessControl), ShaderStages.TessControl, out tescPath, out error))
				return false;
			if (hasTese && !GLSLV.Compile(options, visitor.GLSL.GetGLSLOutput(ShaderStages.TessEval), ShaderStages.TessEval, out tesePath, out error))
				return false;
			if (hasGeom && !GLSLV.Compile(options, visitor.GLSL.GetGLSLOutput(ShaderStages.Geometry), ShaderStages.Geometry, out geomPath, out error))
				return false;
			if (hasFrag && !GLSLV.Compile(options, visitor.GLSL.GetGLSLOutput(ShaderStages.Fragment), ShaderStages.Fragment, out fragPath, out error))
				return false;

			// Link the files
			var linkOut = options.OptimizeBytecode ? Path.GetTempFileName() : finalPath;
			if (!SPIRVLink.Link(options, new[] { vertPath, tescPath, tesePath, geomPath, fragPath }, linkOut, out error))
				return false;

			// Optimize the files
			if (options.OptimizeBytecode && !SPIRVOpt.Optimize(options, linkOut, finalPath, out error))
				return false;

			error = null;
			return true;
		}

		private bool outputGLSL(CompileOptions options, SSLVisitor visitor, out CompileError error)
		{
			var outDir = options.GLSLPath ?? CompileOptions.MakeDefaultGLSLPath(SourceFile) ?? Directory.GetCurrentDirectory();
			var outName = (SourceFile != null) ? Path.GetFileNameWithoutExtension(SourceFile) : "shader";

			if ((ShaderInfo.Stages & ShaderStages.Vertex) > 0)
			{
				try
				{
					var path = Path.Combine(outDir, outName + ".vert");
					if (File.Exists(path))
						File.Delete(path);
					File.WriteAllText(path, visitor.GLSL.GetGLSLOutput(ShaderStages.Vertex));
				}
				catch (Exception e)
				{
					error = new CompileError(ErrorSource.Output, 0, 0,
						$"Unable to write vertex source file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
					return false;
				}
			}
			if ((ShaderInfo.Stages & ShaderStages.TessControl) > 0)
			{
				try
				{
					var path = Path.Combine(outDir, outName + ".tesc");
					if (File.Exists(path))
						File.Delete(path);
					File.WriteAllText(path, visitor.GLSL.GetGLSLOutput(ShaderStages.TessControl));
				}
				catch (Exception e)
				{
					error = new CompileError(ErrorSource.Output, 0, 0,
						$"Unable to write tess control source file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
					return false;
				}
			}
			if ((ShaderInfo.Stages & ShaderStages.TessEval) > 0)
			{
				try
				{
					var path = Path.Combine(outDir, outName + ".tese");
					if (File.Exists(path))
						File.Delete(path);
					File.WriteAllText(path, visitor.GLSL.GetGLSLOutput(ShaderStages.TessEval));
				}
				catch (Exception e)
				{
					error = new CompileError(ErrorSource.Output, 0, 0,
						$"Unable to write tess eval source file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
					return false;
				}
			}
			if ((ShaderInfo.Stages & ShaderStages.Geometry) > 0)
			{
				try
				{
					var path = Path.Combine(outDir, outName + ".geom");
					if (File.Exists(path))
						File.Delete(path);
					File.WriteAllText(path, visitor.GLSL.GetGLSLOutput(ShaderStages.Geometry));
				}
				catch (Exception e)
				{
					error = new CompileError(ErrorSource.Output, 0, 0,
						$"Unable to write geometry source file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
					return false;
				}
			}
			if ((ShaderInfo.Stages & ShaderStages.Fragment) > 0)
			{
				try
				{
					var path = Path.Combine(outDir, outName + ".frag");
					if (File.Exists(path))
						File.Delete(path);
					File.WriteAllText(path, visitor.GLSL.GetGLSLOutput(ShaderStages.Fragment));
				}
				catch (Exception e)
				{
					error = new CompileError(ErrorSource.Output, 0, 0,
						$"Unable to write fragment source file, reason: '{e.Message.Substring(0, e.Message.Length - 1)}'.");
					return false;
				}
			}

			error = null;
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

		static SSLCompiler()
		{
			TOOL_VERSION = Assembly.GetExecutingAssembly().GetName().Version;
		}
	}
}
