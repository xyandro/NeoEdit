using System.Linq;
using System.Net;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Network.Dialogs
{
	partial class BridgeDialog
	{
		internal class Result
		{
			public IPEndPoint EndPoint { get; set; }
		}

		[DepProp]
		public string HostName { get { return UIHelper<BridgeDialog>.GetPropValue<string>(this); } set { UIHelper<BridgeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Port { get { return UIHelper<BridgeDialog>.GetPropValue<int>(this); } set { UIHelper<BridgeDialog>.SetPropValue(this, value); } }

		static BridgeDialog() { UIHelper<BridgeDialog>.Register(); }

		BridgeDialog()
		{
			InitializeComponent();
			HostName = IPAddress.Any.ToString();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(HostName))
				return;

			IPAddress address;
			if (!IPAddress.TryParse(HostName, out address))
				address = Dns.GetHostEntry(HostName).AddressList.First();

			result = new Result { EndPoint = new IPEndPoint(address, Port) };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new BridgeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
