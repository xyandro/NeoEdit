using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools
{
	class ToolsMenuItem : NEMenuItem<ToolsCommand> { }

	partial class ToolsMenu
	{
		static ToolsMenu() { UIHelper<ToolsMenu>.Register(); }

		public ToolsMenu() { InitializeComponent(); }
	}
}
