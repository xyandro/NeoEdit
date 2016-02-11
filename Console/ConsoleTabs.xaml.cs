using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Console
{
	public class Tabs : Tabs<Console, ConsoleCommand> { }
	public class TabsWindow : TabsWindow<Console, ConsoleCommand> { }

	public partial class ConsoleTabs
	{
		static ConsoleTabs() { UIHelper<ConsoleTabs>.Register(); }

		public static void Create(string path = null, ConsoleTabs consoleTabs = null, bool forceCreate = false) => CreateTab(new Console(path), consoleTabs, forceCreate);

		ConsoleTabs()
		{
			ConsoleMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		void RunCommand(ConsoleCommand command)
		{
			switch (command)
			{
				case ConsoleCommand.File_New: Create(consoleTabs: this); break;
				case ConsoleCommand.View_Full: ItemTabs.SetLayout(TabsLayout.Full); break;
				case ConsoleCommand.View_Grid: ItemTabs.SetLayout(TabsLayout.Grid); break;
				case ConsoleCommand.View_CustomGrid:
					var result = CustomGridDialog.Run(this, ItemTabs.Columns, ItemTabs.Rows);
					if (result != null)
						ItemTabs.SetLayout(TabsLayout.Grid, result.Columns, result.Rows);
					break;
			}
		}
	}
}
