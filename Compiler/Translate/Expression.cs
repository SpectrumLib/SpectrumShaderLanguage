using System;
using SSLang.Reflection;

namespace SSLang
{
	// The return type for the translator visit functions
	internal class Expression
	{
		#region Fields
		public readonly ShaderType Type;
		public readonly uint ArraySize;
		public readonly bool IsArray;
		public readonly Variable SSA; // The SSA variable representing the expression result, if any
		public readonly string ValueText; // The text to inline, or used to initialize the SSA variable
		public readonly bool IsLiteral; // If the expression is a value literal

		public string RefText => SSA?.Name ?? ValueText; // The text to insert in the GLSL to get the value of this expression

		public bool IsInteger => (Type == ShaderType.Int) || (Type == ShaderType.UInt);
		public bool HasSSA => SSA != null;
		#endregion // Fields

		#region Literals
		public float? GetFloatLiteral()
		{
			if (IsLiteral && Type.IsScalarType() && Type != ShaderType.Bool)
			{
				if (!Single.TryParse(RefText, out var res))
					return null;
				return res;
			}
			return null;
		}

		public long? GetIntegerLiteral()
		{
			if (IsLiteral && Type.IsScalarType() && Type != ShaderType.Float && Type != ShaderType.Bool)
			{
				if (!Translator.TryParseIntegerLiteral(RefText, out var value, out var _, out var _))
					return null;
				return value;
			}
			return null;
		}

		public bool? GetBoolLiteral()
		{
			if (IsLiteral && Type == ShaderType.Bool)
				return (RefText == "true") ? true : (RefText == "false") ? false : (bool?)null;
			return null;
		}
		#endregion // Literals
	}
}
