using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SLLC
{
	public static class ArgParser
	{
		public static string[] Args { get; private set; } = null;

		public static bool ContainsArg(string arg) => Args.Contains('@' + arg);

		// Returns if the value was found and is valid, value = null is not found, value != null is found but invalid
		public static bool TryLoadValueArg(out string value, params string[] args)
		{
			value = null;
			var idx = Array.FindIndex(Args, a => args.Contains('@' + a));
			if (idx == -1)
				return false;
			if (idx == Args.Length - 1)
			{
				value = "";
				return false;
			}
			value = Args[idx + 1];
			if (value.StartsWith('@'))
				return false;
			return true;
		}

		public static void Load(string[] args)
		{
			var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			Args = args.Select(arg => {
				bool isFlag = (arg[0] == '-') || (isWin && (arg[0] == '/'));
				if (isFlag)
				{
					bool isLong = arg.StartsWith("--");
					return '@' + arg.Substring(isLong ? 2 : 1);
				}
				return arg;
			}).ToArray();
		}

		public static bool Help => ContainsArg("help") || ContainsArg("?") || ContainsArg("h");

		public static bool NoCompile => ContainsArg("nc") || ContainsArg("no-compile");

		public static bool OutputGLSL => ContainsArg("i") || ContainsArg("glsl");

		public static bool NoOptimize => ContainsArg("Od") || ContainsArg("no-optimize");

		public static bool OutputReflection => ContainsArg("r") || ContainsArg("reflect");

		public static bool UseBinaryReflection => ContainsArg("b") || ContainsArg("binary");

		public static bool ForceContiguousUniforms => ContainsArg("fcu") || ContainsArg("force-cu");

		public static string InputFile => Args[Args.Length - 1].StartsWith('@') ? null : Args[Args.Length - 1];
	}
}
