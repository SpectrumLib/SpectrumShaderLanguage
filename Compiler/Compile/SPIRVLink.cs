using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SSLang
{
	// Manages the unpacking and interface to the spirv-link tool for linking spirv modules
	internal static class SPIRVLink
	{
		// The absolute path to the spirv-link executable
		private static string TOOL_PATH = null;

		public static bool Link(CompileOptions options, string[] modules, string output, out CompileError error)
		{
			if (!Initialize(out string initError))
			{
				error = new CompileError(ErrorSource.Compiler, 0, 0, initError);
				return false;
			}

			error = null;
			return true;
		}

		private static bool Initialize(out string error)
		{
			error = null;
			if (TOOL_PATH != null)
				return true;

			bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
				isLinux = !isWin && RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
				isMac = !isWin && !isLinux;

			// Extract the resource
			var resname = "SSLang.Native.spirv-link." + (isWin ? "w" : isLinux ? "l" : "m");
			TOOL_PATH = isWin ? Path.ChangeExtension(Path.GetTempFileName(), ".exe") : 
				Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
			try
			{
				using (var reader = Assembly.GetExecutingAssembly().GetManifestResourceStream(resname))
				using (var writer = File.Open(TOOL_PATH, FileMode.Create, FileAccess.Write, FileShare.None))
					reader.CopyTo(writer);
			}
			catch (Exception e)
			{
				error = "Unable to extract spirv-link: " + e.Message;
				return false;
			}

			return true;
		}
	}
}
