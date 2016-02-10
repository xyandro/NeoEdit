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
			NetworkMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
		}

		void RunCommand(NetworkCommand command)
		{
			switch (command)
			{
				case NetworkCommand.File_Exit: Close(); break;
				case NetworkCommand.Socket_Forward: Command_Socket_Forward(); break;
				case NetworkCommand.Socket_Bridge: Command_Socket_Bridge(); break;
			}
		}

		void Command_Socket_Forward()
		{
			var result = ForwarderDialog.Run(this);
			if (result == null)
				return;

			new Thread(obj => StartForwarder(result)).Start();
		}

		void Command_Socket_Bridge()
		{
			var result = BridgeDialog.Run(this);
			if (result == null)
				return;

			new Thread(obj => StartBridge(result)).Start();
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
					client2 = new TcpClient(result.Dest.AddressFamily);
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

		void StartBridge(BridgeDialog.Result result)
		{
			var server = new TcpListener(result.EndPoint);
			server.Start();
			while (true)
			{
				TcpClient client1 = null, client2 = null;
				try
				{
					client1 = server.AcceptTcpClient();
					client2 = server.AcceptTcpClient();
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
