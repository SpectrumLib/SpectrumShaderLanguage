using System;
using System.Collections.Generic;
using System.Text;

namespace SSLang.Translate
{
	// Manages the variable scope stack for a translator instance
	internal class ScopeManager
	{
		#region Fields
		private readonly Dictionary<string, Variable> _attributes;
		public IReadOnlyDictionary<string, Variable> Attributes => _attributes;

		private readonly Dictionary<string, Variable> _outputs;
		public IReadOnlyDictionary<string, Variable> Outputs => _outputs;

		private readonly Dictionary<string, Variable> _uniforms;
		public IReadOnlyDictionary<string, Variable> Uniforms => _uniforms;

		private readonly Dictionary<string, Variable> _internals;
		public IReadOnlyDictionary<string, Variable> Internals => _internals;

		private readonly Dictionary<string, Variable> _constants;
		public IReadOnlyDictionary<string, Variable> Constants => _constants;
		#endregion // Fields
	}
}
