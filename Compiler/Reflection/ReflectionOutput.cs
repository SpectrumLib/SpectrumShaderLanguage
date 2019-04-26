using System;
using System.IO;
using System.Linq;
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

			sb.AppendLine($"SSL Reflection Dump (v{TOOL_VERSION})");
			sb.AppendLine();

			// General shader info
			var stagestr = "Vertex";
			for (int stage = 1; stage < 5; ++stage)
			{
				var ss = (ShaderStages)(0x01 << stage);
				if ((info.Stages & ss) > 0)
					stagestr += $", {ss}";
			}
			sb.AppendLine($"Stages = {stagestr}");
			sb.AppendLine();

			// Write the uniforms
			sb.AppendLine("Uniforms");
			sb.AppendLine("--------");
			uint uidx = 0;
			foreach (var uni in info.Uniforms)
			{
				var qualstr =
					uni.Variable.IsArray ? $"[{uni.Variable.ArraySize}]" :
					uni.Variable.Type.IsSubpassInput() ? $"<{uni.Variable.SubpassIndex}>" :
					uni.Variable.Type.IsImageHandle() ? $"<{uni.Variable.ImageFormat.ToKeyword()}>" : "";
				var tstr = $"{uni.Variable.Type}{qualstr}";
				var bstr = "";
				if (uni.Variable.Type.IsValueType()) // It will be in a block
				{
					var block = info.Blocks.First(b => b.Location == uni.Location);
					var idx = Array.FindIndex(block.Members, mem => mem == uidx);
					bstr = $"Block={block.Location,-3} Idx={idx,-3} Off={uni.Offset,-3}";
				}
				var size = uni.Variable.Type.GetSize() * Math.Max(1, uni.Variable.ArraySize);
				sb.AppendLine($"{uni.Variable.Name,-20} {tstr,-20} Loc={uni.Location,-3} Size={size,-3} {bstr}");
				++uidx;
			}
			sb.AppendLine();
			if (!info.AreUniformsContiguous())
			{
				sb.AppendLine("Warning: The uniforms are not contiguous!");
				sb.AppendLine();
			}

			// Write the vertex attributes
			sb.AppendLine("Attributes");
			sb.AppendLine("----------");
			foreach (var attr in info.Attributes)
			{
				var tstr = $"{attr.Variable.Type}{(attr.Variable.IsArray ? $"[{attr.Variable.ArraySize}]" : "")}";
				var size = attr.Variable.Type.GetSize() * Math.Max(1, attr.Variable.ArraySize);
				var bsize = attr.Variable.Type.GetSlotCount(attr.Variable.ArraySize);
				sb.AppendLine($"{attr.Variable.Name,-20} {tstr,-20} Loc={attr.Location,-3} Size={size,-3} Slots={bsize,-3}");
			}
			sb.AppendLine();

			// Write the outputs
			sb.AppendLine("Outputs");
			sb.AppendLine("-------");
			uint oidx = 0;
			foreach (var output in info.Outputs)
			{
				sb.AppendLine($"{output.Name,-20} {output.Type,-20} Loc={oidx,-3}");
				++oidx;
			}
			sb.AppendLine();

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
