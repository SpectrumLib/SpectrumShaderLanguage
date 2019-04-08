using System;
using System.Linq;
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

		// Tracks the indentation for scopes to output more readable code
		private string _indent = "";
		public uint IndentLevel => (uint)_indent.Length;
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
		public void EmitCommentFunc(string cmt) => _funcSource.AppendLine($"{_indent}// " + cmt);

		public void PushIndent() => _indent += "\t";
		public void PopIndent() => _indent = new string('\t', (IndentLevel > 0) ? _indent.Length - 1 : 0);

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

		#region Functions
		public void EmitOpenBlock() { _funcSource.AppendLine($"{_indent}{{"); PushIndent(); }
		public void EmitCloseBlock() { PopIndent(); _funcSource.AppendLine($"{_indent}}}"); }

		public void EmitFunctionHeader(StandardFunction func)
		{
			var plist = String.Join(", ", func.Params.Select(par =>
				$"{par.Access.ToString().ToLower()} {par.Type.ToGLSL()} {par.Name}"
			));
			if (plist.Length == 0)
				plist = "void";

			_funcSource.AppendLine($"{func.ReturnType.ToGLSL()} {func.OutputName}({plist})");
		}

		public void EmitStageFunctionHeader(ShaderStages stage)
		{
			string fname = "ERROR_FUNCTION";
			switch (stage)
			{
				case ShaderStages.Vertex: fname = "VERT_MAIN"; break;
				case ShaderStages.TessControl: fname = "TESC_MAIN"; break;
				case ShaderStages.TessEval: fname = "TESE_MAIN"; break;
				case ShaderStages.Geometry: fname = "GEOM_MAIN"; break;
				case ShaderStages.Fragment: fname = "FRAG_MAIN"; break;
			}
			_funcSource.AppendLine($"void {fname}(void)");
		}
		#endregion // Functions
	}
}
