using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkScanPortsDialog
	{
		[DepProp]
		public string Ports { get { return UIHelper<NetworkScanPortsDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Attempts { get { return UIHelper<NetworkScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<NetworkScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Timeout { get { return UIHelper<NetworkScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<NetworkScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Concurrency { get { return UIHelper<NetworkScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<NetworkScanPortsDialog>.SetPropValue(this, value); } }

		static NetworkScanPortsDialog() { UIHelper<NetworkScanPortsDialog>.Register(); }

		NetworkScanPortsDialog()
		{
			InitializeComponent();

			Ports = "1-1023";
			Attempts = 1;
			Timeout = 1000;
			Concurrency = 10000;
		}

		NetworkScanPortsDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ranges = Ports.Split(',').Where(str => str.Length != 0).Select(range => range.Trim().Split('-').Where(str => str.Length != 0).Select(port => int.Parse(port.Trim())).ToList()).Where(range => range.Count > 0).ToList();
			var portRanges = ranges.Select(range => Tuple.Create(range[0], range.Count == 2 ? Math.Max(range[0], range[1]) : range[0])).ToList();
			result = new NetworkScanPortsDialogResult { Ports = portRanges, Attempts = Attempts, Timeout = Timeout, Concurrency = Concurrency };
			DialogResult = true;
		}

		public static NetworkScanPortsDialogResult Run(Window parent)
		{
			var dialog = new NetworkScanPortsDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
