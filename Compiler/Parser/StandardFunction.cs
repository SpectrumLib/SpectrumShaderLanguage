using System;
using System.Collections.Generic;
using SSLang.Generated;
using SSLang.Reflection;

namespace SSLang
{
	// Used to track standard (non-stage) functions in a shader
	internal class StandardFunction
	{
		#region Fields
		// The name of the function
		public readonly string Name;
		// The mangled name used in the GLSL output
		public readonly string OutputName;
		// The return type of the function
		public readonly ShaderType ReturnType;
		// The arguments to the function
		public readonly Param[] Params;

		public uint ParamCount => (uint)Params.Length;
		public bool HasParams => Params.Length > 0;
		#endregion // Fields

		public StandardFunction(string n, ShaderType rt, Param[] pars)
		{
			Name = n;
			OutputName = $"_func_{n}";
			ReturnType = rt;
			Params = pars;
		}

		public static StandardFunction FromContext(SSLParser.StandardFunctionContext ctx, SSLVisitor vis)
		{
			var rtype = ReflectionUtils.TranslateTypeContext(ctx.type());
			var fname = ctx.Name.Text;

			List<Param> pars = new List<Param>();
			if (ctx.Params != null)
			{
				foreach (var pctx in ctx.Params._PList)
				{
					var pname = pctx.Name.Text;
					var aidx = pctx.Access?.Type ?? SSLParser.KW_IN;
					var acs = (aidx == SSLParser.KW_OUT) ? Access.Out : (aidx == SSLParser.KW_INOUT) ? Access.InOut : Access.In;
					var ptype = ReflectionUtils.TranslateTypeContext(pctx.type());
					if (ptype == ShaderType.Void)
						vis.Error(ctx, $"The parameter '{pname}' cannot have type 'void'.");
					var fidx = pars.FindIndex(ep => ep.Name == pname);
					if (fidx != -1)
						vis.Error(ctx, $"Duplicate parameter name '{pname}' in the function parameter list.");
					pars.Add(new Param { Name = pname, Type = ptype.Value, Access = acs });
				}
			}

			return new StandardFunction(fname, rtype.Value, pars.ToArray());
		}

		// Contains information about a function parameter
		public class Param
		{
			public string Name;
			public ShaderType Type;
			public Access Access;
			public bool IsConst => (Access == Access.In);
		}

		// The parameter access qualifiers
		public enum Access
		{
			In,
			Out,
			InOut
		}
	}
}
