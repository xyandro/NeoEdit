using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.GUI;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorWindow
	{
		static List<string>[] keysAndValues = new List<string>[10] { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };
		static Dictionary<string, int> keysHash = new Dictionary<string, int>();

		TextEditCommand GetKeysValuesCommand(TextEditCommand command)
		{
			if ((command == TextEditCommand.Keys_SetKeys) || (command == TextEditCommand.Keys_SetValues1) || (command == TextEditCommand.Keys_SetValues2) || (command == TextEditCommand.Keys_SetValues3) || (command == TextEditCommand.Keys_SetValues4) || (command == TextEditCommand.Keys_SetValues5) || (command == TextEditCommand.Keys_SetValues6) || (command == TextEditCommand.Keys_SetValues7) || (command == TextEditCommand.Keys_SetValues8) || (command == TextEditCommand.Keys_SetValues9))
				return TextEditCommand.Keys_SetValues1;
			if ((command == TextEditCommand.Keys_SelectionReplace1) || (command == TextEditCommand.Keys_SelectionReplace2) || (command == TextEditCommand.Keys_SelectionReplace3) || (command == TextEditCommand.Keys_SelectionReplace4) || (command == TextEditCommand.Keys_SelectionReplace5) || (command == TextEditCommand.Keys_SelectionReplace6) || (command == TextEditCommand.Keys_SelectionReplace7) || (command == TextEditCommand.Keys_SelectionReplace8) || (command == TextEditCommand.Keys_SelectionReplace9))
				return TextEditCommand.Keys_SelectionReplace1;
			if ((command == TextEditCommand.Keys_GlobalFindKeys) || (command == TextEditCommand.Keys_GlobalFind1) || (command == TextEditCommand.Keys_GlobalFind2) || (command == TextEditCommand.Keys_GlobalFind3) || (command == TextEditCommand.Keys_GlobalFind4) || (command == TextEditCommand.Keys_GlobalFind5) || (command == TextEditCommand.Keys_GlobalFind6) || (command == TextEditCommand.Keys_GlobalFind7) || (command == TextEditCommand.Keys_GlobalFind8) || (command == TextEditCommand.Keys_GlobalFind9))
				return TextEditCommand.Keys_GlobalFind1;
			if ((command == TextEditCommand.Keys_GlobalReplace1) || (command == TextEditCommand.Keys_GlobalReplace2) || (command == TextEditCommand.Keys_GlobalReplace3) || (command == TextEditCommand.Keys_GlobalReplace4) || (command == TextEditCommand.Keys_GlobalReplace5) || (command == TextEditCommand.Keys_GlobalReplace6) || (command == TextEditCommand.Keys_GlobalReplace7) || (command == TextEditCommand.Keys_GlobalReplace8) || (command == TextEditCommand.Keys_GlobalReplace9))
				return TextEditCommand.Keys_GlobalReplace1;
			if ((command == TextEditCommand.Keys_CopyKeys) || (command == TextEditCommand.Keys_CopyValues1) || (command == TextEditCommand.Keys_CopyValues2) || (command == TextEditCommand.Keys_CopyValues3) || (command == TextEditCommand.Keys_CopyValues4) || (command == TextEditCommand.Keys_CopyValues5) || (command == TextEditCommand.Keys_CopyValues6) || (command == TextEditCommand.Keys_CopyValues7) || (command == TextEditCommand.Keys_CopyValues8) || (command == TextEditCommand.Keys_CopyValues9))
				return TextEditCommand.Keys_CopyValues1;
			if ((command == TextEditCommand.Keys_HitsKeys) || (command == TextEditCommand.Keys_HitsValues1) || (command == TextEditCommand.Keys_HitsValues2) || (command == TextEditCommand.Keys_HitsValues3) || (command == TextEditCommand.Keys_HitsValues4) || (command == TextEditCommand.Keys_HitsValues5) || (command == TextEditCommand.Keys_HitsValues6) || (command == TextEditCommand.Keys_HitsValues7) || (command == TextEditCommand.Keys_HitsValues8) || (command == TextEditCommand.Keys_HitsValues9))
				return TextEditCommand.Keys_HitsValues1;
			if ((command == TextEditCommand.Keys_MissesKeys) || (command == TextEditCommand.Keys_MissesValues1) || (command == TextEditCommand.Keys_MissesValues2) || (command == TextEditCommand.Keys_MissesValues3) || (command == TextEditCommand.Keys_MissesValues4) || (command == TextEditCommand.Keys_MissesValues5) || (command == TextEditCommand.Keys_MissesValues6) || (command == TextEditCommand.Keys_MissesValues7) || (command == TextEditCommand.Keys_MissesValues8) || (command == TextEditCommand.Keys_MissesValues9))
				return TextEditCommand.Keys_MissesValues1;

			return TextEditCommand.None;
		}

		int GetKeysValuesIndex(TextEditCommand command)
		{
			if ((command == TextEditCommand.Keys_SetKeys) || (command == TextEditCommand.Keys_CopyKeys) || (command == TextEditCommand.Keys_GlobalFindKeys) || (command == TextEditCommand.Keys_HitsKeys) || (command == TextEditCommand.Keys_MissesKeys))
				return 0;
			if ((command == TextEditCommand.Keys_SetValues1) || (command == TextEditCommand.Keys_SelectionReplace1) || (command == TextEditCommand.Keys_GlobalFind1) || (command == TextEditCommand.Keys_GlobalReplace1) || (command == TextEditCommand.Keys_CopyValues1) || (command == TextEditCommand.Keys_HitsValues1) || (command == TextEditCommand.Keys_MissesValues1))
				return 1;
			if ((command == TextEditCommand.Keys_SetValues2) || (command == TextEditCommand.Keys_SelectionReplace2) || (command == TextEditCommand.Keys_GlobalFind2) || (command == TextEditCommand.Keys_GlobalReplace2) || (command == TextEditCommand.Keys_CopyValues2) || (command == TextEditCommand.Keys_HitsValues2) || (command == TextEditCommand.Keys_MissesValues2))
				return 2;
			if ((command == TextEditCommand.Keys_SetValues3) || (command == TextEditCommand.Keys_SelectionReplace3) || (command == TextEditCommand.Keys_GlobalFind3) || (command == TextEditCommand.Keys_GlobalReplace3) || (command == TextEditCommand.Keys_CopyValues3) || (command == TextEditCommand.Keys_HitsValues3) || (command == TextEditCommand.Keys_MissesValues3))
				return 3;
			if ((command == TextEditCommand.Keys_SetValues4) || (command == TextEditCommand.Keys_SelectionReplace4) || (command == TextEditCommand.Keys_GlobalFind4) || (command == TextEditCommand.Keys_GlobalReplace4) || (command == TextEditCommand.Keys_CopyValues4) || (command == TextEditCommand.Keys_HitsValues4) || (command == TextEditCommand.Keys_MissesValues4))
				return 4;
			if ((command == TextEditCommand.Keys_SetValues5) || (command == TextEditCommand.Keys_SelectionReplace5) || (command == TextEditCommand.Keys_GlobalFind5) || (command == TextEditCommand.Keys_GlobalReplace5) || (command == TextEditCommand.Keys_CopyValues5) || (command == TextEditCommand.Keys_HitsValues5) || (command == TextEditCommand.Keys_MissesValues5))
				return 5;
			if ((command == TextEditCommand.Keys_SetValues6) || (command == TextEditCommand.Keys_SelectionReplace6) || (command == TextEditCommand.Keys_GlobalFind6) || (command == TextEditCommand.Keys_GlobalReplace6) || (command == TextEditCommand.Keys_CopyValues6) || (command == TextEditCommand.Keys_HitsValues6) || (command == TextEditCommand.Keys_MissesValues6))
				return 6;
			if ((command == TextEditCommand.Keys_SetValues7) || (command == TextEditCommand.Keys_SelectionReplace7) || (command == TextEditCommand.Keys_GlobalFind7) || (command == TextEditCommand.Keys_GlobalReplace7) || (command == TextEditCommand.Keys_CopyValues7) || (command == TextEditCommand.Keys_HitsValues7) || (command == TextEditCommand.Keys_MissesValues7))
				return 7;
			if ((command == TextEditCommand.Keys_SetValues8) || (command == TextEditCommand.Keys_SelectionReplace8) || (command == TextEditCommand.Keys_GlobalFind8) || (command == TextEditCommand.Keys_GlobalReplace8) || (command == TextEditCommand.Keys_CopyValues8) || (command == TextEditCommand.Keys_HitsValues8) || (command == TextEditCommand.Keys_MissesValues8))
				return 8;
			if ((command == TextEditCommand.Keys_SetValues9) || (command == TextEditCommand.Keys_SelectionReplace9) || (command == TextEditCommand.Keys_GlobalFind9) || (command == TextEditCommand.Keys_GlobalReplace9) || (command == TextEditCommand.Keys_CopyValues9) || (command == TextEditCommand.Keys_HitsValues9) || (command == TextEditCommand.Keys_MissesValues9))
				return 9;
			throw new Exception("Invalid keys/values command");
		}

		bool RunKeysCommand(TextEditCommand command)
		{
			var result = true;

			if (GetKeysValuesCommand(command) == TextEditCommand.Keys_SetValues1)
			{
				// Handles keys as well as values
				var index = GetKeysValuesIndex(command);
				var values = Selections.Select(range => GetString(range)).ToList();
				if ((index == 0) && (values.Distinct().Count() != values.Count))
					throw new ArgumentException("Cannot have duplicate keys.");
				keysAndValues[index] = values;
				if (index == 0)
					keysHash = values.Select((key, pos) => new { key = key, pos = pos }).ToDictionary(entry => entry.key, entry => entry.pos);
			}
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_SelectionReplace1)
			{
				var index = GetKeysValuesIndex(command);
				if (keysAndValues[0].Count != keysAndValues[index].Count)
					throw new Exception("Keys and values count must match.");

				var strs = new List<string>();
				foreach (var range in Selections)
				{
					var str = GetString(range);
					if (!keysHash.ContainsKey(str))
						strs.Add(str);
					else
						strs.Add(keysAndValues[index][keysHash[str]]);
				}
				Replace(Selections, strs, true);
			}
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_GlobalFind1)
			{
				var index = GetKeysValuesIndex(command);
				if (keysAndValues[0].Count != keysAndValues[index].Count)
					throw new Exception("Keys and values count must match.");

				var searcher = Searcher.Create(keysAndValues[0]);
				var ranges = new RangeList();
				var selections = Selections;
				if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
					selections = new RangeList { new Range(BeginOffset(), EndOffset()) };
				foreach (var selection in selections)
					ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

				ranges = ranges.OrderBy(range => range.Start).ToList();
				Selections.Replace(ranges);
			}
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_GlobalReplace1)
			{
				var index = GetKeysValuesIndex(command);
				if (keysAndValues[0].Count != keysAndValues[index].Count)
					throw new Exception("Keys and values count must match.");

				var searcher = Searcher.Create(keysAndValues[0]);
				var ranges = new RangeList();
				var selections = Selections;
				if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
					selections = new RangeList { new Range(BeginOffset(), EndOffset()) };
				foreach (var selection in selections)
					ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

				ranges = ranges.OrderBy(range => range.Start).ToList();

				var strs = new List<string>();
				foreach (var range in ranges)
				{
					var str = GetString(range);
					if (!keysHash.ContainsKey(str))
						strs.Add(str);
					else
						strs.Add(keysAndValues[index][keysHash[str]]);
				}
				Replace(ranges, strs, true);
			}
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_CopyValues1)
				ClipboardWindow.Set(keysAndValues[GetKeysValuesIndex(command)].ToArray());
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_HitsValues1)
			{
				var index = GetKeysValuesIndex(command);
				Selections.Replace(Selections.Where(range => keysAndValues[index].Contains(GetString(range))).ToList());
			}
			else if (GetKeysValuesCommand(command) == TextEditCommand.Keys_MissesValues1)
			{
				var index = GetKeysValuesIndex(command);
				Selections.Replace(Selections.Where(range => !keysAndValues[index].Contains(GetString(range))).ToList());
			}
			else
				result = false;

			return result;
		}
	}
}
