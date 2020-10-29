using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Network_ScanPorts_Dialog
	{
		[DepProp]
		public string Ports { get { return UIHelper<Network_ScanPorts_Dialog>.GetPropValue<string>(this); } set { UIHelper<Network_ScanPorts_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Attempts { get { return UIHelper<Network_ScanPorts_Dialog>.GetPropValue<int>(this); } set { UIHelper<Network_ScanPorts_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Timeout { get { return UIHelper<Network_ScanPorts_Dialog>.GetPropValue<int>(this); } set { UIHelper<Network_ScanPorts_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Concurrency { get { return UIHelper<Network_ScanPorts_Dialog>.GetPropValue<int>(this); } set { UIHelper<Network_ScanPorts_Dialog>.SetPropValue(this, value); } }

		static Network_ScanPorts_Dialog() { UIHelper<Network_ScanPorts_Dialog>.Register(); }

		Network_ScanPorts_Dialog()
		{
			InitializeComponent();

			Ports = "1-1023";
			Attempts = 1;
			Timeout = 1000;
			Concurrency = 10000;
		}

		Configuration_Network_ScanPorts result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ranges = Ports.Split(',').Where(str => str.Length != 0).Select(range => range.Trim().Split('-').Where(str => str.Length != 0).Select(port => int.Parse(port.Trim())).ToList()).Where(range => range.Count > 0).ToList();
			var portRanges = ranges.Select(range => Tuple.Create(range[0], range.Count == 2 ? Math.Max(range[0], range[1]) : range[0])).ToList();
			result = new Configuration_Network_ScanPorts { Ports = portRanges, Attempts = Attempts, Timeout = Timeout, Concurrency = Concurrency };
			DialogResult = true;
		}

		public static Configuration_Network_ScanPorts Run(Window parent)
		{
			var dialog = new Network_ScanPorts_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
