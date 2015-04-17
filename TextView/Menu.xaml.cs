using NeoEdit.GUI.Common;

namespace NeoEdit.TextView
{
	class TextViewMenuItem : NEMenuItem<TextViewCommand> { }

	partial class TextViewMenu
	{
		[DepProp]
		public new TextViewerTabs Parent { get { return UIHelper<TextViewMenu>.GetPropValue(() => this.Parent); } set { UIHelper<TextViewMenu>.SetPropValue(() => this.Parent, value); } }

		static TextViewMenu() { UIHelper<TextViewMenu>.Register(); }

		public TextViewMenu()
		{
			InitializeComponent();
		}
	}
}
