using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		List<Tuple<Range, List<Range>>> GetRegionsWithSelections(int useRegion)
		{
			var result = new List<Tuple<Range, List<Range>>>();
			var currentSelection = 0;
			foreach (var region in GetRegions(useRegion))
			{
				var sels = new List<Range>();
				if ((currentSelection < Selections.Count) && (Selections[currentSelection].Start < region.Start))
					throw new Exception("No region found. All selections must be inside a region.");
				while ((currentSelection < Selections.Count) && (Selections[currentSelection].End <= region.End))
					sels.Add(Selections[currentSelection++]);

				result.Add(Tuple.Create(region, sels));
			}
			if (currentSelection != Selections.Count)
				throw new Exception("No region found. All selections must be inside a region.");

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
			var useRegions = GetRegions(useRegion);
			if (!useRegions.Any())
				throw new Exception("Must have selected regions");
			if (!list.Any())
				return;
			if ((mustBeSameSize) && (list.Select(items => items.Count).Distinct().Count() > 1))
				throw new Exception("All regions must have the same number of selections");

			var sb = new StringBuilder(list.Sum(items => items.Sum(item => item.Length) + TextView.DefaultEnding.Length));
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
				sb.Append(TextView.DefaultEnding);
				newRegions.Add(Range.FromIndex(start + regionStart, sb.Length - regionStart));
			}

			var strs = new List<string> { sb.ToString() };
			strs.AddRange(Enumerable.Repeat("", useRegions.Count - 1));
			Replace(useRegions.ToList(), strs);
			SetRegions(useRegion, newRegions);
			Selections = newSelections;
		}

		List<Range> GetSearchRegions(List<int> regions)
		{
			var searchList = new List<Range>();
			var useRegions = regions.Select(index => GetRegions(index)).ToList();
			if (!useRegions.SelectMany().Any())
				return searchList;

			var useRegionPos = Enumerable.Repeat(0, useRegions.Count).ToList();
			while (true)
			{
				var minRegion = -1;
				var minRange = new Range(int.MaxValue);
				for (var region = 0; region < useRegions.Count; ++region)
					if (useRegionPos[region] < useRegions[region].Count)
						if ((useRegions[region][useRegionPos[region]].Start < minRange.Start) || ((useRegions[region][useRegionPos[region]].Start == minRange.Start) && (useRegions[region][useRegionPos[region]].End < minRange.End)))
						{
							minRegion = region;
							minRange = useRegions[region][useRegionPos[region]];
						}

				if (minRegion == -1)
					break;

				if ((searchList.Count > 0) && (searchList[searchList.Count - 1].End > minRange.Start))
					searchList[searchList.Count - 1] = new Range(minRange.Start, searchList[searchList.Count - 1].Start);
				searchList.Add(minRange);
				++useRegionPos[minRegion];
			}

			return searchList;
		}

		object Configure_Edit_ModifyRegions() => EditModifyRegionsDialog.Run(TabsWindow);

		void Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Result result)
		{
			switch (result.Action)
			{
				case EditModifyRegionsDialog.Action.Select_Select: Command_Edit_ModifyRegions_Select_Select(result.Regions); break;
				case EditModifyRegionsDialog.Action.Select_Previous: Command_Edit_ModifyRegions_Select_Previous(result.Regions); break;
				case EditModifyRegionsDialog.Action.Select_Next: Command_Edit_ModifyRegions_Select_Next(result.Regions); break;
				case EditModifyRegionsDialog.Action.Select_Enclosing: Command_Edit_ModifyRegions_Select_Enclosing(result.Regions); break;
				case EditModifyRegionsDialog.Action.Select_WithEnclosing: Command_Edit_ModifyRegions_Select_WithEnclosing(result.Regions); break;
				case EditModifyRegionsDialog.Action.Select_WithoutEnclosing: Command_Edit_ModifyRegions_Select_WithoutEnclosing(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Set: Command_Edit_ModifyRegions_Modify_Set(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Clear: Command_Edit_ModifyRegions_Modify_Clear(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Remove: Command_Edit_ModifyRegions_Modify_Remove(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Add: Command_Edit_ModifyRegions_Modify_Add(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Unite: Command_Edit_ModifyRegions_Modify_Unite(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Intersect: Command_Edit_ModifyRegions_Modify_Intersect(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Exclude: Command_Edit_ModifyRegions_Modify_Exclude(result.Regions); break;
				case EditModifyRegionsDialog.Action.Modify_Repeat: Command_Edit_ModifyRegions_Modify_Repeat(result.Regions); break;
				case EditModifyRegionsDialog.Action.Copy_Enclosing: Command_Edit_ModifyRegions_Copy_Enclosing(result.Regions); break;
				case EditModifyRegionsDialog.Action.Copy_EnclosingIndex: Command_Edit_ModifyRegions_Copy_EnclosingIndex(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_Flatten: Command_Edit_ModifyRegions_Transform_Flatten(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_Transpose: Command_Edit_ModifyRegions_Transform_Transpose(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_RotateLeft: Command_Edit_ModifyRegions_Transform_RotateLeft(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_RotateRight: Command_Edit_ModifyRegions_Transform_RotateRight(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_Rotate180: Command_Edit_ModifyRegions_Transform_Rotate180(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_MirrorHorizontal: Command_Edit_ModifyRegions_Transform_MirrorHorizontal(result.Regions); break;
				case EditModifyRegionsDialog.Action.Transform_MirrorVertical: Command_Edit_ModifyRegions_Transform_MirrorVertical(result.Regions); break;
			}
		}

		void Command_Edit_ModifyRegions_Select_Select(List<int> regions) => Selections = regions.SelectMany(useRegion => GetRegions(useRegion)).ToList();

		void Command_Edit_ModifyRegions_Select_Previous(List<int> regions)
		{
			if (!Selections.Any())
				return;

			var searchList = GetSearchRegions(regions);

			var newSels = new List<Range>();
			var searchIndex = searchList.Count - 1;
			for (var selCtr = Selections.Count - 1; selCtr >= 0; --selCtr)
			{
				var selection = Selections[selCtr];
				while ((searchIndex >= 0) && ((searchList[searchIndex].Start >= selection.End) || (searchList[searchIndex].Equals(selection))))
					--searchIndex;

				if (searchIndex < 0)
					newSels.Add(searchList[searchList.Count - 1]);
				else
					newSels.Add(searchList[searchIndex]);
			}
			newSels.Reverse();
			Selections = newSels;
		}

		void Command_Edit_ModifyRegions_Select_Next(List<int> regions)
		{
			if (!Selections.Any())
				return;

			var searchList = GetSearchRegions(regions);
			var newSels = new List<Range>();

			if (searchList.Any())
			{
				var searchIndex = 0;
				foreach (var selection in Selections)
				{
					while ((searchIndex < searchList.Count) && ((searchList[searchIndex].End <= selection.Start) || (searchList[searchIndex].Equals(selection))))
						++searchIndex;

					if (searchIndex == searchList.Count)
						newSels.Add(searchList[0]);
					else
						newSels.Add(searchList[searchIndex]);
				}
			}

			Selections = newSels;
		}

		void Command_Edit_ModifyRegions_Select_Enclosing(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			Selections = GetEnclosingRegions(useRegion);
		}

		void Command_Edit_ModifyRegions_Select_WithEnclosing(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			Selections = Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList();
		}

		void Command_Edit_ModifyRegions_Select_WithoutEnclosing(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			Selections = Selections.Zip(GetEnclosingRegions(useRegion, mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList();
		}

		void Command_Edit_ModifyRegions_Modify_Set(List<int> regions) => regions.ForEach(useRegion => SetRegions(useRegion, Selections.ToList()));

		void Command_Edit_ModifyRegions_Modify_Clear(List<int> regions) => regions.ForEach(useRegion => SetRegions(useRegion, new List<Range>()));

		void Command_Edit_ModifyRegions_Modify_Remove(List<int> regions)
		{
			foreach (var useRegion in regions)
			{
				var newRegions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < GetRegions(useRegion).Count) && (GetRegions(useRegion)[regionIndex].End <= selection.Start) && (!GetRegions(useRegion)[regionIndex].Equals(selection)))
						newRegions.Add(GetRegions(useRegion)[regionIndex++]);
					while ((regionIndex < GetRegions(useRegion).Count) && ((GetRegions(useRegion)[regionIndex].Equals(selection)) || ((GetRegions(useRegion)[regionIndex].End > selection.Start) && (GetRegions(useRegion)[regionIndex].Start < selection.End))))
						++regionIndex;
				}
				while (regionIndex < GetRegions(useRegion).Count)
					newRegions.Add(GetRegions(useRegion)[regionIndex++]);
				SetRegions(useRegion, newRegions);
			}
		}

		void Command_Edit_ModifyRegions_Modify_Add(List<int> regions)
		{
			foreach (var useRegion in regions)
			{
				var newRegions = new List<Range>();
				var regionIndex = 0;
				foreach (var selection in Selections)
				{
					while ((regionIndex < GetRegions(useRegion).Count) && (GetRegions(useRegion)[regionIndex].End <= selection.Start) && (!GetRegions(useRegion)[regionIndex].Equals(selection)))
						newRegions.Add(GetRegions(useRegion)[regionIndex++]);
					while ((regionIndex < GetRegions(useRegion).Count) && ((GetRegions(useRegion)[regionIndex].Equals(selection)) || ((GetRegions(useRegion)[regionIndex].End > selection.Start) && (GetRegions(useRegion)[regionIndex].Start < selection.End))))
						++regionIndex;
					newRegions.Add(selection);
				}
				while (regionIndex < GetRegions(useRegion).Count)
					newRegions.Add(GetRegions(useRegion)[regionIndex++]);
				SetRegions(useRegion, newRegions);
			}
		}

		void Command_Edit_ModifyRegions_Modify_Unite(List<int> regions)
		{
			foreach (var useRegion in regions)
			{
				var newRegions = new List<Range>();
				int regionIndex = 0, selectionIndex = 0;
				Range region = null;
				while (true)
				{
					if ((region == null) && (regionIndex < GetRegions(useRegion).Count))
						region = GetRegions(useRegion)[regionIndex++];

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
							newRegions.Add(new Range(Selections[selectionIndex].Start, region.Start));
						if (region.End <= Selections[selectionIndex].End)
							region = null;
						else
							region = new Range(region.End, Selections[selectionIndex].End);
					}
				}
				SetRegions(useRegion, newRegions);
			}
		}

		void Command_Edit_ModifyRegions_Modify_Intersect(List<int> regions)
		{
			foreach (var useRegion in regions)
			{
				var newRegions = new List<Range>();
				var startRegionIndex = 0;
				foreach (var selection in Selections)
				{
					var regionIndex = startRegionIndex;
					while ((regionIndex < GetRegions(useRegion).Count) && (GetRegions(useRegion)[regionIndex].End < selection.Start))
						++regionIndex;
					startRegionIndex = regionIndex;
					while ((regionIndex < GetRegions(useRegion).Count) && (GetRegions(useRegion)[regionIndex].Start <= selection.End))
					{
						if ((!GetRegions(useRegion)[regionIndex].HasSelection) || (!selection.HasSelection) || ((GetRegions(useRegion)[regionIndex].End != selection.Start) && (GetRegions(useRegion)[regionIndex].Start != selection.End)))
						{
							var newRegion = new Range(Math.Min(GetRegions(useRegion)[regionIndex].End, selection.End), Math.Max(GetRegions(useRegion)[regionIndex].Start, selection.Start));
							if ((newRegions.Count == 0) || (!newRegion.Equals(newRegions[newRegions.Count - 1])))
								newRegions.Add(newRegion);
						}
						++regionIndex;
					}
				}
				SetRegions(useRegion, newRegions);
			}
		}

		void Command_Edit_ModifyRegions_Modify_Exclude(List<int> regions)
		{
			foreach (var useRegion in regions)
			{
				var regions2 = GetRegions(useRegion).ToList();
				var newRegions = new List<Range>();
				var regionIndex = 0;
				var selectionIndex = 0;
				while (regionIndex < regions2.Count)
				{
					if (selectionIndex >= Selections.Count)
						newRegions.Add(regions2[regionIndex++]);
					else if (Selections[selectionIndex].Equals(regions2[regionIndex]))
						regionIndex++;
					else if (regions2[regionIndex].End < Selections[selectionIndex].Start)
						newRegions.Add(regions2[regionIndex++]);
					else if (Selections[selectionIndex].End < regions2[regionIndex].Start)
						++selectionIndex;
					else
					{
						if (regions2[regionIndex].Start < Selections[selectionIndex].Start)
							newRegions.Add(new Range(Selections[selectionIndex].Start, regions2[regionIndex].Start));
						while ((regionIndex < regions2.Count) && (regions2[regionIndex].End <= Selections[selectionIndex].End))
							regionIndex++;
						if ((regionIndex < regions2.Count) && (regions2[regionIndex].Start < Selections[selectionIndex].End))
							regions2[regionIndex] = new Range(regions2[regionIndex].End, Selections[selectionIndex].End);
						++selectionIndex;
					}
				}
				SetRegions(useRegion, newRegions);
			}
		}

		void Command_Edit_ModifyRegions_Modify_Repeat(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var useRegions = GetRegions(useRegion);
			var regionsWithSelections = GetRegionsWithSelections(useRegion);
			var position = 0;
			var newRegionStrs = new List<string>();
			var newRegions = new List<Range>();
			var newSelections = new List<Range>();
			foreach (var regionsWithSelection in regionsWithSelections)
			{
				newRegionStrs.Add(string.Join("", Enumerable.Repeat(Text.GetString(regionsWithSelection.Item1), regionsWithSelection.Item2.Count)));
				position -= regionsWithSelection.Item1.Length;
				foreach (var selection in regionsWithSelection.Item2)
				{
					position += regionsWithSelection.Item1.Length;
					newRegions.Add(new Range(regionsWithSelection.Item1.Cursor + position, regionsWithSelection.Item1.Anchor + position));
					newSelections.Add(new Range(selection.Cursor + position, selection.Anchor + position));
				}
			}
			Replace(useRegions.ToList(), newRegionStrs);
			SetRegions(useRegion, newRegions);
			Selections = newSelections;
		}

		void Command_Edit_ModifyRegions_Copy_Enclosing(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			Clipboard = GetEnclosingRegions(useRegion).Select(range => Text.GetString(range)).ToList();
		}

		void Command_Edit_ModifyRegions_Copy_EnclosingIndex(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			Clipboard = GetEnclosingRegions(useRegion).Select(region => (GetRegions(useRegion).IndexOf(region) + 1).ToString()).ToList();
		}

		void Command_Edit_ModifyRegions_Transform_Flatten(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			SetRegionsWithSelectionsText(useRegion, GetRegionsWithSelectionsText(useRegion, false), false);
		}

		void Command_Edit_ModifyRegions_Transform_Transpose(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			var count = regions2.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions2.Select(strs => strs[index]).ToList()).ToList());
		}

		void Command_Edit_ModifyRegions_Transform_RotateLeft(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			var count = regions2.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions2.Select(region => region[region.Count - 1 - index]).ToList()).ToList());
		}

		void Command_Edit_ModifyRegions_Transform_RotateRight(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			regions2.Reverse();
			var count = regions2.Select(region => region.Count).FirstOrDefault();
			SetRegionsWithSelectionsText(useRegion, Enumerable.Range(0, count).Select(index => regions2.Select(region => region[index]).ToList()).ToList());
		}

		void Command_Edit_ModifyRegions_Transform_Rotate180(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			regions2.Reverse();
			regions2.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(useRegion, regions2);
		}

		void Command_Edit_ModifyRegions_Transform_MirrorHorizontal(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			regions2.ForEach(list => list.Reverse());
			SetRegionsWithSelectionsText(useRegion, regions2);
		}

		void Command_Edit_ModifyRegions_Transform_MirrorVertical(List<int> regions)
		{
			if (regions.Count != 1)
				throw new Exception("Can only select single region");

			var useRegion = regions[0];
			var regions2 = GetRegionsWithSelectionsText(useRegion);
			regions2.Reverse();
			SetRegionsWithSelectionsText(useRegion, regions2);
		}

		void Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action action, int region) => Execute_Edit_ModifyRegions(new EditModifyRegionsDialog.Result { Action = action, Regions = new List<int> { region } });
	}
}
