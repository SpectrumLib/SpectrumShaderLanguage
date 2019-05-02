using System;
using System.Collections.Generic;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Represents a named variable object in a shader program.
	internal sealed class Variable
	{
		private readonly Dictionary<string, string> BUILTIN_MAP = new Dictionary<string, string>() {
			{ "$Position", "gl_Position" }, { "$VertexIndex", "gl_VertexIndex" }, { "$InstanceIndex", "gl_InstanceIndex" },
			{ "$PointSize", "gl_PointSize" }, { "$FragCoord", "gl_FragCoord" }, { "$FrontFacing", "gl_FrontFacing" },
			{ "$PointCoord", "gl_PointCoord" }, { "$SampleId", "gl_SampleId" }, { "$NumSamples", "gl_NumSamples" },
			{ "$SamplePosition", "gl_SamplePosition" }, { "$FragDepth", "gl_FragDepth" }
		};

		#region Fields
		public readonly string Name;
		public readonly ShaderType Type;
		public readonly VariableScope Scope;
		public readonly bool Constant;
		public readonly uint ArraySize; // Will be 1 if the variable is not an array
		public readonly bool IsArray;
		// The stages that read from this variable, only used for some scope types
		public ShaderStages ReadStages { get; internal set; } = ShaderStages.None;
		// The stages that write to this variable, only used for some scope types
		public ShaderStages WriteStages { get; internal set; } = ShaderStages.None;
		public readonly ImageFormat? ImageFormat;
		public readonly uint? SubpassIndex;
		public readonly uint? ConstantIndex;
		// Used internally to track if the variable has flat interpolation (internals only)
		public readonly bool IsFlat;
		// Used internally to throw an error for attempting to read write-only built-in variables or 'out' function parameters
		public readonly bool CanRead;

		public uint Size => Type.GetSize() * ArraySize;
		public bool IsUniform => (Scope == VariableScope.Uniform);
		public bool IsAttribute => (Scope == VariableScope.Attribute);
		public bool IsFragmentOutput => (Scope == VariableScope.FragmentOutput);
		public bool IsInternal => (Scope == VariableScope.Internal);
		public bool IsConstant => (Scope == VariableScope.Constant);
		public bool IsSpecialized => (Scope == VariableScope.Constant) && ConstantIndex.HasValue;
		public bool IsBuiltin => (Scope == VariableScope.Builtin);
		public bool IsArgument => (Scope == VariableScope.Argument);
		public bool IsLocal => (Scope == VariableScope.Local);
		public bool IsGlobal => (Scope != VariableScope.Local) && (Scope != VariableScope.Argument);
		#endregion // Fields

		public Variable(ShaderType type, string name, VariableScope scope, bool @const, uint? asize, bool cr = true, ImageFormat? ifmt = null, uint? si = null, bool flat = false, uint? cidx = null)
		{
			Type = type;
			Name = name;
			Scope = scope;
			Constant = (Scope == VariableScope.Uniform) || (Scope == VariableScope.Attribute) || (Scope == VariableScope.Constant) || @const;
			ArraySize = Math.Max(asize.GetValueOrDefault(1), 1);
			IsArray = asize.HasValue && asize.Value != 0;
			CanRead = cr;
			ImageFormat = ifmt;
			SubpassIndex = si;
			IsFlat = flat;
			ConstantIndex = cidx;
		}

		public string GetGLSLDecl(ShaderStages? stage = null) =>
			$"{Type.ToGLSLKeyword(ImageFormat)} {GetOutputName(stage)}{(IsArray ? $"[{ArraySize}]" : "")}";

		public string GetOutputName(ShaderStages? stage = null)
		{
			if (IsBuiltin) return BUILTIN_MAP[Name];
			else if (IsInternal) return $"_{stage.Value.GetShortName()}_{Name}";
			else return Name;
		}

		internal static Variable FromContext(SSLParser.VariableDeclarationContext ctx, SSLVisitor vis, VariableScope scope)
		{
			var name = ctx.Name.Text;
			if (name[0] == '$')
				vis.Error(ctx, "Cannot start a variable with the character '$', this is reserved for built-in variables.");
			if (name.Length > 32)
				vis.Error(ctx, "Variable names cannot be longer than 32 characters.");

			if (ctx.KW_FLAT() != null && scope != VariableScope.Internal)
				vis.Error(ctx, "'flat' qualifier is only valid for internals.");

			var type = ReflectionUtils.TranslateTypeContext(ctx.type());
			if (!type.HasValue)
				vis.Error(ctx, $"Unable to convert variable '{name}' to internal type.");
			if (type.Value == ShaderType.Void)
				vis.Error(ctx, $"The variable '{name}' cannot be of type 'void'.");

			uint? asize = null;
			if (ctx.arrayIndexer() != null)
			{
				if (!vis.TryParseArrayIndexer(ctx.arrayIndexer(), (null, null), out var aidx, out var error))
					vis.Error(ctx.arrayIndexer(), error);
				if (aidx.Index2 != null)
					vis.Error(ctx.arrayIndexer(), "Cannot declare multi-dimensional arrays.");
				if (!aidx.Index1.GetIntegerLiteral().HasValue) // Must be a literal (OR A SPEC CONSTANT TODO)
					vis.Error(ctx.arrayIndexer(), "Must use integer literals when declaring arrays.");
				asize = (uint)aidx.Index1.GetIntegerLiteral().Value;
				if (asize.Value > 255)
					vis.Error(ctx.arrayIndexer(), "Cannot declare arrays larger than 255.");
			}

			return new Variable(type.Value, name, scope, false, asize, flat: (ctx.KW_FLAT() != null));
		}

		internal static Variable FromContext(SSLParser.VariableDefinitionContext ctx, SSLVisitor vis, VariableScope scope)
		{
			var name = ctx.Name.Text;
			if (name[0] == '$')
				vis.Error(ctx, "Cannot start a variable with the character '$', this is reserved for built-in variables.");
			if (name.Length > 32)
				vis.Error(ctx, "Variable names cannot be longer than 32 characters.");

			var type = ReflectionUtils.TranslateTypeContext(ctx.type());
			if (!type.HasValue)
				vis.Error(ctx, $"Unable to convert variable '{name}' to internal type.");
			if (type.Value == ShaderType.Void)
				vis.Error(ctx, $"The variable '{name}' cannot be of type 'void'.");

			uint? asize = null;
			if (ctx.arrayIndexer() != null)
			{
				if (!vis.TryParseArrayIndexer(ctx.arrayIndexer(), (null, null), out var aidx, out var error))
					vis.Error(ctx.arrayIndexer(), error);
				if (aidx.Index2 != null)
					vis.Error(ctx.arrayIndexer(), "Cannot declare multi-dimensional arrays.");
				if (!aidx.Index1.GetIntegerLiteral().HasValue) // Must be a literal (OR A SPEC CONSTANT TODO)
					vis.Error(ctx.arrayIndexer(), "Must use integer literals when declaring arrays.");
				asize = (uint)aidx.Index1.GetIntegerLiteral().Value;
				if (asize.Value > 255)
					vis.Error(ctx.arrayIndexer(), "Cannot declare arrays larger than 255.");
			}

			return new Variable(type.Value, name, scope, ctx.KW_CONST() != null, asize);
		}

		internal static Variable FromContext(SSLParser.UniformVariableContext ctx, SSLVisitor vis)
		{
			var name = ctx.Name.Text;
			if (name[0] == '$')
				vis.Error(ctx, "Cannot start a variable with the character '$', this is reserved for built-in variables.");
			if (name.Length > 32)
				vis.Error(ctx, "Uniform names cannot be longer than 32 characters.");

			var type = ReflectionUtils.TranslateTypeContext(ctx.type());
			if (!type.HasValue)
				vis.Error(ctx, $"Unable to convert variable '{name}' to internal type.");
			if (type == ShaderType.Void)
				vis.Error(ctx, $"The variable '{name}' cannot be of type 'void'.");

			ImageFormat? ifmt = null;
			uint? si = null;
			if (type.Value.IsImageType())
			{
				if (ctx.Qualifier?.imageLayoutQualifier() == null)
					vis.Error(ctx, "Storage image types must have a format qualifier.");
				ifmt = ReflectionUtils.TranslateImageFormat(ctx.Qualifier.imageLayoutQualifier());
			}
			else if (type.Value.IsSubpassInput())
			{
				if (ctx.Qualifier?.INTEGER_LITERAL() == null)
					vis.Error(ctx, "Subpass inputs must have an index qualifier.");
				var pv = SSLVisitor.ParseIntegerLiteral(ctx.Qualifier.INTEGER_LITERAL().GetText(), out var isus, out var error);
				if (!pv.HasValue)
					vis.Error(ctx, error);
				if (pv.Value < 0)
					vis.Error(ctx, "Subpass input index cannot be less than 0.");
				if (pv.Value > 255)
					vis.Error(ctx, "Subpass input index cannot be greater than 255.");
				si = (uint)pv.Value;
			}
			else
			{
				if (ctx.Qualifier?.imageLayoutQualifier() != null || ctx.Qualifier?.INTEGER_LITERAL() != null)
					vis.Error(ctx, $"The handle type '{type}' cannot have qualifiers.");
			}

			return new Variable(type.Value, name, VariableScope.Uniform, false, null, ifmt: ifmt, si: si);
		}

		internal static Variable FromConstant(SSLParser.ConstantStatementContext ctx, SSLVisitor vis)
		{
			var name = ctx.Name.Text;
			if (name[0] == '$')
				vis.Error(ctx, "Cannot start a variable with the character '$', this is reserved for built-in variables.");
			if (name.Length > 32)
				vis.Error(ctx, "Variable names cannot be longer than 32 characters.");

			uint? cidx = null;
			if (ctx.SpecIndex != null)
			{
				var ival = SSLVisitor.ParseIntegerLiteral(ctx.SpecIndex.Text, out var isus, out var perror);
				if (!ival.HasValue)
					vis.Error(ctx, "Could not parse integer literal.");
				if (ival.Value < 0)
					vis.Error(ctx, "Specialization constant index cannot be less than 0.");
				if (ival.Value > 255)
					vis.Error(ctx, "Specialization constant index cannot be greater than 255.");
				cidx = (uint)ival.Value;
			}

			var type = ReflectionUtils.TranslateTypeContext(ctx.type());
			if (!type.HasValue)
				vis.Error(ctx, $"Unable to convert constant '{name}' to internal type.");
			if (type == ShaderType.Void)
				vis.Error(ctx, $"The variable '{name}' cannot be of type 'void'.");
			if (!type.Value.IsValueType())
				vis.Error(ctx, "Globals constants must be a value type.");

			return new Variable(type.Value, name, VariableScope.Constant, true, null, cidx: cidx);
		}
	}

	// Represents the different scopes that variable objects can occur in in a shader program.
	internal enum VariableScope : byte
	{
		// Uniform value
		Uniform,
		// Input vertex attribute (only visible inside of vertex stage)
		Attribute,
		// Output from the fragment stage
		FragmentOutput,
		// A value passed internally between stages
		Internal,
		// One of the reserved built-in variables
		Builtin,
		// An argument to that function
		Argument,
		// Within a function as a variable within the function body
		Local,
		// The global constants and specialization constants
		Constant
	}
}
