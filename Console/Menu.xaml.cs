using NeoEdit.GUI.Common;

namespace NeoEdit.Console
{
	class ConsoleMenuItem : NEMenuItem<ConsoleCommand> { }

	partial class ConsoleMenu
	{
		[DepProp]
		public new ConsoleTabs Parent { get { return UIHelper<ConsoleMenu>.GetPropValue(() => this.Parent); } set { UIHelper<ConsoleMenu>.SetPropValue(() => this.Parent, value); } }

		static ConsoleMenu() { UIHelper<ConsoleMenu>.Register(); }

		public ConsoleMenu()
		{
			InitializeComponent();
		}
	}
}
