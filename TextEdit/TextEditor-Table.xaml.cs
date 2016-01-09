using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		[DepProp]
		public Table.TableType TableType { get { return UIHelper<TextEditor>.GetPropValue<Table.TableType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasHeaders { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		internal AggregateTableDialog.Result Command_Table_Aggregate_Dialog() => AggregateTableDialog.Run(WindowParent, AllText, TableType, HasHeaders);

		internal void Command_Table_Aggregate(AggregateTableDialog.Result result)
		{
			var table = new Table(AllText, TableType, HasHeaders);
			table = table.Aggregate(result.AggregateData);
			table = table.Sort(result.SortData);
			var output = table.ToString(Data.DefaultEnding, TableType);

			Replace(new List<Range> { FullRange }, new List<string> { output });
			Selections.Replace(FullRange);
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
