using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a fragment shader output value and its information in an SSL program.
	/// </summary>
	public sealed class FragmentOutput
	{
		#region Fields
		/// <summary>
		/// The name of the output.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The type of the output.
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		/// The binding index of the output variable.
		/// </summary>
		public readonly uint Index;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal FragmentOutput(string name, ShaderType type, uint idx)
		{
			Name = name;
			Type = type;
			Index = idx;
		}
	}
}
