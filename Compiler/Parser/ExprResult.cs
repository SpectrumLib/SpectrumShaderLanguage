using System;
using SSLang.Reflection;

namespace SSLang
{
	// The object type returned by functions in SSLVisitor
	internal class ExprResult
	{
		// The type that the expression generates
		public ShaderType ResultType;
	}
}
