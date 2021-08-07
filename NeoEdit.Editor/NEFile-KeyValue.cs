using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		void Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(int index, bool matchCase = true) => SetKeysAndValues(index, GetSelectionStrings(), matchCase);

		void Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Concat(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(int index)
		{
			var keysAndValues = GetKeysAndValues(index);
			SetKeysAndValues(index, keysAndValues.Values.Except(GetSelectionStrings()).ToList(), keysAndValues.MatchCase);
		}

		void Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(int index)
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
