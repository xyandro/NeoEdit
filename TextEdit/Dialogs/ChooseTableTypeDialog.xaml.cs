using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ChooseTableTypeDialog
	{
		internal class Result
		{
			public Table.TableType TableType { get; set; }
		}

		[DepProp]
		public Table.TableType TableType { get { return UIHelper<ChooseTableTypeDialog>.GetPropValue<Table.TableType>(this); } set { UIHelper<ChooseTableTypeDialog>.SetPropValue(this, value); } }

		public List<Table.TableType> TableTypes { get; } = Helpers.GetValues<Table.TableType>().Where(type => type != Table.TableType.None).ToList();

		static ChooseTableTypeDialog() { UIHelper<ChooseTableTypeDialog>.Register(); }

		ChooseTableTypeDialog(Table.TableType tableType)
		{
			InitializeComponent();
			TableType = tableType;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { TableType = TableType };
			DialogResult = true;
		}

		static public Result Run(Window parent, Table.TableType tableType)
		{
			var dialog = new ChooseTableTypeDialog(tableType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
