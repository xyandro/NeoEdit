using NeoEdit.GUI.Controls;

namespace NeoEdit.Network
{
	class NetworkMenuItem : NEMenuItem<NetworkCommand> { }

	partial class NetworkMenu
	{
		static NetworkMenu() { UIHelper<NetworkMenu>.Register(); }

		public NetworkMenu() { InitializeComponent(); }
	}
}
