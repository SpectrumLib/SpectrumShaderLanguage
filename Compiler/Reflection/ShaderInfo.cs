using System;
using System.Collections.Generic;

namespace SSLang.Reflection
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

		internal readonly List<(Variable, uint)> _attributes = new List<(Variable, uint)>();
		/// <summary>
		/// The vertex attributes that are used to pass vertex information to the shader. They are in the order
		/// they appear in the shader 'attributes' block.
		/// </summary>
		public IReadOnlyList<(Variable Variable, uint Location)> Attributes => _attributes;

		internal readonly List<Variable> _outputs = new List<Variable>();
		/// <summary>
		/// The fragment stage outputs in this shader. They are in the order of their binding locations.
		/// </summary>
		public IReadOnlyList<Variable> Outputs => _outputs;

		internal readonly List<(Variable, uint, uint)> _uniforms = new List<(Variable, uint, uint)>();
		/// <summary>
		/// The uniforms for the shader. Each entry has its binding location (all entries in the same block will have
		/// the same location), and its index (order) within the block. The index for handle types will be zero.
		/// Each location will never be repeated outside of the same block, and each index will never be repeated
		/// inside of any block.
		/// </summary>
		public IReadOnlyList<(Variable Variable, uint Location, uint Index)> Uniforms => _uniforms;
		#endregion // Fields
	}

	/// <summary>
	/// Contains the different stages present in a graphics pipeline shader. Can be used as bit flags.
	/// </summary>
	[Flags]
	public enum ShaderStages : byte
	{
		/// <summary>
		/// A special set of flags representing no stages. Is not a valid stage by itself.
		/// </summary>
		None = 0x00,
		/// <summary>
		/// The vertex stage (stage 1).
		/// </summary>
		Vertex = 0x01,
		/// <summary>
		/// The tessellation control stage (stage 2).
		/// </summary>
		TessControl = 0x02,
		/// <summary>
		/// The tessellation evaluation stage (stage 3).
		/// </summary>
		TessEval = 0x04,
		/// <summary>
		/// The geometry stage (stage 4).
		/// </summary>
		Geometry = 0x08,
		/// <summary>
		/// The fragment stage (stage 5).
		/// </summary>
		Fragment = 0x10,
		/// <summary>
		/// A special set of flags representing all stages. It is not a valid stage by itself.
		/// </summary>
		All = 0x1F
	}
}
