using NeoEdit.GUI.Controls;

namespace NeoEdit.Tables
{
	class TablesMenuItem : NEMenuItem<TablesCommand> { }

	partial class TablesMenu
	{
		[DepProp]
		public new TablesTabs Parent { get { return UIHelper<TablesMenu>.GetPropValue<TablesTabs>(this); } set { UIHelper<TablesMenu>.SetPropValue(this, value); } }

		static TablesMenu() { UIHelper<TablesMenu>.Register(); }

		public TablesMenu()
		{
			InitializeComponent();
		}
	}
}
