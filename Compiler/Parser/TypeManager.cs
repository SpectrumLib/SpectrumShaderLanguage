using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Core code that deduces and manages types from all expression contexts
	internal static class TypeManager
	{
		// Checks the arugments and return types for built in functions
		// Note: the visitor ensures that the correct number of args are present, we dont need to check that in this function
		public static ShaderType CheckBuiltinCall(SSLVisitor vis, IToken token, string name, int type, ExprResult[] args)
		{
			var aidx = Array.FindIndex(args, a => a.IsArray);
			if (aidx != -1)
				vis._THROW(token, $"Arguments to built-in functions cannot be arrays (arg {aidx}).");

			if (type >= SSLParser.BIF_DEG2RAD && type <= SSLParser.BIF_ATAN) // 1-Arg angle and trig functions
			{
				var a1t = args[0].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a float-compatible scalar or vector type.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_ATAN2 || type == SSLParser.BIF_POW) // 2-arg trig/exponential functions
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects two float-compatible scalar or vector types.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' requires that both arguments have the same vector size.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type >= SSLParser.BIF_EXP && type <= SSLParser.BIF_FRACT) // 1-arg exponential/common functions
			{
				var a1t = args[0].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a float-compatible scalar or vector type.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_MOD) // 'mod' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a float-compatible vector, then a scalar or vector type.");
				if (!a2t.IsScalarType() && (a1t.GetVectorSize() != a2t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size, or a scalar for the second argument.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_MIN || type == SSLParser.BIF_MAX) // 'min' and 'max' functions
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type,
						   a1c = a1t.GetComponentType();
				if (!a1c.CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or integer vector or scalar arguments.");
				if (!a2t.GetComponentType().CanCastTo(a1c))
					vis._THROW(token, $"The built-in function '{name}' expects a matching component type for the second argument.");
				if (!a2t.IsScalarType() && (a1t.GetVectorSize() != a2t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size, or a scalar for the second argument.");
				return a1t;
			}
			else if (type == SSLParser.BIF_CLAMP) // 'clamp' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type, a3t = args[2].Type,
						   a1c = a1t.GetComponentType();
				if (!a1c.CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or integer vector or scalar arguments.");
				if (!a2t.GetComponentType().CanCastTo(a1c) || !a3t.GetComponentType().CanCastTo(a1c))
					vis._THROW(token, $"The built-in function '{name}' expects matching component types for the second and third arguments.");
				if (a2t.GetVectorSize() != a3t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for the second and third arguments.");
				if (!a2t.IsScalarType() && (a2t.GetVectorSize() != a1t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return a1t;
			}
			else if (type == SSLParser.BIF_MIX) // 'mix' function, unlike GLSL this only supports floating points
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type, a3t = args[2].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float) || !a3t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or integer vector or scalar arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for the first two arguments.");
				if (!a3t.IsScalarType() && (a1t.GetVectorSize() != a3t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_STEP) // 'step' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a1t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or integer vector or scalar arguments.");
				if (!a1t.IsScalarType() && (a1t.GetVectorSize() != a2t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for the first two arguments.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_SSTEP) // 'smoothstep' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type, a3t = args[2].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float) || !a3t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or integer vector or scalar arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for the first and second arguments.");
				if (!a1t.IsScalarType() && (a1t.GetVectorSize() != a3t.GetVectorSize()))
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Float.ToVectorType(a3t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_LENGTH) // 'length' function
			{
				var a1t = args[0].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a floating-point vector argument.");
				return ShaderType.Float;
			}
			else if (type == SSLParser.BIF_DISTANCE || type == SSLParser.BIF_DOT) // 'distance' and 'dot' functions
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vector arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Float;
			}
			else if (type == SSLParser.BIF_CROSS) // 'cross' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vector arguments.");
				if (a1t.GetVectorSize() != 3 || a2t.GetVectorSize() != 3)
					vis._THROW(token, $"The built-in function '{name}' expects 3-component floating-point vector arguments.");
				return ShaderType.Float3;
			}
			else if (type == SSLParser.BIF_NORMALIZE) // 'normalize' function
			{
				var a1t = args[0].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a floating-point vector argument.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_FFORWARD) // 'faceforward' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type, a3t = args[2].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float) || !a3t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vector arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize() || a1t.GetVectorSize() != a3t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_REFLECT) // 'reflect' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vector arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_REFRACT) // 'refract' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type, a3t = args[2].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vectors for the first and second arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for the first and second arguments.");
				if (!a3t.CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects a floating-point scalar for the third argument.");
				return ShaderType.Float.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_MATCOMPMUL) // 'matCompMul' function
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.IsMatrixType() || !a2t.IsMatrixType() || (a1t != a2t))
					vis._THROW(token, $"The built-in function '{name}' expects two identically sized matrix types for both arguments.");
				return a1t;
			}
			else if (type == SSLParser.BIF_TRANSPOSE || type == SSLParser.BIF_INVERSE) // 'transpose' and 'inverse' functions
			{
				ShaderType a1t = args[0].Type;
				if (!a1t.IsMatrixType())
					vis._THROW(token, $"The built-in function '{name}' expects a matrix type for its argument.");
				return a1t;
			}
			else if (type == SSLParser.BIF_DETERMINANT) // 'determinant' function
			{
				ShaderType a1t = args[0].Type;
				if (!a1t.IsMatrixType())
					vis._THROW(token, $"The built-in function '{name}' expects a matrix type for its argument.");
				return ShaderType.Float;
			}
			else if (type >= SSLParser.BIF_VECLT && type <= SSLParser.BIF_VECGE) // 2-arg relational vector functions
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type;
				if (!a1t.GetComponentType().CanCastTo(ShaderType.Float) || !a2t.GetComponentType().CanCastTo(ShaderType.Float))
					vis._THROW(token, $"The built-in function '{name}' expects floating-point vectors for all arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Bool.ToVectorType(a1t.GetVectorSize());
			}
			else if (type == SSLParser.BIF_VECEQ || type == SSLParser.BIF_VECNE) // 2-arg logical vector functions
			{
				ShaderType a1t = args[0].Type, a2t = args[1].Type,
						   a1c = a1t.GetComponentType();
				if (a1c.CanCastTo(ShaderType.Float))
				{
					if (!a2t.GetComponentType().CanCastTo(a1c))
						vis._THROW(token, $"The built-in function '{name}' expects floating-point vectors for all arguments.");
				}
				else if (a1c == ShaderType.Bool)
				{
					if (!a2t.GetComponentType().CanCastTo(a1c))
						vis._THROW(token, $"The built-in function '{name}' expects boolean vectors for all arguments.");
				}
				else
					vis._THROW(token, $"The built-in function '{name}' expects floating-point or boolean vectors for all arguments.");
				if (a1t.GetVectorSize() != a2t.GetVectorSize())
					vis._THROW(token, $"The built-in function '{name}' expects the same vector size for all arguments.");
				return ShaderType.Bool.ToVectorType(a1t.GetVectorSize());
			}
			else if (type >= SSLParser.BIF_VECANY && type <= SSLParser.BIF_VECNOT) // 1-arg logical vector functions
			{
				ShaderType a1t = args[0].Type;
				if (a1t.GetComponentType() != ShaderType.Bool)
					vis._THROW(token, $"The built-in function '{name}' expects a boolean vector type for its argument.");
				return ShaderType.Bool;
			}

			// Error
			vis._THROW(token, $"The built-in function '{name}' was not understood.");
			return ShaderType.Error;
		}

		// Gets if the type can be constructed from the given list of expressions
		public static bool CanConstructType(ShaderType type, List<ExprResult> args, out string error)
		{
			error = null;

			if (type == ShaderType.Void)
			{
				error = "Cannot construct the 'void' type.";
				return false;
			}
			if (type.IsHandleType())
			{
				error = $"Cannot manually construct a handle type ('{type}').";
				return false;
			}

			// Immediately fail if any arguments are arrays or handle types
			var badarg = args.FindIndex(arg => arg.IsArray || arg.Type.IsHandleType());
			if (badarg != -1)
			{
				error = $"Construction argument {badarg} cannot be an array or handle type.";
				return false;
			}

			if (!type.IsMatrixType()) // Vectors and scalars
			{
				var ccount = type.GetVectorSize();
				if (ccount == 1) // Scalars
				{
					if (args.Count != 1)
					{
						error = "Scalar constructors can only have one argument.";
						return false;
					}
					if (!args[0].Type.IsScalarType())
					{
						error = "Scalars can only be constructed from other non-array scalar types.";
						return false;
					}
					return true;
				}
				else // Vectors
				{
					var comptype = type.GetComponentType();
					if (args.Count > ccount) // Too many arguments
					{
						error = $"Too many arguments for constructing vector type '{type}'.";
						return false;
					}
					if (args.Count == 1) // Direct casting between vectors -or- filling out entire vector with one value
					{
						if (args[0].Type.IsScalarType())
						{
							if (!args[0].Type.CanCastTo(comptype))
							{
								error = $"Cannot construct vector type '{type}' from scalar type '{args[0].Type}'.";
								return false;
							}
							return true;
						}
						if (!args[0].Type.IsVectorType() || (args[0].Type.GetVectorSize() < ccount))
						{
							error = "Can only cast between vector types of the same size or larger (through concatenation).";
							return false;
						}
						if (!args[0].Type.CanCastTo(type))
						{
							error = $"No casting rules exist to cast a '{args[0].Type}' vector to a '{type}' vector.";
							return false;
						}
						return true;
					}
					if (args.Count == ccount) // Directly supplying each component
					{
						var bidx = args.FindIndex(arg => !arg.Type.IsScalarType() || !arg.Type.CanCastTo(comptype));
						if (bidx != -1)
						{
							error = $"Type constructor argument {bidx} must be a '{comptype}' compatible non-array scalar type.";
							return false;
						}
						return true;
					}
					else // Some mix of other argument types
					{
						var bidx = args.FindIndex(arg => arg.Type.IsMatrixType() || !arg.Type.GetComponentType().CanCastTo(comptype));
						if (bidx != -1)
						{
							error = $"The type constructor argument {bidx} must be a '{comptype}' compatible non-array scalar or vector type.";
							return false;
						}
						var csum = args.Sum(arg => arg.Type.GetVectorSize());
						if (csum != ccount)
						{
							error = $"The type constructor arguments must provide the exact numer of components required ({ccount} != {csum}).";
							return false;
						}
						return true;
					}
				}
			}
			else // Matrices
			{
				if (args.Count == 1) // Constructing a diagonal matrix, or a direct matrix cast
				{
					if (args[0].Type.IsMatrixType())
						return true; // Direct matrix casts always work

					if (!args[0].Type.IsScalarType() || !args[0].Type.CanCastTo(ShaderType.Float))
					{
						error = $"Diagonal matrices can only be constructed from capatible scalar types.";
						return false;
					}

					return true;
				}

				// Simply need enough compatible arguments to fill each matrix component exactly
				var bidx = args.FindIndex(arg => arg.Type.IsMatrixType() || !arg.Type.GetComponentType().CanCastTo(ShaderType.Float));
				if (bidx != -1)
				{
					error = $"The matrix constructor argument {bidx} must be a 'Float' compatible scalar or vector type.";
					return false;
				}
				var csum = args.Sum(arg => arg.Type.GetVectorSize());
				var tsum = (type == ShaderType.Mat2) ? 4 : (type == ShaderType.Mat3) ? 9 : 16;
				if (tsum != csum)
				{
					error = $"Matrix constructors must be given exactly enough arguments to fill their components ({csum} != {tsum}).";
					return false;
				}

				return true;
			}
		}

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
