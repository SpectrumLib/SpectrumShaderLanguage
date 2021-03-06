﻿using System;
using System.IO;
using SSLang;

namespace SLLC
{
	static class Program
	{
		public static bool NoWarn { get; private set; } = false;

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				CConsole.Error("Please pass arguments to the program, or use '/?' to get the help text.");
				return;
			}
			ArgParser.Load(args);
			if (ArgParser.Help)
			{
				PrintHelp();
				return;
			}
			if (ArgParser.InputFile == null)
			{
				CConsole.Error("No input file specified. Make sure the input file is the last argument.");
				return;
			}
			NoWarn = ArgParser.NoWarn;

			// Try to get the paths
			string oPath = null;
			if (!ArgParser.TryLoadValueArg(out oPath, "output", "o") && oPath != null)
			{
				CConsole.Error("Output path invalid, or not specified.");
				return;
			}
			string iPath = null;
			if (!ArgParser.TryLoadValueArg(out iPath, "ipath", "ip") && iPath != null)
			{
				CConsole.Error("GLSL path invalid, or not specified.");
				return;
			}
			string rPath = null;
			if (!ArgParser.TryLoadValueArg(out rPath, "rpath", "rp") && rPath != null)
			{
				CConsole.Error("Reflection path invalid, or not specified.");
				return;
			}

			// Load other arguments
			if (!LoadIntegerArg(out var timeout, CompileOptions.DEFAULT_TIMEOUT, "timeout", "to"))
				return;
			if (!LoadIntegerArg(out var limAttr, CompileOptions.DEFAULT_LIMIT_ATTRIBUTES, "rla"))
				return;
			if (!LoadIntegerArg(out var limOut, CompileOptions.DEFAULT_LIMIT_OUTPUTS, "rlo"))
				return;
			if (!LoadIntegerArg(out var limInt, CompileOptions.DEFAULT_LIMIT_INTERNALS, "rli"))
				return;
			if (!LoadIntegerArg(out var limUni, CompileOptions.DEFUALT_LIMIT_UNIFORMS, "rlu"))
				return;
			if (!LoadIntegerArg(out var limSI, CompileOptions.DEFAULT_LIMIT_SUBPASS_INPUTS, "rlsi"))
				return;

			try
			{
				using (var compiler = SSLCompiler.FromFile(ArgParser.InputFile))
				{
					var fileName = Path.GetFileName(compiler.SourceFile);

					var options = new CompileOptions {
						WarnCallback = WarnCallback,
						OutputPath = oPath,
						GLSLPath = iPath,
						ReflectionPath = rPath,
						Compile = !ArgParser.NoCompile,
						OutputGLSL = ArgParser.OutputGLSL,
						OptimizeBytecode = !ArgParser.NoOptimize,
						OutputReflection = ArgParser.OutputReflection || ArgParser.UseBinaryReflection,
						UseBinaryReflection = ArgParser.UseBinaryReflection,
						ForceContiguousUniforms = ArgParser.ForceContiguousUniforms,
						CompilerTimeout = timeout,
						LimitAttributes = limAttr,
						LimitOutputs = limOut,
						LimitInternals = limInt,
						LimitUniforms = limUni,
						LimitSubpassInputs = limSI
					};

					if (!compiler.Compile(options, out var error))
					{
						if (error.Source == ErrorSource.Translator || error.Source == ErrorSource.Parser)
						{
							CConsole.Error($"'{fileName}'[{error.Line}:{error.CharIndex}] - " + error.Message);
							if (error.RuleStack != null)
								CConsole.Error($"    Rule Stack:  {String.Join(" -> ", error.RuleStack)}");
						}
						else
						{
							foreach (var err in error.Message.Split('\n'))
								CConsole.Error($"'{fileName}' - " + err);
						}
						return;
					}
				}
			}
			catch (ArgumentException e) // Bad filename
			{
				CConsole.Error(e);
			}
			catch (CompileOptionException e) // Bad compiler option
			{
				CConsole.Error(e.Message);
			}
			catch (FileNotFoundException e) // Input file does not exist
			{
				CConsole.Error(e);
			}
			catch (IOException e) // Error with writing one of the output files
			{
				CConsole.Error(e.Message);
			}
			catch (Exception e) // Unknown error
			{
				CConsole.Error($"({e.GetType()}) {e.Message}");
				CConsole.Error('\n' + e.StackTrace);
			}
		}

		private static bool LoadIntegerArg(out uint value, uint @default, params string[] names)
		{
			value = @default;
			if (!ArgParser.TryLoadValueArg(out var valstr, names) && valstr != null)
			{
				CConsole.Error("Invalid value for timeout option.");
				return false;
			}
			if (valstr != null && !UInt32.TryParse(valstr, out value))
			{
				CConsole.Error("Could not parse the timeout argument into an unsigned integer.");
				return false;
			}
			return true;
		}

		private static void WarnCallback(SSLCompiler compiler, ErrorSource source, uint line, string msg)
		{
			if (NoWarn)
				return;

			if ((source == ErrorSource.Parser) || (source == ErrorSource.Translator))
				CConsole.Warn($"'{compiler.SourceFile}'[line {line}] - {msg}");
			else
				CConsole.Warn($"'{compiler.SourceFile}' - {msg}");
		}

		private static void PrintHelp()
		{
			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("slcc");
			Console.WriteLine("----");
			Console.WriteLine("slcc is the command line tool used to compile Spectrum Shader Language files (.ssl) into");
			Console.WriteLine("Vulkan-compatible SPIR-V bytecode. It can also create reflection info about the shaders.");
			Console.WriteLine("It is designed for use with the Spectrum graphics library, but can be used standalone,");
			Console.WriteLine("and is designed to generate easy to consume files for any third party programs.");

			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("Usage:        sslc.exe [args] <file>");
			Console.WriteLine("The input file must always be the last argument.");
			Console.WriteLine("For compatiblity, all arguments can be specified with '-', '--', or '/' (Windows only).");

			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("The command line arguments with values are:");
			Console.WriteLine("    > out;o             - Specifies the output file path. Ignored if '/no-compile' is");
			Console.WriteLine("                           present. Defaults to the input file with the extension '.spv'.");
			Console.WriteLine("    > ipath;ip          - Specifies the path to place the generated glsl files into. Must");
			Console.WriteLine("                           point to a directory that exists. Ignored if '/glsl' is not");
			Console.WriteLine("                           present. Defaults to the input file directory.");
			Console.WriteLine("    > rpath;rp          - Specifies the file for the reflection info. Ignored if '/reflect'");
			Console.WriteLine("                           is not present. Defaults to the input file with the extension");
			Console.WriteLine("                           '.refl'.");
			Console.WriteLine("    > timeout;to        - Specifies the time, in milliseconds, to wait to compile/optimize");
			Console.WriteLine("                           each shader stage before throwing an error. A value of <= 0 will");
			Console.WriteLine("                           wait indefinitely. By default, the timout is 5000 (5 seconds).");
			Console.WriteLine("    > rla               - Sets the limit on vertex attribute binding slots. Defaults to 16.");
			Console.WriteLine("    > rlo               - Sets the limit on fragment shader outputs. Defaults to 4.");
			Console.WriteLine("    > rli               - Sets the limit on internal variable binding slots. Defaults to 16.");
			Console.WriteLine("    > rlu               - Sets the limit on bound uniforms. Defaults to 16.");
			Console.WriteLine("    > rlsi              - Sets the limit on subpass inputs. Defaults to 4.");

			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("The command line flags are:");
			Console.WriteLine("    > help;?;h          - Prints this help message, then exits.");
			Console.WriteLine("    > no-warn;nw        - Disables printing warning messages.");
			Console.WriteLine("    > no-compile;nc     - Disables compiling to SPIR-V. If this option is set, then the");
			Console.WriteLine("                           tool can be run without the Vulkan SDK installed.");
			Console.WriteLine("    > glsl;i            - Output the generated intermediate glsl to files. One file will");
			Console.WriteLine("                           be generated for each stage, with the extension changed to the");
			Console.WriteLine("                           stage contained in the file (e.g. '.vert').");
			Console.WriteLine("    > no-optimize;Od    - Disables optimization of SPIR-V bytecode. Not recommended.");
			Console.WriteLine("    > reflect;r         - Outputs reflection information about the shader in text format.");
			Console.WriteLine("    > binary;b          - Outputs the reflection information in a binary format. Implicitly");
			Console.WriteLine("                           activates '/reflect'.");
			Console.WriteLine("    > force-cu;fcu      - Throws an error if the uniforms in the shader are not contiguous.");
			Console.WriteLine("                           Non-contiguous uniforms are valid but hurt shader performance.");
			Console.WriteLine();
		}
	}
}
