using System;
using Antlr4.Runtime.Tree;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Core code that deduces and manages types from all expression contexts
	internal static class TypeManager
	{
		public static ExprResult ApplyModifiers(SSLVisitor vis, ExprResult res, SSLParser.ArrayIndexerContext actx, ITerminalNode swizzle)
		{
			bool hasa = (actx != null);
			bool hass = (swizzle != null);

			if (hasa)
			{
				var aidx = SSLVisitor.ParseIntegerLiteral(actx.Index.Text, out var iu, out var error);
				if (!aidx.HasValue)
					vis._THROW(actx.Index, error);
				if (aidx.Value < 0)
					vis._THROW(actx.Index, "An array indexer cannot be negative.");
				if (!res.IsArray && !res.Type.IsVectorType())
					vis._THROW(actx, "The preceeding expression is not an array indexable type.");
				var asize = res.IsArray ? res.ArraySize : res.Type.GetVectorSize();
				if (aidx.Value >= asize)
					vis._THROW(actx.Index, "The array indexer is too large for the preceeding expression.");
				res = new ExprResult(res.Type, 0, $"{res.RefText}[{aidx.Value}]");
			}

			if (hass)
			{
				var stxt = swizzle.Symbol.Text.Substring(1);
				if (stxt.Length > 4)
					vis._THROW(swizzle.Symbol, "A swizzle cannot have more than four components.");
				if (!res.Type.IsVectorType())
					vis._THROW(swizzle.Symbol, "Cannot apply a swizzle to a non-vector type.");
				foreach (var swc in stxt)
				{
					if (!res.Type.IsSwizzleValid(swc))
						vis._THROW(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this type.");
				}
				res = new ExprResult(res.Type.ToVectorType((uint)stxt.Length), 0, $"{res.RefText}.{stxt}");
			}

			return res;
		}
	}
}
