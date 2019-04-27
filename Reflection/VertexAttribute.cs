using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a vertex attribute shader input value and its information in an SSL program.
	/// </summary>
	public sealed class VertexAttribute
	{
		#region Fields
		/// <summary>
		/// The name of the attribute.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The type of the attribute.
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		/// Gets the size of the attribute array. If the attribute is not an array, this will be 1.
		/// </summary>
		/// <seealso cref="IsArray"/>
		public readonly uint ArraySize;
		/// <summary>
		/// Gets if the attribute is an array.
		/// </summary>
		public readonly bool IsArray;
		/// <summary>
		/// The binding point of the attribute.
		/// </summary>
		public readonly uint Location;

		/// <summary>
		/// The size of the attribute, in bytes. Takes the array size into account.
		/// </summary>
		public uint Size => Type.GetSize() * ArraySize;
		/// <summary>
		/// Gets the number of binding slots the attribute fills.
		/// </summary>
		public uint SlotCount => Type.GetSlotCount(ArraySize);
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal VertexAttribute(string name, ShaderType type, uint? arrSize, uint loc)
		{
			Name = name;
			Type = type;
			ArraySize = arrSize.GetValueOrDefault(1);
			IsArray = arrSize.HasValue;
			Location = loc;
		}
	}
}
