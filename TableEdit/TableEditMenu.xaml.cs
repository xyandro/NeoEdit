using NeoEdit.GUI.Controls;

namespace NeoEdit.TableEdit
{
	class TableEditMenuItem : NEMenuItem<TableEditCommand> { }

	partial class TableEditMenu
	{
		[DepProp]
		public new TableEditTabs Parent { get { return UIHelper<TableEditMenu>.GetPropValue<TableEditTabs>(this); } set { UIHelper<TableEditMenu>.SetPropValue(this, value); } }

		static TableEditMenu() { UIHelper<TableEditMenu>.Register(); }

		public TableEditMenu() { InitializeComponent(); }
	}
}
