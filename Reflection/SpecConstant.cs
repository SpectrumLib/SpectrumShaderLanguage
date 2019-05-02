using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a specialization constant and its information in an SSL program.
	/// </summary>
	public sealed class SpecConstant
	{
		#region Fields
		/// <summary>
		/// The name of the specialization constant.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The type of the specialization constant.
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		/// The constant index occupied by the specialization constant.
		/// </summary>
		public readonly uint Index;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal SpecConstant(string n, ShaderType t, uint i)
		{
			Name = n;
			Type = t;
			Index = i;
		}
	}
}
