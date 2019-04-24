using System;
using System.Collections.Generic;
using SSLang.Generated;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a named variable object in a shader program.
	/// </summary>
	public sealed class Variable
	{
		private readonly Dictionary<string, string> BUILTIN_MAP = new Dictionary<string, string>() {
			{ "$Position", "gl_Position" }, { "$VertexIndex", "gl_VertexIndex" }, { "$InstanceIndex", "gl_InstanceIndex" },
			{ "$PointSize", "gl_PointSize" }, { "$FragCoord", "gl_FragCoord" }, { "$FrontFacing", "gl_FrontFacing" },
			{ "$PointCoord", "gl_PointCoord" }, { "$SampleId", "gl_SampleId" }, { "$SamplePosition", "gl_SamplePosition" },
			{ "$FragDepth", "gl_FragDepth" }
		};
		private readonly Dictionary<ShaderStages, string> STAGE_PREFIXES = new Dictionary<ShaderStages, string>() {
			{ ShaderStages.None, "none" }, { ShaderStages.Vertex, "vert" }, { ShaderStages.TessControl, "tesc" },
			{ ShaderStages.TessEval, "tese" }, { ShaderStages.Geometry, "geom" }, { ShaderStages.Fragment, "frag" }
		};

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
		public readonly VariableScope Scope;
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
		/// The texel format for the image. If this variable is not an image type, this will have the value of
		/// <see cref="ImageFormat.Error"/>.
		/// </summary>
		public readonly ImageFormat ImageFormat;

		/// <summary>
		/// Gets if the variable is an array of values.
		/// </summary>
		public bool IsArray => (ArraySize > 0);

		/// <summary>
		/// The size of the variable, in bytes.
		/// </summary>
		public uint Size => Type.GetSize() * (IsArray ? ArraySize : 1);

		/// <summary>
		/// Gets if this variable is a uniform in the shader program.
		/// </summary>
		public bool IsUniform => (Scope == VariableScope.Uniform);
		/// <summary>
		/// Gets if this variable is a vertex attribute in the shader program.
		/// </summary>
		public bool IsAttribute => (Scope == VariableScope.Attribute);
		/// <summary>
		/// Gets if this variable is a fragment stage output in the shader program.
		/// </summary>
		public bool IsFragmentOutput => (Scope == VariableScope.FragmentOutput);
		/// <summary>
		/// Gets if this variable is an internal in the shader program.
		/// </summary>
		public bool IsInternal => (Scope == VariableScope.Internal);
		/// <summary>
		/// Gets if this variable is a builtin variable in the shader program.
		/// </summary>
		public bool IsBuiltin => (Scope == VariableScope.Builtin);
		/// <summary>
		/// Gets if the variable is an argument to a function.
		/// </summary>
		public bool IsArgument => (Scope == VariableScope.Argument);
		/// <summary>
		/// Gets if this variable is local within a function (does not include arguments).
		/// </summary>
		public bool IsLocal => (Scope == VariableScope.Local);

		/// <summary>
		/// Gets if the variable appears in the global scope in the program.
		/// </summary>
		public bool IsGlobal => (Scope != VariableScope.Local) && (Scope != VariableScope.Argument);

		// Used internally to throw an error for attempting to read write-only built-in variables or 'out' function parameters
		internal readonly bool CanRead;
		#endregion // Fields

		internal Variable(ShaderType type, string name, VariableScope scope, bool @const = false, uint asize = 0, bool cr = true, ImageFormat ifmt = ImageFormat.Error)
		{
			Type = type;
			Name = name;
			Scope = scope;
			Constant = (Scope == VariableScope.Uniform) || (Scope == VariableScope.Attribute) || @const;
			ArraySize = asize;
			CanRead = cr;
			ImageFormat = ifmt;
		}

		internal string GetGLSLDecl(bool @const = true, ShaderStages? stage = null) => 
			$"{((@const && Constant) ? "const " : "")}{Type.ToGLSL(ImageFormat)} {GetOutputName(stage)}{(IsArray ? $"[{ArraySize}]" : "")}";

		internal string GetOutputName(ShaderStages? stage = null)
		{
			if (IsBuiltin) return BUILTIN_MAP[Name];
			else if (IsInternal) return $"_{STAGE_PREFIXES[stage.GetValueOrDefault()]}_{Name}";
			else return Name;
		}

		internal static bool TryFromContext(SSLParser.VariableDeclarationContext ctx, VariableScope scope, out Variable v, out string error)
		{
			v = null;
			error = null;

			var name = ctx.Name.Text;
			if (name[0] == '$')
			{
				error = "Cannot start a variable with the character '$', this is reserved for built-in variables.";
				return false;
			}

			var type = ShaderTypeHelper.FromTypeContext(ctx.type());
			if (type == ShaderType.Void)
			{
				error = $"The variable '{name}' cannot be of type 'void'.";
				return false;
			}
			if (type == ShaderType.Error)
			{
				error = $"Unable to convert variable '{name}' to internal type.";
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
					error = $"The variable '{name}' cannot have a negative or zero array size.";
					return false;
				}
				asize = (uint)val.Value;
			}

			v = new Variable(type, name, scope, false, asize);
			return true;
		}

		internal static bool TryFromContext(SSLParser.VariableDefinitionContext ctx, VariableScope scope, out Variable v, out string error)
		{
			v = null;
			error = null;

			var name = ctx.Name.Text;
			if (name[0] == '$')
			{
				error = "Cannot start a variable with the character '$', this is reserved for built-in variables.";
				return false;
			}

			var type = ShaderTypeHelper.FromTypeContext(ctx.type());
			if (type == ShaderType.Void)
			{
				error = $"The variable '{name}' cannot be of type 'void'.";
				return false;
			}
			if (type == ShaderType.Error)
			{
				error = $"Unable to convert variable '{name}' to internal type.";
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
					error = $"The variable '{name}' cannot have a negative or zero array size.";
					return false;
				}
				asize = (uint)val.Value;
			}

			v = new Variable(type, name, scope, ctx.KW_CONST() != null, asize);
			return true;
		}

		internal static bool TryFromContext(SSLParser.UniformVariableContext ctx, VariableScope scope, out Variable v, out string error)
		{
			v = null;
			error = null;

			var name = ctx.Name.Text;
			if (name[0] == '$')
			{
				error = "Cannot start a variable with the character '$', this is reserved for built-in variables.";
				return false;
			}

			var type = ShaderTypeHelper.FromTypeContext(ctx.type());
			if (type == ShaderType.Void)
			{
				error = $"The variable '{name}' cannot be of type 'void'.";
				return false;
			}
			if (type == ShaderType.Error)
			{
				error = $"Unable to convert variable '{name}' to internal type.";
				return false;
			}

			ImageFormat ifmt = ImageFormat.Error;
			if (type.IsImageHandle())
			{
				if (ctx.Qualifier?.imageLayoutQualifier() == null)
				{
					error = "Storage image types must have a format qualifier.";
					return false;
				}
				ifmt = ImageFormatHelper.FromQualifier(ctx.Qualifier.imageLayoutQualifier());
			}

			v = new Variable(type, name, scope, false, 0, ifmt: ifmt);
			return true;
		}
	}

	/// <summary>
	/// Represents the different scopes that variable objects can occur in in a shader program.
	/// </summary>
	public enum VariableScope : byte
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
		Internal,
		/// <summary>
		/// The variable appears in the global scope as one of the reserved built-in variables.
		/// </summary>
		Builtin,
		/// <summary>
		/// The variable appears locally within a function as an argument to that function.
		/// </summary>
		Argument,
		/// <summary>
		/// The variable appears locally within a function as a variable within the function body.
		/// </summary>
		Local
	}
}
