using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Generates human-readable GLSL code
	internal class GLSLBuilder
	{
		private static readonly string GENERATED_COMMENT =
			"// Generated from Spectrum Shader Language input using sslc.";
		private static readonly string VERSION_STRING = "#version 450 core";
		private static readonly string[] EXTENSIONS = {
			"GL_EXT_scalar_block_layout"
		};

		#region Fields
		// Contains the variable listings - the inputs, outputs, uniforms, and locals
		private readonly StringBuilder _varSource;
		// Contains the function output for general functions and stages
		private readonly Dictionary<ShaderStages, StringBuilder> _funcSources;

		// Tracks the indentation for scopes to output more readable code
		private string _indent = "";
		public uint IndentLevel => (uint)_indent.Length;

		// Controls which function output that emitted glsl goes to
		// ShaderStages.None puts it in global functions
		public ShaderStages CurrentStage = ShaderStages.None;
		// Shortcut to current func source
		private StringBuilder _funcSource => _funcSources[CurrentStage];
		#endregion // Fields

		public GLSLBuilder()
		{
			_varSource = new StringBuilder(2048);
			_funcSources = new Dictionary<ShaderStages, StringBuilder>() {
				{ ShaderStages.None, new StringBuilder(2048) }, { ShaderStages.Vertex, new StringBuilder(2048) }, { ShaderStages.TessControl, new StringBuilder(2048) },
				{ ShaderStages.TessEval, new StringBuilder(2048) }, { ShaderStages.Geometry, new StringBuilder(2048) }, { ShaderStages.Fragment, new StringBuilder(2048) }
			};

			// Initial generated glsl
			_varSource.AppendLine(GENERATED_COMMENT);
			_varSource.AppendLine(VERSION_STRING);
			foreach (var ext in EXTENSIONS)
				_varSource.AppendLine($"#extension {ext} : require");
			_varSource.AppendLine();
		}

		// Gets the full glsl output with all stages combined together
		public string GetGLSLOutput()
		{
			return _varSource.ToString() 
				   + _funcSources[ShaderStages.None].ToString()
				   + _funcSources[ShaderStages.Vertex].ToString()
				   + _funcSources[ShaderStages.TessControl].ToString()
				   + _funcSources[ShaderStages.TessEval].ToString()
				   + _funcSources[ShaderStages.Geometry].ToString()
				   + _funcSources[ShaderStages.Fragment].ToString();
		}

		public string GetStageSource(ShaderStages stage) =>
			_varSource.ToString() + _funcSources[ShaderStages.None].ToString() + 
			((stage != ShaderStages.None) ? _funcSources[stage].ToString() : "");

		public void EmitBlankLineVar() => _varSource.AppendLine();
		public void EmitBlankLineFunc() => _funcSource.AppendLine();

		public void EmitCommentVar(string cmt) => _varSource.AppendLine("// " + cmt);
		public void EmitCommentFunc(string cmt) => _funcSource.AppendLine($"{_indent}// " + cmt);

		public void PushIndent() => _indent += "\t";
		public void PopIndent() => _indent = new string('\t', (IndentLevel > 0) ? _indent.Length - 1 : 0);

		#region Variables
		public void EmitVertexAttribute(Variable vrbl, uint loc) => 
			_varSource.AppendLine($"layout(location = {loc}) in {vrbl.GetGLSLDecl(false)};");

		public void EmitFragmentOutput(Variable vrbl, uint loc) => // Wont ever be an array
			_varSource.AppendLine($"layout(location = {loc}) out {vrbl.GetGLSLDecl(false)};");

		public void EmitUniform(Variable vrbl, uint loc) =>
			_varSource.AppendLine($"layout(set = 0, binding = {loc}) uniform {vrbl.GetGLSLDecl(false)};");

		public void EmitUniformBlockHeader(string name, uint loc) =>
			_varSource.AppendLine($"layout(scalar, set = 0, binding = {loc}) uniform {name} {{");

		public void EmitUniformBlockMember(Variable vrbl, uint offset) =>
			_varSource.AppendLine($"\tlayout(offset = {offset}) {vrbl.GetGLSLDecl(false)};");

		public void EmitUniformBlockClose() => _varSource.AppendLine("};");

		public void EmitInternal(Variable vrbl, uint loc, ShaderStages stage)
		{
			var access = (vrbl.ReadStages.HasFlag(stage) ? "in" : "") + (vrbl.WriteStages.HasFlag(stage) ? "out" : "");
			_varSource.AppendLine($"layout(location = {loc}) {access} {vrbl.GetGLSLDecl(false, stage)};");
		}
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

		public void EmitStageFunctionHeader()
		{
			string fname = "ERROR_FUNCTION";
			switch (CurrentStage)
			{
				case ShaderStages.Vertex: fname = "vert_main"; break;
				case ShaderStages.TessControl: fname = "tesc_main"; break;
				case ShaderStages.TessEval: fname = "tese_main"; break;
				case ShaderStages.Geometry: fname = "geom_main"; break;
				case ShaderStages.Fragment: fname = "frag_main"; break;
			}
			_funcSource.AppendLine($"void {fname}(void)");
		}

		public void EmitDeclaration(Variable v) => _funcSource.AppendLine($"{_indent}{v.GetGLSLDecl()};");
		public void EmitDefinition(Variable v, ExprResult expr) =>
			_funcSource.AppendLine($"{_indent}{v.GetGLSLDecl(false)} = {expr.ValueText};");

		public void EmitAssignment(string name, long? arrIndex, string swiz, string op, ExprResult expr) =>
			_funcSource.AppendLine($"{_indent}{name}{(arrIndex.HasValue ? $"[{arrIndex.Value}]" : "")}{swiz ?? ""} {op} {expr.RefText};");

		public void EmitReturn(ExprResult res) => _funcSource.AppendLine($"{_indent}return {res?.RefText ?? ""};");
		public void EmitDiscard() => _funcSource.AppendLine(_indent + "discard;");
		public void EmitBreak() => _funcSource.AppendLine(_indent + "break;");
		public void EmitContinue() => _funcSource.AppendLine(_indent + "continue;");

		public void EmitIfStatement(ExprResult cond) => _funcSource.AppendLine($"{_indent}if ({cond.RefText}) {{");
		public void EmitElifStatement(ExprResult cond) => _funcSource.AppendLine($"{_indent}else if ({cond.RefText}) {{");
		public void EmitElseStatement() => _funcSource.AppendLine(_indent + "else {");
		#endregion // Functions

		// Gets the glsl builtin function name
		public static string GetBuiltinFuncName(string fname, int ftype)
		{
			return BUILTIN_NAMES.ContainsKey(ftype) ? BUILTIN_NAMES[ftype] : fname;
		}

		// GLSL names for builtin functions (where the SSL name does not match)
		private static readonly Dictionary<int, string> BUILTIN_NAMES = new Dictionary<int, string>()
		{
			{ SSLParser.BIF_DEG2RAD, "radians" },
			{ SSLParser.BIF_RAD2DEG, "degrees" },
			{ SSLParser.BIF_ATAN2, "atan" },
			{ SSLParser.BIF_INVSQRT, "inversesqrt" },
			{ SSLParser.BIF_MATCOMPMUL, "matrixCompMult" },
			{ SSLParser.BIF_VECLT, "lessThan" },
			{ SSLParser.BIF_VECLE, "lessThanEqual" },
			{ SSLParser.BIF_VECGT, "greaterThan" },
			{ SSLParser.BIF_VECGE, "greaterThanEqual" },
			{ SSLParser.BIF_VECEQ, "equal" },
			{ SSLParser.BIF_VECNE, "notEqual" },
			{ SSLParser.BIF_VECANY, "any" },
			{ SSLParser.BIF_VECALL, "all" },
			{ SSLParser.BIF_VECNOT, "not" }
		};
	}
}
