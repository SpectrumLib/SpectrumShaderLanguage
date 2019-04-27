using System;
using System.Collections.Generic;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents a uniform value and its information in an SSL program.
	/// </summary>
	public sealed class Uniform
	{
		#region Fields
		/// <summary>
		/// The name of the uniform.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The type of the uniform.
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		/// Gets the size of the uniform array, in bytes. Takes the array size into account.
		/// </summary>
		/// <seealso cref="IsArray"/>
		public readonly uint ArraySize;
		/// <summary>
		/// Gets if the uniform is an array.
		/// </summary>
		public readonly bool IsArray;
		/// <summary>
		/// The block that this uniform belongs to. Will only be non-null if the uniform is a value type.
		/// </summary>
		public readonly UniformBlock Block;
		/// <summary>
		/// Gets the index of this uniform into the member list of its block. If the uniform is not in a block, this
		/// will be 0.
		/// </summary>
		public readonly uint Index;
		/// <summary>
		/// Gets the offset of this uniform into the memory of its block, in bytes. If the uniform is not in a block,
		/// this will be 0.
		/// </summary>
		public readonly uint Offset;

		/// <summary>
		/// Gets if the uniform is a value type. Value type uniforms will always appear inside of a uniform block.
		/// </summary>
		public bool IsValueType => Type.IsValueType();
		/// <summary>
		/// Gets if the uniform is a handle type. Handle type uniforms will never appear inside of a uniform block.
		/// </summary>
		public bool IsHandleType => Type.IsHandleType();
		/// <summary>
		/// The binding location of the uniform. If the uniform is part of a block, then this will be the blocks'
		/// binding location.
		/// </summary>
		public uint Location => Block?.Location ?? _location;
		private readonly uint _location;
		/// <summary>
		/// Gets the size of the uniform, in bytes. Takes into account the size of the array, if the uniform is an array.
		/// </summary>
		public uint Size => Type.GetSize() * ArraySize;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal Uniform(string name, ShaderType type, uint? arrSize, uint loc, UniformBlock block, uint idx, uint off)
		{
			Name = name;
			Type = type;
			ArraySize = arrSize.GetValueOrDefault(1);
			IsArray = arrSize.HasValue;
			Block = block;
			Index = idx;
			Offset = off;
			_location = loc;
		}
	}

	/// <summary>
	/// Represents a block of value-type uniforms in an SSL program. All uniforms within a block pull from
	/// tightly-packed and contiguous memory.
	/// </summary>
	public sealed class UniformBlock
	{
		#region Fields
		/// <summary>
		/// The binding location of the uniform block.
		/// </summary>
		public readonly uint Location;
		/// <summary>
		/// The uniforms that are members of the uniform block. These will be in the order they are declared.
		/// </summary>
		public IReadOnlyList<Uniform> Members => _members;
		private readonly List<Uniform> _members;
		/// <summary>
		/// The size of the uniform block, in bytes.
		/// </summary>
		public uint Size { get; private set; }
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal UniformBlock(uint loc)
		{
			Location = loc;
			_members = new List<Uniform>();
			Size = 0;
		}

		// Adds a uniform as a member and sorts it into its place by offset
		internal void AddMember(Uniform u)
		{
			_members.Add(u);
			_members.Sort((u1, u2) => u1.Offset.CompareTo(u2.Offset));
			Size += u.Size;
		}
	}
}
