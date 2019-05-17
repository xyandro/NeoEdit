using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.MenuKeys
{
	public static class KeysFunctions
	{
		static public void Command_Keys_Set(ITextEditor te, int index, bool caseSensitive = true)
		{
			te.GlobalKeys = te.TabsParent.ActiveCount == 1;
			// Handles keys as well as values
			var values = te.GetSelectionStrings();
			if ((index == 0) && (values.Distinct(str => caseSensitive ? str : str.ToLowerInvariant()).Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			te.KeysAndValues[index] = new ObservableCollection<string>(values);
			if (index == 0)
				te.CalculateKeysHash(caseSensitive);
		}

		static public void Command_Keys_Add(ITextEditor te, int index)
		{
			// Handles keys as well as values
			var values = te.GetSelectionStrings();
			var caseSensitive = te.keysHash.Comparer == StringComparer.Ordinal;
			if ((index == 0) && (te.KeysAndValues[0].Concat(values).GroupBy(key => caseSensitive ? key : key.ToLowerInvariant()).Any(group => group.Count() > 1)))
				throw new ArgumentException("Cannot have duplicate keys");
			foreach (var value in values)
				te.KeysAndValues[index].Add(value);
			if (index == 0)
				te.CalculateKeysHash(caseSensitive);
		}

		static public void Command_Keys_Remove(ITextEditor te, int index)
		{
			// Handles keys as well as values
			var values = te.GetSelectionStrings().Distinct().ToList();
			foreach (var value in values)
				te.KeysAndValues[index].Remove(value);
		}

		static public void Command_Keys_Replace(ITextEditor te, int index)
		{
			// Handles keys as well as values
			if (te.KeysAndValues[0].Count != te.KeysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in te.Selections)
			{
				var str = te.GetString(range);
				if (!te.keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(te.KeysAndValues[index][te.keysHash[str]]);
			}
			te.ReplaceSelections(strs);
		}
	}
}
