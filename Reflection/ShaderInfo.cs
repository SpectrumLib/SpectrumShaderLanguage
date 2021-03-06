﻿using System;
using System.Collections.Generic;
using System.IO;
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
		/// The version of the tool used to compile the shader, will be the same version that generated the reflection info.
		/// </summary>
		public Version CompilerVersion { get; internal set; } = new Version();
		/// <summary>
		/// The minimum SSL version required by the shader (from the source 'version' statement).
		/// </summary>
		public Version SourceVersion { get; internal set; } = new Version();
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
		public IReadOnlyList<VertexAttribute> Attributes => _attributes;
		internal readonly List<VertexAttribute> _attributes;
		/// <summary>
		/// The fragment shader outputs in the shader program.
		/// </summary>
		public IReadOnlyList<FragmentOutput> Outputs => _outputs;
		internal readonly List<FragmentOutput> _outputs;

		/// <summary>
		/// The specialization constants in the shader program.
		/// </summary>
		public IReadOnlyList<SpecConstant> Specializations => _specializations;
		internal readonly List<SpecConstant> _specializations;

		// Cached value for contiguous uniforms
		private bool? _contiguousCache = null;
		#endregion // Fields

		// Can only construct from this assembly and friend assemblies
		internal ShaderInfo()
		{
			_uniforms = new List<Uniform>();
			_blocks = new List<UniformBlock>();
			_attributes = new List<VertexAttribute>();
			_outputs = new List<FragmentOutput>();
			_specializations = new List<SpecConstant>();
		}
		
		/// <summary>
		/// Sorts all of the uniforms, attributes, and outputs in the info by binding location and offset. Note that
		/// if you get information from the compiler, or loaded from a file, the information will already be sorted.
		/// </summary>
		public void Sort()
		{
			_uniforms.Sort((u1, u2) => (u1.Location == u2.Location) ? u1.Offset.CompareTo(u2.Offset) : u1.Location.CompareTo(u2.Location));
			_blocks.Sort((b1, b2) => b1.Location.CompareTo(b2.Location));
			_attributes.Sort((a1, a2) => a1.Location.CompareTo(a2.Location));
			_outputs.Sort((o1, o2) => o1.Index.CompareTo(o2.Index));
			_specializations.Sort((s1, s2) => s1.Index.CompareTo(s2.Index));
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

		/// <summary>
		/// Saves the reflection information to a file, either in a binary or text format.
		/// </summary>
		/// <param name="path">The path to save the reflection data to.</param>
		/// <param name="binary"><c>true</c> for binary-encoded data, <c>false</c> for human-readable text.</param>
		public void SaveToFile(string path, bool binary)
		{
			ReflectionWriter.SaveTo(path, binary, this);
		}

		/// <summary>
		/// Loads reflection information from a binary-encoded reflection file generated by the compiler library. The
		/// version of the loading library must be greater than or equal to the version that generated the reflection.
		/// The text-encoded reflection files cannot be loaded with this function.
		/// </summary>
		/// <param name="path">The path to the binary reflection file.</param>
		/// <returns>The shader info object describing the loaded reflection information.</returns>
		public static ShaderInfo LoadFromFile(string path)
		{
			if (!File.Exists(path))
				throw new IOException($"The path is invalid, or points to a file that does not exist.");

			using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
			using (var reader = new BinaryReader(file))
			{
				var header = reader.ReadBytes(10);
				if (header[0] != 'S' || header[1] != 'S' || header[2] != 'L' || header[3] != 'R')
					throw new InvalidOperationException($"The file does not appear to be a valid binary SSL reflection file.");
				var cVer = new Version(header[4], header[5], header[6]);
				var sVer = new Version(header[7], header[8], header[9]);
				return ReflectionReader.LoadFrom(reader, cVer, sVer);
			}
		}
	}
}
