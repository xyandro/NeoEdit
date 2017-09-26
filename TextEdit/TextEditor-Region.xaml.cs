using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		List<Tuple<Range, List<Range>>> GetRegionsWithSelections(int useRegion)
		{
			var result = new List<Tuple<Range, List<Range>>>();
			var currentSelection = 0;
			foreach (var region in Regions[useRegion])
			{
				var sels = new List<Range>();
				if ((currentSelection < Selections.Count) && (Selections[currentSelection].Start < region.Start))
					throw new Exception("No region found.  All selections must be inside a region.");
				while ((currentSelection < Selections.Count) && (Selections[currentSelection].End <= region.End))
					sels.Add(Selections[currentSelection++]);

				result.Add(Tuple.Create(region, sels));
			}
			if (currentSelection != Selections.Count)
				throw new Exception("No region found.  All selections must be inside a region.");

			return result;
		}

		List<List<string>> GetRegionsWithSelectionsText(int useRegion, bool mustBeSameSize = true)
		{
			var list = GetSelectionStrings().Zip(GetEnclosingRegions(useRegion, true), (selection, region) => new { selection, region }).GroupBy(obj => obj.region).Select(group => group.Select(obj => obj.selection).ToList()).ToList();
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");
			return list;
		}

		void SetRegionsWithSelectionsText(int useRegion, List<List<string>> list, bool mustBeSameSize = true)
		{
			var useRegions = Regions[useRegion];
			if (!useRegions.Any())
				throw new Exception("Must have selected regions");
			if (!list.Any())
				return;
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");

			var sb = new StringBuilder(list.Sum(items => items.Sum(item => item.Length) + Data.DefaultEnding.Length));
			var start = useRegions.First().Start;
			var newRegions = new List<Range>();
			var newSelections = new List<Range>();
			foreach (var row in list)
			{
				var regionStart = sb.Length;
				foreach (var column in row)
				{
					newSelections.Add(Range.FromIndex(start + sb.Length, column.Length));
					sb.Append(column);
				}
				sb.Append(Data.DefaultEnding);
				newRegions.Add(Range.FromIndex(start + regionStart, sb.Length - regionStart));
			}

			var strs = new List<string> { sb.ToString() };
			strs.AddRange(Enumerable.Repeat("", useRegions.Count - 1));
			Replace(useRegions, strs);
			useRegions.Replace(newRegions);
			Selections.Replace(newSelections);
		}

		void Command_Region_SetSelections_Region(int? useRegion = null) => Regions.Where(pair => pair.Key == (useRegion ?? pair.Key)).ForEach(pair => pair.Value.Replace(Selections));

		void Command_Region_AddSelections_Region(int? useRegion = null) => Regions.Where(pair => pair.Key == (useRegion ?? pair.Key)).ForEach(pair => pair.Value.AddRange(Selections));

		void Command_Region_RemoveSelections_Region(int? useRegion = null)
		{
			foreach (var pair in Regions)
			{
				if (pair.Key != (useRegion ?? pair.Key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < pair.Value.Count) && (pair.Value[regionIndex].End <= selection.Start) && (!pair.Value[regionIndex].Equals(selection)))
						regions.Add(pair.Value[regionIndex++]);
					while ((regionIndex < pair.Value.Count) && ((pair.Value[regionIndex].Equals(selection)) || ((pair.Value[regionIndex].End > selection.Start) && (pair.Value[regionIndex].Start < selection.End))))
						++regionIndex;
				}
				while (regionIndex < pair.Value.Count)
					regions.Add(pair.Value[regionIndex++]);
				pair.Value.Replace(regions);
			}
		}

		void Command_Region_ReplaceSelections_Region(int? useRegion = null)
		{
			foreach (var pair in Regions)
			{
				if (pair.Key != (useRegion ?? pair.Key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < pair.Value.Count) && (pair.Value[regionIndex].End <= selection.Start) && (!pair.Value[regionIndex].Equals(selection)))
						regions.Add(pair.Value[regionIndex++]);
					while ((regionIndex < pair.Value.Count) && ((pair.Value[regionIndex].Equals(selection)) || ((pair.Value[regionIndex].End > selection.Start) && (pair.Value[regionIndex].Start < selection.End))))
						++regionIndex;
					regions.Add(selection);
				}
				while (regionIndex < pair.Value.Count)
					regions.Add(pair.Value[regionIndex++]);
				pair.Value.Replace(regions);
			}
		}

		void Command_Region_LimitToSelections_Region(int? useRegion = null)
		{
			foreach (var pair in Regions)
			{
				if (pair.Key != (useRegion ?? pair.Key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < pair.Value.Count) && (pair.Value[regionIndex].Start < selection.Start))
						++regionIndex;
					while ((regionIndex < pair.Value.Count) && (pair.Value[regionIndex].Start >= selection.Start) && (pair.Value[regionIndex].End <= selection.End))
						regions.Add(pair.Value[regionIndex++]);
				}
				pair.Value.Replace(regions);
			}
		}

		void Command_Region_Clear_Region(int? useRegion = null) => Regions.Where(pair => pair.Key == (useRegion ?? pair.Key)).ForEach(pair => pair.Value.Clear());

		void Command_Region_RepeatBySelections_Region(int useRegion)
		{
			var useRegions = Regions[useRegion];
			var regionsWithSelections = GetRegionsWithSelections(useRegion);
			var offset = 0;
			var newRegionStrs = new List<string>();
			var newRegions = new List<Range>();
			var newSelections = new List<Range>();
			foreach (var regionsWithSelection in regionsWithSelections)
			{
				newRegionStrs.Add(string.Join("", Enumerable.Repeat(GetString(regionsWithSelection.Item1), regionsWithSelection.Item2.Count)));
				offset -= regionsWithSelection.Item1.Length;
				foreach (var selection in regionsWithSelection.Item2)
				{
					offset += regionsWithSelection.Item1.Length;
					newRegions.Add(new Range(regionsWithSelection.Item1.Cursor + offset, regionsWithSelection.Item1.Anchor + offset));
					newSelections.Add(new Range(selection.Cursor + offset, selection.Anchor + offset));
				}
			}
			Replace(useRegions, newRegionStrs);
			useRegions.Replace(newRegions);
			Selections.Replace(newSelections);
		}

		void Command_Region_CopyEnclosingRegion_Region(int useRegion) => SetClipboardStrings(GetEnclosingRegions(useRegion).Select(range => GetString(range)).ToList());

		void Command_Region_CopyEnclosingRegionIndex_Region(int useRegion) => SetClipboardStrings(GetEnclosingRegions(useRegion).Select(region => (Regions[useRegion].IndexOf(region) + 1).ToString()).ToList());

		void Command_Region_TransformSelections_Flatten_Region(int useRegion) => SetRegionsWithSelectionsText(useRegion, GetRegionsWithSelectionsText(useRegion, false), false);

		void Command_Region_TransformSelections_Transpose_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions.Select(strs => strs[index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_RotateLeft_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions.Select(region => region[region.Count - 1 - index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_RotateRight_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			regions.Reverse();
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions.Select(region => region[index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_Rotate180_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			regions.Reverse();
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(useRegion, regions);
		}

		void Command_Region_TransformSelections_MirrorHorizontal_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(useRegion, regions);
		}

		void Command_Region_TransformSelections_MirrorVertical_Region(int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(useRegion);
			regions.Reverse();
			SetRegionsWithSelectionsText(useRegion, regions);
		}

		void Command_Region_Select_Regions_Region(bool shiftDown, int? useRegion = null)
		{
			var sels = shiftDown ? Selections.ToList() : new List<Range>();
			sels.AddRange(Regions.Where(pair => pair.Key == (useRegion ?? pair.Key)).SelectMany(pair => pair.Value));
			Selections.Replace(sels);
		}

		void Command_Region_Select_EnclosingRegion_Region(int useRegion) => Selections.Replace(GetEnclosingRegions(useRegion));

		void Command_Region_Select_WithEnclosingRegion_Region(int useRegion) => Selections.Replace(Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		void Command_Region_Select_WithoutEnclosingRegion_Region(int useRegion) => Selections.Replace(Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());
	}
}
