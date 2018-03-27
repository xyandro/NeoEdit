using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.TextEdit.Dialogs;

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
			Replace(useRegions.ToList(), strs);
			SetRegions(useRegion, newRegions);
			SetSelections(newSelections);
		}

		RegionModifyRegionsDialog.Result Command_Region_ModifyRegions_Dialog() => RegionModifyRegionsDialog.Run(WindowParent);

		void Command_Region_ModifyRegions(RegionModifyRegionsDialog.Result result)
		{
			switch (result.Action)
			{
				case RegionModifyRegionsDialog.Action.Select:
					SetSelections(result.Regions.SelectMany(useRegion => Regions[useRegion]).ToList());
					break;
				case RegionModifyRegionsDialog.Action.Set:
					foreach (var useRegion in result.Regions)
						SetRegions(useRegion, Selections.ToList());
					break;
				case RegionModifyRegionsDialog.Action.Clear:
					foreach (var useRegion in result.Regions)
						SetRegions(useRegion, new List<Range>());
					break;
				case RegionModifyRegionsDialog.Action.Remove:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var regionIndex = 0;
						foreach (var selection in Selections)
						{
							while ((regionIndex < Regions[useRegion].Count) && (Regions[useRegion][regionIndex].End <= selection.Start) && (!Regions[useRegion][regionIndex].Equals(selection)))
								newRegions.Add(Regions[useRegion][regionIndex++]);
							while ((regionIndex < Regions[useRegion].Count) && ((Regions[useRegion][regionIndex].Equals(selection)) || ((Regions[useRegion][regionIndex].End > selection.Start) && (Regions[useRegion][regionIndex].Start < selection.End))))
								++regionIndex;
						}
						while (regionIndex < Regions[useRegion].Count)
							newRegions.Add(Regions[useRegion][regionIndex++]);
						SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Replace:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var regionIndex = 0;
						foreach (var selection in Selections)
						{
							while ((regionIndex < Regions[useRegion].Count) && (Regions[useRegion][regionIndex].End <= selection.Start) && (!Regions[useRegion][regionIndex].Equals(selection)))
								newRegions.Add(Regions[useRegion][regionIndex++]);
							while ((regionIndex < Regions[useRegion].Count) && ((Regions[useRegion][regionIndex].Equals(selection)) || ((Regions[useRegion][regionIndex].End > selection.Start) && (Regions[useRegion][regionIndex].Start < selection.End))))
								++regionIndex;
							newRegions.Add(selection);
						}
						while (regionIndex < Regions[useRegion].Count)
							newRegions.Add(Regions[useRegion][regionIndex++]);
						SetRegions(useRegion, newRegions);
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
							if ((region == null) && (regionIndex < Regions[useRegion].Count))
								region = Regions[useRegion][regionIndex++];

							if (selectionIndex >= Selections.Count)
							{
								if (region == null)
									break;
								newRegions.Add(region);
								region = null;
							}
							else if (region == null)
								newRegions.Add(Selections[selectionIndex++]);
							else if (region.Equals(Selections[selectionIndex]))
								region = null;
							else if (region.End <= Selections[selectionIndex].Start)
							{
								newRegions.Add(region);
								region = null;
							}
							else if (Selections[selectionIndex].End <= region.Start)
								newRegions.Add(Selections[selectionIndex++]);
							else
							{
								if (region.Start < Selections[selectionIndex].Start)
									newRegions.Add(new Range(region.Start, Selections[selectionIndex].Start));
								if (region.End <= Selections[selectionIndex].End)
									region = null;
								else
									region = new Range(Selections[selectionIndex].End, region.End);
							}
						}
						SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Intersect:
					foreach (var useRegion in result.Regions)
					{
						var newRegions = new List<Range>();
						var startRegionIndex = 0;
						foreach (var selection in Selections)
						{
							var regionIndex = startRegionIndex;
							while ((regionIndex < Regions[useRegion].Count) && (Regions[useRegion][regionIndex].End < selection.Start))
								++regionIndex;
							startRegionIndex = regionIndex;
							while ((regionIndex < Regions[useRegion].Count) && (Regions[useRegion][regionIndex].Start <= selection.End))
							{
								if ((!Regions[useRegion][regionIndex].HasSelection) || (!selection.HasSelection) || ((Regions[useRegion][regionIndex].End != selection.Start) && (Regions[useRegion][regionIndex].Start != selection.End)))
								{
									var newRegion = new Range(Math.Max(Regions[useRegion][regionIndex].Start, selection.Start), Math.Min(Regions[useRegion][regionIndex].End, selection.End));
									if ((newRegions.Count == 0) || (!newRegion.Equals(newRegions[newRegions.Count - 1])))
										newRegions.Add(newRegion);
								}
								++regionIndex;
							}
						}
						SetRegions(useRegion, newRegions);
					}
					break;
				case RegionModifyRegionsDialog.Action.Exclude:
					foreach (var useRegion in result.Regions)
					{
						var regions = Regions[useRegion].ToList();
						var newRegions = new List<Range>();
						var regionIndex = 0;
						var selectionIndex = 0;
						while (regionIndex < regions.Count)
						{
							if (selectionIndex >= Selections.Count)
								newRegions.Add(regions[regionIndex++]);
							else if (Selections[selectionIndex].Equals(regions[regionIndex]))
								regionIndex++;
							else if (regions[regionIndex].End < Selections[selectionIndex].Start)
								newRegions.Add(regions[regionIndex++]);
							else if (Selections[selectionIndex].End < regions[regionIndex].Start)
								++selectionIndex;
							else
							{
								if (regions[regionIndex].Start < Selections[selectionIndex].Start)
									newRegions.Add(new Range(regions[regionIndex].Start, Selections[selectionIndex].Start));
								while ((regionIndex < regions.Count) && (regions[regionIndex].End <= Selections[selectionIndex].End))
									regionIndex++;
								if ((regionIndex < regions.Count) && (regions[regionIndex].Start < Selections[selectionIndex].End))
									regions[regionIndex] = new Range(Selections[selectionIndex].End, regions[regionIndex].End);
								++selectionIndex;
							}
						}
						SetRegions(useRegion, newRegions);
					}
					break;

			}
		}

		void Command_Region_SetSelections_Region(int? useRegion = null) => Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => SetRegions(key, Selections.ToList()));

		void Command_Region_AddSelections_Region(int? useRegion = null) => Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => SetRegions(key, Regions[key].Concat(Selections).ToList()));

		void Command_Region_RemoveSelections_Region(int? useRegion = null)
		{
			foreach (var key in Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < Regions[key].Count) && (Regions[key][regionIndex].End <= selection.Start) && (!Regions[key][regionIndex].Equals(selection)))
						regions.Add(Regions[key][regionIndex++]);
					while ((regionIndex < Regions[key].Count) && ((Regions[key][regionIndex].Equals(selection)) || ((Regions[key][regionIndex].End > selection.Start) && (Regions[key][regionIndex].Start < selection.End))))
						++regionIndex;
				}
				while (regionIndex < Regions[key].Count)
					regions.Add(Regions[key][regionIndex++]);
				SetRegions(key, regions);
			}
		}

		void Command_Region_ReplaceSelections_Region(int? useRegion = null)
		{
			foreach (var key in Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < Regions[key].Count) && (Regions[key][regionIndex].End <= selection.Start) && (!Regions[key][regionIndex].Equals(selection)))
						regions.Add(Regions[key][regionIndex++]);
					while ((regionIndex < Regions[key].Count) && ((Regions[key][regionIndex].Equals(selection)) || ((Regions[key][regionIndex].End > selection.Start) && (Regions[key][regionIndex].Start < selection.End))))
						++regionIndex;
					regions.Add(selection);
				}
				while (regionIndex < Regions[key].Count)
					regions.Add(Regions[key][regionIndex++]);
				SetRegions(key, regions);
			}
		}

		void Command_Region_LimitToSelections_Region(int? useRegion = null)
		{
			foreach (var key in Regions.Keys.ToList())
			{
				if (key != (useRegion ?? key))
					continue;

				var regions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < Regions[key].Count) && (Regions[key][regionIndex].Start < selection.Start))
						++regionIndex;
					while ((regionIndex < Regions[key].Count) && (Regions[key][regionIndex].Start >= selection.Start) && (Regions[key][regionIndex].End <= selection.End))
						regions.Add(Regions[key][regionIndex++]);
				}
				SetRegions(key, regions);
			}
		}

		void Command_Region_Clear_Region(int? useRegion = null) => Regions.Keys.ToList().Where(key => key == (useRegion ?? key)).ForEach(key => SetRegions(key, new List<Range>()));

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
			Replace(useRegions.ToList(), newRegionStrs);
			SetRegions(useRegion, newRegions);
			SetSelections(newSelections);
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
			SetSelections(sels);
		}

		void Command_Region_Select_EnclosingRegion_Region(int useRegion) => SetSelections(GetEnclosingRegions(useRegion));

		void Command_Region_Select_WithEnclosingRegion_Region(int useRegion) => SetSelections(Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		void Command_Region_Select_WithoutEnclosingRegion_Region(int useRegion) => SetSelections(Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());
	}
}
