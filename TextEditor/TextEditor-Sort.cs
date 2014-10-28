using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.TextEditor.Dialogs;

namespace NeoEdit.TextEditor
{
	public partial class TextEditor
	{
		internal enum SortScope { Selections, Lines, Regions }
		internal enum SortType { String, StringNonNumeric, Keys, Reverse, Randomize, Length }

		List<Range> GetSortLines()
		{
			return Selections.Select(range => Data.GetOffsetLine(range.Start)).Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line))).ToList();
		}

		List<Range> GetSortRegions()
		{
			var regions = new List<Range>();
			foreach (var selection in Selections)
			{
				var region = Marks.Where(mark => (selection.Start >= mark.Start) && (selection.End <= mark.End)).ToList();
				if (region.Count == 0)
					throw new Exception("No region found.  All selections must be inside a marked region.");
				if (region.Count != 1)
					throw new Exception("Multiple regions found.  All selections must be inside a single marked region.");
				regions.Add(region.Single());
			}

			if (Marks.Count != regions.Count)
				throw new Exception("Extra regions found.");

			return regions;
		}

		string NumericSort(string str)
		{
			return Regex.Replace(str, @"\d+", match => new string('0', Math.Max(0, 20 - match.Value.Length)) + match.Value);
		}

		List<Range> GetRegions(SortScope scope)
		{
			List<Range> regions = null;
			switch (scope)
			{
				case SortScope.Selections: regions = Selections.ToList(); break;
				case SortScope.Lines: regions = GetSortLines(); break;
				case SortScope.Regions: regions = GetSortRegions(); break;
				default: throw new Exception("Invalid sort type");
			}

			// Sanity check; soundn't happen
			if (Selections.Count != regions.Count)
				throw new Exception("Selections and regions counts must match");

			var orderedRegions = regions.OrderBy(range => range.Start).ToList();
			var pos = 0;
			foreach (var range in orderedRegions)
			{
				if (range.Start < pos)
					throw new Exception("Regions cannot overlap");
				pos = range.End;
			}

			return regions;
		}

		IOrderedEnumerable<TSource> OrderByAscDesc<TSource, TKey>(bool ascending, IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> comparer = null)
		{
			if (ascending)
				return comparer == null ? source.OrderBy(keySelector) : source.OrderBy(keySelector, comparer);
			else
				return comparer == null ? source.OrderByDescending(keySelector) : source.OrderByDescending(keySelector, comparer);
		}

		List<int> GetOrdering(SortType type, bool ascending)
		{
			var entries = Selections.Select((range, index) => new { value = GetString(range), index = index }).ToList();
			switch (type)
			{
				case SortType.String: entries = OrderByAscDesc(ascending, entries, entry => NumericSort(entry.value)).ToList(); break;
				case SortType.StringNonNumeric: entries = OrderByAscDesc(ascending, entries, entry => entry.value).ToList(); break;
				case SortType.Keys:
					{
						var sort = keysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = OrderByAscDesc(ascending, entries, entry => entry.value, (value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue)).ToList();
					}
					break;
				case SortType.Reverse: entries.Reverse(); break;
				case SortType.Randomize:
					{
						var random = new Random();
						entries = entries.OrderBy(entry => random.Next()).ToList();
					}
					break;
				case SortType.Length: entries = OrderByAscDesc(ascending, entries, entry => entry.value.Length).ToList(); break;
			}

			return entries.Select(entry => entry.index).ToList();
		}

		internal void Command_Data_Sort()
		{
			var result = SortDialog.Run();
			if (result == null)
				return;

			var regions = GetRegions(result.SortScope);
			var ordering = GetOrdering(result.SortType, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				if ((Selections[ctr].Start < regions[ctr].Start) || (Selections[ctr].End > regions[ctr].End))
					throw new Exception("All selections must be a region");
			}

			var newSelections = Selections.ToList();
			var orderedRegions = ordering.Select(index => regions[index]).ToList();
			var orderedRegionText = orderedRegions.Select(range => GetString(range)).ToList();

			Replace(regions, orderedRegionText);

			var newRegions = regions.ToList();
			var add = 0;
			for (var ctr = 0; ctr < newSelections.Count; ++ctr)
			{
				var orderCtr = ordering[ctr];
				newSelections[orderCtr] = new Range(newSelections[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add, newSelections[orderCtr].Highlight - regions[orderCtr].Start + regions[ctr].Start + add);
				newRegions[orderCtr] = new Range(newRegions[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add, newRegions[orderCtr].Highlight - regions[orderCtr].Start + regions[ctr].Start + add);
				add += orderedRegionText[ctr].Length - regions[ctr].Length;
			}
			newSelections = ordering.Select(num => newSelections[num]).ToList();

			Selections.Replace(newSelections);
			if (result.SortScope == SortScope.Regions)
				Marks.Replace(newRegions);
		}
	}
}
