using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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

			// Build the args
			StringBuilder args = new StringBuilder(512);
			args.Append($"--create-library --target-env vulkan1.0 --verify-ids");
			args.Append($" -o \"{output}\"");
			foreach (var mfile in modules)
			{
				if (mfile != null)
					args.Append($" \"{mfile}\"");
			}

			// Describe the process
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = $"\"{TOOL_PATH}\"",
				Arguments = args.ToString(),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				ErrorDialog = false
			};

			// Run the linker
			string stdout = null;
			using (Process proc = new Process())
			{
				proc.StartInfo = psi;
				proc.Start();
				bool done = proc.WaitForExit(options.CompilerTimeout);
				if (!done)
				{
					proc.Kill();
					error = new CompileError(ErrorSource.Compiler, 0, 0, "Linking process timed out.");
					return false;
				}
				stdout = proc.StandardOutput.ReadToEnd() + '\n' + proc.StandardError.ReadToEnd();
			}

			// Convert the output to a list of error messages
			var lines = stdout
				.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(line => line.StartsWith("error:")) 
				.Select(line => line.Trim().Substring(7)) // Trim the "error: " text off of the front
				.ToList();

			// Report the errors, if present
			if (lines.Count > 0)
			{
				error = new CompileError(ErrorSource.Compiler, 0, 0, String.Join("\n", lines));
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
