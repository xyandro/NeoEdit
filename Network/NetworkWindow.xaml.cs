using System;
using System.Net.Sockets;
using System.Threading;
using NeoEdit.GUI.Controls;
using NeoEdit.Network.Dialogs;

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
				case NetworkCommand.Socket_Forward: Command_Socket_Forward(); break;
			}
		}

		void Command_Socket_Forward()
		{
			var result = ForwarderDialog.Run(this);
			if (result == null)
				return;

			new Thread(obj => StartForwarder(result)).Start();
		}

		void StartForwarder(ForwarderDialog.Result result)
		{
			var server = new TcpListener(result.Source);
			server.Start();
			while (true)
			{
				TcpClient client1 = null, client2 = null;
				try
				{
					client1 = server.AcceptTcpClient();
					client2 = new TcpClient();
					client2.Connect(result.Dest);
					Connect(client1, client2);
				}
				catch
				{
					client1?.Dispose();
					client2?.Dispose();
				}
			}
		}

		void Connect(TcpClient client1, TcpClient client2)
		{
			new Thread(obj => StartConnected(client1, client2)).Start();
			new Thread(obj => StartConnected(client2, client1)).Start();
		}

		void StartConnected(TcpClient client1, TcpClient client2)
		{
			try { client1.GetStream().CopyTo(client2.GetStream()); } catch { }
			client1.Dispose();
			client2.Dispose();
		}
	}
}
