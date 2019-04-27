using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents the built-in types in the SSL specification.
	/// <para>
	/// The types are generally split into two groups: value types and handle types. Value types are raw data, such as
	/// vectors, and handle types are references to resources, such as images and textures.
	/// </para>
	/// </summary>
	/// <remarks>
	/// These must remain contiguous and in the same order as the type tokens in the SSL grammar.
	/// </remarks>
	public enum ShaderType
	{
		/// <summary>
		/// Void ("none") type.
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
		SubpassInput = 139
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="ShaderType"/> values.
	/// </summary>
	public static class ShaderTypeHelper
	{
		// Constant reference points in the types enum to make adding new types in the future easier, and the helper
		//    functions below be less error prone
		internal const ShaderType VALUE_TYPE_START = ShaderType.Bool;
		internal const ShaderType VALUE_TYPE_END = ShaderType.Mat4;
		internal const ShaderType HANDLE_TYPE_START = ShaderType.Tex1D;
		internal const ShaderType HANDLE_TYPE_END = ShaderType.SubpassInput;
		internal const ShaderType TEX_TYPE_START = ShaderType.Tex1D;
		internal const ShaderType TEX_TYPE_END = ShaderType.Tex2DArray;
		internal const ShaderType IMG_TYPE_START = ShaderType.Image1D;
		internal const ShaderType IMG_TYPE_END = ShaderType.Image2DArray;
		internal const ShaderType SCALAR_VECTOR_TYPE_START = ShaderType.Bool;
		internal const ShaderType SCALAR_VECTOR_TYPE_END = ShaderType.Float4;
		internal const ShaderType MATRIX_TYPE_START = ShaderType.Mat2;
		internal const ShaderType MATRIX_TYPE_END = ShaderType.Mat4;
		internal const ShaderType TEXEL_DATA_START = ShaderType.Tex1D;
		internal const ShaderType TEXEL_DATA_END = ShaderType.Image2DArray;

		#region Type Checking
		/// <summary>
		/// Gets if the type is the void type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is the void type.</returns>
		public static bool IsVoid(this ShaderType type) => (type == ShaderType.Void);

		/// <summary>
		/// Gets if the type is a value type (raw data, such as vectors or matrices).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a value type.</returns>
		public static bool IsValueType(this ShaderType type) => (type >= VALUE_TYPE_START) && (type <= VALUE_TYPE_END);

		/// <summary>
		/// Gets if the type is a handle type (references to resources, such as images and textures).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a handle type.</returns>
		public static bool IsHandleType(this ShaderType type) => (type >= HANDLE_TYPE_START) && (type <= HANDLE_TYPE_END);

		/// <summary>
		/// Gets if the type represents a scalar type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a scalar type.</returns>
		public static bool IsScalarType(this ShaderType type) => (type <= SCALAR_VECTOR_TYPE_END) && (((int)type % 4) == 1);

		/// <summary>
		/// Gets if the type represents a vector type. Scalars, which can sometimes act like one-component vectors,
		/// are nevertheless not vectors.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a vector type.</returns>
		public static bool IsVectorType(this ShaderType type) =>
			(type != ShaderType.Void) && (type <= SCALAR_VECTOR_TYPE_END) && (((int)type % 4) != 1);

		/// <summary>
		/// Gets if the type represents a matrix type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a matrix type.</returns>
		public static bool IsMatrixType(this ShaderType type) => (type >= MATRIX_TYPE_START) && (type <= MATRIX_TYPE_END);

		/// <summary>
		/// Gets if the type represents a handle to a sampled texture.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a sampled texture handle.</returns>
		public static bool IsTextureType(this ShaderType type) => (type >= TEX_TYPE_START) && (type <= TEX_TYPE_END);

		/// <summary>
		/// Gets if the type represents a handle to a storage image.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is a storage image handle.</returns>
		public static bool IsImageType(this ShaderType type) => (type >= IMG_TYPE_START) && (type <= IMG_TYPE_END);

		/// <summary>
		/// Gets if the type is the subpass input handle type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>If the type is the subpass input handle type.</returns>
		public static bool IsSubpassInput(this ShaderType type) => (type == ShaderType.SubpassInput);
		#endregion // Type Checking

		#region Sizing
		/// <summary>
		/// Gets the size of the shader type, in bytes. Note that this will match the type sizes on the CPU in most
		/// cases, except for booleans, which will always be 4 bytes in SSL but may be a different size on the CPU.
		/// </summary>
		/// <param name="type">The type to get the size of.</param>
		/// <returns>The type size. All handle types return 8. The void type returns 0.</returns>
		public static uint GetSize(this ShaderType type)
		{
			if (type == ShaderType.Void)
				return 0;
			if (type >= HANDLE_TYPE_START) // All handle types
				return 8;
			if (type >= MATRIX_TYPE_START) // All matrix types
				return (type == ShaderType.Mat2) ? 16u : (type == ShaderType.Mat3) ? 36u : 64u;

			// All other value types (only works as long as all types are 32-bits)
			var vecSize = (type - SCALAR_VECTOR_TYPE_START) % 4;
			return (uint)(4 * (vecSize + 1));
		}

		/// <summary>
		/// Gets the number of binding slots the type takes up in the graphics hardware.
		/// <para>
		/// Each slot is 16 bytes wide, but adjacent values do not pack. Additionally, each array component takes up
		/// its own set of slots. This means that any value will take at least one entire slot, and an array of N
		/// components will take at least N slots, regardless of the component size.
		/// </para>
		/// </summary>
		/// <param name="type">The type to get the slot count for.</param>
		/// <param name="arraySize">The size of the array, or 0 if not checking for an array.</param>
		/// <returns>The number of binding slots the type takes.</returns>
		public static uint GetSlotCount(this ShaderType type, uint arraySize = 0) => 
			(uint)Math.Ceiling(GetSize(type) / 16f) * Math.Max(arraySize, 1);

		/// <summary>
		/// Gets the number of components that make up a value type.
		/// </summary>
		/// <param name="type">The type to get the component count for.</param>
		/// <returns>The number of components in the value type. Non-value types will return 0.</returns>
		public static uint GetComponentCount(this ShaderType type)
		{
			if (type == ShaderType.Void || type >= HANDLE_TYPE_START)
				return 0;

			if (type >= SCALAR_VECTOR_TYPE_START || type <= SCALAR_VECTOR_TYPE_END)
				return (uint)(((int)type - 1) % 4) + 1;
			return (type == ShaderType.Mat2) ? 4u : (type == ShaderType.Mat3) ? 9u : 16u;
		}

		/// <summary>
		/// Gets the dimensionality for types that hold texel data.
		/// </summary>
		/// <param name="type">The type to get the dimensionality of.</param>
		/// <returns>The dimensionality of the texel data for the type. Returns 0 for types that don't hold texel data.</returns>
		public static uint GetTexelDim(this ShaderType type)
		{
			switch (type)
			{
				case ShaderType.Tex1D:
				case ShaderType.Image1D:
					return 1;
				case ShaderType.Tex2D:
				case ShaderType.Tex1DArray:
				case ShaderType.Image2D:
				case ShaderType.Image1DArray:
				case ShaderType.SubpassInput:
					return 2;
				case ShaderType.Tex3D:
				case ShaderType.TexCube:
				case ShaderType.Tex2DArray:
				case ShaderType.Image3D:
				case ShaderType.Image2DArray:
					return 3;
				default: return 0;
			}
		}
		#endregion // Sizing

		#region Casting
		/// <summary>
		/// Gets the base type of the components of a value type.
		/// </summary>
		/// <param name="type">The type to get the component type for.</param>
		/// <returns>The component type for value types. Non-value types will return themselves.</returns>
		public static ShaderType GetComponentType(this ShaderType type)
		{
			if (type == ShaderType.Void || type >= HANDLE_TYPE_START)
				return type;

			if (type >= SCALAR_VECTOR_TYPE_START || type <= SCALAR_VECTOR_TYPE_END)
				return (ShaderType)(((((int)type - 1) / 4) * 4) + 1);
			return ShaderType.Float; // Matrices
		}

		/// <summary>
		/// Converts the scalar type into a vector type with the given number of components of the scalar type.
		/// </summary>
		/// <param name="type">The scalar type to convert to a vector type.</param>
		/// <param name="vectorSize">The size of the new vector type.</param>
		/// <returns>
		/// The scalar type as a vector type. If the type is not a scalar type, or if the vector size is not in the
		/// range [1, 4], <c>null</c> is returned.
		/// </returns>
		public static ShaderType? ToVectorType(this ShaderType type, uint vectorSize)
		{
			if (!IsScalarType(type) || vectorSize < 1 || vectorSize > 4)
				return null;

			return (ShaderType)((int)type + (vectorSize - 1));
		}

		/// <summary>
		/// Gets if the source type can be implicitly cast to the destination type in SSL.
		/// <para>
		/// Summary: Non-value types can only cast to themselves. Any matrix type can cast to any other matrix type.
		/// Value types follow the promotion order int -> uint -> float and must have the same number of components. 
		/// Booleans cannot promote.
		/// </para>
		/// </summary>
		/// <param name="srcType">The source type.</param>
		/// <param name="dstType">The destination type.</param>
		/// <returns>If the source type can be implicitly cast to the destination type.</returns>
		public static bool CanPromoteTo(this ShaderType srcType, ShaderType dstType)
		{
			if (srcType == dstType)
				return true;
			if (!IsValueType(srcType) || !IsValueType(dstType)) // No handle/void type casting allowed
				return false;

			if (IsMatrixType(srcType))
				return IsMatrixType(dstType);
			else
			{
				if (IsMatrixType(dstType))
					return false;
				if (GetComponentCount(srcType) != GetComponentCount(dstType))
					return false;

				var srcc = GetComponentType(srcType);
				var dstc = GetComponentType(dstType);
				if (srcc == ShaderType.Bool || dstc == ShaderType.Bool)
					return false;
				return srcc <= dstc; // Only works until we add more value types that are out of promotion order
			}
		}
		#endregion // Casting
	}
}
