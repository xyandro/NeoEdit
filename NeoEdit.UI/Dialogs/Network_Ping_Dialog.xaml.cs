﻿using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Network_Ping_Dialog
	{
		[DepProp]
		public int Timeout { get { return UIHelper<Network_Ping_Dialog>.GetPropValue<int>(this); } set { UIHelper<Network_Ping_Dialog>.SetPropValue(this, value); } }

		static Network_Ping_Dialog() { UIHelper<Network_Ping_Dialog>.Register(); }

		Network_Ping_Dialog()
		{
			InitializeComponent();

			Timeout = 1000;
		}

		Configuration_Network_Ping result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_Ping { Timeout = Timeout };
			DialogResult = true;
		}

		public static Configuration_Network_Ping Run(Window parent)
		{
			var dialog = new Network_Ping_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
