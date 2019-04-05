using System;
using System.Text;
using SSLang.Reflection;

namespace SSLang
{
	// Generates human-readable GLSL code
	internal class GLSLBuilder
	{
		private static readonly string GENERATED_COMMENT =
			"// Generated from Spectrum Shader Language input using sslc.";
		private static readonly string VERSION_STRING = "#version 450";

		#region Fields
		// Contains the variable listings - the inputs, outputs, uniforms, and locals
		private readonly StringBuilder _varSource;
		// Contains the functions and their code
		private readonly StringBuilder _funcSource;
		#endregion // Fields

		public GLSLBuilder()
		{
			_varSource = new StringBuilder(2048);
			_funcSource = new StringBuilder(8192);
			_varSource.AppendLine(GENERATED_COMMENT);
			_varSource.AppendLine(VERSION_STRING);
			_varSource.AppendLine();
			_funcSource.AppendLine();
		}

		public string GetSource() => _varSource.ToString() + _funcSource.ToString();

		public void EmitBlankLineVar() => _varSource.AppendLine();
		public void EmitBlankLineFunc() => _funcSource.AppendLine();

		public void EmitCommentVar(string cmt) => _varSource.AppendLine("// " + cmt);
		public void EmitCommentFunc(string cmt) => _funcSource.AppendLine("// " + cmt);

		public void EmitVertexAttribute(Variable @var, uint loc) => 
			_varSource.AppendLine($"layout(location = {loc}) in {var.Type.ToGLSL()} {var.Name}{(@var.IsArray ? $"[{var.ArraySize}]" : "")};");
	}
}
