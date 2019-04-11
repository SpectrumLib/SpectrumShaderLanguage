using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// The root type that handles stepping through a parsed ssl file
	internal class SSLVisitor : SSLParserBaseVisitor<ExprResult>
	{
		#region Fields
		// Stream of tokens used to generate the visited tree
		private readonly CommonTokenStream _tokens;

		// The compiler using this visitor
		public readonly SSLCompiler Compiler;

		// The generated GLSL
		public readonly GLSLBuilder GLSL;

		// The reflection info built by the visitor
		public readonly ShaderInfo Info;

		// The scope/variable manager
		public readonly ScopeManager ScopeManager;

		// The options to compile with
		public readonly CompileOptions Options;

		// The current stage (if inside of a stage function)
		private ShaderStages _currStage = ShaderStages.None;

		// The current array type to check array literal elements against
		private ShaderType _currArrayType = ShaderType.Void;
		#endregion // Fields

		public SSLVisitor(CommonTokenStream tokens, SSLCompiler compiler, CompileOptions options)
		{
			_tokens = tokens;
			Compiler = compiler;
			GLSL = new GLSLBuilder();
			Info = new ShaderInfo();
			ScopeManager = new ScopeManager();
			Options = options;
		}

		#region Utilities
		public void _WARN(uint line, string msg) => Options.WarnCallback?.Invoke(Compiler, ErrorSource.Translator, line, msg);
		public void _WARN(RuleContext ctx, string msg) => Options.WarnCallback?.Invoke(Compiler, ErrorSource.Translator, GetContextLine(ctx), msg);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void _THROW(RuleContext ctx, string msg)
		{
			var tk = _tokens.Get(ctx.SourceInterval.a);
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void _THROW(IToken tk, string msg)
		{
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}

		private List<IToken> GetContextTokens(RuleContext ctx) => _tokens.Get(ctx.SourceInterval.a, ctx.SourceInterval.b) as List<IToken>;
		private IToken GetContextToken(RuleContext ctx, uint index) => _tokens.Get(ctx.SourceInterval.a + (int)index);
		private uint GetContextLine(RuleContext ctx) => (uint)_tokens.Get(ctx.SourceInterval.a).Line;
		#endregion // Utilities

		#region Public Helpers
		// Returns a long, but the value is guarenteed to be in the valid 32-bit range for the signedness
		public static long? ParseIntegerLiteral(string text, out bool isUnsigned, out string error)
		{
			string orig = text;
			bool isNeg = text.StartsWith("-");
			isUnsigned = text.EndsWith("u") || text.EndsWith("U");
			text = isNeg ? text.Substring(1) : text;
			text = isUnsigned ? text.Substring(0, text.Length - 1) : text;
			bool isHex = text.StartsWith("0x");
			text = isHex ? text.Substring(2) : text;

			uint res = 0;
			try
			{
				res = Convert.ToUInt32(text, isHex ? 16 : 10);
			}
			catch
			{
				error = $"Could not convert the text '{orig}' to an integer literal";
				return null;
			}

			if (isNeg && (res > ((uint)Int32.MaxValue + 1)))
			{
				error = $"The value {orig} is too large for a signed 32-bit integer";
				return null;
			}

			error = null;
			return isNeg ? -res : res;
		}
		#endregion // Public Helpers

		// This has to be overridden because we must manually visit all of the variable blocks before the functions
		public override ExprResult VisitFile([NotNull] SSLParser.FileContext context)
		{
			// Visit the meta statement first
			var meta = context.shaderMetaStatement();
			if (meta != null)
				Visit(meta);

			var childs = context.children as List<IParseTree>;

			// Visit all of the blocks that create named variables first
			foreach (var ch in childs)
			{ 
				var cctx = ch as SSLParser.TopLevelStatementContext;
				if (cctx == null)
					continue;

				bool isFunc = (cctx.stageFunction() != null) || (cctx.standardFunction() != null);
				if (!isFunc)
					Visit(cctx);
			}

			// Validate that the required blocks are present
			if (ScopeManager.Attributes.Count == 0)
				_THROW(context, "A shader is required to have an 'attributes' block to describe the vertex input.");
			if (ScopeManager.Outputs.Count == 0)
				_THROW(context, "A shader is required to have an 'output' block to describe the fragment stage output.");
			if ((ScopeManager.Uniforms.Count > 0) && !Info.AreUniformsContiguous())
			{
				if (Options.ForceContiguousUniforms)
					_THROW(context, "The shader uniform locations must be contiguous from zero.");
				else
					_WARN(0, "The uniforms are not contiguous, which will result in sub-optimal performance.");
			}

			// Visit all standard functions
			foreach (var ch in childs)
			{
				var cctx = ch as SSLParser.TopLevelStatementContext;
				if (cctx == null)
					continue;
				if (cctx.standardFunction() != null)
					Visit(cctx);
			}

			// Visit the stage functions (in order, but not necessary)
			var vert = childs.FindAll(ipt => IsStageFunction(ipt, ShaderStages.Vertex));
			var tesc = childs.FindAll(ipt => IsStageFunction(ipt, ShaderStages.TessControl));
			var tese = childs.FindAll(ipt => IsStageFunction(ipt, ShaderStages.TessEval));
			var geom = childs.FindAll(ipt => IsStageFunction(ipt, ShaderStages.Geometry));
			var frag = childs.FindAll(ipt => IsStageFunction(ipt, ShaderStages.Fragment));
			if (vert.Count == 0) _THROW(context, "A vertex stage is required for the shader.");
			if (vert.Count > 1) _THROW(vert[1] as RuleContext, "Only one vertex stage is allowed in a shader.");
			if (frag.Count == 0) _THROW(context, "A fragment stage is required for the shader.");
			if (frag.Count > 1) _THROW(frag[1] as RuleContext, "Only one fragment stage is allowed in a shader.");
			if (tesc.Count > 0) _THROW(tesc[0] as RuleContext, "Tessellation control stages are not yet implemented.");
			if (tese.Count > 0) _THROW(tese[0] as RuleContext, "Tessellation evaluation stages are not yet implemented.");
			if (geom.Count > 0) _THROW(geom[0] as RuleContext, "Geometry stages are not yet implemented.");
			Visit(vert[0]);
			Visit(frag[0]);

			return null;
		}

		private static bool IsStageFunction(IParseTree ctx, ShaderStages stage)
		{
			var sctx = (ctx as SSLParser.TopLevelStatementContext)?.stageFunction();
			if (sctx == null)
				return false;

			switch (stage)
			{
				case ShaderStages.Vertex: return sctx is SSLParser.VertFunctionContext;
				case ShaderStages.Fragment: return sctx is SSLParser.FragFunctionContext;
				default: return false;
			}
		}

		private void exitFunction()
		{
			GLSL.EmitCloseBlock();
			GLSL.EmitBlankLineFunc();
			ScopeManager.PopScope();
			_currStage = ShaderStages.None;
		}

		#region Top-Level
		public override ExprResult VisitShaderMetaStatement([NotNull] SSLParser.ShaderMetaStatementContext context)
		{
			var name = context.Name.Text;
			name = name.Substring(1, name.Length - 2);

			if (name.Length > 0)
			{
				GLSL.EmitCommentVar($"Shader name: \"{name}\"");
				GLSL.EmitBlankLineVar();
				Info.Name = name;
			}
			else
				_WARN(context, "Shader name is an empty string, ignoring.");

			return null;
		}

		public override ExprResult VisitAttributesStatement([NotNull] SSLParser.AttributesStatementContext context)
		{
			if (ScopeManager.Attributes.Count > 0) // Already encountered an attributes block
				_THROW(context, "A shader cannot have more than one 'attributes' block.");

			var block = context.typeBlock();
			if (block._Types.Count == 0)
				_THROW(context, "The 'attributes' block cannot be empty.");

			GLSL.EmitCommentVar("Vertex attributes");
			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				if (!ScopeManager.TryAddAttribute(tctx, out var vrbl, out var error))
					_THROW(tctx, error);
				if (!vrbl.Type.IsValueType())
					_THROW(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type.ToKeyword()}', only value types are allowed as attributes.");
				GLSL.EmitVertexAttribute(vrbl, loc);
				Info._attributes.Add((vrbl, loc));
				loc += vrbl.Type.GetSlotCount(vrbl.ArraySize);
			}
			GLSL.EmitBlankLineVar();

			return null;
		}

		public override ExprResult VisitOutputsStatement([NotNull] SSLParser.OutputsStatementContext context)
		{
			if (ScopeManager.Outputs.Count > 0) // Already encountered an outputs block
				_THROW(context, "A shader cannot have more than one 'outputs' block.");

			var block = context.typeBlock();
			if (block._Types.Count == 0)
				_THROW(context, "The 'output' block cannot be empty.");

			GLSL.EmitCommentVar("Fragment stage outputs");
			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				if (!ScopeManager.TryAddOutput(tctx, out var vrbl, out var error))
					_THROW(tctx, error);
				if (!vrbl.Type.IsValueType())
					_THROW(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type.ToKeyword()}', only value types are allowed as outputs.");
				if (vrbl.IsArray)
					_THROW(tctx, $"The output variable '{vrbl.Name}' cannot be an array.");
				GLSL.EmitFragmentOutput(vrbl, loc++);
				Info._outputs.Add(vrbl);
			}
			GLSL.EmitBlankLineVar();

			return null;
		}

		public override ExprResult VisitUniformStatement([NotNull] SSLParser.UniformStatementContext context)
		{
			var head = context.uniformHeader();
			var loc = ParseIntegerLiteral(head.Index.Text, out var isUnsigned, out var error);
			if (!loc.HasValue)
				_THROW(context, error);
			if (loc.Value < 0)
				_THROW(context, $"Uniforms cannot have a negative location.");

			// Check if the location is already taken
			var fidx = Info._uniforms.FindIndex(u => u.l == loc.Value);
			if (fidx >= 0)
				_THROW(context, $"The uniform location {loc.Value} is already bound.");

			GLSL.EmitCommentVar($"Uniform binding {loc.Value}");
			bool isHandle = context.variableDeclaration() != null;
			if (isHandle)
			{
				var vdec = context.variableDeclaration();
				if (!ScopeManager.TryAddUniform(vdec, out var vrbl, out error))
					_THROW(vdec, error);
				if (!vrbl.Type.IsHandleType())
					_THROW(context, $"The uniform '{vrbl.Name}' must be a handle type if declared outside of a block.");
				Info._uniforms.Add((vrbl, (uint)loc.Value, 0, 0));
				GLSL.EmitUniform(vrbl, (uint)loc.Value);
			}
			else
			{
				var tblock = context.typeBlock();
				if (tblock._Types.Count == 0)
					_THROW(context, "Uniform blocks must have at least once member.");
				uint idx = 0, offset = 0, sIdx = (uint)Info.Uniforms.Count;
				GLSL.EmitUniformBlockHeader($"Block{loc.Value}", (uint)loc.Value);
				foreach (var tctx in tblock._Types)
				{
					if (!ScopeManager.TryAddUniform(tctx, out var vrbl, out error))
						_THROW(tctx, error);
					if (!vrbl.Type.IsValueType())
						_THROW(context, $"The uniform '{vrbl.Name}' must be a value type if declared inside of a block.");
					Info._uniforms.Add((vrbl, (uint)loc.Value, idx++, offset));
					GLSL.EmitUniformBlockMember(vrbl, offset);
					offset += vrbl.Size;
				}
				Info._blocks.Add(((uint)loc.Value, offset, Enumerable.Range((int)sIdx, tblock._Types.Count).Select(i => (uint)i).ToArray()));
				GLSL.EmitUniformBlockClose();
			}
			GLSL.EmitBlankLineVar();

			return null;
		}

		public override ExprResult VisitInternalsStatement([NotNull] SSLParser.InternalsStatementContext context)
		{
			var types = context.typeBlock()._Types;
			foreach (var tctx in types)
			{
				if (!ScopeManager.TryAddInternal(tctx, out var vrbl, out var error))
					_THROW(tctx, error);
				if (!vrbl.Type.IsValueType())
					_THROW(context, $"The local '{vrbl.Name}' must be a value type.");
			}

			return null;
		}

		public override ExprResult VisitStandardFunction([NotNull] SSLParser.StandardFunctionContext context)
		{
			if (!ScopeManager.TryAddFunction(context, out var func, out var error))
				_THROW(context, error);

			GLSL.EmitCommentFunc($"Standard function: \"{func.Name}\"");
			GLSL.EmitFunctionHeader(func);
			GLSL.EmitOpenBlock();

			// Push the arguments
			ScopeManager.PushScope();
			foreach (var par in func.Params)
			{
				if (!ScopeManager.TryAddParameter(par, out error))
					_THROW(context, error);
			}

			// Visit the statements
			foreach (var stmt in context.block().statement())
			{
				Visit(stmt);
			}

			exitFunction();
			return null;
		}
		#endregion // Top-Level

		#region Stage Functions
		private void enterStageFunction(ShaderStages stage)
		{
			GLSL.EmitCommentFunc($"Shader stage function ({stage})");
			GLSL.EmitStageFunctionHeader(stage);
			GLSL.EmitOpenBlock();
			ScopeManager.PushScope();
			ScopeManager.AddBuiltins(stage);
			Info.Stages |= stage;
			_currStage = stage;
		}

		public override ExprResult VisitVertFunction([NotNull] SSLParser.VertFunctionContext context)
		{
			enterStageFunction(ShaderStages.Vertex);

			// Visit the statements
			foreach (var stmt in context.block().statement())
			{
				Visit(stmt);
			}

			exitFunction();
			return null;
		}

		public override ExprResult VisitFragFunction([NotNull] SSLParser.FragFunctionContext context)
		{
			enterStageFunction(ShaderStages.Fragment);

			// Visit the statements
			foreach (var stmt in context.block().statement())
			{
				Visit(stmt);
			}

			exitFunction();
			return null;
		}
		#endregion // Stage Functions

		#region Statements
		public override ExprResult VisitStatement([NotNull] SSLParser.StatementContext context)
		{
			if (context.variableDeclaration() != null)
			{
				var varDec = context.variableDeclaration();
				if (!ScopeManager.TryAddLocal(varDec, out var vrbl, out var error))
					_THROW(varDec, error);
				GLSL.EmitDeclaration(vrbl);
			}
			else if (context.variableDefinition() != null)
			{
				var varDef = context.variableDefinition();
				if (!ScopeManager.TryAddLocal(varDef, out var vrbl, out var error))
					_THROW(varDef, error);
				if (varDef.arrayIndexer() != null) // We are creating a new array
				{
					if (varDef.arrayLiteral() != null)
					{
						_currArrayType = vrbl.Type;
						var alit = Visit(varDef.arrayLiteral()); // Also validates the array type
						_currArrayType = ShaderType.Void;
						if (vrbl.ArraySize != alit.ArraySize)
							_THROW(varDef.arrayLiteral(), $"Array size mismatch ({vrbl.ArraySize} != {alit.ArraySize}) in definition.");
						GLSL.EmitDefinition(vrbl, alit);
					}
					else
						_THROW(varDef.expression(), "An array type must be initialized with an array literal.");
				}
				else
				{
					if (varDef.arrayLiteral() != null)
						_THROW(varDef.arrayLiteral(), "A non-array type cannot be initialized with an array literal.");
					else
					{
						var exp = Visit(varDef.expression());
						if (!exp.Type.CanCastTo(vrbl.Type))
							_THROW(varDef.expression(), $"Cannot assign the type '{exp.Type}' to the type '{vrbl.Type}'.");
						GLSL.EmitDefinition(vrbl, exp);
					}
				}
			}

			return null;
		}

		public override ExprResult VisitArrayLiteral([NotNull] SSLParser.ArrayLiteralContext context)
		{
			var exprs = context._Values.Select(e => Visit(e)).ToList();
			var bidx = exprs.FindIndex(e => !e.Type.CanCastTo(_currArrayType));
			if (bidx != -1)
				_THROW(context._Values[bidx], $"Array literal ({_currArrayType}) has incompatible type '{exprs[bidx].Type}' at element {bidx}.");

			return new ExprResult(_currArrayType, (uint)exprs.Count, $"{{ {String.Join(", ", exprs.Select(e => e.RefText))} }}");
		}
		#endregion // Statements

		#region Expressions
		#endregion // Expressions

		#region Atoms
		public override ExprResult VisitParenAtom([NotNull] SSLParser.ParenAtomContext context)
		{
			var inner = Visit(context.expression());
			return TypeManager.ApplyModifiers(this, inner, context.arrayIndexer(), context.SWIZZLE());
		}

		public override ExprResult VisitFunctionCallAtom([NotNull] SSLParser.FunctionCallAtomContext context)
		{
			var fc = context.functionCall();
			var func = ScopeManager.FindFunction(fc.FName.Text);
			if (func == null)
				_THROW(fc.FName, $"There is no user defined function with the name '{fc.FName.Text}'.");

			if (func.Params.Length != fc._Args.Count)
				_THROW(context, $"Function ('{func.Name}') call expected {func.Params.Length} arguments, but {fc._Args.Count} were given.");
			var ptypes = func.Params;
			var avars = fc._Args.Select(arg => Visit(arg)).ToList();
			for (int i = 0; i < ptypes.Length; ++i)
			{
				if (!avars[i].Type.CanCastTo(ptypes[i].Type))
					_THROW(fc._Args[i], $"Function ('{func.Name}') argument {i} expected '{ptypes[i].Type}' type, but got non-castable type '{avars[i].Type}'.");
			}

			var ssa = ScopeManager.TryAddSSALocal(func.ReturnType, 0);
			var ret = new ExprResult(ssa, $"{func.OutputName}({String.Join(", ", avars.Select(arg => arg.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return TypeManager.ApplyModifiers(this, ret, null, context.SWIZZLE());
		}

		public override ExprResult VisitLiteralAtom([NotNull] SSLParser.LiteralAtomContext context)
		{
			var vl = context.valueLiteral();
			if (vl.BOOLEAN_LITERAL() != null)
				return new ExprResult(ShaderType.Bool, 0, vl.BOOLEAN_LITERAL().Symbol.Text);
			if (vl.FLOAT_LITERAL() != null)
				return new ExprResult(ShaderType.Float, 0, vl.FLOAT_LITERAL().Symbol.Text);
			if (vl.INTEGER_LITERAL() != null)
			{
				var ltxt = vl.INTEGER_LITERAL().Symbol.Text;
				var val = ParseIntegerLiteral(ltxt, out bool isus, out var error);
				if (!val.HasValue)
					_THROW(vl.INTEGER_LITERAL().Symbol, "Unable to parse the integer literal.");
				return new ExprResult(isus ? ShaderType.UInt : ShaderType.Int, 0, val.Value.ToString());
			}
			return null; // Never reached
		}

		public override ExprResult VisitVariableAtom([NotNull] SSLParser.VariableAtomContext context)
		{
			var vname = context.IDENTIFIER().Symbol.Text;
			var vrbl = ScopeManager.FindAny(vname);
			if (vrbl == null)
				_THROW(context.IDENTIFIER().Symbol, $"A variable with the name '{vname}' does not exist in the current scope.");
			return TypeManager.ApplyModifiers(this, new ExprResult(vrbl.Type, vrbl.ArraySize, vname), context.arrayIndexer(), context.SWIZZLE());
		}
		#endregion // Atoms
	}
}
