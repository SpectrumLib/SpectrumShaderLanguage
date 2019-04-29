using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SSLang.Reflection
{
	// Contains the logic for converting reflection information into text or binary format and writing them to a file
	internal static class ReflectionWriter
	{
		private static readonly Version TOOL_VERSION;

		public static void SaveTo(string outPath, bool binary, ShaderInfo info)
		{
			if (binary) SaveBinary(outPath, info);
			else SaveText(outPath, info);
		}

		private static void SaveText(string outPath, ShaderInfo info)
		{
			StringBuilder sb = new StringBuilder(1024);

			sb.AppendLine($"SSL Reflection Dump (v{TOOL_VERSION.Major}.{TOOL_VERSION.Minor}.{TOOL_VERSION.Revision})");
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
					uni.IsArray ? $"[{uni.ArraySize}]" :
					uni.Type.IsSubpassInput() ? $"<{uni.SubpassIndex.Value}>" :
					uni.Type.IsImageType() ? $"<{uni.ImageFormat.Value.ToSSLKeyword()}>" : "";
				var tstr = $"{uni.Type}{qualstr}";
				var bstr = "";
				if (uni.Type.IsValueType()) // It will be in a block
					bstr = $"Block={uni.Location,-3} Idx={uni.Index,-3} Off={uni.Offset,-3}";
				sb.AppendLine($"{uni.Name,-20} {tstr,-20} Loc={uni.Location,-3} Size={uni.Size,-3} {bstr}");
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
				var tstr = $"{attr.Type}{(attr.IsArray ? $"[{attr.ArraySize}]" : "")}";
				sb.AppendLine($"{attr.Name,-20} {tstr,-20} Loc={attr.Location,-3} Size={attr.Size,-3} Slots={attr.SlotCount,-3}");
			}
			sb.AppendLine();

			// Write the outputs
			sb.AppendLine("Outputs");
			sb.AppendLine("-------");
			foreach (var output in info.Outputs)
			{
				sb.AppendLine($"{output.Name,-20} {output.Type,-20}");
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
				throw new Exception("The output path is too long.");
			}
			catch (DirectoryNotFoundException)
			{
				throw new Exception("The output directory could not be found, or does not exist.");
			}
		}

		private static void SaveBinary(string outPath, ShaderInfo info)
		{
			using (MemoryStream buffer = new MemoryStream(1024))
			using (BinaryWriter writer = new BinaryWriter(buffer))
			{
				// Write the header and tool version
				writer.Write(Encoding.ASCII.GetBytes("SSLR"));
				writer.Write((byte)TOOL_VERSION.Major);
				writer.Write((byte)TOOL_VERSION.Minor);
				writer.Write((byte)TOOL_VERSION.Revision);

				// Write shader info
				writer.Write((byte)info.Stages); // Stage mask

				// Write the uniform
				info.Sort();
				writer.Write((byte)info.Uniforms.Count);
				writer.Write(info.AreUniformsContiguous());
				writer.Write((byte)info.Blocks.Count);
				foreach (var block in info.Blocks)
					writer.Write((byte)block.Location);
				foreach (var uni in info.Uniforms)
				{
					writer.Write((byte)uni.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(uni.Name));
					writer.Write((byte)uni.Type);
					writer.Write(uni.IsArray ? (byte)uni.ArraySize : (byte)0);
					writer.Write(uni.Type.IsSubpassInput() ? (byte)uni.SubpassIndex : uni.Type.IsImageType() ? (byte)uni.ImageFormat : (byte)0xFF);
					writer.Write((byte)uni.Location);
					writer.Write((byte)uni.Index);
				}

				// Write the vertex attributes
				writer.Write((byte)info.Attributes.Count);
				foreach (var attr in info.Attributes)
				{
					writer.Write((byte)attr.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(attr.Name));
					writer.Write((byte)attr.Type);
					writer.Write(attr.IsArray ? (byte)attr.ArraySize : (byte)0);
					writer.Write((byte)attr.Location);
				}

				// Write the outputs
				writer.Write((byte)info.Outputs.Count);
				foreach (var output in info.Outputs)
				{
					writer.Write((byte)output.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(output.Name));
					writer.Write((byte)output.Type);
				}

				// Write the file
				try
				{
					writer.Flush();
					using (var file = File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.None))
						file.Write(buffer.GetBuffer(), 0, (int)buffer.Position);
				}
				catch (PathTooLongException)
				{
					throw new Exception("The output path is too long.");
				}
				catch (DirectoryNotFoundException)
				{
					throw new Exception("The output directory could not be found, or does not exist.");
				}
			}
		}

		static ReflectionWriter()
		{
			TOOL_VERSION = Assembly.GetExecutingAssembly().GetName().Version;
		}
	}
}
