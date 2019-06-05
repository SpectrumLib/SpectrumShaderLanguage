using SSLang.Reflection;
using System;

namespace SSLang
{
	// Represents a named variable object in a shader program
	internal sealed class Variable
	{
		#region Fields
		public readonly string Name;
		public readonly ShaderType Type;
		public readonly bool IsArray;
		public readonly uint ArraySize; // Will be 1 if the variable is not an array
		public readonly VariableScope Scope;
		public readonly bool CanWrite;
		public readonly bool CanRead;
		// The stages that write to and read from this variable
		public ShaderStages ReadStages { get; internal set; } = ShaderStages.None;
		public ShaderStages WriteStages { get; internal set; } = ShaderStages.None;

		// Situationally-specific attributes
		public readonly ImageFormat? ImageFormat;
		public readonly uint? Index;
		public bool IsFlat;

		public uint Size => Type.GetSize() * ArraySize;
		public bool IsUniform => Scope == VariableScope.Uniform;
		public bool IsAttribute => Scope == VariableScope.Uniform;
		public bool IsOutput => Scope == VariableScope.Uniform;
		public bool IsLocal => Scope == VariableScope.Uniform;
		public bool IsBuiltin => Scope == VariableScope.Uniform;
		public bool IsArgument => Scope == VariableScope.Uniform;
		public bool IsFunction => Scope == VariableScope.Uniform; // Doesnt actually check if this is a function, just if it is function local
		public bool IsConstant => Scope == VariableScope.Uniform;
		#endregion // Fields

		public Variable(ShaderType type, string name, uint? asize, VariableScope scope, bool read, bool write, ImageFormat? ifmt = null, uint? index = null, bool flat = false)
		{
			Name = name;
			Type = type;
			IsArray = asize.HasValue;
			ArraySize = asize.GetValueOrDefault(1);
			Scope = scope;
			CanRead = read;
			CanWrite = write;
			ImageFormat = ifmt;
			Index = index;
			IsFlat = flat;
		}
	}

	// The different scopes (loose term here) that variables can exist in
	internal enum VariableScope
	{
		Uniform,
		Attribute,
		Output,
		Local, // The values passed between shader stages
		Builtin,
		Argument,
		Function,
		Constant
	}
}
