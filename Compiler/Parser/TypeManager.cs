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
