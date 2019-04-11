using System;
using SSLang.Reflection;

namespace SSLang
{
	// The object type returned by functions in SSLVisitor
	internal class ExprResult
	{
		// The type that the expression generates
		public readonly ShaderType Type;
		// The size of the array, if the result is an array
		public readonly uint ArraySize;
		// The SSA variable holding the expr result, if any
		public readonly Variable SSA;
		// This is the GLSL text used to initialize the SSA, or to inline the value
		public readonly string ValueText;

		// Used to reference the value, either be the ssa name or inlined code
		public string RefText => SSA?.Name ?? ValueText;

		public bool IsArray => ArraySize != 0;
		public bool HasSSA => SSA != null;

		public ExprResult(ShaderType type, uint asize, string text)
		{
			Type = type;
			ArraySize = asize;
			SSA = null;
			ValueText = text;
		}

		public ExprResult(Variable ssa, string text)
		{
			Type = ssa.Type;
			ArraySize = ssa.ArraySize;
			SSA = ssa;
			ValueText = text;
		}
	}
}
