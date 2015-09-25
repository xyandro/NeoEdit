using System.Collections.ObjectModel;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console> { }
	public class TabsWindow : TabsWindow<Console> { }

	public partial class ConsoleTabs
	{
		[DepProp]
		public ObservableCollection<Console> Consoles { get { return UIHelper<ConsoleTabs>.GetPropValue<ObservableCollection<Console>>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Console Active { get { return UIHelper<ConsoleTabs>.GetPropValue<Console>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<ConsoleTabs>.GetPropValue<bool>(this); } set { UIHelper<ConsoleTabs>.SetPropValue(this, value); } }

		static ConsoleTabs() { UIHelper<ConsoleTabs>.Register(); }

		public static void Create(string path = null, ConsoleTabs consoleTabs = null, bool forceCreate = false)
		{
			CreateTab(new Console(path), consoleTabs, forceCreate);
		}

		ConsoleTabs()
		{
			ConsoleMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);

			Consoles = new ObservableCollection<Console>();
		}

		void RunCommand(ConsoleCommand command)
		{
			switch (command)
			{
				case ConsoleCommand.File_New: Create(consoleTabs: this); break;
			}
		}
	}
}
