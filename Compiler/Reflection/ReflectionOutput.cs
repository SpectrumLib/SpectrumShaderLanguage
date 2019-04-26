using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SSLang.Reflection
{
	// Controls formatting and output of reflection info to a file
	internal static class ReflectionOutput
	{
		private static readonly string TOOL_VERSION;

		public static bool Generate(string outPath, bool binary, ShaderInfo info, out string error)
		{
			if (binary)
				return GenerateBinary(outPath, info, out error);
			return GenerateText(outPath, info, out error);
		}

		private static bool GenerateText(string outPath, ShaderInfo info, out string error)
		{
			error = null;
			StringBuilder sb = new StringBuilder(1024);

			sb.AppendLine($"SSL Reflection Dump (v{TOOL_VERSION})\n");

			// Write the file
			try
			{
				using (var file = File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var writer = new StreamWriter(file))
					writer.Write(sb.ToString());
			}
			catch (PathTooLongException)
			{
				error = "the output path is too long.";
				return false;
			}
			catch (DirectoryNotFoundException)
			{
				error = "the output directory could not be found, or does not exist.";
				return false;
			}
			catch (Exception e)
			{
				error = $"could not open and write output file ({e.Message}).";
				return false;
			}

			return true;
		}

		private static bool GenerateBinary(string outPath, ShaderInfo info, out string error)
		{
			error = "Binary reflection generation not yet implemented.";
			return false;
		}

		static ReflectionOutput()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;
			TOOL_VERSION = $"{ver.Major}.{ver.Minor}.{ver.Revision}";
		}
	}
}
