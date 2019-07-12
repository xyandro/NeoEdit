using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	partial class NEMenu
	{
		[DepProp]
		public new Tabs Parent { get { return UIHelper<NEMenu>.GetPropValue<Tabs>(this); } set { UIHelper<NEMenu>.SetPropValue(this, value); } }

		static NEMenu() { UIHelper<NEMenu>.Register(); }

		public NEMenu() { InitializeComponent(); }
	}
}
