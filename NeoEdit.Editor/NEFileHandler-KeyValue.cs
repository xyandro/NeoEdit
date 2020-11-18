﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Editor
{
	partial class NEFileHandler
	{
		void Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(int index, bool matchCase = true) => SetKeysAndValues(index, GetSelectionStrings(), matchCase);

		void Execute_KeyValue_Add_KeysValues(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Concat(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute_KeyValue_Remove_KeysValues(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Except(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute_KeyValue_Replace_Values(int index)
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