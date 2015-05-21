namespace NeoEdit.GUI.Controls
{
	enum WindowCommand
	{
		None,
		Window_Clipboard,
		Window_Console,
		Window_DBViewer,
		Window_Disk,
		Window_Handles,
		Window_HexEditor,
		Window_Processes,
		Window_Registry,
		Window_SystemInfo,
		Window_TextEditor,
		Window_TextViewer,
	}

	class WindowMenuItem : NEMenuItem<WindowCommand> { }

	partial class WindowMenu
	{
		public WindowMenu()
		{
			InitializeComponent();
		}
	}
}
