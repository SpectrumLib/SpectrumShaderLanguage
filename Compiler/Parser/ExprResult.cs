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
		// The variable holding the expr result, if any, which will be either a predefined variable or a new SSA variable.
		public readonly Variable Variable;
		// This is the GLSL text used to initialize the SSA
		public readonly string InitText;
		// The GLSL source used to reference these results, either the variable name or inline value
		public readonly string RefText;

		public bool IsArray => ArraySize != 0;
		public bool HasVariable => Variable != null;

		public ExprResult(ShaderType type, uint asize, string text)
		{
			Type = type;
			ArraySize = asize;
			Variable = null;
			InitText = text;
			RefText = text;
		}

		public ExprResult(Variable var, string text)
		{
			Type = var.Type;
			ArraySize = var.ArraySize;
			Variable = var;
			InitText = text;
			RefText = var.Name;
		}
	}
}
