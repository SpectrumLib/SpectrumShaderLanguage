﻿using SSLang;
using System;

namespace SSLC
{
	static class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				CConsole.Error("Usage: `sslc[.exe] <args> input_file.ssl`   -   use `-h` to get the help text.");
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
				CConsole.Error("No input file specified, please make sure the last argument is the input file.");
				return;
			}

			try
			{
				using (var compiler = new Compiler(ArgParser.InputFile))
				{
					var options = new CompilerOptions {
						WarnCallback = WarnCallback,
						VerboseCallback = VerboseCallback
					};

					if (!compiler.Compile(options, out var error))
					{
						if (error.Stage == CompilerStage.Parser || error.Stage == CompilerStage.Translator)
						{
							CConsole.Error($"'{compiler.SourceFile}'[{error.Line}:{error.CharIndex}] - " + error.Message);
							if (error.RuleStack != null)
								CConsole.Error($"    Rule Stack:  {String.Join(" -> ", error.RuleStack)}");
						}
						else
						{
							foreach (var err in error.Message.Split('\n'))
								CConsole.Error($"'{compiler.SourceFile}' - " + err);
						}
						return;
					}
				}
			}
			catch (Exception e)
			{
				CConsole.Error(e);
			}
		}

		private static void WarnCallback(Compiler compiler, CompilerStage stage, uint line, string msg)
		{
			if ((stage == CompilerStage.Parser) || (stage == CompilerStage.Translator))
				CConsole.Warn($"'{compiler.SourceFile}'[line {line}] - {msg}");
			else
				CConsole.Warn($"'{compiler.SourceFile}' - {msg}");
		}

		private static void VerboseCallback(Compiler compiler, CompilerStage stage, string msg)
		{
			CConsole.Info(msg);
		}

		private static void PrintHelp()
		{
			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("sslc");
			Console.WriteLine("----");
			Console.WriteLine("sslc is the command line tool used to compile Spectrum Shader Language files (.ssl) into");
			Console.WriteLine("Vulkan-compatible SPIR-V bytecode. It can also create reflection info about the shaders.");
			Console.WriteLine("It is designed for use with the Spectrum graphics library, but can be used standalone,");
			Console.WriteLine("and is designed to generate easy to consume files for any third party programs.");

			// =============================================================================================================
			Console.WriteLine();
			Console.WriteLine("Usage:        sslc[.exe] [args] <input_file>.ssl");
			Console.WriteLine("The input file must always be the last argument.");
			Console.WriteLine("For compatiblity, all arguments can be specified with '-', '--', or '/' (Windows only).");
		}
	}
}
