using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit
{
	class TextEditMenuItem : NEMenuItem<TextEditCommand> { }

	partial class TextEditMenu
	{
		[DepProp]
		public new TextEditTabs Parent { get { return UIHelper<TextEditMenu>.GetPropValue(() => this.Parent); } set { UIHelper<TextEditMenu>.SetPropValue(() => this.Parent, value); } }

		static TextEditMenu() { UIHelper<TextEditMenu>.Register(); }

		public TextEditMenu()
		{
			InitializeComponent();
		}
	}
}
