using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools
{
	public partial class ToolsMain
	{
		static ToolsMain() { UIHelper<ToolsMain>.Register(); }

		public ToolsMain()
		{
			ToolsMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
		}

		void RunCommand(ToolsCommand command)
		{
			switch (command)
			{
				case ToolsCommand.File_Exit: Close(); break;
			}
		}
	}
}
