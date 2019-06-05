using System;
using System.Runtime.CompilerServices;

// Needs to go somewhere, this works
[assembly: InternalsVisibleTo("SSLang")]

namespace SSLang.Reflection
{
	/// <summary>
	/// Contains reflection information about an SSL shader.
	/// </summary>
	public sealed class ShaderInfo
	{
		#region Fields
		/// <summary>
		/// The set of stages that are implemented in the shader.
		/// </summary>
		public ShaderStages Stages = ShaderStages.None;
		/// <summary>
		/// The SSL version that the shader was compiled with.
		/// </summary>
		public Version CompilerVersion { get; internal set; } = new Version();
		/// <summary>
		/// The minimum SSL version required by the shader source.
		/// </summary>
		public Version SourceVersion { get; internal set; } = new Version();
		#endregion // Fields

		internal ShaderInfo(Version cv)
		{
			CompilerVersion = cv;
		}
	}
}
