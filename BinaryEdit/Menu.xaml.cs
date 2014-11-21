using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEdit
{
	class BinaryEditMenuItem : NEMenuItem<BinaryEditCommand> { }

	partial class BinaryEditMenu
	{
		[DepProp]
		public new BinaryEditTabs Parent { get { return UIHelper<BinaryEditMenu>.GetPropValue<BinaryEditTabs>(this); } set { UIHelper<BinaryEditMenu>.SetPropValue(this, value); } }

		static BinaryEditMenu() { UIHelper<BinaryEditMenu>.Register(); }

		public BinaryEditMenu()
		{
			InitializeComponent();
		}
	}
}
