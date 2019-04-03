using System;
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

					if (!compiler.Compile(new CompileOptions { Compile = false, OutputGLSL = true }, out var error))
					{
						CConsole.Error($"'{fileName}'[{error.Line}:{error.CharIndex}] - {error.Message}");
						return;
					}

					// Print the warnings
					foreach (var warn in compiler.Warnings)
					{
						CConsole.Warn($"[line {warn.Line}] - {warn.Message}");
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
			}
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
