using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console> { }
	public class TabsWindow : TabsWindow<Console> { }

	public partial class ConsoleTabs
	{
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
