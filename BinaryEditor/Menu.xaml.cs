using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	class BinaryEditMenuItem : NEMenuItem<BinaryEditCommand> { }

	partial class BinaryEditMenu
	{
		[DepProp]
		public new BinaryEditorTabs Parent { get { return UIHelper<BinaryEditMenu>.GetPropValue<BinaryEditorTabs>(this); } set { UIHelper<BinaryEditMenu>.SetPropValue(this, value); } }

		static BinaryEditMenu() { UIHelper<BinaryEditMenu>.Register(); }

		public BinaryEditMenu()
		{
			InitializeComponent();
		}
	}
}
