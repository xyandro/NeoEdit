using System.Collections.ObjectModel;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console> { }

	public partial class ConsoleTabs
	{
		[DepProp]
		public ObservableCollection<Console> Consoles { get { return UIHelper<ConsoleTabs>.GetPropValue<ObservableCollection<Console>>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Console Active { get { return UIHelper<ConsoleTabs>.GetPropValue<Console>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<ConsoleTabs>.GetPropValue<bool>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }

		static ConsoleTabs() { UIHelper<ConsoleTabs>.Register(); }

		public ConsoleTabs(string path = null)
		{
			ConsoleMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			Consoles = new ObservableCollection<Console>();
			Add(new Console());
		}

		void Add(Console console)
		{
			Consoles.Add(console);
			Active = console;
		}

		void RunCommand(ConsoleCommand command)
		{
			switch (command)
			{
				case ConsoleCommand.File_New: Add(new Console()); break;
			}
		}
	}
}
