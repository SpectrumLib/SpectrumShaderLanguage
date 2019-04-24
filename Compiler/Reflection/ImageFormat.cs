using System;
using SSLang.Generated;

namespace SSLang.Reflection
{
	/// <summary>
	/// The texel layout formats that storage images can use in shaders.
	/// </summary>
	public enum ImageFormat : byte
	{
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit floats (<see cref="ShaderType.Float4"/>).
		/// </summary>
		RGBA_F = 0,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit signed integers (<see cref="ShaderType.Int4"/>).
		/// </summary>
		RGBA_I,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit unsigned integers (<see cref="ShaderType.UInt4"/>).
		/// </summary>
		RGBA_U,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit floats (<see cref="ShaderType.Float2"/>).
		/// </summary>
		RG_F,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit signed integers (<see cref="ShaderType.Int2"/>).
		/// </summary>
		RG_I,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit unsigned integers (<see cref="ShaderType.UInt2"/>).
		/// </summary>
		RG_U,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit floats (<see cref="ShaderType.Float"/>).
		/// </summary>
		R_F,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit signed integers (<see cref="ShaderType.Int"/>).
		/// </summary>
		R_I,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit unsigned integers (<see cref="ShaderType.UInt"/>).
		/// </summary>
		R_U,
		/// <summary>
		/// A special type used internally to represent an invalid format, or a handle type that does not have a
		/// shader-defined texel layout.
		/// </summary>
		Error = 255
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="ImageFormat"/> values.
	/// </summary>
	public static class ImageFormatHelper
	{
		// This must be kept in the same order as the enums, as it depends on direct casting to access
		internal static readonly string[] SSL_KEYWORDS = {
			"rgba_f", "rgba_i", "rgba_u", "rg_f", "rg_i", "rg_u", "r_f", "r_i", "r_u"
		};
		// This must be kept in the same order as the enums, as it depends on direct casting to access
		internal static readonly string[] GLSL_KEYWORDS = {
			"rgba32f", "rgba32i", "rgba32ui", "rg32f", "rg32i", "rg32ui", "r32f", "r32i", "r32ui"
		};

		/// <summary>
		/// Gets the SSL qualifier that represents the format.
		/// </summary>
		/// <param name="fmt">The format to get the qualifier for.</param>
		/// <returns>The SSL qualifier keyword.</returns>
		public static string ToKeyword(this ImageFormat fmt) => SSL_KEYWORDS[(int)fmt];

		/// <summary>
		/// Gets the GLSL qualifier that represents the format.
		/// </summary>
		/// <param name="fmt">The format to get the qualifier for.</param>
		/// <returns>The GLSL format qualifier.</returns>
		public static string ToGLSL(this ImageFormat fmt) => GLSL_KEYWORDS[(int)fmt];

		/// <summary>
		/// Gets the number of color channels per texel for the format.
		/// </summary>
		/// <param name="fmt">The format to get the channel count for.</param>
		/// <returns>The format channel count.</returns>
		public static uint GetChannelCount(this ImageFormat fmt)
		{
			if (fmt <= ImageFormat.RGBA_U) return 4;
			if (fmt <= ImageFormat.RG_U) return 2;
			if (fmt <= ImageFormat.R_U) return 1;
			return 0;
		}

		/// <summary>
		/// Gets the underlying <see cref="ShaderType"/> that the texel channels are made of.
		/// </summary>
		/// <param name="fmt">The format to get the component type for.</param>
		/// <returns>The component type of the format.</returns>
		public static ShaderType GetComponentType(this ImageFormat fmt)
		{
			if (fmt == ImageFormat.Error) return ShaderType.Error;
			switch (((int)fmt) % 3)
			{
				case 0: return ShaderType.Float;
				case 1: return ShaderType.Int;
				case 2: return ShaderType.UInt;
				default: return ShaderType.Error; // Never reached
			}
		}

		/// <summary>
		/// Gets the <see cref="ShaderType"/> that represents the texel format.
		/// </summary>
		/// <param name="fmt">The format to get the type for.</param>
		/// <returns>The type representing the format.</returns>
		public static ShaderType GetTexelType(this ImageFormat fmt)
		{
			if (fmt == ImageFormat.Error) return ShaderType.Error;
			return GetComponentType(fmt).ToVectorType(GetChannelCount(fmt));
		}

		// Gets the layout from the qualifier, depends on the enum being in the same order as the grammar tokens
		internal static ImageFormat FromQualifier(SSLParser.ImageLayoutQualifierContext ctx) => (ImageFormat)(ctx.Start.Type - SSLParser.IFQ_RGBA_F);
	}
}
