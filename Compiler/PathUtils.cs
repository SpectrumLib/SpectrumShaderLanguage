using System;
using System.IO;

namespace SSLang
{
	internal static class PathUtils
	{
		// Checks if the path is a valid filesystem path
		public static bool IsValid(string path, bool allowRelative = true)
		{
			try
			{
				var fpath = Path.GetFullPath(path);
				return allowRelative || Path.IsPathRooted(path);
			}
			catch
			{
				return false;
			}
		}

		// Attempts to make the path absolute, returns if it could
		public static bool TryGetAbsolute(string path, out string abs)
		{
			try
			{
				abs = Path.GetFullPath(path);
				return true;
			}
			catch
			{
				abs = null;
				return false;
			}
		}

		// Checks if the path is a valid filesystem path and points to a directory
		public static bool IsValidDirectory(string path, bool allowRelative = true)
		{
			try
			{
				var fpath = Path.GetFullPath(path);
				return (allowRelative || Path.IsPathRooted(path)) && Path.GetExtension(path) == String.Empty;
			}
			catch
			{
				return false;
			}
		}

		// Replaces the extension of a path
		public static string ReplaceExtension(string path, string newExt)
		{
			if (String.IsNullOrWhiteSpace(path) || !IsValid(path))
				throw new IOException($"The path '{path}' is not a valid filesystem path.");

			return Path.GetFileNameWithoutExtension(path) + newExt;
		}
	}
}
