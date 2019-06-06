using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit
{
	partial class TextEditor
	{
		void Command_Keys_Set(int index, bool caseSensitive = true)
		{
			GlobalKeys = TabsParent.ActiveCount == 1;
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (values.Distinct(str => caseSensitive ? str : str.ToLowerInvariant()).Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			KeysAndValues[index] = new ObservableCollection<string>(values);
			if (index == 0)
				CalculateKeysHash(caseSensitive);
		}

		void Command_Keys_Add(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			var caseSensitive = keysHash.Comparer == StringComparer.Ordinal;
			if ((index == 0) && (KeysAndValues[0].Concat(values).GroupBy(key => caseSensitive ? key : key.ToLowerInvariant()).Any(group => group.Count() > 1)))
				throw new ArgumentException("Cannot have duplicate keys");
			foreach (var value in values)
				KeysAndValues[index].Add(value);
			if (index == 0)
				CalculateKeysHash(caseSensitive);
		}

		void Command_Keys_Remove(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings().Distinct().ToList();
			foreach (var value in values)
				KeysAndValues[index].Remove(value);
		}

		void Command_Keys_Replace(int index)
		{
			// Handles keys as well as values
			if (KeysAndValues[0].Count != KeysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(KeysAndValues[index][keysHash[str]]);
			}
			ReplaceSelections(strs);
		}
	}
}
