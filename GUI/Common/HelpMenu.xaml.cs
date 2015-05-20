namespace NeoEdit.GUI.Common
{
	enum HelpCommand
	{
		None,
		Help_About,
		Help_RunGC,
	}

	class HelpMenuItem : NEMenuItem<HelpCommand> { }

	partial class HelpMenu
	{
		public HelpMenu()
		{
			InitializeComponent();
		}
	}
}
