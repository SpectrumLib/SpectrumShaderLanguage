using System;
using SSLang.Generated;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a named variable object in a shader program.
	/// </summary>
	public sealed class Variable
	{
		#region Fields
		/// <summary>
		/// The name of the variable.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The type of the variable.
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		/// The type of the scope that the variable appears in.
		/// </summary>
		public readonly ScopeType Scope;
		/// <summary>
		/// If the variable is constant, and cannot have its value reassigned.
		/// </summary>
		public readonly bool Constant;
		/// <summary>
		/// A set of flags representing the stages that read from this variable. Only used for some scope types.
		/// </summary>
		public ShaderStages ReadStages { get; internal set; } = ShaderStages.None;
		/// <summary>
		/// A set of flags representing the stages that write to this variable. Only used for some scope types.
		/// </summary>
		public ShaderStages WriteStages { get; internal set; } = ShaderStages.None;
		/// <summary>
		/// The size of the array, if the variable is an array of values. Will be zero if it is not an array.
		/// </summary>
		public readonly uint ArraySize;

		/// <summary>
		/// Gets if the variable is an array of values.
		/// </summary>
		public bool IsArray => (ArraySize > 0);

		/// <summary>
		/// Gets if this variable is a uniform in the shader program.
		/// </summary>
		public bool IsUniform => (Scope == ScopeType.Uniform);
		/// <summary>
		/// Gets if this variable is a vertex attribute in the shader program.
		/// </summary>
		public bool IsAttribute => (Scope == ScopeType.Attribute);
		/// <summary>
		/// Gets if this variable is a fragment stage output in the shader program.
		/// </summary>
		public bool IsFragmentOutput => (Scope == ScopeType.FragmentOutput);
		/// <summary>
		/// Gets if this variable is a local in the shader program.
		/// </summary>
		public bool IsLocal => (Scope == ScopeType.Local);
		/// <summary>
		/// Gets if this variable is a builtin variable in the shader program.
		/// </summary>
		public bool IsBuiltin => (Scope == ScopeType.Builtin);
		/// <summary>
		/// Gets if this variable is a function local in the shader program.
		/// </summary>
		public bool IsFunction => (Scope == ScopeType.Function);

		/// <summary>
		/// Gets if the variable appears in the global scope in the program.
		/// </summary>
		public bool IsGlobal => (Scope != ScopeType.Function);
		#endregion // Fields

		internal Variable(ShaderType type, string name, ScopeType scope, bool @const = false, uint asize = 0)
		{
			Type = type;
			Name = name;
			Scope = scope;
			Constant = (Scope == ScopeType.Uniform) || (Scope == ScopeType.Attribute) || @const;
			ArraySize = asize;
		}

		internal static bool TryFromContext(SSLParser.VariableDeclarationContext ctx, ScopeType scope, out Variable v, out string error)
		{
			v = null;
			error = null;

			var name = ctx.Name.Text;
			var type = ShaderTypeHelper.FromTypeContext(ctx.type());
			if (type == ShaderType.Void)
			{
				error = $"The variable '{name}' cannot be of type 'void'";
				return false;
			}
			if (type == ShaderType.Error)
			{
				error = $"Unable to convert variable '{name}' to internal type";
				return false;
			}

			uint asize = 0;
			if (ctx.arrayIndexer() != null)
			{
				var val = SSLVisitor.ParseIntegerLiteral(ctx.arrayIndexer().Index.Text, out var isUnsigned, out error);
				if (!val.HasValue)
					return false;

				if (val.Value <= 0)
				{
					error = $"The variable '{name}' cannot have a negative or zero array size";
					return false;
				}
				asize = (uint)val.Value;
			}

			v = new Variable(type, name, scope, false, asize);
			return true;
		}

		internal static bool TryFromContext(SSLParser.VariableDefinitionContext ctx, ScopeType scope, out Variable v, out string error)
		{
			v = null;
			error = null;

			var vctx = ctx as SSLParser.ValueDefinitionContext;
			var actx = ctx as SSLParser.ArrayDefinitionContext;

			var name = (vctx?.Name ?? actx.Name).Text;
			var type = ShaderTypeHelper.FromTypeContext(vctx?.type() ?? actx.type());
			if (type == ShaderType.Void)
			{
				error = $"The variable '{name}' cannot be of type 'void'";
				return false;
			}
			if (type == ShaderType.Error)
			{
				error = $"Unable to convert variable '{name}' to internal type";
				return false;
			}

			uint asize = 0;
			if (actx != null)
			{
				var val = SSLVisitor.ParseIntegerLiteral(actx.arrayIndexer().Index.Text, out var isUnsigned, out error);
				if (!val.HasValue)
					return false;

				if (val.Value <= 0)
				{
					error = $"The variable '{name}' cannot have a negative or zero array size";
					return false;
				}
				asize = (uint)val.Value;
			}

			v = new Variable(type, name, scope, false, asize);
			return true;
		}
	}

	/// <summary>
	/// Represents the different scopes that variable objects can occur in in a shader program.
	/// </summary>
	public enum ScopeType : byte
	{
		/// <summary>
		/// The variable appears in the global scope as a uniform value.
		/// </summary>
		Uniform,
		/// <summary>
		/// The variable appears in the global scope as an input vertex attribute (only visible inside of vertex stage).
		/// </summary>
		Attribute,
		/// <summary>
		/// The variable appears in the global scope as an output from the fragment stage.
		/// </summary>
		FragmentOutput,
		/// <summary>
		/// The variable appears in the global scope as a value passed internally between stages.
		/// </summary>
		Local,
		/// <summary>
		/// The variable appears in the global scope as one of the reserved built-in variables.
		/// </summary>
		Builtin,
		/// <summary>
		/// The variable appears locally within a function. Includes function arguments.
		/// </summary>
		Function
	}
}
