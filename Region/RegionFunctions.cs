using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	public static class RegionFunctions
	{
		static List<Tuple<Range, List<Range>>> GetRegionsWithSelections(ITextEditor te, int useRegion)
		{
			var result = new List<Tuple<Range, List<Range>>>();
			var currentSelection = 0;
			foreach (var region in te.Regions[useRegion])
			{
				var sels = new List<Range>();
				if ((currentSelection < te.Selections.Count) && (te.Selections[currentSelection].Start < region.Start))
					throw new Exception("No region found.  All selections must be inside a region.");
				while ((currentSelection < te.Selections.Count) && (te.Selections[currentSelection].End <= region.End))
					sels.Add(te.Selections[currentSelection++]);

				result.Add(Tuple.Create(region, sels));
			}
			if (currentSelection != te.Selections.Count)
				throw new Exception("No region found.  All selections must be inside a region.");

			return result;
		}

		static List<List<string>> GetRegionsWithSelectionsText(ITextEditor te, int useRegion, bool mustBeSameSize = true)
		{
			var list = te.GetSelectionStrings().Zip(te.GetEnclosingRegions(useRegion, true), (selection, region) => new { selection, region }).GroupBy(obj => obj.region).Select(group => group.Select(obj => obj.selection).ToList()).ToList();
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");
			return list;
		}

		static void SetRegionsWithSelectionsText(ITextEditor te, int useRegion, List<List<string>> list, bool mustBeSameSize = true)
		{
			var useRegions = te.Regions[useRegion];
			if (!useRegions.Any())
				throw new Exception("Must have selected regions");
			if (!list.Any())
				return;
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");

			var sb = new StringBuilder(list.Sum(items => items.Sum(item => item.Length) + te.Data.DefaultEnding.Length));
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
				sb.Append(te.Data.DefaultEnding);
				newRegions.Add(Range.FromIndex(start + regionStart, sb.Length - regionStart));
			}

			var strs = new List<string> { sb.ToString() };
			strs.AddRange(Enumerable.Repeat("", useRegions.Count - 1));
			te.Replace(useRegions.ToList(), strs);
			te.SetRegions(useRegion, newRegions);
			te.SetSelections(newSelections);
		}

		static public RegionModifyRegionsDialog.Result Command_Region_ModifyRegions_Dialog(ITextEditor te) => RegionModifyRegionsDialog.Run(te.WindowParent);

		static public void Command_Region_ModifyRegions(ITextEditor te, RegionModifyRegionsDialog.Result result)
		{
			switch (result.Action)
			{
				case RegionModifyRegionsDialog.Action.Select:
					te.SetSelections(result.Regions.SelectMany(useRegion => te.Regions[useRegion]).ToList());
					break;
				case RegionModifyRegionsDialog.Action.Set:
					foreach (var useRegion in result.Regions)
						te.SetRegions(useRegion, te.Selections.ToList());
					break;
				case RegionModifyRegionsDialog.Action.Clear:
					foreach (var useRegion in result.Regions)
						te.SetRegions(useRegion, new List<Range>());
					break;
				case RegionModifyRegionsDialog.Action.Remove:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var regionIndex = 0;
						foreach (var selection in te.Selections)
						{
							while ((regionIndex < te.Regions[useRegion].Count) && (te.Regions[useRegion][regionIndex].End <= selection.Start) && (!te.Regions[useRegion][regionIndex].Equals(selection)))
								newRegions.Add(te.Regions[useRegion][regionIndex++]);
							while ((regionIndex < te.Regions[useRegion].Count) && ((te.Regions[useRegion][regionIndex].Equals(selection)) || ((te.Regions[useRegion][regionIndex].End > selection.Start) && (te.Regions[useRegion][regionIndex].Start < selection.End))))
								++regionIndex;
						}
						while (regionIndex < te.Regions[useRegion].Count)
							newRegions.Add(te.Regions[useRegion][regionIndex++]);
						te.SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Replace:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var regionIndex = 0;
						foreach (var selection in te.Selections)
						{
							while ((regionIndex < te.Regions[useRegion].Count) && (te.Regions[useRegion][regionIndex].End <= selection.Start) && (!te.Regions[useRegion][regionIndex].Equals(selection)))
								newRegions.Add(te.Regions[useRegion][regionIndex++]);
							while ((regionIndex < te.Regions[useRegion].Count) && ((te.Regions[useRegion][regionIndex].Equals(selection)) || ((te.Regions[useRegion][regionIndex].End > selection.Start) && (te.Regions[useRegion][regionIndex].Start < selection.End))))
								++regionIndex;
							newRegions.Add(selection);
						}
						while (regionIndex < te.Regions[useRegion].Count)
							newRegions.Add(te.Regions[useRegion][regionIndex++]);
						te.SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Unite:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						int regionIndex = 0, selectionIndex = 0;
						Range region = null;
						while (true)
						{
							if ((region == null) && (regionIndex < te.Regions[useRegion].Count))
								region = te.Regions[useRegion][regionIndex++];

							if (selectionIndex >= te.Selections.Count)
							{
								if (region == null)
									break;
								newRegions.Add(region);
								region = null;
							}
							else if (region == null)
								newRegions.Add(te.Selections[selectionIndex++]);
							else if (region.Equals(te.Selections[selectionIndex]))
								region = null;
							else if (region.End <= te.Selections[selectionIndex].Start)
							{
								newRegions.Add(region);
								region = null;
							}
							else if (te.Selections[selectionIndex].End <= region.Start)
								newRegions.Add(te.Selections[selectionIndex++]);
							else
							{
								if (region.Start < te.Selections[selectionIndex].Start)
									newRegions.Add(new Range(region.Start, te.Selections[selectionIndex].Start));
								if (region.End <= te.Selections[selectionIndex].End)
									region = null;
								else
									region = new Range(te.Selections[selectionIndex].End, region.End);
							}
						}
						te.SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Intersect:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var startRegionIndex = 0;
						foreach (var selection in te.Selections)
						{
							var regionIndex = startRegionIndex;
							while ((regionIndex < te.Regions[useRegion].Count) && (te.Regions[useRegion][regionIndex].End < selection.Start))
								++regionIndex;
							startRegionIndex = regionIndex;
							while ((regionIndex < te.Regions[useRegion].Count) && (te.Regions[useRegion][regionIndex].Start <= selection.End))
							{
								if ((!te.Regions[useRegion][regionIndex].HasSelection) || (!selection.HasSelection) || ((te.Regions[useRegion][regionIndex].End != selection.Start) && (te.Regions[useRegion][regionIndex].Start != selection.End)))
								{
									var newRegion = new Range(Math.Max(te.Regions[useRegion][regionIndex].Start, selection.Start), Math.Min(te.Regions[useRegion][regionIndex].End, selection.End));
									if ((newRegions.Count == 0) || (!newRegion.Equals(newRegions[newRegions.Count - 1])))
										newRegions.Add(newRegion);
								}
								++regionIndex;
							}
						}
						te.SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Exclude:
					foreach (var useRegion in result.Regions)
					{
						var regions = te.Regions[useRegion].ToList();
						var newRegions = new List<Range>();
						var regionIndex = 0;
						var selectionIndex = 0;
						while (regionIndex < regions.Count)
						{
							if (selectionIndex >= te.Selections.Count)
								newRegions.Add(regions[regionIndex++]);
							else if (te.Selections[selectionIndex].Equals(regions[regionIndex]))
								regionIndex++;
							else if (regions[regionIndex].End < te.Selections[selectionIndex].Start)
								newRegions.Add(regions[regionIndex++]);
							else if (te.Selections[selectionIndex].End < regions[regionIndex].Start)
								++selectionIndex;
							else
							{
								if (regions[regionIndex].Start < te.Selections[selectionIndex].Start)
									newRegions.Add(new Range(regions[regionIndex].Start, te.Selections[selectionIndex].Start));
								while ((regionIndex < regions.Count) && (regions[regionIndex].End <= te.Selections[selectionIndex].End))
									regionIndex++;
								if ((regionIndex < regions.Count) && (regions[regionIndex].Start < te.Selections[selectionIndex].End))
									regions[regionIndex] = new Range(te.Selections[selectionIndex].End, regions[regionIndex].End);
								++selectionIndex;
							}
						}
						te.SetRegions(useRegion, newRegions);
					}
					break;

			}
		}

		static public void Command_Region_SetSelections_Region(ITextEditor te, int? useRegion = null) => te.Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => te.SetRegions(key, te.Selections.ToList()));

		static public void Command_Region_AddSelections_Region(ITextEditor te, int? useRegion = null) => te.Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => te.SetRegions(key, te.Regions[key].Concat(te.Selections).ToList()));

		static public void Command_Region_RemoveSelections_Region(ITextEditor te, int? useRegion = null)
		{
			foreach (var key in te.Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in te.Selections)
				{
					while ((regionIndex < te.Regions[key].Count) && (te.Regions[key][regionIndex].End <= selection.Start) && (!te.Regions[key][regionIndex].Equals(selection)))
						regions.Add(te.Regions[key][regionIndex++]);
					while ((regionIndex < te.Regions[key].Count) && ((te.Regions[key][regionIndex].Equals(selection)) || ((te.Regions[key][regionIndex].End > selection.Start) && (te.Regions[key][regionIndex].Start < selection.End))))
						++regionIndex;
				}
				while (regionIndex < te.Regions[key].Count)
					regions.Add(te.Regions[key][regionIndex++]);
				te.SetRegions(key, regions);
			}
		}

		static public void Command_Region_ReplaceSelections_Region(ITextEditor te, int? useRegion = null)
		{
			foreach (var key in te.Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in te.Selections)
				{
					while ((regionIndex < te.Regions[key].Count) && (te.Regions[key][regionIndex].End <= selection.Start) && (!te.Regions[key][regionIndex].Equals(selection)))
						regions.Add(te.Regions[key][regionIndex++]);
					while ((regionIndex < te.Regions[key].Count) && ((te.Regions[key][regionIndex].Equals(selection)) || ((te.Regions[key][regionIndex].End > selection.Start) && (te.Regions[key][regionIndex].Start < selection.End))))
						++regionIndex;
					regions.Add(selection);
				}
				while (regionIndex < te.Regions[key].Count)
					regions.Add(te.Regions[key][regionIndex++]);
				te.SetRegions(key, regions);
			}
		}

		static public void Command_Region_LimitToSelections_Region(ITextEditor te, int? useRegion = null)
		{
			foreach (var key in te.Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in te.Selections)
				{
					while ((regionIndex < te.Regions[key].Count) && (te.Regions[key][regionIndex].Start < selection.Start))
						++regionIndex;
					while ((regionIndex < te.Regions[key].Count) && (te.Regions[key][regionIndex].Start >= selection.Start) && (te.Regions[key][regionIndex].End <= selection.End))
						regions.Add(te.Regions[key][regionIndex++]);
				}
				te.SetRegions(key, regions);
			}
		}

		static public void Command_Region_Clear_Region(ITextEditor te, int? useRegion = null) => te.Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => te.SetRegions(key, new List<Range>()));

		static public void Command_Region_RepeatBySelections_Region(ITextEditor te, int useRegion)
		{
			var useRegions = te.Regions[useRegion];
			var regionsWithSelections = GetRegionsWithSelections(te, useRegion);
			var offset = 0;
			var newRegionStrs = new List<string>();
			var newRegions = new List<Range>();
			var newSelections = new List<Range>();
			foreach (var regionsWithSelection in regionsWithSelections)
			{
				newRegionStrs.Add(string.Join("", Enumerable.Repeat(te.GetString(regionsWithSelection.Item1), regionsWithSelection.Item2.Count)));
				offset -= regionsWithSelection.Item1.Length;
				foreach (var selection in regionsWithSelection.Item2)
				{
					offset += regionsWithSelection.Item1.Length;
					newRegions.Add(new Range(regionsWithSelection.Item1.Cursor + offset, regionsWithSelection.Item1.Anchor + offset));
					newSelections.Add(new Range(selection.Cursor + offset, selection.Anchor + offset));
				}
			}
			te.Replace(useRegions.ToList(), newRegionStrs);
			te.SetRegions(useRegion, newRegions);
			te.SetSelections(newSelections);
		}

		static public void Command_Region_CopyEnclosingRegion_Region(ITextEditor te, int useRegion) => te.SetClipboardStrings(te.GetEnclosingRegions(useRegion).Select(range => te.GetString(range)).ToList());

		static public void Command_Region_CopyEnclosingRegionIndex_Region(ITextEditor te, int useRegion) => te.SetClipboardStrings(te.GetEnclosingRegions(useRegion).Select(region => (te.Regions[useRegion].IndexOf(region) + 1).ToString()).ToList());

		static public void Command_Region_TransformSelections_Flatten_Region(ITextEditor te, int useRegion) => SetRegionsWithSelectionsText(te, useRegion, GetRegionsWithSelectionsText(te, useRegion, false), false);

		static public void Command_Region_TransformSelections_Transpose_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(te, useRegion, Enumerable.Range(0, count).Select(index => regions.Select(strs => strs[index]).ToList()).ToList());
		}

		static public void Command_Region_TransformSelections_RotateLeft_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(te, useRegion, Enumerable.Range(0, count).Select(index => regions.Select(region => region[region.Count - 1 - index]).ToList()).ToList());
		}

		static public void Command_Region_TransformSelections_RotateRight_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			regions.Reverse();
			var count = regions.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(te, useRegion, Enumerable.Range(0, count).Select(index => regions.Select(region => region[index]).ToList()).ToList());
		}

		static public void Command_Region_TransformSelections_Rotate180_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			regions.Reverse();
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(te, useRegion, regions);
		}

		static public void Command_Region_TransformSelections_MirrorHorizontal_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			regions.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(te, useRegion, regions);
		}

		static public void Command_Region_TransformSelections_MirrorVertical_Region(ITextEditor te, int useRegion)
		{
			var regions = GetRegionsWithSelectionsText(te, useRegion);
			regions.Reverse();
			SetRegionsWithSelectionsText(te, useRegion, regions);
		}

		static public void Command_Region_Select_Regions_Region(ITextEditor te, bool shiftDown, int? useRegion = null)
		{
			var sels = shiftDown ? te.Selections.ToList() : new List<Range>();
			sels.AddRange(te.Regions.Where(pair => pair.Key == (useRegion ?? pair.Key)).SelectMany(pair => pair.Value));
			te.SetSelections(sels);
		}

		static public void Command_Region_Select_EnclosingRegion_Region(ITextEditor te, int useRegion) => te.SetSelections(te.GetEnclosingRegions(useRegion));

		static public void Command_Region_Select_WithEnclosingRegion_Region(ITextEditor te, int useRegion) => te.SetSelections(te.Selections.Zip(te.GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		static public void Command_Region_Select_WithoutEnclosingRegion_Region(ITextEditor te, int useRegion) => te.SetSelections(te.Selections.Zip(te.GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());
	}
}
