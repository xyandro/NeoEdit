using System.Linq;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		void Command_Region_SetSelections() => Regions.Replace(Selections);

		void Command_Region_AddSelections() => Regions.AddRange(Selections);

		void Command_Region_RemoveSelections() => Regions.Replace(Regions.Where(region => Selections.All(selection => ((region.Start != selection.Start) || (region.End != selection.End)) && ((region.End <= selection.Start) || (region.Start >= selection.End)))).ToList());

		void Command_Region_ReplaceSelections() => Regions.Replace(Regions.Where(region => Selections.All(selection => (region.End <= selection.Start) || (region.Start >= selection.End))).Concat(Selections).ToList());

		void Command_Region_LimitToSelections() => Regions.Replace(Regions.Where(region => Selections.Any(selection => (region.Start >= selection.Start) && (region.End <= selection.End))).ToList());

		void Command_Region_Clear() => Regions.Clear();

		void Command_Region_WithEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		void Command_Region_WithoutEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());

		void Command_Region_SelectEnclosingRegion() => Selections.Replace(GetEnclosingRegions());

		void Command_Region_CopyEnclosingRegion() => SetClipboardStrings(GetEnclosingRegions().Select(range => GetString(range)).ToList());

		void Command_Region_CopyEnclosingRegionIndex() => SetClipboardStrings(GetEnclosingRegions().Select(region => (Regions.IndexOf(region) + 1).ToString()).ToList());
	}
}
