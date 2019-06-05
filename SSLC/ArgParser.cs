using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SSLC
{
	public static class ArgParser
	{
		public static string[] Args { get; private set; } = null;

		public static bool ContainsAny(params string[] args)
		{
			foreach (var a in args)
			{
				if (Args.Contains('@' + a))
					return true;
			}
			return false;
		}

		// Returns if the value was found and is valid, value = null is not found, value != null is found but invalid
		public static bool TryGetValueArg(out string value, params string[] args)
		{
			value = null;
			var idx = Array.FindIndex(Args, a => a[0] == '@' && args.Contains(a.Substring(1)));
			if (idx == -1)
				return false;
			if (idx == (Args.Length - 1))
			{
				value = "";
				return false;
			}
			value = Args[idx + 1];
			return !value.StartsWith('@');
		}

		public static void Load(string[] args)
		{
			var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			Args = args.Select(arg => {
				bool isopt = (arg[0] == '-') || (isWin && arg[0] == '/');
				return isopt ? ('@' + arg.Substring(arg.StartsWith("--") ? 2 : 1)) : arg;
			}).ToArray();
		}

		public static bool Help => ContainsAny("help", "h", "?");

		public static string InputFile => Args[Args.Length - 1].StartsWith('@') ? null : Args[Args.Length - 1];
	}
}
