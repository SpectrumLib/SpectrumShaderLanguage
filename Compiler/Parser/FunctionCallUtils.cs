using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Contains code for checking and validating function calls and type construction
	internal static class FunctionCallUtils
	{
		// Checks the arugments and return types for built in functions
		// Note: the visitor ensures that the correct number of args are present, we dont need to check that in this function
		public static ShaderType CheckBuiltinCall(SSLVisitor vis, IToken token, string name, int type, ExprResult[] args)
		{
			var btidx = Array.FindIndex(args, a => a.IsArray);
			if (btidx != -1)
				vis._THROW(token, $"Arguments to built-in functions cannot be arrays (arg {btidx+1}).");

			ShaderType a1t = args[0].Type,
					   a2t = (args.Length > 1) ? args[1].Type : ShaderType.Void,
					   a3t = (args.Length > 2) ? args[2].Type : ShaderType.Void;
			ShaderType a1c = a1t.GetComponentType(),
					   a2c = a2t.GetComponentType(),
					   a3c = a3t.GetComponentType();

			if (type >= SSLParser.BIF_TEXSIZE && type <= SSLParser.BIF_TEXFETCH) // Functions that deal with texture handles
			{
				if (!a1t.IsTextureType())
					vis._THROW(token, $"The built-in function '{name}' requires a texture handle as argument 1.");
				var tdim = a1t.GetTexelDim();

				if (type == SSLParser.BIF_TEXSIZE)
					return ShaderType.Int.ToVectorType(tdim).Value;
				else if (type == SSLParser.BIF_TEXTURE)
				{
					if (!a2c.CanPromoteTo(ShaderType.Float))
						vis._THROW(token, $"The built-in function '{name}' requires a floating-point vector or scalar as argument 2.");
					if (a2t.GetComponentCount() != tdim)
						vis._THROW(token, "The size of the texture access coordinates does not match the size of the texture.");
				}
				else
				{
					if (a2c != ShaderType.Int)
						vis._THROW(token, $"The built-in function '{name}' requires integer texture coordinates.");
					if (a2t.GetComponentCount() != tdim)
						vis._THROW(token, "The size of the texture access coordinates does not match the size of the texture.");
				}
				return ShaderType.Float4;
			}
			else if (type >= SSLParser.BIF_IMAGESIZE && type <= SSLParser.BIF_IMAGESTORE) // Functions that deal with image handles
			{
				if (!a1t.IsImageType())
					vis._THROW(token, $"The built-in function '{name}' requires an image handle as argument 1.");
				var tdim = a1t.GetTexelDim();

				if (type == SSLParser.BIF_IMAGESIZE)
					return ShaderType.Int.ToVectorType(tdim).Value;

				if (a2c != ShaderType.Int)
					vis._THROW(token, $"The built-in function '{name}' requires integer texture coordinates.");
				if (a2t.GetComponentCount() != tdim)
					vis._THROW(token, "The size of the texture access coordinates does not match the size of the texture.");

				var ttype = args[0].ImageFormat.Value;
				if ((type == SSLParser.BIF_IMAGESTORE) && !a3t.CanPromoteTo(ttype.GetTexelType()))
					vis._THROW(token, $"The built-in function '{name}' requires the type {ttype.GetTexelType()} for argument 3.");
				return (type == SSLParser.BIF_IMAGESTORE) ? ShaderType.Void : ttype.GetTexelType();
			}
			else if (type >= SSLParser.BIF_MATCOMPMUL && type <= SSLParser.BIF_DETERMINANT) // Functions that deal with matrices
			{
				btidx = Array.FindIndex(args, a => !a.Type.IsMatrixType());
				if (btidx != -1)
					vis._THROW(token, $"The built-in function '{name}' does not support non-matrix arguments (arg {btidx+1}).");

				if (type == SSLParser.BIF_MATCOMPMUL) // 'matCompMul' function
				{
					if (a1t != a2t)
						vis._THROW(token, $"The built-in function '{name}' expects two identically sized matrix types for both arguments.");
					return a1t;
				}
				else if (type == SSLParser.BIF_TRANSPOSE || type == SSLParser.BIF_INVERSE) // 'transpose' and 'inverse' functions
					return a1t;
				else if (type == SSLParser.BIF_DETERMINANT) // 'determinant' function
					return ShaderType.Float;
			}
			else if (type == SSLParser.BIF_SUBPASSLOAD) // 'subpassLoad' function
			{
				if (!a1t.IsSubpassInput())
					vis._THROW(token, "The 'subpassLoad' function requires a subpass input as an argument.");
				return ShaderType.Float4;
			}
			else // Functions that deal with scalar and vector types
			{
				btidx = Array.FindIndex(args, a => !(a.Type.IsScalarType() || a.Type.IsVectorType()));
				if (btidx != -1)
					vis._THROW(token, $"The built-in function '{name}' does not support handle or matrix arguments (arg {btidx+1}).");

				if (type >= SSLParser.BIF_DEG2RAD && type <= SSLParser.BIF_FRACT) // trig, exponential, and 1-arg common functions
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_MOD) // 'mod' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureSizeIfNotScalar(vis, token, name, a1t, a2t, 1, 2);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_MIN || type == SSLParser.BIF_MAX) // 'min' and 'max' functions
				{
					EnsureCastableComponents(vis, token, name, a1c, ShaderType.Void, ShaderType.Void, ShaderType.Float);
					EnsureMatchingComponents(vis, token, name, a1c, a2c, a3c);
					EnsureSizeIfNotScalar(vis, token, name, a1t, a2t, 1, 2);
					return a1t;
				}
				else if (type == SSLParser.BIF_CLAMP) // 'clamp' function
				{
					EnsureCastableComponents(vis, token, name, a1c, ShaderType.Void, ShaderType.Void, ShaderType.Float);
					EnsureMatchingComponents(vis, token, name, a1c, a2c, a3c);
					EnsureVectorSizes(vis, token, name, ShaderType.Void, a2t, a3t);
					EnsureSizeIfNotScalar(vis, token, name, a1t, a2t, 1, 2);
					return a1t;
				}
				else if (type == SSLParser.BIF_MIX) // 'mix' function, unlike GLSL this only supports floating points
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, ShaderType.Void);
					EnsureSizeIfNotScalar(vis, token, name, a1t, a3t, 1, 3);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_STEP) // 'step' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureSizeIfNotScalar(vis, token, name, a2t, a1t, 2, 1);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_SSTEP) // 'smoothstep' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					EnsureSizeIfNotScalar(vis, token, name, a2t, a1t, 2, 1);
					return ShaderType.Float.ToVectorType(a3t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_LENGTH) // 'length' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					return ShaderType.Float;
				}
				else if (type == SSLParser.BIF_DISTANCE || type == SSLParser.BIF_DOT) // 'distance' and 'dot' functions
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Float;
				}
				else if (type == SSLParser.BIF_CROSS) // 'cross' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					if (a1t.GetComponentCount() != 3 || a2t.GetComponentCount() != 3)
						vis._THROW(token, $"The built-in function '{name}' expects 3-component floating-point vector arguments.");
					return ShaderType.Float3;
				}
				else if (type == SSLParser.BIF_NORMALIZE) // 'normalize' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_FFORWARD) // 'faceforward' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_REFLECT) // 'reflect' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_REFRACT) // 'refract' function
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, ShaderType.Void);
					if (!a3t.CanPromoteTo(ShaderType.Float))
						vis._THROW(token, $"The built-in function '{name}' expects a floating-point scalar for argument 3.");
					return ShaderType.Float.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type >= SSLParser.BIF_VECLT && type <= SSLParser.BIF_VECGE) // 2-arg relational vector functions
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Float);
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Bool.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type == SSLParser.BIF_VECEQ || type == SSLParser.BIF_VECNE) // 2-arg logical vector functions
				{
					if ((a1c == ShaderType.Bool) || a1c.CanPromoteTo(ShaderType.Float))
						EnsureMatchingComponents(vis, token, name, a1c, a2c, a3c);
					else
						vis._THROW(token, $"The built-in function '{name}' expects floating-point or boolean vectors for all arguments.");
					EnsureVectorSizes(vis, token, name, a1t, a2t, a3t);
					return ShaderType.Bool.ToVectorType(a1t.GetComponentCount()).Value;
				}
				else if (type >= SSLParser.BIF_VECANY && type <= SSLParser.BIF_VECNOT) // 1-arg logical vector functions
				{
					EnsureCastableComponents(vis, token, name, a1c, a2c, a3c, ShaderType.Bool);
					return ShaderType.Bool;
				}
			}

			// Error
			vis._THROW(token, $"The built-in function '{name}' was not understood.");
			return ShaderType.Void;
		}

		// These functions provide common checks for built-in function argument types
		// They are safe for nearly all cases of argument lengths, and shouldnt give false reports in general cases
		// ShaderType.Error will disable checks against that specific argument
		private static void EnsureCastableComponents(SSLVisitor vis, IToken token, string name, ShaderType a1c, ShaderType a2c, ShaderType a3c, ShaderType dstType)
		{
			if (!a1c.CanPromoteTo(dstType))
				vis._THROW(token, $"The built-in function '{name}' requires a '{dstType}' compatible type for argument 1 (actual is '{a1c}').");
			if ((a2c != ShaderType.Void) && !a2c.CanPromoteTo(dstType))
				vis._THROW(token, $"The built-in function '{name}' requires a '{dstType}' compatible type for argument 2 (actual is '{a2c}').");
			if ((a3c != ShaderType.Void) && !a3c.CanPromoteTo(dstType))
				vis._THROW(token, $"The built-in function '{name}' requires a '{dstType}' compatible type for argument 3 (actual is '{a3c}').");
		}
		private static void EnsureMatchingComponents(SSLVisitor vis, IToken token, string name, ShaderType a1c, ShaderType a2c, ShaderType a3c)
		{
			if (!a2c.CanPromoteTo(a1c))
				vis._THROW(token, $"The built-in function '{name}' requires a '{a1c}' compatible type for argument 2 (actual is '{a2c}').");
			if ((a3c != ShaderType.Void) && !a3c.CanPromoteTo(a1c))
				vis._THROW(token, $"The built-in function '{name}' requires a '{a1c}' compatible type for argument 3 (actual is '{a3c}').");
		}
		private static void EnsureVectorSizes(SSLVisitor vis, IToken token, string name, ShaderType a1t, ShaderType a2t, ShaderType a3t)
		{
			if ((a1t != ShaderType.Void) && (a2t != ShaderType.Void) && (a2t.GetComponentCount() != a1t.GetComponentCount()))
				vis._THROW(token, $"The built-in function '{name}' requires matching vector sizes for arguments 1 and 2.");
			if ((a1t != ShaderType.Void) && (a3t != ShaderType.Void) && (a3t.GetComponentCount() != a1t.GetComponentCount()))
				vis._THROW(token, $"The built-in function '{name}' requires matching vector sizes for arguments 1 and 3.");
			if ((a2t != ShaderType.Void) && (a3t != ShaderType.Void) && (a3t.GetComponentCount() != a2t.GetComponentCount()))
				vis._THROW(token, $"The built-in function '{name}' requires matching vector sizes for arguments 2 and 3.");
		}
		private static void EnsureSizeIfNotScalar(SSLVisitor vis, IToken token, string name, ShaderType vtype, ShaderType ctype, int vp, int cp)
		{
			if (!ctype.IsScalarType() && (ctype.GetComponentCount() != vtype.GetComponentCount()))
				vis._THROW(token, $"The built-in function '{name}' requires argument {cp} to be a scalar, or a matching vector size to argument {vp}.");
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
				error = $"Type construction argument {badarg} cannot be an array or handle type.";
				return false;
			}

			if (!type.IsMatrixType()) // Vectors and scalars
			{
				var ccount = type.GetComponentCount();
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
							if (!args[0].Type.CanPromoteTo(comptype))
							{
								error = $"Cannot construct vector type '{type}' from scalar type '{args[0].Type}'.";
								return false;
							}
							return true;
						}
						if (!args[0].Type.IsVectorType() || (args[0].Type.GetComponentCount() < ccount))
						{
							error = "Can only cast between vector types of the same size or larger (through concatenation).";
							return false;
						}
						if (!args[0].Type.CanPromoteTo(type))
						{
							error = $"No casting rules exist to cast a '{args[0].Type}' vector to a '{type}' vector.";
							return false;
						}
						return true;
					}
					if (args.Count == ccount) // Directly supplying each component
					{
						var bidx = args.FindIndex(arg => !arg.Type.IsScalarType() || !arg.Type.CanPromoteTo(comptype));
						if (bidx != -1)
						{
							error = $"Type constructor argument {bidx} must be a '{comptype}' compatible non-array scalar type.";
							return false;
						}
						return true;
					}
					else // Some mix of other argument types
					{
						var bidx = args.FindIndex(arg => arg.Type.IsMatrixType() || !arg.Type.GetComponentType().CanPromoteTo(comptype));
						if (bidx != -1)
						{
							error = $"The type constructor argument {bidx} must be a '{comptype}' compatible non-array scalar or vector type.";
							return false;
						}
						var csum = args.Sum(arg => arg.Type.GetComponentCount());
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

					if (!args[0].Type.IsScalarType() || !args[0].Type.CanPromoteTo(ShaderType.Float))
					{
						error = $"Diagonal matrices can only be constructed from capatible scalar types.";
						return false;
					}

					return true;
				}

				// Simply need enough compatible arguments to fill each matrix component exactly
				var bidx = args.FindIndex(arg => arg.Type.IsMatrixType() || !arg.Type.GetComponentType().CanPromoteTo(ShaderType.Float));
				if (bidx != -1)
				{
					error = $"The matrix constructor argument {bidx} must be a 'Float' compatible scalar or vector type.";
					return false;
				}
				var csum = args.Sum(arg => arg.Type.GetComponentCount());
				var tsum = (type == ShaderType.Mat2) ? 4 : (type == ShaderType.Mat3) ? 9 : 16;
				if (tsum != csum)
				{
					error = $"Matrix constructors must be given exactly enough arguments to fill their components ({csum} != {tsum}).";
					return false;
				}

				return true;
			}
		}
	}
}
