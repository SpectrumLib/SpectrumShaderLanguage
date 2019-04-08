using System;
using System.Collections.Generic;
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

		#region Globals
		public bool TryAddAttribute(SSLParser.VariableDeclarationContext ctx, out Variable v, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.Attribute, out v, out error))
				return false;

			var pre = FindGlobal(v.Name);
			if (pre != null)
			{
				error = $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.";
				return false;
			}

			_attributes.Add(v.Name, v);
			return true;
		}

		public bool TryAddOutput(SSLParser.VariableDeclarationContext ctx, out Variable v, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.FragmentOutput, out v, out error))
				return false;

			var pre = FindGlobal(v.Name);
			if (pre != null)
			{
				error = $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.";
				return false;
			}

			_outputs.Add(v.Name, v);
			return true;
		}

		public bool TryAddUniform(SSLParser.VariableDeclarationContext ctx, out Variable v, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.Uniform, out v, out error))
				return false;

			var pre = FindGlobal(v.Name);
			if (pre != null)
			{
				error = $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.";
				return false;
			}

			_uniforms.Add(v.Name, v);
			return true;
		}

		public bool TryAddInternal(SSLParser.VariableDeclarationContext ctx, out Variable v, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.Internal, out v, out error))
				return false;

			var pre = FindGlobal(v.Name);
			if (pre != null)
			{
				error = $"A variable with the name '{v.Name}' already exists in the global {v.Scope} context.";
				return false;
			}

			_internals.Add(v.Name, v);
			return true;
		}

		public bool TryAddFunction(SSLParser.StandardFunctionContext ctx, out StandardFunction func, out string error)
		{
			if (!StandardFunction.TryFromContext(ctx, out func, out error))
				return false;

			var pre = FindFunction(func.Name);
			if (pre != null)
			{
				error = $"A function with the name '{func.Name}' already exists in the shader.";
				return false;
			}

			_functions.Add(func.Name, func);
			return true;
		}
		#endregion // Globals

		#region Functions
		public void PushScope() => _scopes.Push(new Scope(this));
		public void PopScope() => _scopes.Pop();

		public bool TryAddParameter(StandardFunction.Param p, out string error) =>
			_scopes.Peek().TryAddParam(p, out error);

		public bool TryAddLocal(SSLParser.VariableDeclarationContext ctx, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.Local, out var v, out error))
				return false;
			return _scopes.Peek().TryAddVariable(v, out error);
		}

		public bool TryAddLocal(SSLParser.VariableDefinitionContext ctx, out string error)
		{
			if (!Variable.TryFromContext(ctx, ScopeType.Local, out var v, out error))
				return false;
			return _scopes.Peek().TryAddVariable(v, out error);
		}

		public Variable TryAddSSALocal(ShaderType type, uint arrSize = 0)
		{
			var v = new Variable(type, $"_r{_ssaIndex++}", ScopeType.Local, true, arrSize);
			if (!_scopes.Peek().TryAddVariable(v, out var error))
				return null;
			return v;
		}

		public void AddBuiltins(ShaderStages stage)
		{
			var scope = _scopes.Peek();
			if (stage == ShaderStages.Vertex)
			{
				scope.AddBuiltin(new Variable(ShaderType.Int, "$VertexIndex", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Int, "$InstanceIndex", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float4, "$Position", ScopeType.Builtin, false, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float, "$PointSize", ScopeType.Builtin, false, 0));
			}
			else
			{
				scope.AddBuiltin(new Variable(ShaderType.Float4, "$FragCoord", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Bool, "$FrontFacing", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float2, "$PointCoord", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Int, "$SampleId", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float2, "$SamplePosition", ScopeType.Builtin, true, 0));
				scope.AddBuiltin(new Variable(ShaderType.Float, "$FragDepth", ScopeType.Builtin, false, 0));
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

		public readonly ScopeManager Manager;
		#endregion // Fields

		public Scope(ScopeManager m)
		{
			Manager = m;
			_params = new Dictionary<string, (Variable, StandardFunction.Param)>();
			_locals = new Dictionary<string, Variable>();
			_builtins = new Dictionary<string, Variable>();
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

			var vrbl = new Variable(p.Type, p.Name, ScopeType.Argument, p.Access == StandardFunction.Access.In, 0);
			_params.Add(p.Name, (vrbl, p));

			error = null;
			return true;
		}
	}
}
