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
		// The vertex attributes
		private readonly Dictionary<string, Variable> _attributes;
		public IReadOnlyDictionary<string, Variable> Attributes => _attributes;

		// The fragment shader outputs
		private readonly Dictionary<string, Variable> _outputs;
		public IReadOnlyDictionary<string, Variable> Outputs => _outputs;

		// The uniforms
		private readonly Dictionary<string, Variable> _uniforms;
		public IReadOnlyDictionary<string, Variable> Uniforms => _uniforms;
		#endregion // Fields

		public ScopeManager()
		{
			_attributes = new Dictionary<string, Variable>();
			_outputs = new Dictionary<string, Variable>();
			_uniforms = new Dictionary<string, Variable>();
		}

		// Will search all of the global scopes for a variable with the matching name
		public Variable FindGlobal(string name) =>
			_attributes.ContainsKey(name) ? _attributes[name] :
			_outputs.ContainsKey(name) ? _outputs[name] :
			_uniforms.ContainsKey(name) ? _uniforms[name] : null;

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
		#endregion // Globals
	}
}
