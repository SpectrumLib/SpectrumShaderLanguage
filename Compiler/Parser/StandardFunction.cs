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
		// The return type of the function
		public readonly ShaderType ReturnType;
		// The arguments to the function
		public readonly Param[] Params;

		public bool HasParams => Params.Length > 0;
		#endregion // Fields

		public StandardFunction(string n, ShaderType rt, Param[] pars)
		{
			Name = n;
			ReturnType = rt;
			Params = pars;
		}

		public static bool TryFromContext(SSLParser.StandardFunctionContext ctx, out StandardFunction func, out string error)
		{
			func = null;

			var rtype = ShaderTypeHelper.FromTypeContext(ctx.type());
			var fname = ctx.Name.Text;

			List<Param> pars = new List<Param>();
			if (ctx.Params != null)
			{
				foreach (var pctx in ctx.Params._PList)
				{
					var pname = pctx.Name.Text;
					var aidx = pctx.Access?.TokenIndex ?? SSLParser.KW_IN;
					var acs = (aidx == SSLParser.KW_OUT) ? Access.Out : (aidx == SSLParser.KW_INOUT) ? Access.InOut : Access.In;
					var ptype = ShaderTypeHelper.FromTypeContext(ctx.type());
					if (ptype == ShaderType.Void)
					{
						error = $"The parameter '{pname}' cannot have type 'void'.";
						return false;
					}
					var fidx = pars.FindIndex(ep => ep.Name == pname);
					if (fidx != -1)
					{
						error = $"Duplicate parameter name '{pname}' in the function parameter list.";
						return false;
					}
					pars.Add(new Param { Name = pname, Type = ptype, Access = acs });
				}
			}

			func = new StandardFunction(fname, rtype, pars.ToArray());
			error = null;
			return true;
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
