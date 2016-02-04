using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class JoinDialog
	{
		internal class Result
		{
			public List<int> LeftColumns { get; set; }
			public List<int> RightColumns { get; set; }
			public Table.JoinType JoinType { get; set; }
		}

		[DepProp]
		public Table LeftTable { get { return UIHelper<JoinDialog>.GetPropValue<Table>(this); } set { UIHelper<JoinDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table RightTable { get { return UIHelper<JoinDialog>.GetPropValue<Table>(this); } set { UIHelper<JoinDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Table.JoinType JoinType { get { return UIHelper<JoinDialog>.GetPropValue<Table.JoinType>(this); } set { UIHelper<JoinDialog>.SetPropValue(this, value); } }

		static JoinDialog() { UIHelper<JoinDialog>.Register(); }

		JoinDialog(Table leftTable, Table rightTable)
		{
			InitializeComponent();
			LeftTable = leftTable;
			RightTable = rightTable;
			JoinType = Table.JoinType.Left;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (leftTable.Selected.Count != rightTable.Selected.Count)
				throw new Exception("Tables must have same selection count");

			result = new Result
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

		static public Result Run(Window parent, Table leftTable, Table rightTable)
		{
			var dialog = new JoinDialog(leftTable, rightTable) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
