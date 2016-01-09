using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ChooseJoinTypeDialog
	{
		internal class Result
		{
			public Table.JoinType JoinType { get; set; }
		}

		[DepProp]
		public Table.JoinType JoinType { get { return UIHelper<ChooseJoinTypeDialog>.GetPropValue<Table.JoinType>(this); } set { UIHelper<ChooseJoinTypeDialog>.SetPropValue(this, value); } }

		public List<Table.JoinType> JoinTypes { get; } = Helpers.GetValues<Table.JoinType>().ToList();

		static ChooseJoinTypeDialog() { UIHelper<ChooseJoinTypeDialog>.Register(); }

		ChooseJoinTypeDialog()
		{
			InitializeComponent();
			JoinType = Table.JoinType.Inner;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { JoinType = JoinType };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ChooseJoinTypeDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
