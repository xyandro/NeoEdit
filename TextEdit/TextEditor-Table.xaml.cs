using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Tables;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		internal AggregateTableDialog.Result Command_Table_Aggregate_Dialog()
		{
			SetTableSelection();
			return AggregateTableDialog.Run(WindowParent, GetSelectionStrings());
		}

		internal void Command_Table_Aggregate(AggregateTableDialog.Result result)
		{
			SetTableSelection();

			var table = new Table(GetSelectionStrings(), result.InputType, result.InputHeaders);
			table = table.Aggregate(result.AggregateData, false);
			table = table.Sort(result.SortData);
			var output = table.ConvertToString(Data.DefaultEnding, result.OutputType, result.OutputHeaders);

			var location = Data.GetOffset(Data.GetOffsetLine(Selections[0].Start), 0);
			Replace(new List<Range> { new Range(location) }, new List<string> { output + Data.DefaultEnding });
			Selections.Replace(Range.FromIndex(location, output.Length));
		}

		internal void Command_Table_RegionsSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var regions = GetEnclosingRegions();
			var lines = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => String.Join("\t", group.Select(index => Table.ToTCSV(GetString(Selections[index]), '\t')))).ToList();
			Selections.Replace(Regions);
			Regions.Clear();
			ReplaceSelections(lines);
		}
	}
}
