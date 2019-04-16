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

		// The current function being parsed
		private StandardFunction _currFunc = null;

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

			// Write the internals
			uint intOff = Math.Max(
				Info.Attributes.Max(pair => pair.Location + pair.Variable.Type.GetSlotCount(pair.Variable.ArraySize)),
				(uint)Info.Outputs.Count
			);
			GLSL.EmitCommentVar("Internal variables");
			foreach (var i in ScopeManager.Internals.Values)
			{
				bool any = false;
				for (int sh = 0; sh < 5; ++sh)
				{
					var stage = (ShaderStages)(0x01 << sh);
					if (i.ReadStages.HasFlag(stage) || i.WriteStages.HasFlag(stage))
					{
						GLSL.EmitInternal(i, intOff, stage);
						any = true;
					}
				}
				if (any)
					intOff += i.Type.GetSlotCount(i.ArraySize);
			}
			GLSL.EmitBlankLineVar();

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
			GLSL.CurrentStage = ShaderStages.None;
			ScopeManager.PopScope();
			_currStage = ShaderStages.None;
			_currFunc = null;
		}

		// Ensures that all required variable assignments in the current block have been made
		private void ensureExitAssignments(RuleContext ctx)
		{
			if (_currStage == ShaderStages.None)
			{
				// Ensure all of the 'out' parameters have been assigned
				foreach (var par in _currFunc.Params)
				{
					var pvar = ScopeManager.FindLocal(par.Name);
					if (par.Access == StandardFunction.Access.Out && !pvar.IsWritten)
						_THROW(ctx, $"The 'out' parameter '{par.Name}' must be assigned to before the function exits.");
				}
			}
			else if (_currStage == ShaderStages.Vertex)
			{
				var req = ScopeManager.FindLocal("$Position");
				if (!req.IsWritten)
					_THROW(ctx, $"The builtin variable '$Position' must be assigned before the vertex stage exits.");
			}
			else if (_currStage == ShaderStages.Fragment)
			{
				foreach (var fout in ScopeManager.Outputs.Values)
				{
					if (!fout.IsWritten)
						_THROW(ctx, $"The fragment output variable '{fout.Name}' must be assigned before the fragment stage exits.");
				}
			}
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
			_currFunc = func;

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

			ensureExitAssignments(context);
			exitFunction();
			return null;
		}
		#endregion // Top-Level

		#region Stage Functions
		private void enterStageFunction(ShaderStages stage)
		{
			GLSL.CurrentStage = stage;
			GLSL.EmitCommentFunc($"Shader stage function ({stage})");
			GLSL.EmitStageFunctionHeader();
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

			ensureExitAssignments(context);
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

			ensureExitAssignments(context);
			exitFunction();
			return null;
		}
		#endregion // Stage Functions

		#region Statements
		public override ExprResult VisitVariableDeclaration([NotNull] SSLParser.VariableDeclarationContext context)
		{
			if (!ScopeManager.TryAddLocal(context, out var vrbl, out var error))
				_THROW(context, error);
			GLSL.EmitDeclaration(vrbl);
			return null;
		}

		public override ExprResult VisitVariableDefinition([NotNull] SSLParser.VariableDefinitionContext context)
		{
			if (!ScopeManager.TryAddLocal(context, out var vrbl, out var error))
				_THROW(context, error);
			if (context.arrayIndexer() != null) // We are creating a new array
			{
				if (context.arrayLiteral() != null)
				{
					_currArrayType = vrbl.Type;
					var alit = Visit(context.arrayLiteral()); // Also validates the array type
					_currArrayType = ShaderType.Void;
					if (vrbl.ArraySize != alit.ArraySize)
						_THROW(context.arrayLiteral(), $"Array size mismatch ({vrbl.ArraySize} != {alit.ArraySize}) in definition.");
					GLSL.EmitDefinition(vrbl, alit);
				}
				else
					_THROW(context.expression(), "An array type must be initialized with an array literal.");
			}
			else
			{
				if (context.arrayLiteral() != null)
					_THROW(context.arrayLiteral(), "A non-array type cannot be initialized with an array literal.");
				else
				{
					var exp = Visit(context.expression());
					if (!exp.Type.CanCastTo(vrbl.Type))
						_THROW(context.expression(), $"Cannot assign the type '{exp.Type}' to the type '{vrbl.Type}'.");
					GLSL.EmitDefinition(vrbl, exp);
				}
			}
			vrbl.IsWritten = true;
			return null;
		}

		public override ExprResult VisitAssignment([NotNull] SSLParser.AssignmentContext context)
		{
			var vname = context.Name.Text;
			var vrbl = ScopeManager.FindAny(vname);
			if (vrbl == null)
				_THROW(context.Name, $"A variable with the name '{vname}' does not exist in the current context.");
			if (vrbl.Constant)
				_THROW(context, $"The variable '{vname}' is read only and cannot be assigned to.");
			if (vrbl.IsAttribute && _currStage != ShaderStages.Vertex)
				_THROW(context.IDENTIFIER().Symbol, $"The vertex attribute '{vname}' can only be accessed in the vertex shader stage.");
			if (vrbl.IsFragmentOutput && _currStage != ShaderStages.Fragment)
				_THROW(context.IDENTIFIER().Symbol, $"The fragment output '{vname}' can only be accessed in the fragment shader stage.");
			vrbl.WriteStages |= _currStage;
			var actx = context.arrayIndexer();
			var swiz = context.SWIZZLE();
			var ltype = TypeUtils.ApplyLValueModifier(this, context.Name, vrbl, actx, swiz, out var arrIndex);

			var expr = Visit(context.Value);
			if (!expr.Type.CanCastTo(ltype))
				_THROW(context.Value, $"The expression type '{expr.Type}' cannot be assigned to the variable type '{ltype}'.");
			if (expr.ArraySize != vrbl.ArraySize)
				_THROW(context.Value, $"The expression has a mismatched array size with the assignment variable.");

			GLSL.EmitAssignment(vrbl.GetOutputName(_currStage), arrIndex, swiz?.Symbol?.Text, expr);
			vrbl.IsWritten = true;
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

		public override ExprResult VisitControlFlowStatement([NotNull] SSLParser.ControlFlowStatementContext context)
		{
			if (context.KW_RETURN() != null) // return statement
			{
				var rexpr = (context.RVal != null) ? Visit(context.RVal) : null;
				var rtype = rexpr?.Type ?? ShaderType.Void;
				var etype = _currFunc?.ReturnType ?? ShaderType.Void;
				if (etype.IsVoid())
				{
					if (rexpr != null)
						_THROW(context, $"{((_currFunc != null) ? $"The function '{_currFunc.Name}' does" : "Shader stage functions do")} not allow return values.");
				}
				else if (!rtype.CanCastTo(etype))
					_THROW(context, $"The given return type '{rtype}' cannot be cast to the expected type '{etype}'.");

				ensureExitAssignments(context);
				GLSL.EmitReturn(rexpr);
			}
			else if (context.KW_DISCARD() != null) // discard statement
			{
				if (_currStage != ShaderStages.Fragment)
					_THROW(context, "The 'discard' keyword is not allowed outside of the fragment stage.");
				GLSL.EmitDiscard();
			}

			return null;
		}
		#endregion // Statements

		#region Expressions
		#endregion // Expressions

		#region Atoms
		public override ExprResult VisitParenAtom([NotNull] SSLParser.ParenAtomContext context)
		{
			var inner = Visit(context.expression());
			return TypeUtils.ApplyModifiers(this, inner, context.arrayIndexer(), context.SWIZZLE());
		}

		public override ExprResult VisitConstructionAtom([NotNull] SSLParser.ConstructionAtomContext context)
		{
			var tctx = context.typeConstruction();
			var ntype = ShaderTypeHelper.FromTypeContext(tctx.Type);
			if (ntype == ShaderType.Error)
				_THROW(tctx, $"Did not understand the type '{tctx.Type.Start.Text}' in type construction.");

			var args = tctx._Args.Select(e => Visit(e)).ToList();
			if (!FunctionCallUtils.CanConstructType(ntype, args, out var error))
				_THROW(tctx, error);

			var ssa = ScopeManager.TryAddSSALocal(ntype, 0);
			var expr = new ExprResult(ssa, $"{tctx.Type.Start.Text}( {String.Join(", ", args.Select(a => a.RefText))} )");
			GLSL.EmitDefinition(expr.SSA, expr);
			return TypeUtils.ApplyModifiers(this, expr, context.arrayIndexer(), context.SWIZZLE());
		}

		public override ExprResult VisitBuiltinCallAtom([NotNull] SSLParser.BuiltinCallAtomContext context)
		{
			var cexpr = Visit(context.builtinFunctionCall());
			return TypeUtils.ApplyModifiers(this, cexpr, context.arrayIndexer(), context.SWIZZLE());
		}

		public override ExprResult VisitBuiltinCall1([NotNull] SSLParser.BuiltinCall1Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			var ssa = ScopeManager.TryAddSSALocal(rtype, 0);
			var ret = new ExprResult(ssa, $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return ret;
		}

		public override ExprResult VisitBuiltinCall2([NotNull] SSLParser.BuiltinCall2Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1), Visit(context.A2) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			var ssa = ScopeManager.TryAddSSALocal(rtype, 0);
			var ret = new ExprResult(ssa, $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return ret;
		}

		public override ExprResult VisitBuiltinCall3([NotNull] SSLParser.BuiltinCall3Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1), Visit(context.A2), Visit(context.A3) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			var ssa = ScopeManager.TryAddSSALocal(rtype, 0);
			var ret = new ExprResult(ssa, $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return ret;
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
				// Checks for reference-passed values
				if (ptypes[i].Access != StandardFunction.Access.In)
				{
					if (avars[i].LValue == null)
						_THROW(fc._Args[i], $"Must pass a modifiable variable to 'out' or 'inout' function (argument {i+1}).");
					if (avars[i].LValue.Constant)
						_THROW(fc._Args[i], $"Cannot pass a constant variable as a reference to a function (argument {i+1}).");
				}

				// Type checking
				if (!avars[i].Type.CanCastTo(ptypes[i].Type))
					_THROW(fc._Args[i], $"Function ('{func.Name}') argument {i+1} expected '{ptypes[i].Type}' type, but got non-castable type '{avars[i].Type}'.");
			}

			var ssa = ScopeManager.TryAddSSALocal(func.ReturnType, 0);
			var ret = new ExprResult(ssa, $"{func.OutputName}({String.Join(", ", avars.Select(arg => arg.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return TypeUtils.ApplyModifiers(this, ret, null, context.SWIZZLE());
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
			if (!vrbl.CanRead)
				_THROW(context.IDENTIFIER().Symbol, $"The {(vrbl.IsBuiltin ? "built-in" : "'out' parameter")} variable '{vname}' is write-only.");
			if (vrbl.IsAttribute && _currStage != ShaderStages.Vertex)
				_THROW(context.IDENTIFIER().Symbol, $"The vertex attribute '{vname}' can only be accessed in the vertex shader stage.");
			if (vrbl.IsFragmentOutput && _currStage != ShaderStages.Fragment)
				_THROW(context.IDENTIFIER().Symbol, $"The fragment output '{vname}' can only be accessed in the fragment shader stage.");
			vrbl.ReadStages |= _currStage;
			var expr = new ExprResult(vrbl.Type, vrbl.ArraySize, vrbl.GetOutputName(_currStage));
			expr.LValue = vrbl;
			return TypeUtils.ApplyModifiers(this, expr, context.arrayIndexer(), context.SWIZZLE());
		}
		#endregion // Atoms
	}
}
