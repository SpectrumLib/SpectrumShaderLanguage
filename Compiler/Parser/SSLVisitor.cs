using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
		public void Warn(uint line, string msg) => Options.WarnCallback?.Invoke(Compiler, ErrorSource.Translator, line, msg);
		public void Warn(RuleContext ctx, string msg) => Options.WarnCallback?.Invoke(Compiler, ErrorSource.Translator, GetContextLine(ctx), msg);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Error(RuleContext ctx, string msg)
		{
			var tk = _tokens.Get(ctx.SourceInterval.a);
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Error(IToken tk, string msg)
		{
			throw new VisitException(new CompileError(ErrorSource.Translator, (uint)tk.Line, (uint)tk.Column, msg));
		}

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

		// Parses the two integers in an array indexer
		public bool TryParseArrayIndexer(SSLParser.ArrayIndexerContext ctx, (uint? L1, uint? L2) lims, out (ExprResult Index1, ExprResult Index2) idx, out string error)
		{
			idx = (null, null);

			var lexpr = Visit(ctx.Index1);
			if (lexpr.Type != ShaderType.Int && lexpr.Type != ShaderType.UInt)
			{
				error = $"The array indexer expression must be of type Int.";
				return false;
			}
			var left = lexpr.GetIntegerLiteral();
			if (left.HasValue) // Check the literal
			{
				if (left.Value < 0)
				{
					error = "Array indices cannot be negative.";
					return false; 
				}
				if (lims.L1.HasValue && left.Value > lims.L1.Value)
				{
					error = $"The first array indexer has a value that is too large for the expression ({left.Value} > {lims.L1.Value}).";
					return false;
				}
			}

			ExprResult rexpr = (ctx.Index2 != null) ? Visit(ctx.Index2) : null;
			if (rexpr != null)
			{
				var right = rexpr.GetIntegerLiteral();
				if (right.HasValue) // Check the literal
				{
					if (right.Value < 0)
					{
						error = "Array indices cannot be negative.";
						return false;
					}
					if (lims.L2.HasValue && right.Value > lims.L2.Value)
					{
						error = $"The second array indexer has a value that is too large for the expression ({right.Value} > {lims.L2.Value}).";
						return false;
					} 
				}
			}

			idx = (lexpr, rexpr);
			error = null;
			return true;
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
			Info.Sort();
			if (ScopeManager.Attributes.Count == 0)
				Error(context, "A shader is required to have an 'attributes' block to describe the vertex input.");
			if (ScopeManager.Outputs.Count == 0)
				Error(context, "A shader is required to have an 'output' block to describe the fragment stage output.");
			if ((ScopeManager.Uniforms.Count > 0) && !Info.AreUniformsContiguous())
			{
				if (Options.ForceContiguousUniforms)
					Error(context, "The shader uniform locations must be contiguous from zero.");
				else
					Warn(0, "The uniforms are not contiguous, which will result in sub-optimal performance.");
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
			if (vert.Count == 0) Error(context, "A vertex stage is required for the shader.");
			if (vert.Count > 1) Error(vert[1] as RuleContext, "Only one vertex stage is allowed in a shader.");
			if (frag.Count == 0) Error(context, "A fragment stage is required for the shader.");
			if (frag.Count > 1) Error(frag[1] as RuleContext, "Only one fragment stage is allowed in a shader.");
			if (tesc.Count > 0) Error(tesc[0] as RuleContext, "Tessellation control stages are not yet implemented.");
			if (tese.Count > 0) Error(tese[0] as RuleContext, "Tessellation evaluation stages are not yet implemented.");
			if (geom.Count > 0) Error(geom[0] as RuleContext, "Geometry stages are not yet implemented.");
			Visit(vert[0]);
			Visit(frag[0]);

			// Write the internals
			uint intOff = Math.Max(
				Info.Attributes.Max(v => v.Location + v.Type.GetSlotCount(v.ArraySize)),
				(uint)Info.Outputs.Count
			);
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
					if (par.Access == StandardFunction.Access.Out && !ScopeManager.IsAssigned(pvar))
						Error(ctx, $"The 'out' parameter '{par.Name}' must be assigned to before the function exits.");
				}
			}
			else if (_currStage == ShaderStages.Vertex)
			{
				var req = ScopeManager.FindLocal("$Position");
				if (!ScopeManager.IsAssigned(req))
					Error(ctx, $"The builtin variable '$Position' must be assigned before the vertex stage exits.");
			}
			else if (_currStage == ShaderStages.Fragment)
			{
				foreach (var fout in ScopeManager.Outputs.Values)
				{
					if (!ScopeManager.IsAssigned(fout))
						Error(ctx, $"The fragment output variable '{fout.Name}' must be assigned before the fragment stage exits.");
				}
			}
		}

		// Unified function for finding, checking, and updating the stage flags for a variable
		private Variable findVariable(RuleContext ctx, string vname, bool read, bool write)
		{
			var vrbl = ScopeManager.FindAny(vname);
			if (vrbl == null)
				Error(ctx, $"A variable with the name '{vname}' does not exist in the current scope.");
			if (read && !vrbl.CanRead)
				Error(ctx, $"The {(vrbl.IsBuiltin ? "built-in" : "'out' parameter")} variable '{vname}' is write-only.");
			if (write && vrbl.Constant)
				Error(ctx, $"The variable '{vname}' is constant, and cannot be modified.");
			if (vrbl.IsAttribute && _currStage != ShaderStages.Vertex)
				Error(ctx, $"The vertex attribute '{vname}' can only be accessed in the vertex shader stage.");
			if (vrbl.IsFragmentOutput && _currStage != ShaderStages.Fragment)
				Error(ctx, $"The fragment output '{vname}' can only be accessed in the fragment shader stage.");
			if (vrbl.Type.IsSubpassInput() && _currStage != ShaderStages.Fragment)
				Error(ctx, $"The subpass input '{vname}' can only be accessed in the fragment shader stage.");
			if (vrbl.IsInternal && _currStage == ShaderStages.None)
				Error(ctx, $"The internal '{vname}' cannot be accessed outside of a stage function.");

			if (read)
				vrbl.ReadStages |= _currStage;
			if (write)
				vrbl.WriteStages |= _currStage;
			return vrbl;
		}

		#region Top-Level
		public override ExprResult VisitShaderMetaStatement([NotNull] SSLParser.ShaderMetaStatementContext context)
		{
			return null;
		}

		public override ExprResult VisitAttributesStatement([NotNull] SSLParser.AttributesStatementContext context)
		{
			if (ScopeManager.Attributes.Count > 0) // Already encountered an attributes block
				Error(context, "A shader cannot have more than one 'attributes' block.");

			var block = context.typeBlock();

			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				var vrbl = ScopeManager.AddAttribute(tctx, this);
				if (!vrbl.Type.IsValueType())
					Error(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type}', only value types are allowed as attributes.");
				GLSL.EmitVertexAttribute(vrbl, loc);
				Info._attributes.Add(new VertexAttribute(vrbl.Name, vrbl.Type, vrbl.IsArray ? vrbl.ArraySize : (uint?)null, loc));
				loc += vrbl.Type.GetSlotCount(vrbl.ArraySize);
			}

			if (loc > Options.LimitAttributes)
				Error(context, $"The vertex attributes cannot take up more than {Options.LimitAttributes} binding points.");

			return null;
		}

		public override ExprResult VisitOutputsStatement([NotNull] SSLParser.OutputsStatementContext context)
		{
			if (ScopeManager.Outputs.Count > 0) // Already encountered an outputs block
				Error(context, "A shader cannot have more than one 'outputs' block.");

			var block = context.typeBlock();
			if (block._Types.Count == 0)
				Error(context, "The 'output' block cannot be empty.");
			if (block._Types.Count > Options.LimitOutputs)
				Error(context, $"A maximum of {Options.LimitOutputs} shader outputs can be specified.");

			uint loc = 0;
			foreach (var tctx in block._Types)
			{
				var vrbl = ScopeManager.AddOutput(tctx, this);
				if (!vrbl.Type.IsValueType())
					Error(tctx, $"The variable '{vrbl.Name}' cannot have type '{vrbl.Type}', only value types are allowed as outputs.");
				if (vrbl.IsArray)
					Error(tctx, $"The output variable '{vrbl.Name}' cannot be an array.");
				GLSL.EmitFragmentOutput(vrbl, loc++);
				Info._outputs.Add(new FragmentOutput(vrbl.Name, vrbl.Type, loc));
			}

			return null;
		}

		public override ExprResult VisitUniformStatement([NotNull] SSLParser.UniformStatementContext context)
		{
			var head = context.uniformHeader();
			var loc = ParseIntegerLiteral(head.Index.Text, out var isUnsigned, out var error);
			if (!loc.HasValue)
				Error(context, error);
			if (loc.Value < 0)
				Error(context, $"Uniforms cannot have a negative location.");
			if (loc.Value >= Options.LimitUniforms)
				Error(context, $"Uniforms cannot have binding points past the limit {Options.LimitUniforms}.");
			var ucnt = Info._blocks.Count + Info._uniforms.Sum(u => u.Type.IsHandleType() ? 1 : 0);
			if (ucnt >= Options.LimitUniforms)
				Error(context, $"Cannot bind more than {Options.LimitUniforms} uniforms.");

			// Check if the location is already taken
			var fidx = Info._uniforms.FindIndex(u => u.Location == loc.Value);
			if (fidx >= 0)
				Error(context, $"The uniform location {loc.Value} is already bound.");

			bool isHandle = context.uniformVariable() != null;
			if (isHandle)
			{
				var uvar = context.uniformVariable();
				var vrbl = ScopeManager.AddUniform(uvar, this);
				if (!vrbl.Type.IsHandleType())
					Error(context, $"The uniform '{vrbl.Name}' must be a handle type if declared outside of a block.");
				if (vrbl.Type.IsSubpassInput())
				{
					if (vrbl.SubpassIndex >= Options.LimitSubpassInputs)
						Error(context, $"Cannot attach more than {Options.LimitSubpassInputs} subpass inputs ({vrbl.SubpassIndex} given).");
					fidx = Info._uniforms.FindIndex(u => u.Type.IsSubpassInput() && u.SubpassIndex == vrbl.SubpassIndex);
					if (fidx != -1)
						Error(context, $"Cannot bind two subpasses to the same index ({vrbl.SubpassIndex}).");
				}

				var uni = new Uniform(vrbl.Name, vrbl.Type, vrbl.IsArray ? vrbl.ArraySize : (uint?)null, (uint)loc.Value, null, 0, 0);
				uni.SubpassIndex = vrbl.SubpassIndex;
				uni.ImageFormat = vrbl.ImageFormat;
				Info._uniforms.Add(uni);
				GLSL.EmitUniform(vrbl, (uint)loc.Value);
			}
			else
			{
				var tblock = context.typeBlock();
				if (tblock._Types.Count == 0)
					Error(context, "Uniform blocks must have at least once member.");
				GLSL.EmitUniformBlockHeader($"Block{loc.Value}", (uint)loc.Value);
				UniformBlock block = new UniformBlock((uint)loc.Value);
				foreach (var tctx in tblock._Types)
				{
					var vrbl = ScopeManager.AddUniform(tctx, this);
					if (!vrbl.Type.IsValueType())
						Error(context, $"The uniform '{vrbl.Name}' must be a value type if declared inside of a block.");
					var uni = new Uniform(vrbl.Name, vrbl.Type, vrbl.IsArray ? vrbl.ArraySize : (uint?)null, (uint)loc.Value, block, block.MemberCount, block.Size);
					Info._uniforms.Add(uni);
					block.AddMember(uni);
					GLSL.EmitUniformBlockMember(vrbl, uni.Offset);
				}
				Info._blocks.Add(block);
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
				var vrbl = ScopeManager.AddInternal(tctx, this);
				if (!vrbl.Type.IsValueType())
					Error(context, $"The local '{vrbl.Name}' must be a value type.");
			}

			var sum = ScopeManager.Internals.Values.Sum(v => v.Type.GetSlotCount(v.ArraySize));
			if (sum > Options.LimitInternals)
				Error(context, $"Internals cannot take up more than {Options.LimitInternals} binding slots.");

			return null;
		}

		public override ExprResult VisitConstantStatement([NotNull] SSLParser.ConstantStatementContext context)
		{
			var vrbl = ScopeManager.AddConstant(context, this);
			var expr = Visit(context.constantValue());
			if (!expr.Type.CanPromoteTo(vrbl.Type))
				Error(context, $"The value '{expr.Type}' cannot be cast to type '{expr.Type}'.");
			if (vrbl.IsSpecialized)
				Info._specializations.Add(new SpecConstant(vrbl.Name, vrbl.Type, vrbl.ConstantIndex.Value));
			GLSL.EmitConstant(vrbl, expr);
			return null;
		}

		public override ExprResult VisitConstantValue([NotNull] SSLParser.ConstantValueContext context)
		{
			if (context.type() != null)
			{
				var ntype = ReflectionUtils.TranslateTypeContext(context.type());
				if (!ntype.HasValue)
					Error(context, $"Did not understand the type '{context.Start.Text}' in type construction.");

				var args = context.valueLiteral().Select(e => Visit(e)).ToList();
				if (!FunctionCallUtils.CanConstructType(ntype.Value, args, out var error))
					Error(context, error);

				return new ExprResult(ntype.Value, 0, $"{context.type().Start.Text}( {String.Join(", ", args.Select(a => a.RefText))} )");
			}
			else
				return Visit(context.Value);
		}

		public override ExprResult VisitStandardFunction([NotNull] SSLParser.StandardFunctionContext context)
		{
			var func = ScopeManager.AddFunction(context, this);
			_currFunc = func;

			GLSL.EmitCommentFunc($"Standard function: \"{func.Name}\"");
			GLSL.EmitFunctionHeader(func);
			GLSL.EmitOpenBlock();

			// Push the arguments
			ScopeManager.PushScope(ScopeType.Function, false);
			foreach (var par in func.Params)
			{
				if (!ScopeManager.TryAddParameter(par, out var error))
					Error(context, error);
			}

			// Visit the statements
			Visit(context.block());

			// Ensure assignments and an accurate return type
			ensureExitAssignments(context);
			if (_currFunc.ReturnType != ShaderType.Void && !ScopeManager.HasReturn())
				Error(context, $"The function '{_currFunc.Name}' cannot exit before returning a value.");

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
			ScopeManager.PushScope(ScopeType.Function, false);
			ScopeManager.AddBuiltins(stage);
			Info.Stages |= stage;
			_currStage = stage;
		}

		public override ExprResult VisitVertFunction([NotNull] SSLParser.VertFunctionContext context)
		{
			enterStageFunction(ShaderStages.Vertex);

			// Visit the statements
			Visit(context.block());

			ensureExitAssignments(context);
			exitFunction();
			return null;
		}

		public override ExprResult VisitFragFunction([NotNull] SSLParser.FragFunctionContext context)
		{
			enterStageFunction(ShaderStages.Fragment);

			// Visit the statements
			Visit(context.block());

			ensureExitAssignments(context);
			exitFunction();
			return null;
		}
		#endregion // Stage Functions

		#region Statements
		public override ExprResult VisitBlock([NotNull] SSLParser.BlockContext context)
		{
			RuleContext exitStmt = null;
			foreach (var stmt in context.statement())
			{
				// First statement after exiting the block, give warning about unreachable code and return
				if (exitStmt != null)
				{
					Warn(stmt, "Unreachable code detected, remaining code in block will be ignored.");
					return null;
				}

				var expr = Visit(stmt);
				if (expr != null && expr.Type != ShaderType.Void)
					Warn(stmt, "Return value of function call is discarded.");

				// Check for unreachable code
				exitStmt = stmt.controlFlowStatement();
			}
			return null;
		}

		public override ExprResult VisitStatement([NotNull] SSLParser.StatementContext context)
		{
			if (context.functionCall() != null) return Visit(context.functionCall());
			if (context.builtinFunctionCall() != null) return Visit(context.builtinFunctionCall());
			Visit(context.GetChild(0));
			return null;
		}

		public override ExprResult VisitVariableDeclaration([NotNull] SSLParser.VariableDeclarationContext context)
		{
			var vrbl = ScopeManager.AddLocal(context, this);
			GLSL.EmitDeclaration(vrbl);
			return null;
		}

		public override ExprResult VisitVariableDefinition([NotNull] SSLParser.VariableDefinitionContext context)
		{
			var vrbl = ScopeManager.AddLocal(context, this);
			if (context.arrayIndexer() != null) // We are creating a new array
			{
				if (context.arrayLiteral() != null)
				{
					_currArrayType = vrbl.Type;
					var alit = Visit(context.arrayLiteral()); // Also validates the array type
					_currArrayType = ShaderType.Void;
					if (vrbl.ArraySize != alit.ArraySize)
						Error(context.arrayLiteral(), $"Array size mismatch ({vrbl.ArraySize} != {alit.ArraySize}) in definition.");
					GLSL.EmitDefinition(vrbl, alit);
				}
				else
					Error(context.expression(), "An array type must be initialized with an array literal.");
			}
			else
			{
				if (context.arrayLiteral() != null)
					Error(context.arrayLiteral(), "A non-array type cannot be initialized with an array literal.");
				else
				{
					var exp = Visit(context.expression());
					if (!exp.Type.CanPromoteTo(vrbl.Type))
						Error(context.expression(), $"Cannot assign the type '{exp.Type}' to the type '{vrbl.Type}'.");
					GLSL.EmitDefinition(vrbl, exp);
				}
			}
			ScopeManager.AddAssignment(vrbl);
			return null;
		}

		public override ExprResult VisitAssignment([NotNull] SSLParser.AssignmentContext context)
		{
			var vrbl = findVariable(context, context.Name.Text, false, true);
			var actx = context.arrayIndexer();
			var swiz = context.SWIZZLE();
			var ltype = TypeUtils.ApplyLValueModifier(this, context.Name, vrbl, actx, swiz, out var arrIndex);
			if (vrbl.IsArray && !arrIndex.HasValue)
				Error(context, $"Cannot assign a value to array variable '{vrbl.Name}'.");

			var cpx = context.Op.Type != SSLParser.OP_ASSIGN;
			var expr = Visit(context.Value);

			if (cpx) // Complex assignment
			{
				var rtype = TypeUtils.CheckOperator(this, context.Op, ltype, expr.Type);
				if (rtype != ltype)
					Error(context, $"Cannot reassign complex operator result type '{rtype}' back to variable type '{ltype}'.");
			}
			else
			{
				if (!expr.Type.CanPromoteTo(ltype))
					Error(context.Value, $"The expression type '{expr.Type}' cannot be assigned to the variable type '{ltype}'.");
			}
			if (expr.ArraySize != vrbl.ArraySize)
				Error(context.Value, $"The expression has a mismatched array size with the assignment variable.");

			ScopeManager.AddAssignment(vrbl);
			GLSL.EmitAssignment(vrbl.GetOutputName(_currStage), arrIndex, swiz?.Symbol?.Text, context.Op.Text, expr);
			return null;
		}

		public override ExprResult VisitArrayLiteral([NotNull] SSLParser.ArrayLiteralContext context)
		{
			var exprs = context._Values.Select(e => Visit(e)).ToList();
			var bidx = exprs.FindIndex(e => !e.Type.CanPromoteTo(_currArrayType));
			if (bidx != -1)
				Error(context._Values[bidx], $"Array literal ({_currArrayType}) has incompatible type '{exprs[bidx].Type}' at element {bidx}.");

			return new ExprResult(_currArrayType, (uint)exprs.Count, $"{{ {String.Join(", ", exprs.Select(e => e.RefText))} }}");
		}

		public override ExprResult VisitIfStatement([NotNull] SSLParser.IfStatementContext context)
		{
			var cexpr = Visit(context.Cond);
			if (cexpr.Type != ShaderType.Bool)
				Error(context.Cond, "The condition in an 'if' statment must have type Bool.");

			GLSL.EmitIfStatement(cexpr);
			ScopeManager.PushScope(ScopeType.Conditional, false);
			GLSL.PushIndent();
			if (context.Block != null)
				Visit(context.Block);
			else
				Visit(context.Statement);
			GLSL.EmitCloseBlock();
			ScopeManager.PopScope();

			foreach (var elif in context._Elifs)
				Visit(elif);

			if (context.Else != null)
				Visit(context.Else);

			return null;
		}

		public override ExprResult VisitElifStatement([NotNull] SSLParser.ElifStatementContext context)
		{
			var cexpr = Visit(context.Cond);
			if (cexpr.Type != ShaderType.Bool)
				Error(context.Cond, "The condition in an 'elif' statment must have type Bool.");

			GLSL.EmitElifStatement(cexpr);
			ScopeManager.PushScope(ScopeType.Conditional, false);
			GLSL.PushIndent();
			if (context.Block != null)
				Visit(context.Block);
			else
				Visit(context.Statement);
			GLSL.EmitCloseBlock();
			ScopeManager.PopScope();

			return null;
		}

		public override ExprResult VisitElseStatement([NotNull] SSLParser.ElseStatementContext context)
		{
			GLSL.EmitElseStatement();
			ScopeManager.PushScope(ScopeType.Conditional, true);
			GLSL.PushIndent();
			if (context.Block != null)
				Visit(context.Block);
			else
				Visit(context.Statement);
			GLSL.EmitCloseBlock();
			ScopeManager.PopScope();

			return null;
		}

		public override ExprResult VisitForLoop([NotNull] SSLParser.ForLoopContext context)
		{
			ScopeManager.PushScope(ScopeType.Loop, false); // No propogation because for loops are not guarenteed to run

			var initText = (context.forLoopInit() != null) ? Visit(context.forLoopInit()).RefText : "";
			var cexpr = (context.Condition != null) ? Visit(context.Condition) : null;
			if ((cexpr?.Type ?? ShaderType.Bool) != ShaderType.Bool)
				Error(context.Condition, "For loop conditions must be of type Bool.");
			var updateText = (context.forLoopUpdate() != null) ? Visit(context.forLoopUpdate()).RefText : "";

			GLSL.EmitForLoopHeader(initText, cexpr, updateText);
			GLSL.EmitOpenBlock();

			if (context.block() != null) Visit(context.block());
			else Visit(context.statement());

			GLSL.EmitCloseBlock();
			ScopeManager.PopScope();

			return null;
		}

		public override ExprResult VisitForLoopInit([NotNull] SSLParser.ForLoopInitContext context)
		{
			if (context.variableDefinition() != null)
			{
				var vdc = context.variableDefinition();
				if (vdc.arrayIndexer() != null || vdc.arrayLiteral() != null)
					Error(vdc, $"Cannot declare an array in a for loop initializer ('{vdc.Name.Text}').");
				if (vdc.KW_CONST() != null)
					Error(vdc, $"Cannot declare a constant in a for loop initializer ('{vdc.Name.Text}').");
				var vrbl = ScopeManager.AddLocal(vdc, this);
				if (!(vrbl.Type.IsScalarType() || vrbl.Type.IsVectorType()))
					Error(vdc, $"Can only declare scalar and vector types in a for loop initializer ('{vrbl.Name}').");

				var expr = Visit(vdc.expression());
				if (!expr.Type.CanPromoteTo(vrbl.Type))
					Error(vdc.expression(), $"The expression type '{expr.Type}' cannot be assigned to the variable type '{vrbl.Type}'.");

				return new ExprResult(ShaderType.Void, null, $"{vrbl.GetGLSLDecl(null)} = {expr.RefText}");
			}
			else
			{
				StringBuilder sb = new StringBuilder(128);
				uint count = 0;

				foreach (var ac in context._Assigns)
				{
					if (count > 0)
						sb.Append(", ");

					var vrbl = findVariable(ac, ac.Name.Text, true, true);
					var ltype = TypeUtils.ApplyLValueModifier(this, ac.Name, vrbl, ac.arrayIndexer(), ac.SWIZZLE(), out var arrIndex);
					if (vrbl.IsArray && !arrIndex.HasValue)
						Error(ac, $"Cannot assign to an array variable ('{ac.Name.Text}').");

					var cpx = ac.Op.Type != SSLParser.OP_ASSIGN;
					if (cpx)
						Error(ac.Op, $"Complex assignments are not allowed in for loop initializers ('{ac.Op.Text}').");
					var expr = Visit(ac.Value);
					if (!expr.Type.CanPromoteTo(ltype))
						Error(ac.Value, $"The expression type '{expr.Type}' cannot be assigned to the variable type '{ltype}'.");

					ScopeManager.AddAssignment(vrbl);
					sb.Append($"{vrbl.Name} = {expr.RefText}");
					++count;
				}

				return new ExprResult(ShaderType.Void, null, sb.ToString());
			}
		}

		public override ExprResult VisitForLoopUpdate([NotNull] SSLParser.ForLoopUpdateContext context)
		{
			StringBuilder sb = new StringBuilder(128);

			foreach (var ipt in context.children)
			{
				if (ipt is SSLParser.AssignmentContext)
				{
					var ac = ipt as SSLParser.AssignmentContext;
					var vrbl = findVariable(ac, ac.Name.Text, true, true);
					var ltype = TypeUtils.ApplyLValueModifier(this, ac.Name, vrbl, ac.arrayIndexer(), ac.SWIZZLE(), out var arrIndex);
					if (vrbl.IsArray && !arrIndex.HasValue)
						Error(ac, $"Cannot assign to an array variable ('{ac.Name.Text}').");

					var cpx = ac.Op.Type != SSLParser.OP_ASSIGN;
					var expr = Visit(ac.Value);
					if (cpx)
					{
						var rtype = TypeUtils.CheckOperator(this, ac.Op, ltype, expr.Type);
						if (rtype != ltype)
							Error(context, $"Cannot reassign complex operator result type '{rtype}' back to variable type '{ltype}'.");
					}
					else
					{
						if (!expr.Type.CanPromoteTo(ltype))
							Error(ac.Value, $"The expression type '{expr.Type}' cannot be assigned to the variable type '{ltype}'.");
					}

					ScopeManager.AddAssignment(vrbl);
					sb.Append($"{vrbl.Name} {ac.Op.Text} {expr.RefText}");
				}
				else if (ipt is SSLParser.ExpressionContext)
				{
					var post = ipt as SSLParser.UnOpPostfixContext;
					var pre = ipt as SSLParser.UnOpPrefixContext;
					if (post == null && pre == null)
						Error(ipt as SSLParser.ExpressionContext, "Only post-fix and pre-fix expressions are allowed in for loop updates.");
					var optxt = post?.Op.Text ?? pre.Op.Text;
					var vrbl = findVariable(ipt as SSLParser.ExpressionContext, post?.Expr.Text ?? pre.Expr.Text, true, true);
					if (vrbl.Type != ShaderType.Int && vrbl.Type != ShaderType.UInt)
						Error(context, $"Pre-fix/post-fix operators cannot be applied to the type '{vrbl.Type}'.");
					if (vrbl.IsArray)
						Error(context, "Pre-fix/post-fix operators cannot be applied to an array.");

					ScopeManager.AddAssignment(vrbl);
					if (post != null) sb.Append($"{vrbl.Name}{optxt}");
					else sb.Append($"{optxt}{vrbl.Name}");
				}
				else if (ipt is ITerminalNode && (ipt as ITerminalNode).GetText() == ",")
					sb.Append(", ");
			}

			return new ExprResult(ShaderType.Void, null, sb.ToString());
		}

		public override ExprResult VisitWhileLoop([NotNull] SSLParser.WhileLoopContext context)
		{
			var cexpr = Visit(context.Condition);
			if (cexpr.Type != ShaderType.Bool)
				Error(context.Condition, "The condition in a while loop must have type Bool.");

			ScopeManager.PushScope(ScopeType.Loop, false); // No propogation because while loops are not guarenteed to run
			GLSL.EmitWhileLoopHeader(cexpr);
			GLSL.EmitOpenBlock();

			if (context.block() != null) Visit(context.block());
			else Visit(context.statement());

			GLSL.EmitCloseBlock();
			ScopeManager.PopScope();

			return null;
		}

		public override ExprResult VisitDoLoop([NotNull] SSLParser.DoLoopContext context)
		{
			var cexpr = Visit(context.Condition);
			if (cexpr.Type != ShaderType.Bool)
				Error(context.Condition, "The condition in a do/while loop must have type Bool.");

			ScopeManager.PushScope(ScopeType.Loop, true); // Yes on propogation because do loops are guarenteed to run at least once
			GLSL.EmitDoLoopHeader();
			GLSL.EmitOpenBlock();

			Visit(context.block());

			GLSL.EmitDoLoopFooter(cexpr);
			ScopeManager.PopScope();

			return null;
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
						Error(context, $"{((_currFunc != null) ? $"The function '{_currFunc.Name}' does" : "Shader stage functions do")} not allow return values.");
				}
				else
				{
					if (rtype.IsVoid())
						Error(context, $"The function '{_currFunc.Name}' expects a return value of type '{etype}'.");
					else if (!rtype.CanPromoteTo(etype))
						Error(context, $"The given return type '{rtype}' cannot be cast to the expected type '{etype}'.");
				}

				ScopeManager.AddReturn();
				ensureExitAssignments(context);
				GLSL.EmitReturn(rexpr);
			}
			else if (context.KW_DISCARD() != null) // discard statement
			{
				if (_currStage != ShaderStages.Fragment)
					Error(context, "The 'discard' keyword is not allowed outside of the fragment stage.");
				GLSL.EmitDiscard();
			}
			else // 'break' and 'continue' statements (have the same effect)
			{
				var isBreak = context.KW_BREAK() != null;
				if (!ScopeManager.InLoopScope())
					Error(context, $"The '{(isBreak ? "break" : "continue")}' statement is not allowed outside of a looping construct.");
				if (isBreak) GLSL.EmitBreak();
				else GLSL.EmitContinue();
			}

			return null;
		}
		#endregion // Statements

		#region Expressions
		public override ExprResult VisitUnOpPostfix([NotNull] SSLParser.UnOpPostfixContext context)
		{
			var vrbl = findVariable(context, context.IDENTIFIER().Symbol.Text, true, true);
			if (vrbl.Type != ShaderType.Int && vrbl.Type != ShaderType.UInt)
				Error(context, $"The postfix operator '{context.Op.Text}' cannot be applied to the type '{vrbl.Type}'.");
			if (vrbl.IsArray)
				Error(context, $"Cannot apply the postfix operator '{context.Op.Text}' to an array.");
			ScopeManager.AddAssignment(vrbl);
			return new ExprResult(vrbl.Type, null, "(" + vrbl.GetOutputName(_currStage) + context.Op.Text + ")");
		}

		public override ExprResult VisitUnOpPrefix([NotNull] SSLParser.UnOpPrefixContext context)
		{
			var vrbl = findVariable(context, context.IDENTIFIER().Symbol.Text, true, true);
			if (vrbl.Type != ShaderType.Int && vrbl.Type != ShaderType.UInt)
				Error(context, $"The prefix operator '{context.Op.Text}' cannot be applied to the type '{vrbl.Type}'.");
			if (vrbl.IsArray)
				Error(context, $"Cannot apply the prefix operator '{context.Op.Text}' to an array.");
			ScopeManager.AddAssignment(vrbl);
			return new ExprResult(vrbl.Type, null, "(" + context.Op.Text + vrbl.GetOutputName(_currStage) + ")");
		}

		public override ExprResult VisitUnOpFactor([NotNull] SSLParser.UnOpFactorContext context)
		{
			var expr = Visit(context.Expr);
			if (!expr.Type.IsValueType() || expr.Type.IsMatrixType() || expr.Type.GetComponentType() == ShaderType.Bool)
				Error(context, $"The unary operator '{context.Op.Text}' cannot be applied to the type '{expr.Type}'.");
			if (expr.IsArray)
				Error(context, $"Cannot apply the unary operator '{context.Op.Text}' to an array.");
			return new ExprResult(expr.Type, null, "(" + context.Op.Text + expr.RefText + ")");
		}

		public override ExprResult VisitUnOpNegate([NotNull] SSLParser.UnOpNegateContext context)
		{
			var expr = Visit(context.Expr);
			if (expr.IsArray)
				Error(context, $"Cannot apply the negation operator '{context.Op.Text}' to an array.");
			if (context.Op.Text == "!")
			{
				if (expr.Type != ShaderType.Bool)
					Error(context, "The negation operator '!' can only be applied to scalar booleans.");
				return new ExprResult(ShaderType.Bool, null, "(!" + expr.RefText + ")");
			}
			else
			{
				if (expr.Type != ShaderType.Int && expr.Type != ShaderType.UInt)
					Error(context, "The negation operator '~' can only be applied to integer types.");
				return new ExprResult(expr.Type, null, "(~" + expr.RefText + ")");
			}
		}

		// Base checker for binary operations
		private ExprResult visitBinOp(SSLParser.ExpressionContext left, SSLParser.ExpressionContext right, IToken op)
		{
			var lexpr = Visit(left);
			var rexpr = Visit(right);
			if (lexpr.IsArray || rexpr.IsArray)
				Error(lexpr.IsArray ? left : right, $"Cannot apply binary operator '{op.Text}' to an array type.");
			var rtype = TypeUtils.CheckOperator(this, op, lexpr.Type, rexpr.Type);
			return new ExprResult(rtype, null, $"({lexpr.RefText} " + op.Text + $" {rexpr.RefText})");
		}

		public override ExprResult VisitBinOpMulDivMod([NotNull] SSLParser.BinOpMulDivModContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpAddSub([NotNull] SSLParser.BinOpAddSubContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpBitShift([NotNull] SSLParser.BinOpBitShiftContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpRelational([NotNull] SSLParser.BinOpRelationalContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpEquality([NotNull] SSLParser.BinOpEqualityContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpBitLogic([NotNull] SSLParser.BinOpBitLogicContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitBinOpBoolLogic([NotNull] SSLParser.BinOpBoolLogicContext context) =>
			visitBinOp(context.Left, context.Right, context.Op);

		public override ExprResult VisitSelectionExpr([NotNull] SSLParser.SelectionExprContext context)
		{
			var cexpr = Visit(context.Cond);
			if (cexpr.Type != ShaderType.Bool)
				Error(context.Cond, $"The selection operation condition must be a scalar boolean type ({cexpr.Type}).");

			var texpr = Visit(context.TVal);
			var fexpr = Visit(context.FVal);
			if (!fexpr.Type.CanPromoteTo(texpr.Type) || texpr.ArraySize != fexpr.ArraySize)
				Error(context.FVal, $"The false selection expression type ({fexpr.Type}) must match the true type ({texpr.Type}).");

			var ftext = (texpr.Type != fexpr.Type) ? $"{texpr.Type.ToGLSLKeyword()}({fexpr.RefText})" : fexpr.RefText;
			return new ExprResult(texpr.Type, texpr.ArraySize, $"(({cexpr.RefText}) ? ({texpr.RefText}) : ({ftext}))");
		}
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
			var ntype = ReflectionUtils.TranslateTypeContext(tctx.Type);
			if (!ntype.HasValue)
				Error(tctx, $"Did not understand the type '{tctx.Type.Start.Text}' in type construction.");

			var args = tctx._Args.Select(e => Visit(e)).ToList();
			if (!FunctionCallUtils.CanConstructType(ntype.Value, args, out var error))
				Error(tctx, error);

			var ssa = ScopeManager.AddSSALocal(ntype.Value, this);
			var expr = new ExprResult(ssa, $"{tctx.Type.Start.Text}( {String.Join(", ", args.Select(a => a.RefText))} )");
			GLSL.EmitDefinition(expr.SSA, expr);
			return TypeUtils.ApplyModifiers(this, expr, context.arrayIndexer(), context.SWIZZLE());
		}

		public override ExprResult VisitBuiltinCallAtom([NotNull] SSLParser.BuiltinCallAtomContext context)
		{
			var cexpr = Visit(context.builtinFunctionCall());
			return TypeUtils.ApplyModifiers(this, cexpr, null, context.SWIZZLE());
		}

		public override ExprResult VisitBuiltinCall1([NotNull] SSLParser.BuiltinCall1Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			if (rtype != ShaderType.Void)
			{
				var ssa = ScopeManager.AddSSALocal(rtype, this);
				var ret = new ExprResult(ssa, $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})");
				GLSL.EmitDefinition(ret.SSA, ret);
				return ret;
			}
			else
			{
				GLSL.EmitCall(GLSLBuilder.GetBuiltinFuncName(fname, ftype), aexpr);
				return new ExprResult(ShaderType.Void, null, "");
			}
		}

		public override ExprResult VisitBuiltinCall2([NotNull] SSLParser.BuiltinCall2Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1), Visit(context.A2) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			if (rtype != ShaderType.Void)
			{
				var ssa = ScopeManager.AddSSALocal(rtype, this);
				var ssatext = (fname == "imageLoad") // Find a more modular way to do this in the future
					? $"imageLoad({String.Join(", ", aexpr.Select(ae => ae.RefText))}).{ReflectionUtils.GetSwizzle(aexpr[0].ImageFormat.Value.GetTexelType(), 1)}"
					: $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})";
				var ret = new ExprResult(ssa, ssatext);
				GLSL.EmitDefinition(ret.SSA, ret);
				return ret; 
			}
			else
			{
				GLSL.EmitCall(GLSLBuilder.GetBuiltinFuncName(fname, ftype), aexpr);
				return new ExprResult(ShaderType.Void, null, "");
			}
		}

		public override ExprResult VisitBuiltinCall3([NotNull] SSLParser.BuiltinCall3Context context)
		{
			var fname = context.FName.Start.Text;
			var ftype = context.FName.Start.Type;
			var aexpr = new ExprResult[] { Visit(context.A1), Visit(context.A2), Visit(context.A3) };
			var rtype = FunctionCallUtils.CheckBuiltinCall(this, context.Start, fname, ftype, aexpr);
			if (rtype != ShaderType.Void)
			{
				var ssa = ScopeManager.AddSSALocal(rtype, this);
				var ret = new ExprResult(ssa, $"{GLSLBuilder.GetBuiltinFuncName(fname, ftype)}({String.Join(", ", aexpr.Select(ae => ae.RefText))})");
				GLSL.EmitDefinition(ret.SSA, ret);
				return ret;
			}
			else
			{
				if (fname == "imageStore")
				{
					var rem = 4 - aexpr[2].Type.GetComponentCount();
					var strtxt = $"vec4({aexpr[2].RefText}" + Enumerable.Repeat(", 0", (int)rem).Aggregate((s1, s2) => s1 + s2) + ")";
					var ssa = ScopeManager.AddSSALocal(ShaderType.Float4, this);
					aexpr[2] = new ExprResult(ssa, strtxt);
					GLSL.EmitDefinition(ssa, aexpr[2]);
				}
				GLSL.EmitCall(GLSLBuilder.GetBuiltinFuncName(fname, ftype), aexpr);
				return new ExprResult(ShaderType.Void, null, "");
			}
		}

		public override ExprResult VisitFunctionCallAtom([NotNull] SSLParser.FunctionCallAtomContext context)
		{
			var fc = context.functionCall();
			var func = ScopeManager.FindFunction(fc.FName.Text);
			if (func == null)
				Error(fc.FName, $"There is no user defined function with the name '{fc.FName.Text}'.");

			if (func.Params.Length != fc._Args.Count)
				Error(context, $"Function ('{func.Name}') call expected {func.Params.Length} arguments, but {fc._Args.Count} were given.");
			var ptypes = func.Params;
			var avars = fc._Args.Select(arg => Visit(arg)).ToList();
			for (int i = 0; i < ptypes.Length; ++i)
			{
				// Checks for reference-passed values
				if (ptypes[i].Access != StandardFunction.Access.In)
				{
					if (avars[i].LValue == null)
						Error(fc._Args[i], $"Must pass a modifiable variable to 'out' or 'inout' function (argument {i+1}).");
					if (avars[i].LValue.Constant)
						Error(fc._Args[i], $"Cannot pass a constant variable as a reference to a function (argument {i+1}).");
				}

				// Type checking
				if (!avars[i].Type.CanPromoteTo(ptypes[i].Type))
					Error(fc._Args[i], $"Function ('{func.Name}') argument {i+1} expected '{ptypes[i].Type}' type, but got non-castable type '{avars[i].Type}'.");
			}

			var ssa = ScopeManager.AddSSALocal(func.ReturnType, this);
			var ret = new ExprResult(ssa, $"{func.OutputName}({String.Join(", ", avars.Select(arg => arg.RefText))})");
			GLSL.EmitDefinition(ret.SSA, ret);
			return TypeUtils.ApplyModifiers(this, ret, null, context.SWIZZLE());
		}

		public override ExprResult VisitLiteralAtom([NotNull] SSLParser.LiteralAtomContext context)
		{
			return Visit(context.valueLiteral());
		}

		public override ExprResult VisitValueLiteral([NotNull] SSLParser.ValueLiteralContext context)
		{
			if (context.BOOLEAN_LITERAL() != null)
				return new ExprResult(ShaderType.Bool, null, context.BOOLEAN_LITERAL().Symbol.Text);
			else if (context.FLOAT_LITERAL() != null)
				return new ExprResult(ShaderType.Float, null, context.FLOAT_LITERAL().Symbol.Text, true);
			else
			{
				var ltxt = context.INTEGER_LITERAL().Symbol.Text;
				var val = ParseIntegerLiteral(ltxt, out bool isus, out var error);
				if (!val.HasValue)
					Error(context.INTEGER_LITERAL().Symbol, error);
				return new ExprResult(isus ? ShaderType.UInt : ShaderType.Int, null, val.Value.ToString(), true);
			}
		}

		public override ExprResult VisitVariableAtom([NotNull] SSLParser.VariableAtomContext context)
		{
			var vrbl = findVariable(context, context.IDENTIFIER().Symbol.Text, true, false);
			var expr = new ExprResult(vrbl.Type, vrbl.IsArray ? vrbl.ArraySize : (uint?)null, vrbl.GetOutputName(_currStage));
			expr.LValue = vrbl;
			return TypeUtils.ApplyModifiers(this, expr, context.arrayIndexer(), context.SWIZZLE());
		}
		#endregion // Atoms
	}
}
