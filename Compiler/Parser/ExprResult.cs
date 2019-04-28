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
		// If the expr is an array type
		public readonly bool IsArray;
		// The SSA variable holding the expr result, if any
		public readonly Variable SSA;
		// This is the GLSL text used to initialize the SSA, or to inline the value
		public readonly string ValueText;

		// Used to reference the value, either be the ssa name or inlined code
		public string RefText => SSA?.Name ?? ValueText;

		// Used for expressions in function calls, to check if the expression is valid to pass as a reference
		// This value will not propgate upwards when constructing new results based off of this one
		public Variable LValue = null;
		// Used sparingly, only for lvalue variable references to image types
		public ImageFormat? ImageFormat => LValue?.ImageFormat;

		public bool HasSSA => SSA != null;

		public ExprResult(ShaderType type, uint? asize, string text)
		{
			Type = type;
			ArraySize = Math.Max(asize.GetValueOrDefault(1), 1);
			IsArray = asize.HasValue && asize.Value != 0;
			SSA = null;
			ValueText = text;
		}

		public ExprResult(Variable ssa, string text)
		{
			Type = ssa.Type;
			ArraySize = ssa.ArraySize;
			IsArray = ssa.IsArray;
			SSA = ssa;
			ValueText = text;
		}
	}
}
