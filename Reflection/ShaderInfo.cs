using System;
using System.Collections.Generic;
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
		/// <summary>
		/// The uniform values in the shader program.
		/// </summary>
		public IReadOnlyList<Uniform> Uniforms => _uniforms;
		private readonly List<Uniform> _uniforms;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal ShaderInfo()
		{
			_uniforms = new List<Uniform>();
		}
	}
}
