using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	partial class Tab
	{
		void Execute_Keys_Set(int index, bool matchCase = true) => SetKeysAndValues(index, GetSelectionStrings(), matchCase);

		void Execute_Keys_Add(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Concat(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute_Keys_Remove(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Except(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute_Keys_Replace(int index)
		{
			var keysHash = GetKeysAndValues(0).Lookup;
			var values = GetKeysAndValues(index).Values;

			if (keysHash.Count != values.Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = Text.GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(values[keysHash[str]]);
			}
			ReplaceSelections(strs);
		}
	}
}
