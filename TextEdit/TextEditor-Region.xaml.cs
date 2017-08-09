using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		List<List<string>> GetRegionsWithSelections(bool mustBeSameSize = true)
		{
			var list = GetSelectionStrings().Zip(GetEnclosingRegions(true), (selection, region) => new { selection, region }).GroupBy(obj => obj.region).Select(group => group.Select(obj => obj.selection).ToList()).ToList();
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");
			return list;
		}

		void SetRegionsWithSelections(List<List<string>> list, bool mustBeSameSize = true)
		{
			if (!Regions.Any())
				throw new Exception("Must have selected regions");
			if (!list.Any())
				return;
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");

			var sb = new StringBuilder(list.Sum(items => items.Sum(item => item.Length) + Data.DefaultEnding.Length));
			var start = Regions.First().Start;
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
			strs.AddRange(Enumerable.Repeat("", Regions.Count - 1));
			Replace(Regions, strs);
			Regions.Replace(newRegions);
			Selections.Replace(newSelections);
		}

		void Command_Region_SetSelections() => Regions.Replace(Selections);

		void Command_Region_AddSelections() => Regions.AddRange(Selections);

		void Command_Region_RemoveSelections()
		{
			var regions = new List<Range>();
			var regionIndex = 0;
			foreach (var selection in Selections)
			{
				while ((regionIndex < Regions.Count) && (Regions[regionIndex].End < selection.Start))
					regions.Add(Regions[regionIndex++]);
				while ((regionIndex < Regions.Count) && (((Regions[regionIndex].Start == selection.Start) && (Regions[regionIndex].End == selection.End)) || ((Regions[regionIndex].End > selection.Start) && (Regions[regionIndex].Start < selection.End))))
					++regionIndex;
			}
			while (regionIndex < Regions.Count)
				regions.Add(Regions[regionIndex++]);
			Regions.Replace(regions);
		}

		void Command_Region_ReplaceSelections()
		{
			var regions = new List<Range>();
			var regionIndex = 0;
			foreach (var selection in Selections)
			{
				while ((regionIndex < Regions.Count) && (Regions[regionIndex].End < selection.Start))
					regions.Add(Regions[regionIndex++]);
				while ((regionIndex < Regions.Count) && (((Regions[regionIndex].Start == selection.Start) && (Regions[regionIndex].End == selection.End)) || ((Regions[regionIndex].End > selection.Start) && (Regions[regionIndex].Start < selection.End))))
					++regionIndex;
				regions.Add(selection);
			}
			while (regionIndex < Regions.Count)
				regions.Add(Regions[regionIndex++]);
			Regions.Replace(regions);
		}

		void Command_Region_LimitToSelections()
		{
			var regions = new List<Range>();
			var regionIndex = 0;
			foreach (var selection in Selections)
			{
				while ((regionIndex < Regions.Count) && (Regions[regionIndex].Start < selection.Start))
					++regionIndex;
				while ((regionIndex < Regions.Count) && (Regions[regionIndex].Start >= selection.Start) && (Regions[regionIndex].End <= selection.End))
					regions.Add(Regions[regionIndex++]);
			}
			Regions.Replace(regions);
		}

		void Command_Region_Clear() => Regions.Clear();

		void Command_Region_WithEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		void Command_Region_WithoutEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());

		void Command_Region_SelectEnclosingRegion() => Selections.Replace(GetEnclosingRegions());

		void Command_Region_CopyEnclosingRegion() => SetClipboardStrings(GetEnclosingRegions().Select(range => GetString(range)).ToList());

		void Command_Region_CopyEnclosingRegionIndex() => SetClipboardStrings(GetEnclosingRegions().Select(region => (Regions.IndexOf(region) + 1).ToString()).ToList());

		void Command_Region_TransformSelections_Flatten() => SetRegionsWithSelections(GetRegionsWithSelections(false), false);

		void Command_Region_TransformSelections_Transpose()
		{
			var regions = GetRegionsWithSelections();
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelections(Enumerable.Range(0, count).Select(index => regions.Select(strs => strs[index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_RotateLeft()
		{
			var regions = GetRegionsWithSelections();
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelections(Enumerable.Range(0, count).Select(index => regions.Select(region => region[region.Count - 1 - index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_RotateRight()
		{
			var regions = GetRegionsWithSelections();
			regions.Reverse();
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelections(Enumerable.Range(0, count).Select(index => regions.Select(region => region[index]).ToList()).ToList());
		}

		void Command_Region_TransformSelections_Rotate180()
		{
			var regions = GetRegionsWithSelections();
			regions.Reverse();
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelections(regions);
		}

		void Command_Region_TransformSelections_MirrorHorizontal()
		{
			var regions = GetRegionsWithSelections();
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelections(regions);
		}

		void Command_Region_TransformSelections_MirrorVertical()
		{
			var regions = GetRegionsWithSelections();
			regions.Reverse();
			SetRegionsWithSelections(regions);
		}
	}
}
