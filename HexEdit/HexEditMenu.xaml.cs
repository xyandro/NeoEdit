using NeoEdit.GUI.Controls;

namespace NeoEdit.HexEdit
{
	class HexEditMenuItem : NEMenuItem<HexEditCommand> { }

	partial class HexEditMenu
	{
		[DepProp]
		public new HexEditTabs Parent { get { return UIHelper<HexEditMenu>.GetPropValue<HexEditTabs>(this); } set { UIHelper<HexEditMenu>.SetPropValue(this, value); } }

		static HexEditMenu() { UIHelper<HexEditMenu>.Register(); }

		public HexEditMenu() { InitializeComponent(); }
	}
}
