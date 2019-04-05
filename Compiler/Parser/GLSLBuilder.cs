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
		private static readonly string[] EXTENSIONS = {
			"GL_EXT_scalar_block_layout"
		};

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
			foreach (var ext in EXTENSIONS)
				_varSource.AppendLine($"#extension {ext} : require");
			_varSource.AppendLine();
			_funcSource.AppendLine();
		}

		public string GetSource() => _varSource.ToString() + _funcSource.ToString();

		public void EmitBlankLineVar() => _varSource.AppendLine();
		public void EmitBlankLineFunc() => _funcSource.AppendLine();

		public void EmitCommentVar(string cmt) => _varSource.AppendLine("// " + cmt);
		public void EmitCommentFunc(string cmt) => _funcSource.AppendLine("// " + cmt);

		#region Variables
		public void EmitVertexAttribute(Variable vrbl, uint loc) => 
			_varSource.AppendLine($"layout(location = {loc}) in {vrbl.GetGLSLDecl()};");

		public void EmitFragmentOutput(Variable vrbl, uint loc) => // Wont ever be an array
			_varSource.AppendLine($"layout(location = {loc}) out {vrbl.GetGLSLDecl()};");

		public void EmitUniform(Variable vrbl, uint loc) =>
			_varSource.AppendLine($"layout(set = 0, binding = {loc}) uniform {vrbl.GetGLSLDecl()};");

		public void EmitUniformBlockHeader(string name, uint loc) =>
			_varSource.AppendLine($"layout(scalar, set = 0, binding = {loc}) uniform {name} {{");

		public void EmitUniformBlockMember(Variable vrbl, uint offset) =>
			_varSource.AppendLine($"\tlayout(offset = {offset}) {vrbl.GetGLSLDecl()};");

		public void EmitUniformBlockClose() => _varSource.AppendLine("};");
		#endregion // Variables
	}
}
