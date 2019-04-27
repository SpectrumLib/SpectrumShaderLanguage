﻿using System;

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
		RGBA_F = 0,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit signed integers (<see cref="ShaderType.Int4"/>).
		/// </summary>
		RGBA_I = 1,
		/// <summary>
		/// The texels are 4-channel (RGBA) 32-bit unsigned integers (<see cref="ShaderType.UInt4"/>).
		/// </summary>
		RGBA_U = 2,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit floats (<see cref="ShaderType.Float2"/>).
		/// </summary>
		RG_F = 3,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit signed integers (<see cref="ShaderType.Int2"/>).
		/// </summary>
		RG_I = 4,
		/// <summary>
		/// The texels are 2-channel (RG) 32-bit unsigned integers (<see cref="ShaderType.UInt2"/>).
		/// </summary>
		RG_U = 5,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit floats (<see cref="ShaderType.Float"/>).
		/// </summary>
		R_F = 6,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit signed integers (<see cref="ShaderType.Int"/>).
		/// </summary>
		R_I = 7,
		/// <summary>
		/// The texels are 1-channel (R) 32-bit unsigned integers (<see cref="ShaderType.UInt"/>).
		/// </summary>
		R_U = 8
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="ImageFormat"/> values.
	/// </summary>
	public static class ImageFormatHelper
	{
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