using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
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

		void OpenTable(Table table)
		{
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8);
			TabsParent.CreateTab(textEditor);
		}

		internal void Command_Table_Type_Detect() => TableType = Table.GuessTableType(AllText);

		internal void Command_Table_RegionsSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var sels = GetSelectionStrings();
			var regions = GetEnclosingRegions();
			var rows = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => group.Select(index => sels[index]).ToList()).ToList();
			OpenTable(new Table(rows, false));
		}

		internal EditTableDialog.Result Command_Table_EditTable_Dialog() => EditTableDialog.Run(WindowParent, AllText, TableType, HasHeaders);

		internal void Command_Table_EditTable(EditTableDialog.Result result)
		{
			var table = new Table(AllText, TableType, HasHeaders);
			table = table.Aggregate(result.AggregateData);
			table = table.Sort(result.SortData);
			var output = table.ToString(Data.DefaultEnding, TableType);

			Replace(new List<Range> { FullRange }, new List<string> { output });
			Selections.Replace(FullRange);
		}
	}
}
