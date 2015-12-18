﻿namespace NeoEdit.GUI.Controls
{
	enum WindowCommand
	{
		None,
		Window_Console,
		Window_Diff,
		Window_Disk,
		Window_Handles,
		Window_HexEditor,
		Window_Network,
		Window_Processes,
		Window_Registry,
		Window_SystemInfo,
		Window_TableEditor,
		Window_TextEditor,
		Window_TextViewer,
		Window_Tools,
	}

	class WindowMenuItem : NEMenuItem<WindowCommand> { }

	partial class WindowMenu
	{
		public WindowMenu() { InitializeComponent(); }
	}
}
