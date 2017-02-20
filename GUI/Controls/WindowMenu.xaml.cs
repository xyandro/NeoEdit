using System.Windows.Input;

namespace NeoEdit.GUI.Controls
{
	enum WindowCommand
	{
		None,
		Window_Diff,
		Window_Disk,
		Window_HexEditor,
		Window_StreamSaver,
		Window_TextEditor,
	}

	class WindowMenuItem : NEMenuItem<WindowCommand> { }

	partial class WindowMenu
	{
		public WindowMenu()
		{
			InitializeComponent();
			WindowMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
		}

		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

		void RunCommand(WindowCommand command)
		{
			switch (command)
			{
				case WindowCommand.Window_Diff: Launcher.Static.LaunchTextEditorDiff(); break;
				case WindowCommand.Window_Disk: Launcher.Static.LaunchDisk(forceCreate: true); break;
				case WindowCommand.Window_HexEditor: Launcher.Static.LaunchHexEditor(forceCreate: true); break;
				case WindowCommand.Window_StreamSaver: Launcher.Static.LaunchStreamSaver(); break;
				case WindowCommand.Window_TextEditor: Launcher.Static.LaunchTextEditorFile(forceCreate: true); break;
			}

			if (shiftDown)
				UIHelper.FindParent<NEWindow>(this).Close();
		}
	}
}
