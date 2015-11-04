namespace NeoEdit.GUI.Controls
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
		public HelpMenu() { InitializeComponent(); }
	}
}
