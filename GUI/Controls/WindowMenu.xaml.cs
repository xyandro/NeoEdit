namespace NeoEdit.GUI.Controls
{
	enum WindowCommand
	{
		None,
		Window_Diff,
		Window_Disk,
		Window_Handles,
		Window_HexEditor,
		Window_ImageEditor,
		Window_Network,
		Window_Processes,
		Window_TextEditor,
		Window_TextViewer,
	}

	class WindowMenuItem : NEMenuItem<WindowCommand> { }

	partial class WindowMenu
	{
		public WindowMenu() { InitializeComponent(); }
	}
}
