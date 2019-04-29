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
				res = new ExprResult(res.Type, null, res.SSA.Name);

			if (hasa)
			{
				if (!SSLVisitor.TryParseArrayIndexer(actx, out var aidx, out var error))
					vis.Error(actx, error);
				if (res.IsArray)
				{
					if (aidx.Index2.HasValue)
						vis.Error(actx, "Multi-dimensional arrays are not supported.");
					if (aidx.Index1 >= res.ArraySize)
						vis.Error(actx, "The array indexer is too large for the array.");
					res = new ExprResult(res.Type, null, $"{res.RefText}[{aidx.Index1}]");
				}
				else if (res.Type.IsVectorType())
				{
					if (aidx.Index2.HasValue)
						vis.Error(actx, "Vectors cannot have more than one array indexer.");
					if (aidx.Index1 >= res.Type.GetComponentCount())
						vis.Error(actx, "The array indexer is too large for the vector.");
					res = new ExprResult(res.Type.GetComponentType(), null, $"{res.RefText}[{aidx.Index1}]");
				}
				else if (res.Type.IsMatrixType())
				{
					if (!aidx.Index2.HasValue)
						vis.Error(actx, "Matrices must have two array indexers to access their members.");
					var dim = (res.Type == ShaderType.Mat2) ? 2u : (res.Type == ShaderType.Mat3) ? 3u : 4u;
					if (aidx.Index1 >= dim || aidx.Index2.Value >= dim)
						vis.Error(actx, $"The array indexers are too large for the matrix type ({res.Type}: {aidx.Index1}, {aidx.Index2.Value}).");
					res = new ExprResult(ShaderType.Float, null, $"{res.RefText}[{aidx.Index1}][{aidx.Index2.Value}]");
				}
				else
					vis.Error(actx, "The preceeding expression cannot have array indexers applied to it.");
			}

			if (hass)
			{
				if (!res.Type.IsVectorType())
					vis.Error(swizzle.Symbol, "Cannot apply a swizzle to a non-vector type.");
				var stxt = swizzle.Symbol.Text.Substring(1);
				if (stxt.Length > 4)
					vis.Error(swizzle.Symbol, "A swizzle cannot have more than four components.");
				foreach (var swc in stxt)
				{
					if (!ReflectionUtils.IsSwizzleValid(res.Type, swc))
						vis.Error(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this type.");
				}
				res = new ExprResult(res.Type.ToVectorType((uint)stxt.Length).Value, null, $"{res.RefText}.{stxt}");
			}

			return res;
		}

		// Gets the type of an lvalue (such as an assignment) with an array indexer and swizzle applied
		public static ShaderType ApplyLValueModifier(SSLVisitor vis, IToken name, Variable vrbl, SSLParser.ArrayIndexerContext actx, ITerminalNode swizzle, out (uint, uint?)? arrIndex)
		{
			bool hasa = (actx != null);
			bool hass = (swizzle != null);
			var ltype = vrbl.Type;
			arrIndex = null;

			if (hasa)
			{
				if (!SSLVisitor.TryParseArrayIndexer(actx, out var aidx, out var error))
					vis.Error(actx, error);
				if (vrbl.IsArray)
				{
					if (aidx.Index2.HasValue)
						vis.Error(actx, "Multi-dimensional arrays are not supported.");
					if (aidx.Index1 >= vrbl.ArraySize)
						vis.Error(actx, "The array indexer is too large for the array.");
					ltype = vrbl.Type;
				}
				else if (vrbl.Type.IsVectorType())
				{
					if (aidx.Index2.HasValue)
						vis.Error(actx, "Vectors cannot have more than one array indexer.");
					if (aidx.Index1 >= vrbl.Type.GetComponentCount())
						vis.Error(actx, "The array indexer is too large for the vector.");
					ltype = vrbl.Type.GetComponentType();
				}
				else if (vrbl.Type.IsMatrixType())
				{
					if (!aidx.Index2.HasValue)
						vis.Error(actx, "Matrices must have two array indexers to access their members.");
					var dim = (vrbl.Type == ShaderType.Mat2) ? 2u : (vrbl.Type == ShaderType.Mat3) ? 3u : 4u;
					if (aidx.Index1 >= dim || aidx.Index2.Value >= dim)
						vis.Error(actx, $"The array indexers are too large for the matrix type ({vrbl.Type}: {aidx.Index1}, {aidx.Index2.Value}).");
					ltype = ShaderType.Float;
				}
				else
					vis.Error(actx, "The lvalue cannot have an array indexer applied to it.");
				arrIndex = aidx;
			}
			else if (vrbl.IsArray)
				vis.Error(name, $"Cannot assign directly to an array lvalue, only individual components can be modified.");

			if (hass)
			{
				if (!ltype.IsVectorType())
					vis.Error(swizzle.Symbol, "Cannot apply a swizzle to a non-vector lvalue.");
				var stxt = swizzle.Symbol.Text.Substring(1);
				if (stxt.Length > 4)
					vis.Error(swizzle.Symbol, "A swizzle cannot have more than four components.");
				foreach (var swc in stxt)
				{
					if (ReflectionUtils.IsSwizzleValid(ltype, swc))
						vis.Error(swizzle.Symbol, $"The swizzle character '{swc}' is not valid for this lvalue type.");
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
				vis.Error(op, $"Cannot apply operators to non-value types ({ltype} {op.Text} {rtype}).");

			if (opnum == SSLParser.OP_MUL || opnum == SSLParser.OP_DIV || opnum == SSLParser.OP_MUL_ASSIGN || opnum == SSLParser.OP_DIV_ASSIGN) // '*', '/', '*=', '/='
			{
				if (ltype.GetComponentType() == ShaderType.Bool || rtype.GetComponentType() == ShaderType.Bool)
					vis.Error(op, $"Cannot mul/div boolean types ({ltype} {op.Text} {rtype}).");

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
						if (opnum == SSLParser.OP_DIV) vis.Error(op, "Cannot divide a scalar by a matrix.");
						else return rtype;
					}
					else // Vectors
					{
						if (opnum == SSLParser.OP_DIV) vis.Error(op, "Cannot divide a scalar by a vector.");
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
						if (opnum == SSLParser.OP_DIV) vis.Error(op, "Cannot divide a matrix by a matrix.");
						else return rtype;
					}
					else // Vectors
					{
						if (opnum == SSLParser.OP_DIV) vis.Error(op, "Cannot divide a matrix by a vector.");
						var vsize = rtype.GetComponentCount();
						var msize = ((uint)ltype - (uint)ShaderType.Mat2) + 2;
						if (vsize != msize) vis.Error(op, $"Can only multiply a matrix by a vector of the same rank ({ltype}, {rtype}).");
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
						vis.Error(op, $"Cannot mul/div a vector to a matrix ({ltype} {op.Text} {rtype}).");
					else // Vectors
					{
						if (ltype.GetComponentCount() != rtype.GetComponentCount())
							vis.Error(op, $"Cannot mul/div vectors of different sizes ({ltype}, {rtype}).");
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
					vis.Error(op, $"The modulus operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ltype;
			}
			else if (opnum == SSLParser.OP_ADD || opnum == SSLParser.OP_SUB || opnum == SSLParser.OP_ADD_ASSIGN || opnum == SSLParser.OP_SUB_ASSIGN) // '+', '-', '+=', '-='
			{
				if (ltype != rtype)
				{
					if (ltype.IsMatrixType() || rtype.IsMatrixType())
						vis.Error(op, $"Can only add/sub a matrix to another matrix of the same size ({ltype} {op.Text} {rtype}).");
					if (ltype.GetComponentCount() != rtype.GetComponentCount())
						vis.Error(op, $"Can only add/sub vectors of the same size ({ltype}, {rtype}).");
					if (ltype.GetComponentType() == ShaderType.Bool || rtype.GetComponentType() == ShaderType.Bool)
						vis.Error(op, $"Cannot add/sub boolean types ({ltype} {op.Text} {rtype}).");
					if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
						vis.Error(op, $"No implicit cast available to add/sub different types ({ltype} {op.Text} {rtype}).");
					// THIS ONLY WORKS BECAUSE OF THE ORDERING OF THE SHADERTYPE ENUM, AND WILL BREAK IF THE ORDERING IS CHANGED
					var ctype = (ShaderType)Math.Max((int)ltype.GetComponentType(), (int)rtype.GetComponentType());
					return ctype.ToVectorType(ltype.GetComponentCount()).Value;
				}
				return ltype;
			}
			else if (opnum == SSLParser.OP_LSHIFT || opnum == SSLParser.OP_RSHIFT || opnum == SSLParser.OP_LS_ASSIGN || opnum == SSLParser.OP_RS_ASSIGN) // '<<', '>>', '<<=', '>>='
			{
				if ((ltype != ShaderType.Int && ltype != ShaderType.UInt) || (rtype != ShaderType.Int && rtype != ShaderType.UInt))
					vis.Error(op, $"The '{op.Text}' operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ShaderType.Int;
			}
			else if (opnum == SSLParser.OP_LT || opnum == SSLParser.OP_GT || opnum == SSLParser.OP_LE || opnum == SSLParser.OP_GE) // '<', '>', '<=', '>='
			{
				if (!ltype.IsScalarType() || !rtype.IsScalarType() || ltype == ShaderType.Bool || rtype == ShaderType.Bool)
					vis.Error(op, $"The relational operator '{op.Text}' can only be applied to numeric scalar types ({ltype}, {rtype}).");
				if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
					vis.Error(op, $"No implicit cast available to compare relation between different types ({ltype} {op.Text} {rtype}).");
				return ShaderType.Bool;
			}
			else if (opnum == SSLParser.OP_EQ || opnum == SSLParser.OP_NE) // '==', '!='
			{
				if (ltype != rtype)
				{
					if (ltype.IsMatrixType() || rtype.IsMatrixType())
						vis.Error(op, $"Can only compare a matrix to another matrix of the same size ({ltype} {op.Text} {rtype}).");
					if (ltype.GetComponentCount() != rtype.GetComponentCount())
						vis.Error(op, $"Can only compare equality of vectors of the same size ({ltype}, {rtype}).");
					if (!ltype.CanPromoteTo(rtype) && !rtype.CanPromoteTo(ltype))
						vis.Error(op, $"No implicit cast available to compare equality between different types ({ltype} {op.Text} {rtype}).");
				}
				return ShaderType.Bool;
			}
			else if (opnum == SSLParser.OP_BITAND || opnum == SSLParser.OP_BITOR || opnum == SSLParser.OP_BITXOR || opnum == SSLParser.OP_AND_ASSIGN
					 || opnum == SSLParser.OP_OR_ASSIGN || opnum == SSLParser.OP_XOR_ASSIGN) // '&', '|', '^', '&=', '|=', '^='
			{
				if ((ltype != ShaderType.Int && ltype != ShaderType.UInt) || (rtype != ShaderType.Int && rtype != ShaderType.UInt))
					vis.Error(op, $"The '{op.Text}' operator requires both operands to be scalar integers ({ltype}, {rtype}).");
				return ltype;
			}
			else if (opnum == SSLParser.OP_AND || opnum == SSLParser.OP_OR || opnum == SSLParser.OP_XOR) // '&&', '||', '^^'
			{
				if (ltype != ShaderType.Bool || rtype != ShaderType.Bool)
					vis.Error(op, $"The '{op.Text}' operator requires both operands to be scalar booleans ({ltype}, {rtype}).");
				return ShaderType.Bool;
			}

			vis.Error(op, $"The binary operator '{op.Text}' was not understood.");
			return ShaderType.Void;
		}
	}
}
