using System;
using System.Linq;
using System.Net;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Network.Dialogs
{
	partial class ForwarderDialog
	{
		internal class Result
		{
			public IPEndPoint Source { get; set; }
			public IPEndPoint Dest { get; set; }
		}

		[DepProp]
		public string SourceHostName { get { return UIHelper<ForwarderDialog>.GetPropValue<string>(this); } set { UIHelper<ForwarderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int SourcePort { get { return UIHelper<ForwarderDialog>.GetPropValue<int>(this); } set { UIHelper<ForwarderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string DestHostName { get { return UIHelper<ForwarderDialog>.GetPropValue<string>(this); } set { UIHelper<ForwarderDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int DestPort { get { return UIHelper<ForwarderDialog>.GetPropValue<int>(this); } set { UIHelper<ForwarderDialog>.SetPropValue(this, value); } }

		static ForwarderDialog() { UIHelper<ForwarderDialog>.Register(); }

		ForwarderDialog()
		{
			InitializeComponent();
			SourceHostName = IPAddress.Any.ToString();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((String.IsNullOrWhiteSpace(SourceHostName)) || (String.IsNullOrWhiteSpace(DestHostName)))
				return;

			IPAddress sourceAddress, destAddress;
			if (!IPAddress.TryParse(SourceHostName, out sourceAddress))
				sourceAddress = Dns.GetHostEntry(SourceHostName).AddressList.First();
			if (!IPAddress.TryParse(DestHostName, out destAddress))
				destAddress = Dns.GetHostEntry(DestHostName).AddressList.First();

			result = new Result { Source = new IPEndPoint(sourceAddress, SourcePort), Dest = new IPEndPoint(destAddress, DestPort) };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new ForwarderDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
