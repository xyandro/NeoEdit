using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Table_Join_Dialog
	{
		[DepProp]
		public Table LeftTable { get { return UIHelper<Configure_Table_Join_Dialog>.GetPropValue<Table>(this); } set { UIHelper<Configure_Table_Join_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table RightTable { get { return UIHelper<Configure_Table_Join_Dialog>.GetPropValue<Table>(this); } set { UIHelper<Configure_Table_Join_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table.JoinType JoinType { get { return UIHelper<Configure_Table_Join_Dialog>.GetPropValue<Table.JoinType>(this); } set { UIHelper<Configure_Table_Join_Dialog>.SetPropValue(this, value); } }

		static Configure_Table_Join_Dialog() { UIHelper<Configure_Table_Join_Dialog>.Register(); }

		Configure_Table_Join_Dialog(Table leftTable, Table rightTable)
		{
			InitializeComponent();
			LeftTable = leftTable;
			RightTable = rightTable;
			JoinType = Table.JoinType.Left;
		}

		Configuration_Table_Join result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (leftTable.Selected.Count != rightTable.Selected.Count)
				throw new Exception("Tables must have same selection count");

			result = new Configuration_Table_Join
			{
				LeftColumns = leftTable.Selected.ToList(),
				RightColumns = rightTable.Selected.ToList(),
				JoinType = JoinType
			};
			// If they didn't select anything, use the highlight
			if (result.LeftColumns.Count == 0)
			{
				result.LeftColumns.Add(leftTable.SelectedColumn);
				result.RightColumns.Add(rightTable.SelectedColumn);
			}
			DialogResult = true;
		}

		public static Configuration_Table_Join Run(Window parent, Table leftTable, Table rightTable)
		{
			var dialog = new Configure_Table_Join_Dialog(leftTable, rightTable) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
