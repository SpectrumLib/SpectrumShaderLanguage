using System;
using SSLang.Generated;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents the data and handle types available in SSL.
	/// </summary>
	public enum ShaderType : byte
	{
		/// <summary>
		/// Void ("none") type, only valid as the return type for functions (void).
		/// </summary>
		Void = 0,

		// =====================================================================
		// VALUE TYPES
		// =====================================================================
		/// <summary>
		/// Single boolean value (bool).
		/// </summary>
		Bool = 1,
		/// <summary>
		/// A 2-component vector of booleans (bvec2).
		/// </summary>
		Bool2 = 2,
		/// <summary>
		/// A 3-component vector of booleans (bvec3).
		/// </summary>
		Bool3 = 3,
		/// <summary>
		/// A 4-component vector of booleans (bvec4).
		/// </summary>
		Bool4 = 4,
		/// <summary>
		/// Single signed 32-bit integer value (int).
		/// </summary>
		Int = 5,
		/// <summary>
		/// A 2-component vector of signed 32-bit integers (ivec2).
		/// </summary>
		Int2 = 6,
		/// <summary>
		/// A 3-component vector of signed 32-bit integers (ivec3).
		/// </summary>
		Int3 = 7,
		/// <summary>
		/// A 4-component vector of signed 32-bit integers (ivec4).
		/// </summary>
		Int4 = 8,
		/// <summary>
		/// Single unsigned 32-bit integer value (uint).
		/// </summary>
		UInt = 9,
		/// <summary>
		/// A 2-component vector of unsigned 32-bit integers (uvec2).
		/// </summary>
		UInt2 = 10,
		/// <summary>
		/// A 3-component vector of unsigned 32-bit integers (uvec3).
		/// </summary>
		UInt3 = 11,
		/// <summary>
		/// A 4-component vector of unsigned 32-bit integers (uvec4).
		/// </summary>
		UInt4 = 12,
		/// <summary>
		/// Single 32-bit floating-point value (float).
		/// </summary>
		Float = 13,
		/// <summary>
		/// A 2-component vector of 32-bit floating-point values (vec2).
		/// </summary>
		Float2 = 14,
		/// <summary>
		/// A 3-component vector of 32-bit floating-point values (vec3).
		/// </summary>
		Float3 = 15,
		/// <summary>
		/// A 4-component vector of 32-bit floating-point values (vec4).
		/// </summary>
		Float4 = 16,
		/// <summary>
		/// A 2x2 matrix of 32-bit floating point values (mat2).
		/// </summary>
		Mat2 = 17,
		/// <summary>
		/// A 3x3 matrix of 32-bit floating point values (mat3).
		/// </summary>
		Mat3 = 18,
		/// <summary>
		/// A 4x4 matrix of 32-bit floating point values (mat4).
		/// </summary>
		Mat4 = 19,

		// =====================================================================
		// HANDLE TYPES
		// =====================================================================
		/// <summary>
		/// A 1-dimensional combined image/sampler (tex1D).
		/// </summary>
		Tex1D = 128,
		/// <summary>
		/// A 2-dimensional combined image/sampler (tex2D).
		/// </summary>
		Tex2D = 129,
		/// <summary>
		/// A 3-dimensional combined image/sampler (tex3D).
		/// </summary>
		Tex3D = 130,
		/// <summary>
		/// A cube-map combined image/sampler (texCube).
		/// </summary>
		TexCube = 131,
		/// <summary>
		/// An array of 1-dimensional combined image/samplers (tex1DArray).
		/// </summary>
		Tex1DArray = 132,
		/// <summary>
		/// An array of 2-dimensional combined image/samplers (tex2DArray).
		/// </summary>
		Tex2DArray = 133,
		/// <summary>
		/// A 1-dimensional read/write storage image (image1D).
		/// </summary>
		Image1D = 134,
		/// <summary>
		/// A 2-dimensional read/write storage image (image2D).
		/// </summary>
		Image2D = 135,
		/// <summary>
		/// A 3-dimensional read/write storage image (image3D).
		/// </summary>
		Image3D = 136,
		/// <summary>
		/// An array of 1-dimensional read/write storage images (image1DArray).
		/// </summary>
		Image1DArray = 137,
		/// <summary>
		/// An array of 2-dimensional read/write storage images (image2DArray).
		/// </summary>
		Image2DArray = 138,
		/// <summary>
		/// A render target image from a previous subpass used as an optimized readonly input to the fragment shader.
		/// </summary>
		SubpassInput = 139,

		/// <summary>
		/// Represents a type error. Only used in temporary situations when errors arise, not a valid type for objects.
		/// </summary>
		Error = 0xFF
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="ShaderType"/> values.
	/// </summary>
	public static class ShaderTypeHelper
	{
		// This must be kept in the same order as the enums, as it depends on direct casting to access
		internal static readonly string[] SSL_KEYWORDS = {
			"void",
			"bool", "bvec2", "bvec3", "bvec4", "int", "ivec2", "ivec3", "ivec4", "uint", "uvec2", "uvec3", "uvec4",
			"float", "vec2", "vec3", "vec4", "mat2", "mat3", "mat4",
			"tex1D", "tex2D", "tex3D", "texCube", "tex1dArray", "tex2DArray", "image1D", "image2D", "image3D",
			"image1dArray", "image2DArray", "subpassInput"
		};
		// This must be kept organized to the enums, as it only contains the types that dont match between SSL and GLSL
		internal static readonly string[] GLSL_KEYWORDS = {
			"sampler1D", "sampler2D", "sampler3D", "samplerCube", "sampler1DArray", "sampler2DArray"
		};

		/// <summary>
		/// Gets the SSL keyword that represents the type.
		/// </summary>
		/// <param name="type">The type to get the keyword for.</param>
		/// <returns>The SSL type keyword.</returns>
		public static string ToKeyword(this ShaderType type)
		{
			if (type <= ShaderType.Mat4) return SSL_KEYWORDS[(int)type];
			else if (type <= ShaderType.SubpassInput) return SSL_KEYWORDS[(int)(type - ShaderType.Tex1D + ShaderType.Mat4) + 1];
			else return "ERROR";
		}

		/// <summary>
		/// Gets the GLSL keyword that represents the type.
		/// </summary>
		/// <param name="type">The type to get the keyword for.</param>
		/// <param name="ifmt">The image format for the type, if the type is an image type.</param>
		/// <returns>The GLSL type keyword.</returns>
		public static string ToGLSL(this ShaderType type, ImageFormat ifmt = ImageFormat.Error)
		{
			if ((type >= ShaderType.Tex1D) && (type <= ShaderType.Tex2DArray)) return GLSL_KEYWORDS[(int)(type - ShaderType.Tex1D)];
			else if (type >= ShaderType.Image1D && type <= ShaderType.Image2DArray)
			{
				if (ifmt == ImageFormat.Error) return "ERROR_IMAGE_TYPE";
				var ctype = ifmt.GetComponentType();
				var prefix = (ctype == ShaderType.Int) ? "i" : (ctype == ShaderType.UInt) ? "u" : "";
				return prefix + ToKeyword(type);
			}
			else return ToKeyword(type);
		}

		/// <summary>
		/// Gets the size of the shader type, in bytes.
		/// </summary>
		/// <param name="type">The type to get the size of. All handle types return 4. Void and error types return 0.</param>
		/// <returns>The type size.</returns>
		public static uint GetSize(this ShaderType type)
		{
			if (type == ShaderType.Void || type == ShaderType.Error)
				return 0;
			if (type >= ShaderType.Tex1D) // All handle types
				return 4;

			if (type >= ShaderType.Mat2) // All matrix types
				return (type == ShaderType.Mat2) ? 16u : (type == ShaderType.Mat3) ? 36u : 64u;

			// All other value types (easy, as all are 32-bits internally)
			var vecSize = (int)(type - ShaderType.Bool) % 4;
			return (uint)(4 * (vecSize + 1));
		}

		/// <summary>
		/// Gets the number of location slots the type takes up in GLSL (each slot is 16 bytes).
		/// </summary>
		/// <param name="type">The type to get the slot count for.</param>
		/// <param name="arrSize">The size of the array, or 0 if there is no array.</param>
		/// <returns>The number of 16-byte 'location' slots the type takes.</returns>
		public static uint GetSlotCount(this ShaderType type, uint arrSize = 0)
		{
			arrSize = Math.Max(arrSize, 1);
			return (uint)Math.Ceiling(GetSize(type) / 16f) * arrSize;
		}

		/// <summary>
		/// Gets if the type is a value type (numeric/boolean values or collections).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a value type.</returns>
		public static bool IsValueType(this ShaderType type) => (type >= ShaderType.Bool) && (type <= ShaderType.Mat4);

		/// <summary>
		/// Gets if the type is a handle type (samplers/images/buffers).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a handle type.</returns>
		public static bool IsHandleType(this ShaderType type) => (type >= ShaderType.Tex1D) && (type <= ShaderType.SubpassInput);

		/// <summary>
		/// Gets if the type is an error type, which represents an error and not an actual valid type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type represents an error.</returns>
		public static bool IsError(this ShaderType type) => (type == ShaderType.Error);

		/// <summary>
		/// Gets if the type is the void type, which represents a none-type or un-typed expression.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is the void type.</returns>
		public static bool IsVoid(this ShaderType type) => (type == ShaderType.Void);

		/// <summary>
		/// Gets if the type represents a vector type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a vector.</returns>
		public static bool IsVectorType(this ShaderType type)
		{
			if (type == ShaderType.Void || type == ShaderType.Error)
				return false;
			if (type <= ShaderType.Float4) return (((int)type % 4) != 1);
			return false;
		}

		/// <summary>
		/// Gets the number of components in the type. If the type is a scalar type, then this function returns 1.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>The number of vector components, or 1.</returns>
		public static uint GetVectorSize(this ShaderType type)
		{
			if (type == ShaderType.Void || type == ShaderType.Error)
				return 0;
			if (type <= ShaderType.Float4) return (uint)(((int)type - 1) % 4) + 1;
			return 1;
		}

		/// <summary>
		/// Gets if the type is one of the matrix types.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a matrix.</returns>
		public static bool IsMatrixType(this ShaderType type)
		{
			return (type >= ShaderType.Mat2) && (type <= ShaderType.Mat4);
		}

		/// <summary>
		/// Gets if the type is one of the scalar types.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a one component scalar.</returns>
		public static bool IsScalarType(this ShaderType type)
		{
			return (type >= ShaderType.Bool) && (type <= ShaderType.Float) && (((int)type % 4) == 1);
		}

		/// <summary>
		/// Gets if the swizzle character is valid for the type. Will always return false for non-vector types or
		/// invalid swizzle characters.
		/// </summary>
		/// <param name="type">The type to check the swizzle against.</param>
		/// <param name="swizzle">
		/// The swizzle character. Must be one of ('x', 'y', 'z', 'w'), ('r', 'g', 'b', 'a'), ('s', 't', 'p', or 'q').
		/// </param>
		/// <returns>If the swizzle character is valid for the type.</returns>
		public static bool IsSwizzleValid(this ShaderType type, char swizzle)
		{
			var sidx = (swizzle == 'x' || swizzle == 'r' || swizzle == 's') ? 1 :
					   (swizzle == 'y' || swizzle == 'g' || swizzle == 't') ? 2 :
					   (swizzle == 'z' || swizzle == 'b' || swizzle == 'p') ? 3 :
					   (swizzle == 'w' || swizzle == 'a' || swizzle == 'q') ? 4 :
					   0;
			if (sidx == 0)
				return false;
			return type.IsVectorType() && (sidx <= type.GetVectorSize());
		}

		/// <summary>
		/// Gets the swizzle text that generates the given type.
		/// </summary>
		/// <param name="type">The type to get the swizzle text for.</param>
		/// <param name="stype">The type of swizzle, 0 = position, 1 = color, 2 = texcoord.</param>
		/// <returns>The swizzle text (without the period), or null if the type is not a vector or scalar type.</returns>
		public static string GetSwizzle(this ShaderType type, uint stype = 0)
		{
			if (type.IsScalarType() || type.IsVectorType())
			{
				var cc = type.GetVectorSize();
				if (cc == 1) return (stype == 0) ? "x" : (stype == 1) ? "r" : "s";
				if (cc == 2) return (stype == 0) ? "xy" : (stype == 1) ? "rg" : "st";
				if (cc == 3) return (stype == 0) ? "xyz" : (stype == 1) ? "rgb" : "stp";
				if (cc == 4) return (stype == 0) ? "xyzw" : (stype == 1) ? "rgba" : "stpq";
			}
			return null;
		}

		/// <summary>
		/// Gets the type of the components of a vector type.
		/// </summary>
		/// <param name="type">The type to get the component type of.</param>
		/// <returns>The component type, or the same type if the type is not a vector.</returns>
		public static ShaderType GetComponentType(this ShaderType type)
		{
			if (type == ShaderType.Void || type == ShaderType.Error)
				return type;
			if (type <= ShaderType.Float4) return (ShaderType)(((((int)type - 1) / 4) * 4) + 1);
			return type;
		}

		/// <summary>
		/// Expands/resizes between scalar and vector types to the given number of components.
		/// </summary>
		/// <param name="type">The scalar type to expand, or vector type to resize.</param>
		/// <param name="count">The number of components. Must be between 1 and 4, inclusive.</param>
		/// <returns>The expanded type, or the same type if the type cannot be expanded.</returns>
		public static ShaderType ToVectorType(this ShaderType type, uint count)
		{
			if (type == ShaderType.Void || type == ShaderType.Error)
				return type;
			if (type <= ShaderType.Float4) return (ShaderType)((uint)GetComponentType(type) + (count - 1));
			return type;
		}

		/// <summary>
		/// Checks if the first type can be automatically promoted to the second type through implicit casting.
		/// </summary>
		/// <param name="srcType">The source type to cast from.</param>
		/// <param name="dstType">The destination type to cast to.</param>
		/// <returns>If the implicit cast dstType(srcType) is valid.</returns>
		public static bool CanCastTo(this ShaderType srcType, ShaderType dstType)
		{
			if (srcType == dstType) return true;
			if (srcType.GetVectorSize() != dstType.GetVectorSize())
				return false; // Can only cast between value types of the same size
			if (srcType.IsVectorType())
			{
				var dct = dstType.GetComponentType();
				switch (srcType.GetComponentType())
				{
					case ShaderType.Int: return (dct == ShaderType.UInt) || (dct == ShaderType.Float);
					case ShaderType.UInt: return (dct == ShaderType.Float);
					default: return false;
				}
			}
			else
			{
				switch (srcType)
				{
					case ShaderType.Int: return (dstType == ShaderType.UInt) || (dstType == ShaderType.Float);
					case ShaderType.UInt: return (dstType == ShaderType.Float);
					default: return false;
				}
			}
		}

		/// <summary>
		/// Gets the number of dimensions for handle types that hold texel data.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>The number of dimensions, or 0 for non-texel-data-handle types.</returns>
		public static uint GetTexelDim(this ShaderType type)
		{
			switch (type)
			{
				case ShaderType.Tex1D:
				case ShaderType.Image1D:
					return 1;
				case ShaderType.Tex2D:
				case ShaderType.Tex1DArray:
				case ShaderType.TexCube:
				case ShaderType.Image2D:
				case ShaderType.Image1DArray:
				case ShaderType.SubpassInput:
					return 2;
				case ShaderType.Tex3D:
				case ShaderType.Tex2DArray:
				case ShaderType.Image3D:
				case ShaderType.Image2DArray:
					return 3;
				default: return 0;
			}
		}

		/// <summary>
		/// Gets if the type is a handle to a sampled texture.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a sampled texture handle.</returns>
		public static bool IsTextureHandle(this ShaderType type) => (type >= ShaderType.Tex1D) && (type <= ShaderType.Tex2DArray);

		/// <summary>
		/// Gets if the type is a handle to a texel storage image.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a storage image handle.</returns>
		public static bool IsImageHandle(this ShaderType type) => (type >= ShaderType.Image1D) && (type <= ShaderType.Image2DArray);

		/// <summary>
		/// Gets if the type is a handle to a subpass input image.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a subpass input handle.</returns>
		public static bool IsSubpassInput(this ShaderType type) => type == ShaderType.SubpassInput;

		// Used to convert parsed tokens into enum values
		// This function relies on the enum being in the same order and having the same contiguous blocks as the grammar
		internal static ShaderType FromTypeContext(SSLParser.TypeContext ctx)
		{
			var token = ctx.Start.Type;

			if (token == SSLParser.KWT_VOID) return ShaderType.Void;
			if (token <= SSLParser.KWT_MAT4) return (ShaderType)((token - SSLParser.KWT_BOOL) + (int)ShaderType.Bool); // Value types
			if (token <= SSLParser.KWT_SUBPASSINPUT) return (ShaderType)((token - SSLParser.KWT_TEX1D) + (int)ShaderType.Tex1D); // Handle types

			return ShaderType.Error;
		}
	}
}
