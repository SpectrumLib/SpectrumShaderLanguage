using System;
using System.Linq;

namespace SLLC
{
	public static class ArgParser
	{
		private static readonly char[] COLON_SPLIT = { ':' };

		public static bool ContainsArg(string[] args, string arg) => args.Contains('/' + arg);

		public static string[] Sanitize(string[] args)
		{
			return args.Select(arg => {
				bool isFlag = (arg[0] == '-') || (arg[0] == '/');
				if (isFlag)
				{
					bool isLong = arg.StartsWith("--");
					var split = arg.Split(COLON_SPLIT, 2);
					var end = (split.Length > 1) ? $":{split[1]}" : "";
					return '/' + split[0].Substring(isLong ? 2 : 1).ToLower() + end;
				}
				return arg;
			}).ToArray();
		}

		public static bool Help(string[] args) => ContainsArg(args, "help") || ContainsArg(args, "?") || ContainsArg(args, "h");
	}
}
