using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	class ConsoleMenuItem : NEMenuItem<ConsoleCommand> { }

	partial class ConsoleMenu
	{
		[DepProp]
		public new ConsoleTabs Parent { get { return UIHelper<ConsoleMenu>.GetPropValue<ConsoleTabs>(this); } set { UIHelper<ConsoleMenu>.SetPropValue(this, value); } }

		static ConsoleMenu() { UIHelper<ConsoleMenu>.Register(); }

		public ConsoleMenu()
		{
			InitializeComponent();
		}
	}
}
