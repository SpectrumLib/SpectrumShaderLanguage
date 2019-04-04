﻿using System;

namespace SSLang
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
		Bool,
		/// <summary>
		/// A 2-component vector of booleans (bvec2).
		/// </summary>
		Bool2,
		/// <summary>
		/// A 3-component vector of booleans (bvec3).
		/// </summary>
		Bool3,
		/// <summary>
		/// A 4-component vector of booleans (bvec4).
		/// </summary>
		Bool4,
		/// <summary>
		/// Single signed 32-bit integer value (int).
		/// </summary>
		Int,
		/// <summary>
		/// A 2-component vector of signed 32-bit integers (ivec2).
		/// </summary>
		Int2,
		/// <summary>
		/// A 3-component vector of signed 32-bit integers (ivec3).
		/// </summary>
		Int3,
		/// <summary>
		/// A 4-component vector of signed 32-bit integers (ivec4).
		/// </summary>
		Int4,
		/// <summary>
		/// Single unsigned 32-bit integer value (uint).
		/// </summary>
		UInt,
		/// <summary>
		/// A 2-component vector of unsigned 32-bit integers (uvec2).
		/// </summary>
		UInt2,
		/// <summary>
		/// A 3-component vector of unsigned 32-bit integers (uvec3).
		/// </summary>
		UInt3,
		/// <summary>
		/// A 4-component vector of unsigned 32-bit integers (uvec4).
		/// </summary>
		UInt4,
		/// <summary>
		/// Single 32-bit floating-point value (float).
		/// </summary>
		Float,
		/// <summary>
		/// A 2-component vector of 32-bit floating-point values (vec2).
		/// </summary>
		Float2,
		/// <summary>
		/// A 3-component vector of 32-bit floating-point values (vec3).
		/// </summary>
		Float3,
		/// <summary>
		/// A 4-component vector of 32-bit floating-point values (vec4).
		/// </summary>
		Float4,
		/// <summary>
		/// Single 64-bit floating-point value (double).
		/// </summary>
		Double,
		/// <summary>
		/// A 2-component vector of 64-bit floating-point values (dvec2).
		/// </summary>
		Double2,
		/// <summary>
		/// A 3-component vector of 64-bit floating-point values (dvec3).
		/// </summary>
		Double3,
		/// <summary>
		/// A 4-component vector of 64-bit floating-point values (dvec4).
		/// </summary>
		Double4,
		/// <summary>
		/// A 2x2 matrix of 32-bit floating point values (mat2).
		/// </summary>
		Mat2,
		/// <summary>
		/// A 3x3 matrix of 32-bit floating point values (mat3).
		/// </summary>
		Mat3,
		/// <summary>
		/// A 4x4 matrix of 32-bit floating point values (mat4).
		/// </summary>
		Mat4,
		/// <summary>
		/// A 2x2 matrix of 64-bit floating point values (dmat2).
		/// </summary>
		DMat2,
		/// <summary>
		/// A 3x3 matrix of 64-bit floating point values (dmat3).
		/// </summary>
		DMat3,
		/// <summary>
		/// A 4x4 matrix of 64-bit floating point values (dmat4).
		/// </summary>
		DMat4,

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
		Tex2D,
		/// <summary>
		/// A 3-dimensional combined image/sampler (tex3D).
		/// </summary>
		Tex3D,
		/// <summary>
		/// A cube-map combined image/sampler (texCube).
		/// </summary>
		TexCube,
		/// <summary>
		/// An array of 1-dimensional combined image/samplers (tex1DArray).
		/// </summary>
		Tex1DArray,
		/// <summary>
		/// An array of 2-dimensional combined image/samplers (tex2DArray).
		/// </summary>
		Tex2DArray,
		/// <summary>
		/// A 1-dimensional read/write storage image (image1D).
		/// </summary>
		Image1D,
		/// <summary>
		/// A 2-dimensional read/write storage image (image2D).
		/// </summary>
		Image2D,
		/// <summary>
		/// A 3-dimensional read/write storage image (image3D).
		/// </summary>
		Image3D,
		/// <summary>
		/// An array of 1-dimensional read/write storage images (image1DArray).
		/// </summary>
		Image1DArray,
		/// <summary>
		/// An array of 2-dimensional read/write storage images (image2DArray).
		/// </summary>
		Image2DArray,

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
			"float", "vec2", "vec3", "vec4", "double", "dvec2", "dvec3", "dvec4", "mat2", "mat3", "mat4", "dmat2",
			"dmat3", "dmat4",
			"tex1D", "tex2D", "tex3D", "texCube", "tex1dArray", "tex2DArray", "image1D", "image2D", "image3D",
			"image1dArray", "image2DArray",
		};
		// This must be kept organized to the enums, as it only contains the types that dont match between SSL and GLSL
		internal static readonly string[] GLSL_KEYWORDS = {
			"sampler1D", "sampler2D", "sampler3D", "samplerCube", "sampler1DArray", "sampler2DArray"
		};

		/// <summary>
		/// Gets the SSL keyword that represents the <see cref="ShaderType"/> type.
		/// </summary>
		/// <param name="type">The type to get the keyword for.</param>
		/// <returns>The SSL type keyword.</returns>
		public static string ToKeyword(this ShaderType type)
		{
			if (type <= ShaderType.DMat4) return SSL_KEYWORDS[(int)type];
			else if (type <= ShaderType.Image2DArray) return SSL_KEYWORDS[(int)(type - ShaderType.Tex1D + ShaderType.DMat4) + 1];
			else return "ERROR";
		}

		/// <summary>
		/// Gets the GLSL keyword that represents the <see cref="ShaderType"/> type.
		/// </summary>
		/// <param name="type">The type to get the keyword for.</param>
		/// <returns>The GLSL type keyword.</returns>
		public static string ToGLSL(this ShaderType type)
		{
			if ((type >= ShaderType.Tex1D) && (type <= ShaderType.Tex2DArray)) return GLSL_KEYWORDS[(int)(type - ShaderType.Tex1D)];
			else return ToKeyword(type);
		}
	}
}