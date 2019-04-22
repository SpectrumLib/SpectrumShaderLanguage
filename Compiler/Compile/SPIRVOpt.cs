using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SSLang
{
	// Contains the interface to the spirv-opt tool in the Vulkan SDK, used to optimize SPIR-V for size and speed
	internal static class SPIRVOpt
	{
		// The absolute path to the spirv-opt executable
		private static string TOOL_PATH = null;

		public static bool Optimize(CompileOptions options, string inFile, string outFile, out CompileError error)
		{
			if (!Initialize(out var initError))
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

			// Get the SDK path from the environment (we already know this will work from GLSLV)
			var sdk = Environment.GetEnvironmentVariable("VULKAN_SDK") ?? Environment.GetEnvironmentVariable("VK_SDK_PATH");

			// Get the tool path and ensure it exists
			var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			TOOL_PATH = Path.Combine(sdk, "Bin", isWin ? "spirv-opt.exe" : "spirv-opt");
			if (!File.Exists(TOOL_PATH))
			{
				error = $"Could not find spir-v optimizer at path '{TOOL_PATH}'.";
				return false;
			}

			return true;
		}
	}
}
