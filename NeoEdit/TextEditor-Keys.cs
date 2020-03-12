using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Command_Keys_Set(int index, bool caseSensitive = true) => commandState.TabsWindow.SetKeysAndValues(index, GetSelectionStrings(), caseSensitive);

		void Command_Keys_Add(int index)
		{
			var values = commandState.TabsWindow.GetKeysAndValues(this, index);
			values.AddRange(GetSelectionStrings());
			commandState.TabsWindow.SetKeysAndValues(index, values);
		}

		void Command_Keys_Remove(int index)
		{
			var values = commandState.TabsWindow.GetKeysAndValues(this, index);
			foreach (var value in GetSelectionStrings().Distinct())
				values.Remove(value);
			commandState.TabsWindow.SetKeysAndValues(index, values);
		}

		void Command_Keys_Replace(int index)
		{
			var keysHash = commandState.TabsWindow.GetKeysHash(this);
			var values = commandState.TabsWindow.GetKeysAndValues(this, index);

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
