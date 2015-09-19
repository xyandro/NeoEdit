using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ScanPortsDialog
	{
		internal class Result
		{
			public List<Tuple<int, int>> Ports { get; set; }
			public int Attempts { get; set; }
			public int Timeout { get; set; }
			public int Concurrency { get; set; }
		}

		[DepProp]
		public string Ports { get { return UIHelper<ScanPortsDialog>.GetPropValue<string>(this); } set { UIHelper<ScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Attempts { get { return UIHelper<ScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<ScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Timeout { get { return UIHelper<ScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<ScanPortsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Concurrency { get { return UIHelper<ScanPortsDialog>.GetPropValue<int>(this); } set { UIHelper<ScanPortsDialog>.SetPropValue(this, value); } }

		static ScanPortsDialog() { UIHelper<ScanPortsDialog>.Register(); }

		ScanPortsDialog()
		{
			InitializeComponent();

			Ports = "1-1023";
			Attempts = 1;
			Timeout = 1000;
			Concurrency = 10000;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var ranges = Ports.Split(',').Where(str => str.Length != 0).Select(range => range.Trim().Split('-').Where(str => str.Length != 0).Select(port => int.Parse(port.Trim())).ToList()).Where(range => range.Count > 0).ToList();
			var portRanges = ranges.Select(range => Tuple.Create(range[0], range.Count == 2 ? Math.Max(range[0], range[1]) : range[0])).ToList();
			result = new Result { Ports = portRanges, Attempts = Attempts, Timeout = Timeout, Concurrency = Concurrency };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ScanPortsDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
