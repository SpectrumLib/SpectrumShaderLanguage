using System;

namespace SLLC
{
	public static class CConsole
	{
		private static readonly ConsoleColor DefaultFGColor;
		private static readonly ConsoleColor DefaultBGColor;

		static CConsole()
		{
			DefaultFGColor = Console.ForegroundColor;
			DefaultBGColor = Console.BackgroundColor;
		}

		public static void Info(string msg) => Console.WriteLine($"INFO: {msg}");

		public static void Warn(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"WARN: {msg}");
			Console.ForegroundColor = DefaultFGColor;
		}

		public static void Error(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"ERROR: {msg}");
			Console.ForegroundColor = DefaultFGColor;
		}
	}
}
