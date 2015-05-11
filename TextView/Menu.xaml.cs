using NeoEdit.GUI.Common;

namespace NeoEdit.TextView
{
	class TextViewMenuItem : NEMenuItem<TextViewCommand> { }

	partial class TextViewMenu
	{
		[DepProp]
		public new TextViewerTabs Parent { get { return UIHelper<TextViewMenu>.GetPropValue<TextViewerTabs>(this); } set { UIHelper<TextViewMenu>.SetPropValue(this, value); } }

		static TextViewMenu() { UIHelper<TextViewMenu>.Register(); }

		public TextViewMenu()
		{
			InitializeComponent();
		}
	}
}
