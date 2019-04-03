using System;
using System.Text;

namespace SSLang
{
	// Generates human-readable GLSL code
	internal class GLSLBuilder
	{
		private static readonly string GENERATED_COMMENT =
			"// Generated from SpectrumShaderLanguage input using sslc.";
		private static readonly string VERSION_STRING = "#version 450";

		#region Fields
		// Contains the glsl source as it is built
		private readonly StringBuilder _source;
		#endregion // Fields

		public GLSLBuilder()
		{
			_source = new StringBuilder(8192);
			_source.Append(GENERATED_COMMENT);
			_source.AppendLine(VERSION_STRING);
			_source.AppendLine();
		}

		public void EmitBlankLine() => _source.AppendLine();

		public void EmitComment(string cmt) => _source.AppendLine("// " + cmt);
	}
}
