using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkPingDialog
	{
		public class Result
		{
			public int Timeout { get; set; }
		}

		[DepProp]
		public int Timeout { get { return UIHelper<NetworkPingDialog>.GetPropValue<int>(this); } set { UIHelper<NetworkPingDialog>.SetPropValue(this, value); } }

		static NetworkPingDialog() { UIHelper<NetworkPingDialog>.Register(); }

		NetworkPingDialog()
		{
			InitializeComponent();

			Timeout = 1000;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Timeout = Timeout };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new NetworkPingDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
