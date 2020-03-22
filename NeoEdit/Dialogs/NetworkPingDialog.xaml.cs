﻿using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class NetworkPingDialog
	{
		[DepProp]
		public int Timeout { get { return UIHelper<NetworkPingDialog>.GetPropValue<int>(this); } set { UIHelper<NetworkPingDialog>.SetPropValue(this, value); } }

		static NetworkPingDialog() { UIHelper<NetworkPingDialog>.Register(); }

		NetworkPingDialog()
		{
			InitializeComponent();

			Timeout = 1000;
		}

		NetworkPingDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NetworkPingDialogResult { Timeout = Timeout };
			DialogResult = true;
		}

		static public NetworkPingDialogResult Run(Window parent)
		{
			var dialog = new NetworkPingDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
