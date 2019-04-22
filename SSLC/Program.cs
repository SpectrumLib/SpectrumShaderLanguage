﻿using System;
using System.IO;
using SSLang;

namespace SLLC
{
	static class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				CConsole.Error("Please pass arguments to the program, or use '/?' to get the help text.");
				return;
			}
			args = ArgParser.Sanitize(args);
			if (ArgParser.Help(args))
			{
				PrintHelp();
				return;
			}

			try
			{
				using (var compiler = SSLCompiler.FromFile(args[args.Length - 1]))
				{
					var fileName = Path.GetFileName(args[args.Length - 1]);

					var options = new CompileOptions {
						WarnCallback = WarnCallback,
						Compile = true,
						OutputGLSL = true,
						OptimizeBytecode = false
					};

					if (!compiler.Compile(options, out var error))
					{
						if (error.Source == ErrorSource.Translator)
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

		private static void WarnCallback(SSLCompiler compiler, ErrorSource source, uint line, string msg)
		{
			if ((source == ErrorSource.Parser) || (source == ErrorSource.Translator))
				CConsole.Warn($"'{compiler.SourceFile}'[line {line}] - {msg}");
			else
				CConsole.Warn($"'{compiler.SourceFile}' - {msg}");
		}

		private static void PrintHelp()
		{
			// ========================================================================================================
			Console.WriteLine();
			Console.WriteLine("slcc");
			Console.WriteLine("----");
			Console.WriteLine("slcc is the command line tool used to compile Spectrum Shader Language files (.ssl) into");
			Console.WriteLine("Vulkan-compatible SPIR-V bytecode. It can also create reflection info about the shaders.");
			Console.WriteLine("It is designed for use with the Spectrum graphics library, but can be used standalone, and");
			Console.WriteLine("is designed to generate easy to consume files for any third party programs.");

			Console.WriteLine();
			Console.WriteLine("Usage:        sslc.exe [args] <file>");
			Console.WriteLine("The input file must always be the last argument.");

			Console.WriteLine();
			Console.WriteLine("The command line arguments are:");
			Console.WriteLine("    > /help;/?;/h      - Prints this help message, then exits.");
			Console.WriteLine("For compatiblity, all arguments can be specified with '/', '-', or '--'.");
			Console.WriteLine();
		}
	}
}
