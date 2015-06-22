using System.Collections.ObjectModel;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console> { }

	public partial class ConsoleTabs
	{
		[DepProp]
		public ObservableCollection<Tabs.ItemData> Consoles { get { return UIHelper<ConsoleTabs>.GetPropValue<ObservableCollection<Tabs.ItemData>>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ItemData Active { get { return UIHelper<ConsoleTabs>.GetPropValue<Tabs.ItemData>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<ConsoleTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }

		static ConsoleTabs() { UIHelper<ConsoleTabs>.Register(); }

		public ConsoleTabs(string path = null)
		{
			ConsoleMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			Consoles = new ObservableCollection<Tabs<Console>.ItemData>();
			Add(new Console());
		}

		void Add(Console console)
		{
			var add = new Tabs.ItemData(console);
			Consoles.Add(add);
			Active = add;
		}

		void RunCommand(ConsoleCommand command)
		{
			switch (command)
			{
				case ConsoleCommand.File_New: Add(new Console()); break;
				case ConsoleCommand.View_Tiles: View = View == Tabs<Console>.ViewType.Tiles ? Tabs<Console>.ViewType.Tabs : Tabs<Console>.ViewType.Tiles; break;
			}
		}
	}
}
