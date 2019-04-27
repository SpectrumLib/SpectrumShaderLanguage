using System;
using System.Collections.Generic;
using System.Linq;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Manages scoping for variables in a visitor, as well as tracking the global variables
	//   and their access
	internal class ScopeManager
	{
		#region Fields
		private readonly Dictionary<string, Variable> _attributes;
		public IReadOnlyDictionary<string, Variable> Attributes => _attributes;
		
		private readonly Dictionary<string, Variable> _outputs;
		public IReadOnlyDictionary<string, Variable> Outputs => _outputs;

		private readonly Dictionary<string, Variable> _uniforms;
		public IReadOnlyDictionary<string, Variable> Uniforms => _uniforms;

		private readonly Dictionary<string, Variable> _internals;
		public IReadOnlyDictionary<string, Variable> Internals => _internals;

		private readonly Dictionary<string, StandardFunction> _functions;
		public IReadOnlyDictionary<string, StandardFunction> Functions => _functions;

		private readonly Stack<Scope> _scopes;
		public IReadOnlyCollection<Scope> ScopeStack => _scopes;

		// Index for SSA locals
		private uint _ssaIndex = 0;
		#endregion // Fields

		public ScopeManager()
		{
			_attributes = new Dictionary<string, Variable>();
			_outputs = new Dictionary<string, Variable>();
			_uniforms = new Dictionary<string, Variable>();
			_internals = new Dictionary<string, Variable>();
			_functions = new Dictionary<string, StandardFunction>();
			_scopes = new Stack<Scope>();
		}

		// Will search all of the global scopes for a variable with the matching name
		public Variable FindGlobal(string name) =>
			_attributes.ContainsKey(name) ? _attributes[name] :
			_outputs.ContainsKey(name) ? _outputs[name] :
			_uniforms.ContainsKey(name) ? _uniforms[name] :
			_internals.ContainsKey(name) ? _internals[name] : null;

		// Attempts to get a standard function
		public StandardFunction FindFunction(string name) => _functions.ContainsKey(name) ? _functions[name] : null;

		// Searches all scopes in the stack
		public Variable FindLocal(string name)
		{
			foreach (var scope in _scopes)
			{
				var v = scope.FindVariable(name);
				if (v != null) return v;
			}
			return null;
		}

		// Searches all local scopes and the global namespace for any name match
		public Variable FindAny(string name) => FindLocal(name) ?? FindGlobal(name);

		// Checks if the visitor is currently in a scope that has a parent (at any depth) that is a looping scope,
		//    used to check if a 'break' or 'continue' statement is valid.
		public bool InLoopScope() => _scopes.Any(sc => sc.Type == ScopeType.Loop);

		// Checks if the variable was assigned to in the current scope or in a parent scope, used to check if required
		//    built-ins and `out`s are written to before exiting from a function.
		public bool IsAssigned(Variable vrbl) => _scopes.Any(sc => sc.IsAssigned(vrbl));

		// Adds an assignment flag to the variable in the current scope
		public void AddAssignment(Variable vrbl) => _scopes.Peek().AddAssignment(vrbl);

		// Adds a return statement flag to the current scope
		public void AddReturn() => _scopes.Peek().AddReturn();
		
		// Checks the current scope to see if it has a return statement
		public bool HasReturn() => _scopes.Peek().HasReturn;

		#region Globals
		public Variable AddAttribute(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.Attribute);

			var pre = FindGlobal(v.Name);
			if (pre != null)
				vis._THROW(ctx, $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.");

			_attributes.Add(v.Name, v);
			return v;
		}

		public Variable AddOutput(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.FragmentOutput);

			var pre = FindGlobal(v.Name);
			if (pre != null)
				vis._THROW(ctx, $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.");

			_outputs.Add(v.Name, v);
			return v;
		}

		public Variable AddUniform(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.Uniform);

			var pre = FindGlobal(v.Name);
			if (pre != null)
				vis._THROW(ctx, $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.");

			_uniforms.Add(v.Name, v);
			return v;
		}

		public Variable AddUniform(SSLParser.UniformVariableContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis);

			var pre = FindGlobal(v.Name);
			if (pre != null)
				vis._THROW(ctx, $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.");

			_uniforms.Add(v.Name, v);
			return v;
		}

		public Variable AddInternal(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.Internal);

			var pre = FindGlobal(v.Name);
			if (pre != null)
				vis._THROW(ctx, $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.");

			_internals.Add(v.Name, v);
			return v;
		}

		public StandardFunction AddFunction(SSLParser.StandardFunctionContext ctx, SSLVisitor vis)
		{
			var func = StandardFunction.FromContext(ctx, vis);

			var pre = FindFunction(func.Name);
			if (pre != null)
				vis._THROW(ctx, $"A function with the name '{func.Name}' already exists in the shader.");

			_functions.Add(func.Name, func);
			return func;
		}
		#endregion // Globals

		#region Functions
		public void PushScope(ScopeType type, bool prop)
		{
			var parent = _scopes.Count > 0 ? _scopes.Peek() : null;
			_scopes.Push(new Scope(this, parent, type, prop));
		}
		public void PopScope() => _scopes.Pop();

		public bool TryAddParameter(StandardFunction.Param p, out string error) =>
			_scopes.Peek().TryAddParam(p, out error);

		public Variable AddLocal(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.Local);
			if (!_scopes.Peek().TryAddVariable(v, out var error))
				vis._THROW(ctx, error);
			return v;
		}

		public Variable AddLocal(SSLParser.VariableDefinitionContext ctx, SSLVisitor vis)
		{
			var v = Variable.FromContext(ctx, vis, VariableScope.Local);
			if (!_scopes.Peek().TryAddVariable(v, out var error))
				vis._THROW(ctx, error);
			return v;
		}

		public Variable AddSSALocal(ShaderType type, SSLVisitor vis, uint? arrSize = null)
		{
			var v = new Variable(type, $"_r{_ssaIndex++}", VariableScope.Local, true, arrSize);
			_scopes.Peek().TryAddVariable(v, out var error);
			return v;
		}

		public void AddBuiltins(ShaderStages stage)
		{
			var scope = _scopes.Peek();
			if (stage == ShaderStages.Vertex)
			{
				scope.AddBuiltin(new Variable(ShaderType.Int, "$VertexIndex", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Int, "$InstanceIndex", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float4, "$Position", VariableScope.Builtin, false, 0, false));
				scope.AddBuiltin(new Variable(ShaderType.Float, "$PointSize", VariableScope.Builtin, false, 0));
			}
			else
			{
				scope.AddBuiltin(new Variable(ShaderType.Float4, "$FragCoord", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Bool, "$FrontFacing", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float2, "$PointCoord", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Int, "$SampleId", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float2, "$SamplePosition", VariableScope.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float, "$FragDepth", VariableScope.Builtin, false, 0, false));
			}
		}
		#endregion // Functions
	}

	// A scope frame, which can be pushed and popped to manage variable scopes
	internal class Scope
	{
		#region Fields
		private readonly Dictionary<string, (Variable, StandardFunction.Param)> _params;
		public IReadOnlyDictionary<string, (Variable V, StandardFunction.Param P)> Params => _params;

		private readonly Dictionary<string, Variable> _locals;
		public IReadOnlyDictionary<string, Variable> Locals => _locals;

		private readonly Dictionary<string, Variable> _builtins;
		public IReadOnlyDictionary<string, Variable> BuiltIns => _builtins;

		private readonly List<string> _assignedVars;
		public IReadOnlyList<string> AssignedVars => _assignedVars;

		public readonly ScopeManager Manager;
		public readonly ScopeType Type;
		public readonly Scope Parent;

		// Some scopes are special in that their assignments and returns propogate to the parent scope
		public readonly bool Propogate;

		// If this scope has a return statement
		public bool HasReturn { get; private set; }
		#endregion // Fields

		public Scope(ScopeManager m, Scope parent, ScopeType type, bool prop)
		{
			Manager = m;
			Type = type;
			Parent = parent;
			_params = new Dictionary<string, (Variable, StandardFunction.Param)>();
			_locals = new Dictionary<string, Variable>();
			_builtins = new Dictionary<string, Variable>();
			_assignedVars = new List<string>();
			Propogate = prop;
			HasReturn = false;
		}

		public Variable FindVariable(string name) =>
			_builtins.ContainsKey(name) ? _builtins[name] :
			_locals.ContainsKey(name) ? _locals[name] :
			_params.ContainsKey(name) ? _params[name].Item1 : null;

		public void AddBuiltin(Variable v) => _builtins.Add(v.Name, v);

		public bool TryAddVariable(Variable v, out string error)
		{
			var pre = Manager.FindAny(v.Name);
			if (pre != null)
			{
				error = $"A variable with the name '{v.Name}' already exists.";
				return false;
			}
			_locals.Add(v.Name, v);
			error = null;
			return true;
		}

		public bool TryAddParam(StandardFunction.Param p, out string error)
		{
			var pre = Manager.FindGlobal(p.Name);
			if (pre != null) // Will probably get caught when the param is being created, but just to be sure
			{
				error = $"A variable with the name '{p.Name}' already exists in the global scope.";
				return false;
			}

			var vrbl = new Variable(p.Type, p.Name, VariableScope.Argument, p.Access == StandardFunction.Access.In, 0, p.Access != StandardFunction.Access.Out);
			_params.Add(p.Name, (vrbl, p));

			error = null;
			return true;
		}

		public void AddAssignment(Variable vrbl)
		{
			if (!_assignedVars.Contains(vrbl.Name))
				_assignedVars.Add(vrbl.Name);
			if (Propogate)
				Parent?.AddAssignment(vrbl);
		}

		public bool IsAssigned(Variable vrbl) => _assignedVars.Contains(vrbl.Name);

		public void AddReturn()
		{
			HasReturn = true;
			if (Propogate)
				Parent?.AddReturn();
		}
	}

	// Used to track scope types for checking control flow statements
	internal enum ScopeType
	{
		Function,		// The top-level scope for a function
		Conditional,	// The the scope for a conditional statement ('if', and soon 'switch')
		Loop			// Looping scope (for, while, do)
	}
}
