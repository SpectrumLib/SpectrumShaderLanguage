using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SSLang.Reflection
{
	// Contains the logic for reading binary reflection data from a file
	internal static class ReflectionReader
	{
		private static readonly Version TOOL_VERSION;

		public static ShaderInfo LoadFrom(BinaryReader reader, Version fileVer)
		{
			if (fileVer > TOOL_VERSION)
				throw new InvalidOperationException($"The file version {fileVer} cannot be loaded by this version of the library ({TOOL_VERSION}).");

			ShaderInfo info = new ShaderInfo();
			
			// Load the stages
			var stages = reader.ReadByte();
			info.Stages = (ShaderStages)stages;

			// Load the uniforms (note that this assumes that the uniforms are sorted when written)
			var ucount = reader.ReadByte();
			var cont = reader.ReadBoolean();
			var bcount = reader.ReadByte();
			info._blocks.AddRange(reader.ReadBytes(bcount).Select(b => new UniformBlock(b)));
			while (ucount > 0)
			{
				var slen = reader.ReadByte();
				var uname = Encoding.ASCII.GetString(reader.ReadBytes(slen));
				var type = (ShaderType)reader.ReadByte();
				var asize = reader.ReadByte();
				var extra = reader.ReadByte();
				var loc = reader.ReadByte();
				var idx = reader.ReadByte();

				UniformBlock b = (idx != 0xFF) ? info._blocks.Find(blk => blk.Location == loc) : null;
				var uni = new Uniform(uname, type, asize == 0 ? (uint?)null : asize, loc, b, idx, b?.Size ?? 0);
				if (type == ShaderType.SubpassInput) uni.SubpassIndex = extra;
				else if (type.IsImageType()) uni.ImageFormat = (ImageFormat)extra;
				b?.AddMember(uni);
				info._uniforms.Add(uni);

				--ucount;
			}

			// Load the attributes
			var acount = reader.ReadByte();
			while (acount > 0)
			{
				var slen = reader.ReadByte();
				var aname = Encoding.ASCII.GetString(reader.ReadBytes(slen));
				var type = (ShaderType)reader.ReadByte();
				var asize = reader.ReadByte();
				var loc = reader.ReadByte();

				info._attributes.Add(new VertexAttribute(aname, type, asize == 0 ? (uint?)null : asize, loc));

				--acount;
			}

			// Load the outputs
			var ocount = reader.ReadByte();
			for (uint i = 0; i < ocount; ++i)
			{
				var slen = reader.ReadByte();
				var oname = Encoding.ASCII.GetString(reader.ReadBytes(slen));
				var type = (ShaderType)reader.ReadByte();

				info._outputs.Add(new FragmentOutput(oname, type, i));
			}

			// Return
			info.Sort();
			return info;
		}

		static ReflectionReader()
		{
			TOOL_VERSION = Assembly.GetExecutingAssembly().GetName().Version;
		}
	}
}
