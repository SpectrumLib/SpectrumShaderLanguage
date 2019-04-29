using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// The texel formats that storage images can be accessed with in SSL.
	/// </summary>
	public enum ImageFormat
	{
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit floats (<see cref="ShaderType.Float4"/>).
		/// </summary>
		F4 = 0,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit signed integers (<see cref="ShaderType.Int4"/>).
		/// </summary>
		I4 = 1,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit unsigned integers (<see cref="ShaderType.UInt4"/>).
		/// </summary>
		U4 = 2,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit floats (<see cref="ShaderType.Float2"/>).
		/// </summary>
		F2 = 3,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit signed integers (<see cref="ShaderType.Int2"/>).
		/// </summary>
		I2 = 4,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit unsigned integers (<see cref="ShaderType.UInt2"/>).
		/// </summary>
		U2 = 5,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit floats (<see cref="ShaderType.Float"/>).
		/// </summary>
		F1 = 6,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit signed integers (<see cref="ShaderType.Int"/>).
		/// </summary>
		I1 = 7,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit unsigned integers (<see cref="ShaderType.UInt"/>).
		/// </summary>
		U1 = 8
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="ImageFormat"/> values.
	/// </summary>
	public static class ImageFormatHelper
	{
		// This must be kept in the same order as the enums, as it depends on direct casting to access
		internal static readonly string[] SSL_KEYWORDS = {
			"f4", "i4", "u4", "f2", "i2", "u2", "f1", "i1", "u1"
		};
		// This must be kept in the same order as the enums, as it depends on direct casting to access
		internal static readonly string[] GLSL_KEYWORDS = {
			"rgba32f", "rgba32i", "rgba32ui", "rg32f", "rg32i", "rg32ui", "r32f", "r32i", "r32ui"
		};

		// Gets the SSL qualifier that represents the format
		public static string ToSSLKeyword(this ImageFormat fmt) => SSL_KEYWORDS[(int)fmt];

		// Gets the GLSL qualifier that represents the format
		public static string ToGLSLKeyword(this ImageFormat fmt) => GLSL_KEYWORDS[(int)fmt];

		/// <summary>
		/// Gets the number of color channels per texel for the format.
		/// </summary>
		/// <param name="fmt">The format to get the channel count for.</param>
		/// <returns>The format channel count.</returns>
		public static uint GetChannelCount(this ImageFormat fmt)
		{
			if (fmt <= ImageFormat.U4) return 4;
			if (fmt <= ImageFormat.U2) return 2;
			if (fmt <= ImageFormat.U1) return 1;
			return 0;
		}

		/// <summary>
		/// Gets the underlying <see cref="ShaderType"/> that the texel channels are comprised of.
		/// </summary>
		/// <param name="fmt">The format to get the component type for.</param>
		/// <returns>The component type of the format.</returns>
		public static ShaderType GetComponentType(this ImageFormat fmt)
		{
			switch (((int)fmt) % 3)
			{
				case 0: return ShaderType.Float;
				case 1: return ShaderType.Int;
				case 2: return ShaderType.UInt;
				default: return ShaderType.Void; // Never reached
			}
		}

		/// <summary>
		/// Gets the <see cref="ShaderType"/> that represents the texel format.
		/// </summary>
		/// <param name="fmt">The format to get the type for.</param>
		/// <returns>The type representing the format.</returns>
		public static ShaderType GetTexelType(this ImageFormat fmt) => GetComponentType(fmt).ToVectorType(GetChannelCount(fmt)).Value;
	}
}
