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

			// This prevents duplication in the event that there is an ssa already, and there is no array indexer or swizzle
			if (res.HasSSA)
				res = new ExprResult(res.Type, 0, res.SSA.Name);

			if (hasa)
			{
				if (!res.IsArray && !res.Type.IsVectorType())
					vis._THROW(actx, "The preceeding expression is not an array indexable type.");
				var aidx = SSLVisitor.ParseIntegerLiteral(actx.Index.Text, out var iu, out var error);
				if (!aidx.HasValue)
					vis._THROW(actx.Index, error);
				if (aidx.Value < 0)
					vis._THROW(actx.Index, "An array indexer cannot be negative.");
				var asize = res.IsArray ? res.ArraySize : res.Type.GetComponentCount();
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
					if (!ReflectionUtils.IsSwizzleValid(res.Type, swc))
						vis._THROW(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this type.");
				}
				res = new ExprResult(res.Type.ToVectorType((uint)stxt.Length).Value, 0, $"{res.RefText}.{stxt}");
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
				var asize = vrbl.IsArray ? vrbl.ArraySize : vrbl.Type.GetComponentCount();
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
					if (ReflectionUtils.IsSwizzleValid(ltype, swc))
						vis._THROW(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this lvalue type.");
				}
				ltype = ltype.ToVectorType((uint)stxt.Length).Value;
			}

			return ltype;
		}

		// Checks the types for a binary or complex assignment operation
		// See http://learnwebgl.brown37.net/12_shader_language/glsl_mathematical_operations.html
		public static ShaderType CheckOperator(SSLVisitor vis, IToken op, ShaderType ltype, ShaderType rtype)
		{
			var opnum = op.Type;
			// Overall check (value types only)
			if (!ltype.IsValueType() || !rtype.IsValueType())
				vis._THROW(op, $"Cannot apply operators to non-value types ({ltype} {op.Text} {rtype}).");

			if (opnum == SSLParser.OP_MUL || opnum == SSLParser.OP_DIV || opnum == SSLParser.OP_MUL_ASSIGN || opnum == SSLParser.OP_DIV_ASSIGN) // '*', '/', '*=', '/='
			{
				if (ltype.GetComponentType() == ShaderType.Bool || rtype.GetComponentType() == ShaderType.Bool)
					vis._THROW(op, $"Cannot mul/div boolean types ({ltype} {op.Text} {rtype}).");

				if (ltype.IsScalarType())
				{
					if (rtype.IsScalarType())
					{
						if (ltype == ShaderType.Float || rtype == ShaderType.Float) return ShaderType.Float;
						if (ltype == ShaderType.Int || rtype == ShaderType.Int) return ShaderType.Int;
						return ShaderType.UInt;
					}
					else if (rtype.IsMatrixType())
					{
						if (opnum == SSLParser.OP_DIV) vis._THROW(op, "Cannot divide a scalar by a matrix.");
						else return rtype;
					}
					else // Vectors
					{
						if (opnum == SSLParser.OP_DIV) vis._THROW(op, "Cannot divide a scalar by a vector.");
						// THIS ONLY WORKS BECAUSE OF THE ORDERING OF THE SHADERTYPE ENUM, AND WILL BREAK IF THE ORDERING IS CHANGED
						var ctype = (ShaderType)Math.Max((int)ltype, (int)rtype.GetComponentType());
						return ctype.ToVectorType(rtype.GetComponentCount()).Value;
					}
				}
				else if (ltype.IsMatrixType())
				{
					if (rtype.IsScalarType())
						return ltype;
					else if (rtype.IsMatrixType())
					{
						if (opnum == SSLParser.OP_DIV) vis._THROW(op, "Cannot divide a matrix by a matrix.");
						else return rtype;
					}
					else // Vectors
					{
						if (opnum == SSLParser.OP_DIV) vis._THROW(op, "Cannot divide a matrix by a vector.");
						var vsize = rtype.GetComponentCount();
						var msize = ((uint)ltype - (uint)ShaderType.Mat2) + 2;
						if (vsize != msize) vis._THROW(op, $"Can only multiply a matrix by a vector of the same rank ({ltype}, {rtype}).");
						return ShaderType.Float.ToVectorType(vsize).Value;
					}
				}
				else // Vectors
				{
					if (rtype.IsScalarType())
					{
						if (ltype.GetComponentType() == ShaderType.Float || rtype == ShaderType.Float)
							return ShaderType.Float.ToVectorType(ltype.GetComponentCount()).Value;
						if (ltype.GetComponentType() == ShaderType.Int || rtype == ShaderType.Int)
							return ShaderType.Int.ToVectorType(ltype.GetComponentCount()).Value;
						return ShaderType.UInt.ToVectorType(ltype.GetComponentCount()).Value;
					}
					else if (rtype.IsMatrixType())
						vis._THROW(op, $"Cannot mul/div a vector to a matrix ({ltype} {op.Text} {rtype}).");
					else // Vectors
					{
						if (ltype.GetComponentCount() != rtype.GetComponentCount())
							vis._THROW(op, $"Cannot mul/div vectors of different sizes ({ltype}, {rtype}).");
						if (ltype.GetComponentType() == ShaderType.Float || rtype.GetComponentType() == ShaderType.Float)
							return ShaderType.Float.ToVectorType(ltype.GetComponentCount()).Value;
						if (ltype.GetComponentType() == ShaderType.Int || rtype.GetComponentType() == ShaderType.Int)
							return ShaderType.Int.ToVectorType(ltype.GetComponentCount()).Value;
						return ShaderType.UInt.ToVectorType(ltype.GetComponentCount()).Value;
					}
				}
			}
			else if (opnum == SSLParser.OP_MOD) // '%'
			{
				if ((ltype != ShaderType.Int && ltype != ShaderType.UInt) || (rtype != ShaderType.Int && rtype != ShaderType.UInt))
					vis._THROW(op, $"The modulus operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ltype;
			}
			else if (opnum == SSLParser.OP_ADD || opnum == SSLParser.OP_SUB || opnum == SSLParser.OP_ADD_ASSIGN || opnum == SSLParser.OP_SUB_ASSIGN) // '+', '-', '+=', '-='
			{
				if (ltype != rtype)
				{
					if (ltype.IsMatrixType() || rtype.IsMatrixType())
						vis._THROW(op, $"Can only add/sub a matrix to another matrix of the same size ({ltype} {op.Text} {rtype}).");
					if (ltype.GetComponentCount() != rtype.GetComponentCount())
						vis._THROW(op, $"Can only add/sub vectors of the same size ({ltype}, {rtype}).");
					if (ltype.GetComponentType() == ShaderType.Bool || rtype.GetComponentType() == ShaderType.Bool)
						vis._THROW(op, $"Cannot add/sub boolean types ({ltype} {op.Text} {rtype}).");
					if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
						vis._THROW(op, $"No implicit cast available to add/sub different types ({ltype} {op.Text} {rtype}).");
					// THIS ONLY WORKS BECAUSE OF THE ORDERING OF THE SHADERTYPE ENUM, AND WILL BREAK IF THE ORDERING IS CHANGED
					var ctype = (ShaderType)Math.Max((int)ltype.GetComponentType(), (int)rtype.GetComponentType());
					return ctype.ToVectorType(ltype.GetComponentCount()).Value;
				}
				return ltype;
			}
			else if (opnum == SSLParser.OP_LSHIFT || opnum == SSLParser.OP_RSHIFT || opnum == SSLParser.OP_LS_ASSIGN || opnum == SSLParser.OP_RS_ASSIGN) // '<<', '>>', '<<=', '>>='
			{
				if ((ltype != ShaderType.Int && ltype != ShaderType.UInt) || (rtype != ShaderType.Int && rtype != ShaderType.UInt))
					vis._THROW(op, $"The '{op.Text}' operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ShaderType.Int;
			}
			else if (opnum == SSLParser.OP_LT || opnum == SSLParser.OP_GT || opnum == SSLParser.OP_LE || opnum == SSLParser.OP_GE) // '<', '>', '<=', '>='
			{
				if (!ltype.IsScalarType() || !rtype.IsScalarType() || ltype == ShaderType.Bool || rtype == ShaderType.Bool)
					vis._THROW(op, $"The relational operator '{op.Text}' can only be applied to numeric scalar types ({ltype}, {rtype}).");
				if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
					vis._THROW(op, $"No implicit cast available to compare relation between different types ({ltype} {op.Text} {rtype}).");
				return ShaderType.Bool;
			}
			else if (opnum == SSLParser.OP_EQ || opnum == SSLParser.OP_NE) // '==', '!='
			{
				if (ltype != rtype)
				{
					if (ltype.IsMatrixType() || rtype.IsMatrixType())
						vis._THROW(op, $"Can only compare a matrix to another matrix of the same size ({ltype} {op.Text} {rtype}).");
					if (ltype.GetComponentCount() != rtype.GetComponentCount())
						vis._THROW(op, $"Can only compare equality of vectors of the same size ({ltype}, {rtype}).");
					if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
						vis._THROW(op, $"No implicit cast available to compare equality between different types ({ltype} {op.Text} {rtype}).");
				}
				return ShaderType.Bool;
			}
			else if (opnum == SSLParser.OP_BITAND || opnum == SSLParser.OP_BITOR || opnum == SSLParser.OP_BITXOR || opnum == SSLParser.OP_AND_ASSIGN
					 || opnum == SSLParser.OP_OR_ASSIGN || opnum == SSLParser.OP_XOR_ASSIGN) // '&', '|', '^', '&=', '|=', '^='
			{
				if ((ltype != ShaderType.Int && ltype != ShaderType.UInt) || (rtype != ShaderType.Int && rtype != ShaderType.UInt))
					vis._THROW(op, $"The '{op.Text}' operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ltype;
			}
			else if (opnum == SSLParser.OP_AND || opnum == SSLParser.OP_OR || opnum == SSLParser.OP_XOR) // '&&', '||', '^^'
			{
				if (ltype != ShaderType.Bool || rtype != ShaderType.Bool)
					vis._THROW(op, $"The '{op.Text}' operator requires both operands to be scalar booleans ({ltype}, {rtype}).");
				return ShaderType.Bool;
			}

			vis._THROW(op, $"The binary operator '{op.Text}' was not understood.");
			return ShaderType.Void;
		}
	}
}
