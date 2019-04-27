using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SSLang.Reflection;

namespace SSLang
{
	// Contains the interface to the glslangValidator tool in the Vulkan SDK, used to compile GLSL to SPIR-V
	internal static class GLSLV
	{
		// Path separators
		private static readonly char[] PATH_SEP = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		// The required SDK version (refine and change this as our requirements change)
		private static readonly Version REQUIRED_SDK_VERSION = new Version(1, 0, 0, 0);
		// Stage names for the compiler arguments
		private static readonly Dictionary<ShaderStages, string> STAGE_NAMES = new Dictionary<ShaderStages, string>() {
			{ ShaderStages.Vertex, "vert" }, { ShaderStages.TessControl, "tesc" }, { ShaderStages.TessEval, "tese" }, { ShaderStages.Geometry, "geom" },
			{ ShaderStages.Fragment, "frag" }
		};
		// Entry point names
		private static readonly Dictionary<ShaderStages, string> ENTRY_NAMES = new Dictionary<ShaderStages, string>() {
			{ ShaderStages.Vertex, "vert_main" }, { ShaderStages.TessControl, "tesc_main" }, { ShaderStages.TessEval, "tese_main" }, { ShaderStages.Geometry, "geom_main" },
			{ ShaderStages.Fragment, "frag_main" }
		};

		// The absolute path to glslangValidator
		private static string TOOL_PATH = null;
		// The SDK version being used
		public static Version SDK_VERSION { get; private set; } = null;

		// Submit some glsl source code for compilation
		public static bool Compile(CompileOptions options, string glsl, ShaderStages stage, out string bcFile, out CompileError error)
		{
			if (!Initialize(out string initError))
			{
				bcFile = null;
				error = new CompileError(ErrorSource.Compiler, 0, 0, initError);
				return false;
			}

			// Create the arguments
			var ep = ENTRY_NAMES[stage];
			bcFile = Path.GetTempFileName();
			var args = $"-V -e {ep} --sep {ep} --client vulkan100 -o \"{bcFile}\" --stdin -S {STAGE_NAMES[stage]}";

			// Define the process info
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = $"\"{TOOL_PATH}\"",
				Arguments = args,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				ErrorDialog = false
			};

			// Run the compiler
			string stdout = null;
			using (Process proc = new Process())
			{
				proc.StartInfo = psi;
				proc.Start();
				proc.StandardInput.AutoFlush = true;
				proc.StandardInput.Write(glsl);
				proc.StandardInput.Close();
				bool done = proc.WaitForExit((int)options.CompilerTimeout);
				if (!done)
				{
					proc.Kill();
					error = new CompileError(ErrorSource.Compiler, 0, 0, $"Compiler timed-out during stage '{stage}'.");
					return false;
				}
				stdout = proc.StandardOutput.ReadToEnd() + '\n' + proc.StandardError.ReadToEnd();
			}

			// Convert the output to a list of error messages
			var lines = stdout
				.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(line => line.StartsWith("ERROR:") && !line.Contains("Source entry point")) // Remove the error message about renaming the entry point
				.Select(line => line.Trim().Substring(7)) // Trim the "ERROR: " text off of the front
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

			var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			// Get the SDK path from the environment
			var sdk = Environment.GetEnvironmentVariable("VULKAN_SDK") ?? Environment.GetEnvironmentVariable("VK_SDK_PATH");
			if (String.IsNullOrWhiteSpace(sdk))
			{
				error = "Could not find Vulkan SDK path. Ensure that 'VULKAN_SDK' or 'VK_SDK_PATH' are in your environment variables.";
				return false;
			}

			// Parse the API version
			var vstr = sdk.Substring(sdk.LastIndexOfAny(PATH_SEP) + 1); // Should be in format "x.x.xxx.x"
			var vcomp = vstr.Split('.');
			if (vcomp.Length != 4)
			{
				error = "Unable to parse Vulkan SDK version. Please ensure the SDK path points to the version folder (e.g. '../VulkanSDK/1.0.100.0/').";
				return false;
			}
			if (!Int32.TryParse(vcomp[0], out var major) || !Int32.TryParse(vcomp[1], out var minor) || !Int32.TryParse(vcomp[2], out var build) || !Int32.TryParse(vcomp[3], out var rev))
			{
				error = $"Unable to parse Vulkan SDK version from path '{sdk}'.";
				return false;
			}
			var sdkv = new Version(major, minor, build, rev);
			if (sdkv < REQUIRED_SDK_VERSION)
			{
				error = $"Vulkan SDK version '{REQUIRED_SDK_VERSION}' is required, but version '{sdkv}' was found.";
				return false;
			}
			SDK_VERSION = sdkv;

			// Get the tool path and ensure it exists
			TOOL_PATH = Path.Combine(sdk, "Bin", isWin ? "glslangValidator.exe" : "glslangValidator");
			if (!File.Exists(TOOL_PATH))
			{
				error = $"Could not find glsl compiler at path '{TOOL_PATH}'.";
				return false;
			}

			return true;
		}
	}
}
