using NeoEdit.GUI.Controls;

namespace NeoEdit.TextView
{
	class TextViewMenuItem : NEMenuItem<TextViewCommand> { }

	partial class TextViewMenu
	{
		[DepProp]
		public new TextViewTabs Parent { get { return UIHelper<TextViewMenu>.GetPropValue<TextViewTabs>(this); } set { UIHelper<TextViewMenu>.SetPropValue(this, value); } }

		static TextViewMenu() { UIHelper<TextViewMenu>.Register(); }

		public TextViewMenu() { InitializeComponent(); }
	}
}
