using System;
using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class KeysAndValues
	{
		public IReadOnlyList<string> Values { get; }
		public IReadOnlyDictionary<string, int> Lookup { get; }
		public bool MatchCase { get; }

		public KeysAndValues(IReadOnlyList<string> values, bool createLookup = false, bool matchCase = false)
		{
			Values = values;
			MatchCase = matchCase;

			var lookup = default(IReadOnlyDictionary<string, int>);
			if (createLookup)
			{
				var newLookup = new Dictionary<string, int>(MatchCase ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
				var index = 0;
				foreach (var value in Values)
				{
					if (newLookup.ContainsKey(value))
						throw new ArgumentException("Cannot have duplicate keys");
					newLookup[value] = index++;
				}
				lookup = newLookup;
			}
			Lookup = lookup;
		}
	}
}
