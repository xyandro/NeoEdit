using NeoEdit.GUI.Controls;

namespace NeoEdit.Network
{
	partial class NetworkWindow
	{
		static NetworkWindow() { UIHelper<NetworkWindow>.Register(); }

		public NetworkWindow()
		{
			NetworkMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
		}

		void RunCommand(NetworkCommand command)
		{
			switch (command)
			{
				case NetworkCommand.File_Exit: Close(); break;
			}
		}
	}
}
