using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorWindow
	{
		public static RoutedCommand Command_Sort_String = new RoutedCommand();
		public static RoutedCommand Command_Sort_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Sort_Keys = new RoutedCommand();
		public static RoutedCommand Command_Sort_Reverse = new RoutedCommand();
		public static RoutedCommand Command_Sort_Randomize = new RoutedCommand();
		public static RoutedCommand Command_Sort_Length = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_String = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_Keys = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_Reverse = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_Randomize = new RoutedCommand();
		public static RoutedCommand Command_Sort_Lines_Length = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_String = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_Keys = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_Reverse = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_Randomize = new RoutedCommand();
		public static RoutedCommand Command_Sort_Regions_Length = new RoutedCommand();

		enum SortScope { Selections, Lines, Regions }
		enum SortType { String, Numeric, Keys, Reverse, Randomize, Length }

		RangeList GetSortLines()
		{
			return Selections.Select(range => Data.GetOffsetLine(range.Start)).Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line))).ToList();
		}

		RangeList GetSortRegions()
		{
			var regions = new RangeList();
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

		string SortStr(string str)
		{
			return Regex.Replace(str, @"\d+", match => new string('0', Math.Max(0, 20 - match.Value.Length)) + match.Value);
		}

		RangeList GetRegions(SortScope scope)
		{
			RangeList regions = null;
			switch (scope)
			{
				case SortScope.Selections: regions = Selections.ToList(); break;
				case SortScope.Lines: regions = GetSortLines(); break;
				case SortScope.Regions: regions = GetSortRegions(); break;
				default: throw new Exception("Invalid sort type");
			}

			if (Selections.Count != regions.Count)
				throw new Exception("Selections and regions counts must match");

			// Sanity check; soundn't happen
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

		List<int> GetOrdering(SortType type)
		{
			var entries = Selections.Select((range, index) => new { value = GetString(range), index = index }).ToList();
			switch (type)
			{
				case SortType.String: entries = entries.OrderBy(entry => entry.value).ToList(); break;
				case SortType.Numeric: entries = entries.OrderBy(entry => SortStr(entry.value)).ToList(); break;
				case SortType.Keys:
					{
						var sort = keysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = entries.OrderBy(entry => entry.value, (value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue)).ToList();
					}
					break;
				case SortType.Reverse: entries.Reverse(); break;
				case SortType.Randomize:
					{
						var random = new Random();
						entries = entries.OrderBy(entry => random.Next()).ToList();
					}
					break;
				case SortType.Length: entries = entries.OrderBy(entry => entry.value.Length).ToList(); break;
			}

			return entries.Select(entry => entry.index).ToList();
		}

		void Sort(SortScope scope, SortType type)
		{
			var regions = GetRegions(scope);
			var ordering = GetOrdering(type);
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

			Replace(regions, orderedRegionText, false);

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
			if (scope == SortScope.Regions)
				Marks.Replace(newRegions);
		}

		bool RunSortCommand(ICommand command)
		{
			var result = true;

			if (command == Command_Sort_String)
				Sort(SortScope.Selections, SortType.String);
			else if (command == Command_Sort_Numeric)
				Sort(SortScope.Selections, SortType.Numeric);
			else if (command == Command_Sort_Keys)
				Sort(SortScope.Selections, SortType.Keys);
			else if (command == Command_Sort_Reverse)
				Sort(SortScope.Selections, SortType.Reverse);
			else if (command == Command_Sort_Randomize)
				Sort(SortScope.Selections, SortType.Randomize);
			else if (command == Command_Sort_Length)
				Sort(SortScope.Selections, SortType.Length);
			else if (command == Command_Sort_Lines_String)
				Sort(SortScope.Lines, SortType.String);
			else if (command == Command_Sort_Lines_Numeric)
				Sort(SortScope.Lines, SortType.Numeric);
			else if (command == Command_Sort_Lines_Keys)
				Sort(SortScope.Lines, SortType.Keys);
			else if (command == Command_Sort_Lines_Reverse)
				Sort(SortScope.Lines, SortType.Reverse);
			else if (command == Command_Sort_Lines_Randomize)
				Sort(SortScope.Lines, SortType.Randomize);
			else if (command == Command_Sort_Lines_Length)
				Sort(SortScope.Lines, SortType.Length);
			else if (command == Command_Sort_Regions_String)
				Sort(SortScope.Regions, SortType.String);
			else if (command == Command_Sort_Regions_Numeric)
				Sort(SortScope.Regions, SortType.Numeric);
			else if (command == Command_Sort_Regions_Keys)
				Sort(SortScope.Regions, SortType.Keys);
			else if (command == Command_Sort_Regions_Reverse)
				Sort(SortScope.Regions, SortType.Reverse);
			else if (command == Command_Sort_Regions_Randomize)
				Sort(SortScope.Regions, SortType.Randomize);
			else if (command == Command_Sort_Regions_Length)
				Sort(SortScope.Regions, SortType.Length);
			else
				result = false;

			return result;
		}
	}
}
