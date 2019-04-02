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

				}
			}
			catch (ArgumentException e) // Bad filename
			{
				CConsole.Error(e);
			}
			catch (FileNotFoundException e) // Input file does not exist
			{
				CConsole.Error(e);
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
