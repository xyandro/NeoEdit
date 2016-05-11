using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using NeoEdit.Common;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		internal enum SortScope { Selections, Lines, Regions }
		internal enum SortType { Smart, String, Length, Integer, Float, Hex, DateTime, Keys, Reverse, Randomize, Frequency }

		List<Range> GetSortLines() => Selections.Select(range => Data.GetOffsetLine(range.Start)).Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line))).ToList();

		List<Range> GetEnclosingRegions(bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var regions = new List<Range>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in Selections)
			{
				while ((currentRegion < Regions.Count) && (Regions[currentRegion].End <= selection.Start))
				{
					if ((useAllRegions) && (!used))
						throw new Exception("Extra regions found.");
					used = false;
					++currentRegion;
				}
				if ((currentRegion < Regions.Count) && (selection.Start >= Regions[currentRegion].Start) && (selection.End <= Regions[currentRegion].End))
				{
					regions.Add(Regions[currentRegion]);
					used = true;
				}
				else if (mustBeInRegion)
					throw new Exception("No region found.  All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((Selections.Any()) && (useAllRegions) && (currentRegion != Regions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		List<Range> GetSortSource(SortScope scope)
		{
			List<Range> sortSource = null;
			switch (scope)
			{
				case SortScope.Selections: sortSource = Selections.ToList(); break;
				case SortScope.Lines: sortSource = GetSortLines(); break;
				case SortScope.Regions: sortSource = GetEnclosingRegions(true); break;
				default: throw new Exception("Invalid sort type");
			}

			if (Selections.Count != sortSource.Count)
				throw new Exception("Selections and regions counts must match");

			var orderedRegions = sortSource.OrderBy(range => range.Start).ToList();
			var pos = 0;
			foreach (var range in orderedRegions)
			{
				if (range.Start < pos)
					throw new Exception("Regions cannot overlap");
				pos = range.End;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				if ((Selections[ctr].Start < sortSource[ctr].Start) || (Selections[ctr].End > sortSource[ctr].End))
					throw new Exception("All selections must be a region");
			}

			return sortSource;
		}

		IOrderedEnumerable<TSource> OrderByAscDesc<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending, IComparer<TKey> comparer = null)
		{
			var func = ascending ? (Func<IEnumerable<TSource>, Func<TSource, TKey>, IComparer<TKey>, IOrderedEnumerable<TSource>>)Enumerable.OrderBy : Enumerable.OrderByDescending;
			return func(source, keySelector, comparer ?? Comparer<TKey>.Default);
		}

		List<int> GetOrdering(bool withinRegions, SortType type, bool caseSensitive, bool ascending)
		{
			var entries = Selections.Select((range, index) => new { value = GetString(range), index = index }).ToList();

			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

			switch (type)
			{
				case SortType.Smart: entries = OrderByAscDesc(entries, entry => entry.value, ascending, Helpers.StringNumericComparer(caseSensitive)).ToList(); break;
				case SortType.String: entries = OrderByAscDesc(entries, entry => entry.value, ascending, stringComparer).ToList(); break;
				case SortType.Length: entries = OrderByAscDesc(entries, entry => entry.value.Length, ascending).ToList(); break;
				case SortType.Integer: entries = OrderByAscDesc(entries, entry => BigInteger.Parse(entry.value), ascending).ToList(); break;
				case SortType.Float: entries = OrderByAscDesc(entries, entry => double.Parse(entry.value), ascending).ToList(); break;
				case SortType.Hex: entries = OrderByAscDesc(entries, entry => BigInteger.Parse("0" + entry.value, NumberStyles.HexNumber), ascending).ToList(); break;
				case SortType.DateTime: entries = OrderByAscDesc(entries, entry => DateTime.Parse(entry.value), ascending).ToList(); break;
				case SortType.Keys:
					{
						var sort = KeysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = OrderByAscDesc(entries, entry => entry.value, ascending, Comparer<string>.Create((value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue))).ToList();
					}
					break;
				case SortType.Reverse: entries.Reverse(); break;
				case SortType.Randomize: entries = entries.OrderBy(entry => random.Next()).ToList(); break;
				case SortType.Frequency:
					{
						var frequency = entries.GroupBy(a => a.value, stringComparer).ToDictionary(a => a.Key, a => a.Count(), stringComparer);
						entries = OrderByAscDesc(entries, entry => frequency[entry.value], ascending).ToList();
					}
					break;
			}

			if (withinRegions)
			{
				var regions = GetEnclosingRegions();
				var regionIndexes = Regions.Select((region, index) => new { region = region, index = index }).ToDictionary(obj => obj.region, obj => obj.index);
				entries = entries.OrderBy(entry => regionIndexes[regions[entry.index]]).ToList();
			}

			return entries.Select(entry => entry.index).ToList();
		}

		internal SortDialog.Result Command_Edit_Sort_Dialog() => SortDialog.Run(WindowParent);

		internal void Command_Edit_Sort(SortDialog.Result result)
		{
			var regions = GetSortSource(result.SortScope);
			var ordering = GetOrdering(result.WithinRegions, result.SortType, result.CaseSensitive, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

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
				Regions.Replace(newRegions);
		}
	}
}
