using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Core code that deduces and manages types from all expression contexts
	internal static class TypeUtils
	{
		// Creates an expression result by applying array indexers and swizzles, if present
		public static ExprResult ApplyModifiers(SSLVisitor vis, ExprResult res, SSLParser.ArrayIndexerContext actx, ITerminalNode swizzle)
		{
			bool hasa = (actx != null);
			bool hass = (swizzle != null);

			if (hasa)
			{
				if (!res.IsArray && !res.Type.IsVectorType())
					vis._THROW(actx, "The preceeding expression is not an array indexable type.");
				var aidx = SSLVisitor.ParseIntegerLiteral(actx.Index.Text, out var iu, out var error);
				if (!aidx.HasValue)
					vis._THROW(actx.Index, error);
				if (aidx.Value < 0)
					vis._THROW(actx.Index, "An array indexer cannot be negative.");
				var asize = res.IsArray ? res.ArraySize : res.Type.GetVectorSize();
				if (aidx.Value >= asize)
					vis._THROW(actx.Index, "The array indexer is too large for the preceeding expression.");
				res = new ExprResult(res.Type, 0, $"{res.RefText}[{aidx.Value}]");
			}

			if (hass)
			{
				if (!res.Type.IsVectorType())
					vis._THROW(swizzle.Symbol, "Cannot apply a swizzle to a non-vector type.");
				var stxt = swizzle.Symbol.Text.Substring(1);
				if (stxt.Length > 4)
					vis._THROW(swizzle.Symbol, "A swizzle cannot have more than four components.");
				foreach (var swc in stxt)
				{
					if (!res.Type.IsSwizzleValid(swc))
						vis._THROW(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this type.");
				}
				res = new ExprResult(res.Type.ToVectorType((uint)stxt.Length), 0, $"{res.RefText}.{stxt}");
			}

			return res;
		}

		// Gets the type of an lvalue (such as an assignment) with an array indexer and swizzle applied
		public static ShaderType ApplyLValueModifier(SSLVisitor vis, IToken name, Variable vrbl, SSLParser.ArrayIndexerContext actx, ITerminalNode swizzle, out int? arrIndex)
		{
			bool hasa = (actx != null);
			bool hass = (swizzle != null);
			var ltype = vrbl.Type;
			arrIndex = null;

			if (hasa)
			{
				if (!vrbl.IsArray && !ltype.IsVectorType())
					vis._THROW(actx, "The lvalue is not an array indexable type.");
				var aidx = SSLVisitor.ParseIntegerLiteral(actx.Index.Text, out var iu, out var error);
				if (!aidx.HasValue)
					vis._THROW(actx.Index, error);
				if (aidx.Value < 0)
					vis._THROW(actx.Index, "An array indexer cannot be negative.");
				var asize = vrbl.IsArray ? vrbl.ArraySize : vrbl.Type.GetVectorSize();
				if (aidx.Value >= asize)
					vis._THROW(actx.Index, "The array indexer is too large for the lvalue.");
				arrIndex = aidx.HasValue ? (int?)(int)aidx.Value : null;
				ltype = vrbl.IsArray ? vrbl.Type : vrbl.Type.GetComponentType();
			}
			else if (vrbl.IsArray)
				vis._THROW(name, $"Cannot assign directly to an array lvalue, only individual components can be modified.");

			if (hass)
			{
				if (!ltype.IsVectorType())
					vis._THROW(swizzle.Symbol, "Cannot apply a swizzle to a non-vector lvalue.");
				var stxt = swizzle.Symbol.Text.Substring(1);
				if (stxt.Length > 4)
					vis._THROW(swizzle.Symbol, "A swizzle cannot have more than four components.");
				foreach (var swc in stxt)
				{
					if (!ltype.IsSwizzleValid(swc))
						vis._THROW(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this lvalue type.");
				}
				ltype = ltype.ToVectorType((uint)stxt.Length);
			}

			return ltype;
		}
	}
}
