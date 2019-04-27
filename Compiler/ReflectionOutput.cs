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
		private static readonly Version TOOL_VERSION;

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
					uni.Type.IsSubpassInput() ? $"<{uni.SubpassIndex}>" :
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
			error = null;

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

				// Write the uniforms
				writer.Write((byte)info.Uniforms.Count());
				writer.Write(info.AreUniformsContiguous());
				uint uidx = 0;
				foreach (var uni in info.Uniforms)
				{
					writer.Write((byte)uni.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(uni.Name));
					writer.Write((byte)uni.Type);
					writer.Write((byte)uni.Type.GetSize());
					writer.Write(uni.IsArray ? (byte)uni.ArraySize : (byte)0);
					writer.Write(uni.Type.IsSubpassInput() ? (byte)uni.SubpassIndex : uni.Type.IsImageType() ? (byte)uni.ImageFormat : (byte)0xFF);
					writer.Write((byte)uni.Location);
					writer.Write(uni.Type.IsValueType());
					if (uni.Type.IsValueType()) // It will be in a block
						writer.Write((byte)uni.Index);
					else
						writer.Write((byte)0xFF);
					++uidx;
				}

				// Write the vertex attributes
				writer.Write((byte)info.Attributes.Count);
				foreach (var attr in info.Attributes)
				{
					writer.Write((byte)attr.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(attr.Name));
					writer.Write((byte)attr.Type);
					writer.Write((byte)attr.Type.GetSize());
					writer.Write(attr.IsArray ? (byte)attr.ArraySize : (byte)0);
					writer.Write((byte)attr.Location);
					writer.Write((byte)attr.SlotCount);
				}

				// Write the outputs
				uint oidx = 0;
				foreach (var output in info.Outputs)
				{
					writer.Write((byte)output.Name.Length);
					writer.Write(Encoding.ASCII.GetBytes(output.Name));
					writer.Write((byte)output.Type);
					writer.Write((byte)output.Type.GetSize());
					writer.Write((byte)oidx);
					++oidx;
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
			}

			return true;
		}

		static ReflectionOutput()
		{
			TOOL_VERSION = Assembly.GetExecutingAssembly().GetName().Version;
		}
	}
}
