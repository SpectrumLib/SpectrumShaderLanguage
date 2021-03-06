﻿using System;
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
		// Contains the variable listings for the uniforms
		private readonly StringBuilder _uniSource;
		// Contains the variable listings for the vertex inputs
		private readonly StringBuilder _attrSource;
		// Contains the variable listings for the fragment outptus
		private readonly StringBuilder _outputSource;
		// Contains the variable listings for the locals
		private readonly Dictionary<ShaderStages, StringBuilder> _localSources;
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
			_uniSource = new StringBuilder(1024);
			_attrSource = new StringBuilder(512);
			_outputSource = new StringBuilder(512);
			_funcSources = new Dictionary<ShaderStages, StringBuilder>() {
				{ ShaderStages.None, new StringBuilder(2048) }, { ShaderStages.Vertex, new StringBuilder(2048) }, { ShaderStages.TessControl, new StringBuilder(2048) },
				{ ShaderStages.TessEval, new StringBuilder(2048) }, { ShaderStages.Geometry, new StringBuilder(2048) }, { ShaderStages.Fragment, new StringBuilder(2048) }
			};
			_localSources = new Dictionary<ShaderStages, StringBuilder>() {
				{ ShaderStages.None, new StringBuilder(512) }, { ShaderStages.Vertex, new StringBuilder(512) }, { ShaderStages.TessControl, new StringBuilder(512) },
				{ ShaderStages.TessEval, new StringBuilder(512) }, { ShaderStages.Geometry, new StringBuilder(512) }, { ShaderStages.Fragment, new StringBuilder(512) }
			};

			// Initial generated glsl
			_uniSource.AppendLine(GENERATED_COMMENT);
			_uniSource.AppendLine(VERSION_STRING);
			foreach (var ext in EXTENSIONS)
				_uniSource.AppendLine($"#extension {ext} : require");
			_uniSource.AppendLine();
			_attrSource.AppendLine("// Vertex attributes");
			_outputSource.AppendLine("// Fragment stage outputs");
			foreach (var sb in _localSources.Values)
				sb.AppendLine("// Internal values");
			foreach (var sb in _funcSources.Values)
				sb.AppendLine();
		}

		public void EmitBlankLineVar() => _uniSource.AppendLine();
		public void EmitBlankLineFunc() => _funcSource.AppendLine();

		public void EmitCommentVar(string cmt) => _uniSource.AppendLine("// " + cmt);
		public void EmitCommentFunc(string cmt) => _funcSource.AppendLine($"{_indent}// " + cmt);

		public void PushIndent() => _indent += "\t";
		public void PopIndent() => _indent = new string('\t', (IndentLevel > 0) ? _indent.Length - 1 : 0);

		#region Variables
		public void EmitVertexAttribute(Variable vrbl, uint loc) =>
			_attrSource.AppendLine($"layout(location = {loc}) in {vrbl.GetGLSLDecl()};");

		public void EmitFragmentOutput(Variable vrbl, uint loc) =>
			_outputSource.AppendLine($"layout(location = {loc}) out {vrbl.GetGLSLDecl()};");

		public void EmitUniform(Variable vrbl, uint loc)
		{
			if (vrbl.Type.IsSubpassInput())
			{
				_funcSources[ShaderStages.Fragment].AppendLine($"// Uniform Subpass Input {vrbl.SubpassIndex}");
				_funcSources[ShaderStages.Fragment].AppendLine(
					$"layout(set = 0, binding = {loc}, input_attachment_index = {vrbl.SubpassIndex}) uniform {vrbl.GetGLSLDecl()};"
				);
				_funcSources[ShaderStages.Fragment].AppendLine();
			}
			else if (vrbl.Type.IsImageType())
			{
				_uniSource.AppendLine($"// Uniform binding {loc}");
				_uniSource.AppendLine($"layout(set = 0, binding = {loc}, {vrbl.ImageFormat.Value.ToGLSLKeyword()}) coherent uniform {vrbl.GetGLSLDecl()};");
			}
			else
			{
				_uniSource.AppendLine($"// Uniform binding {loc}");
				_uniSource.AppendLine($"layout(set = 0, binding = {loc}) uniform {vrbl.GetGLSLDecl()};");
			}
		}

		public void EmitUniformBlockHeader(string name, uint loc)
		{
			_uniSource.AppendLine($"// Uniform binding {loc}");
			_uniSource.AppendLine($"layout(scalar, set = 0, binding = {loc}) uniform {name} {{");
		}

		public void EmitUniformBlockMember(Variable vrbl, uint offset) =>
			_uniSource.AppendLine($"\tlayout(offset = {offset}) {vrbl.GetGLSLDecl()};");

		public void EmitUniformBlockClose() => _uniSource.AppendLine("};");

		public void EmitInternal(Variable vrbl, uint loc, ShaderStages stage)
		{
			var access = (vrbl.ReadStages.HasFlag(stage) ? "in" : "") + (vrbl.WriteStages.HasFlag(stage) ? "out" : "");
			var ct = vrbl.Type.GetComponentType();
			var interp = (vrbl.IsFlat || ct == ShaderType.Int || ct == ShaderType.UInt) ? "flat" : "";
			_localSources[stage].AppendLine($"layout(location = {loc}) {access} {interp} {vrbl.GetGLSLDecl(stage)};");
		}

		public void EmitConstant(Variable vrbl, ExprResult expr)
		{
			var layout = vrbl.IsSpecialized ? $"layout(constant_id = {vrbl.ConstantIndex}) " : "";
			_uniSource.AppendLine($"{layout}const {vrbl.Type.ToGLSLKeyword()} {vrbl.Name} = {expr.RefText};");
			_uniSource.AppendLine();
		}
		#endregion // Variables

		#region Functions
		public void EmitOpenBlock() { _funcSource.AppendLine($"{_indent}{{"); PushIndent(); }
		public void EmitCloseBlock() { PopIndent(); _funcSource.AppendLine($"{_indent}}}"); }

		public void EmitFunctionHeader(StandardFunction func)
		{
			var plist = String.Join(", ", func.Params.Select(par =>
				$"{par.Access.ToString().ToLower()} {par.Type.ToGLSLKeyword()} {par.Name}"
			));
			if (plist.Length == 0)
				plist = "void";

			_funcSource.AppendLine($"{func.ReturnType.ToGLSLKeyword()} {func.OutputName}({plist})");
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
			_funcSource.AppendLine($"{_indent}{v.GetGLSLDecl()} = {expr.ValueText};");

		public void EmitAssignment(string name, (ExprResult i1, ExprResult i2)? aidx, string swiz, string op, ExprResult expr)
		{
			if (aidx.HasValue)
			{
				var atxt = $"[{aidx.Value.i1.RefText}]{((aidx.Value.i2 != null) ? $"[{aidx.Value.i2.RefText}]" : "")}";
				_funcSource.AppendLine($"{_indent}{name}{atxt}{swiz ?? ""} {op} {expr.RefText};");
			}
			else
				_funcSource.AppendLine($"{_indent}{name}{swiz ?? ""} {op} {expr.RefText};");
		}

		public void EmitCall(string fname, ExprResult[] args) =>
			_funcSource.AppendLine($"{_indent}{fname}({String.Join(", ", args.Select(ae => ae.RefText))});");

		public void EmitReturn(ExprResult res) => _funcSource.AppendLine($"{_indent}return {res?.RefText ?? ""};");
		public void EmitDiscard() => _funcSource.AppendLine(_indent + "discard;");
		public void EmitBreak() => _funcSource.AppendLine(_indent + "break;");
		public void EmitContinue() => _funcSource.AppendLine(_indent + "continue;");

		public void EmitIfStatement(ExprResult cond) => _funcSource.AppendLine($"{_indent}if ({cond.RefText}) {{");
		public void EmitElifStatement(ExprResult cond) => _funcSource.AppendLine($"{_indent}else if ({cond.RefText}) {{");
		public void EmitElseStatement() => _funcSource.AppendLine(_indent + "else {");

		public void EmitForLoopHeader(string init, ExprResult cond, string update) =>
			_funcSource.AppendLine($"{_indent}for ( {init} ; {cond.RefText} ; {update} )");
		public void EmitWhileLoopHeader(ExprResult cond) => _funcSource.AppendLine($"{_indent}while ({cond.RefText})");
		public void EmitDoLoopHeader() => _funcSource.AppendLine($"{_indent}do");

		public void EmitDoLoopFooter(ExprResult cond)
		{
			PopIndent();
			_funcSource.AppendLine($"{_indent}}} while ({cond.RefText});");
		}
		#endregion // Functions

		// Gets the glsl output for a specific stage, or a combined shader for ShaderStages.All
		public string GetGLSLOutput(ShaderStages stage)
		{
			switch (stage)
			{
				case ShaderStages.All: return
					"// NOTE THAT THIS FILE WILL NOT COMPILE, AS IT IS NOT VALID GLSL\n"
					+ _uniSource.ToString()
					+ _attrSource.ToString()
					+ _outputSource.ToString()
					+ _funcSources[ShaderStages.None].ToString()
					+ _localSources[ShaderStages.Vertex].ToString()
					+ _funcSources[ShaderStages.Vertex].ToString()
					+ _localSources[ShaderStages.TessControl].ToString()
					+ _funcSources[ShaderStages.TessControl].ToString()
					+ _localSources[ShaderStages.TessEval].ToString()
					+ _funcSources[ShaderStages.TessEval].ToString()
					+ _localSources[ShaderStages.Geometry].ToString()
					+ _funcSources[ShaderStages.Geometry].ToString()
					+ _localSources[ShaderStages.Fragment].ToString()
					+ _funcSources[ShaderStages.Fragment].ToString();
				case ShaderStages.Vertex: return
					_uniSource.ToString()
					+ _attrSource.ToString() + "\n"
					+ _funcSources[ShaderStages.None].ToString()
					+ _localSources[ShaderStages.Vertex].ToString()
					+ _funcSources[ShaderStages.Vertex].ToString();
				case ShaderStages.Fragment: return
					_uniSource.ToString()
					+ _outputSource.ToString() + "\n"
					+ _funcSources[ShaderStages.None].ToString()
					+ _localSources[ShaderStages.Fragment].ToString()
					+ _funcSources[ShaderStages.Fragment].ToString();
				default:
					return "";
			}
		}

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
