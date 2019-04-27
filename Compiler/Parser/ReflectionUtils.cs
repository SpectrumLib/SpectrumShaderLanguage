using System;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Extra functionality for working with the enums from the reflection library
	internal static class ReflectionUtils
	{
		public static ShaderType? TranslateTypeContext(SSLParser.TypeContext ctx)
		{
			var token = ctx.Start.Type;
			if (token == SSLParser.KWT_VOID) return ShaderType.Void;
			if (token <= (int)ShaderTypeHelper.VALUE_TYPE_END) // Value types
				return (ShaderType)((token - SSLParser.KWT_BOOL) + (int)ShaderTypeHelper.VALUE_TYPE_START); 
			if (token <= (int)ShaderTypeHelper.HANDLE_TYPE_END) // Handle types
				return (ShaderType)((token - SSLParser.KWT_TEX1D) + (int)ShaderTypeHelper.HANDLE_TYPE_START);
			return null;
		}

		public static ImageFormat? TranslateImageFormat(SSLParser.ImageLayoutQualifierContext ctx) =>
			(ImageFormat)(ctx.Start.Type - SSLParser.IFQ_RGBA_F);

		public static bool IsSwizzleValid(ShaderType type, char swizzle)
		{
			var sidx = (swizzle == 'x' || swizzle == 'r' || swizzle == 's') ? 1 :
					   (swizzle == 'y' || swizzle == 'g' || swizzle == 't') ? 2 :
					   (swizzle == 'z' || swizzle == 'b' || swizzle == 'p') ? 3 :
					   (swizzle == 'w' || swizzle == 'a' || swizzle == 'q') ? 4 :
					   0;
			if (sidx == 0)
				return false;
			return type.IsVectorType() && (sidx <= type.GetComponentCount());
		}

		// stype = the type of swizzle   -   0 = position, 1 = color, 2 = texcoord
		public static string GetSwizzle(ShaderType type, int stype)
		{
			if (type.IsScalarType() || type.IsVectorType())
			{
				var cc = type.GetComponentCount();
				if (cc == 1) return (stype == 0) ? "x" : (stype == 1) ? "r" : "s";
				if (cc == 2) return (stype == 0) ? "xy" : (stype == 1) ? "rg" : "st";
				if (cc == 3) return (stype == 0) ? "xyz" : (stype == 1) ? "rgb" : "stp";
				if (cc == 4) return (stype == 0) ? "xyzw" : (stype == 1) ? "rgba" : "stpq";
			}
			return null;
		}
	}
}
