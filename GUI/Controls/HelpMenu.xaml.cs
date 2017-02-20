using System;

namespace NeoEdit.GUI.Controls
{
	enum HelpCommand
	{
		None,
		Help_About,
		Help_Update,
		Help_RunGC,
	}

	class HelpMenuItem : NEMenuItem<HelpCommand> { }

	partial class HelpMenu
	{
		public HelpMenu()
		{
			InitializeComponent();
			HelpMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
		}

		void RunCommand(HelpCommand command)
		{
			switch (command)
			{
				case HelpCommand.Help_About: Launcher.Static.LaunchAbout(); break;
				case HelpCommand.Help_Update: Launcher.Static.LaunchUpdate(); break;
				case HelpCommand.Help_RunGC: GC.Collect(); break;
			}
		}
	}
}
