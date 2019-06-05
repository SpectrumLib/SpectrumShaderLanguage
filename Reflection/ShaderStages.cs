using System;

namespace SSLang.Reflection
{
	/// <summary>
	/// Represents the pipeline stages in a shader program. Can be used as flags.
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

	/// <summary>
	/// Contains utility functionality for working with <see cref="ShaderStages"/> values.
	/// </summary>
	public static class ShaderStagesHelper
	{
		/// <summary>
		/// Checks if the set of shader stages contains the given stage.
		/// </summary>
		/// <param name="flags">The stage set to check.</param>
		/// <param name="stage">The stage to check for.</param>
		/// <returns>If the stage is represented in the set of stages.</returns>
		public static bool HasStage(this ShaderStages flags, ShaderStages stage) => (flags & stage) == stage;

		/// <summary>
		/// Adds the given stage to the set of stages.
		/// </summary>
		/// <param name="flags">The initial set of stages.</param>
		/// <param name="stage">The stage to add to the set.</param>
		/// <returns>A new set of stages representing the combination of the old stages and new stage.</returns>
		public static ShaderStages AddStage(this ShaderStages flags, ShaderStages stage) => flags | stage;

		/// <summary>
		/// Removes the given stage from the set of stages.
		/// </summary>
		/// <param name="flags">The initial set of stages.</param>
		/// <param name="stage">The stage to remove to the set.</param>
		/// <returns>A new set of stages representing the stage set with the given stage removed.</returns>
		public static ShaderStages RemoveStage(this ShaderStages flags, ShaderStages stage) => flags & ~stage;

		/// <summary>
		/// Gets the short name for the stage (4 letter, all lowercase). These are the abbreviations that Vulkan uses
		/// to differentiate the stages.
		/// </summary>
		/// <param name="stage">The stage to get a name for. Cannot be a set of stages.</param>
		/// <returns>The short name for the stage. If the stage is not a single stage, then null is returned.</returns>
		public static string GetShortName(this ShaderStages stage)
		{
			switch (stage)
			{
				case ShaderStages.Vertex: return "vert";
				case ShaderStages.TessControl: return "tesc";
				case ShaderStages.TessEval: return "tese";
				case ShaderStages.Geometry: return "geom";
				case ShaderStages.Fragment: return "frag";
				default: return null;
			}
		}
	}
}
