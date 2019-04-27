using System;
using System.Collections.Generic;
using System.Linq;
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
		public ShaderStages Stages { get; internal set; } = ShaderStages.None;
		/// <summary>
		/// The uniform values in the shader program.
		/// </summary>
		public IReadOnlyList<Uniform> Uniforms => _uniforms;
		internal readonly List<Uniform> _uniforms;
		/// <summary>
		/// The uniform blocks in the shader program.
		/// </summary>
		public IReadOnlyList<UniformBlock> Blocks => _blocks;
		internal readonly List<UniformBlock> _blocks;
		/// <summary>
		/// The input vertex attributes in the shader program.
		/// </summary>
		public IReadOnlyList<Attribute> Attributes => _attributes;
		internal readonly List<Attribute> _attributes;
		/// <summary>
		/// The fragment shader outputs in the shader program.
		/// </summary>
		public IReadOnlyList<FragmentOutput> Outputs => _outputs;
		internal readonly List<FragmentOutput> _outputs;

		// Cached value for contiguous uniforms
		private bool? _contiguousCache = null;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal ShaderInfo()
		{
			_uniforms = new List<Uniform>();
			_blocks = new List<UniformBlock>();
			_attributes = new List<Attribute>();
			_outputs = new List<FragmentOutput>();
		}

		/// <summary>
		/// Checks if the uniforms in the shader are contiguous in their bindings.
		/// </summary>
		/// <returns>If the uniform bindings are contiguous from 0.</returns>
		public bool AreUniformsContiguous()
		{
			if (_contiguousCache.HasValue)
				return _contiguousCache.Value;

			var minB = _uniforms.Min(u => u.Location);
			if (minB != 0)
				return false;
			var maxB = _uniforms.Max(u => u.Location);
			if ((maxB - minB) > _uniforms.Count)
				return false;

			bool found = false;
			for (uint i = minB; (i <= maxB) && !found; ++i)
				found = _uniforms.FindIndex(u => u.Location == i) == -1;
			_contiguousCache = !found;
			return !found;
		}
	}
}
