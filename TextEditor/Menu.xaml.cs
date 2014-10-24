using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	class TextEditMenuItem : NEMenuItem<TextEditCommand> { }

	partial class TextEditMenu
	{
		[DepProp]
		public new TextEditorTabs Parent { get { return UIHelper<TextEditMenu>.GetPropValue<TextEditorTabs>(this); } set { UIHelper<TextEditMenu>.SetPropValue(this, value); } }

		static TextEditMenu() { UIHelper<TextEditMenu>.Register(); }

		public TextEditMenu()
		{
			InitializeComponent();
		}
	}
}
