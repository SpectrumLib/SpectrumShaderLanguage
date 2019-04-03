using System;

namespace SSLang
{
	/// <summary>
	/// Contains reflection information about a compiled shader.
	/// </summary>
	public sealed class ShaderInfo
	{
		#region Fields
		/// <summary>
		/// The optional name of the shader. This will be null unless a shader name is given in the source code.
		/// </summary>
		public string Name { get; internal set; } = null;
		#endregion // Fields
	}
}
