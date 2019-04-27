using System;
using System.Runtime.CompilerServices;

// This has to go somewhere... here works
[assembly: InternalsVisibleTo("SSLang")]

namespace SSLang.Reflection
{
	/// <summary>
	/// Contains reflection information about an SSL program.
	/// </summary>
	public sealed class ShaderInfo
	{
		#region Fields
		/// <summary>
		/// The set of stages that are implemented in the shader.
		/// </summary>
		public ShaderStages Stages { get; private set; } = ShaderStages.None;
		#endregion // Fields
	}
}
