using System.Data;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		void Command_Region_SetSelections() => Regions.Replace(Selections);

		void Command_Region_AddSelections() => Regions.AddRange(Selections);

		void Command_Region_RemoveSelections()
		{
			if (!Selections.Any(range => range.HasSelection))
				Regions.Clear();
			else
			{
				foreach (var selection in Selections)
				{
					var toRemove = Regions.Where(region => (region.Start >= selection.Start) && (region.End <= selection.End)).ToList();
					toRemove.ForEach(region => Regions.Remove(region));
				}
			}
		}

		void Command_Region_LimitToSelections() => Regions.Replace(Regions.Where(region => Selections.Any(selection => (region.Start >= selection.Start) && (region.End <= selection.End))).ToList());

		void Command_Region_Clear() => Regions.Clear();

		void Command_Region_WithEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		void Command_Region_WithoutEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());

		void Command_Region_SelectEnclosingRegion() => Selections.Replace(GetEnclosingRegions());

		void Command_Region_CopyEnclosingRegion() => SetClipboardStrings(GetEnclosingRegions().Select(range => GetString(range)).ToList());
	}
}
