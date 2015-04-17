using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	class HexEditMenuItem : NEMenuItem<HexEditCommand> { }

	partial class HexEditMenu
	{
		[DepProp]
		public new HexEditTabs Parent { get { return UIHelper<HexEditMenu>.GetPropValue(() => this.Parent); } set { UIHelper<HexEditMenu>.SetPropValue(() => this.Parent, value); } }

		static HexEditMenu() { UIHelper<HexEditMenu>.Register(); }

		public HexEditMenu()
		{
			InitializeComponent();
		}
	}
}
