﻿using NeoEdit.GUI.Controls;
using NeoEdit.Tools.Dialogs.NSRLTool;
using NeoEdit.Tools.Dialogs.SpiderTool;

namespace NeoEdit.Tools
{
	public partial class ToolsMain
	{
		static ToolsMain() { UIHelper<ToolsMain>.Register(); }

		public ToolsMain()
		{
			ToolsMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
		}

		void RunCommand(ToolsCommand command)
		{
			switch (command)
			{
				case ToolsCommand.File_Exit: Close(); break;
				case ToolsCommand.Tools_NSRLTool: Command_Tools_NSRLTool(); break;
				case ToolsCommand.Tools_SpiderTool: Command_Tools_SpiderTool(); break;
			}
		}

		void Command_Tools_NSRLTool() => NSRLToolDialog.Run();

		void Command_Tools_SpiderTool() => SpiderToolDialog.Run(this);
	}
}
