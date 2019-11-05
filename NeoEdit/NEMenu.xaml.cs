using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	partial class NEMenu
	{
		[DepProp]
		public new TabsWindow Parent { get { return UIHelper<NEMenu>.GetPropValue<TabsWindow>(this); } set { UIHelper<NEMenu>.SetPropValue(this, value); } }

		static NEMenu() { UIHelper<NEMenu>.Register(); }

		public NEMenu() { InitializeComponent(); }
	}
}
