using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NetworkAbsoluteURLDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<NetworkAbsoluteURLDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkAbsoluteURLDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NetworkAbsoluteURLDialog()
		{
			UIHelper<NetworkAbsoluteURLDialog>.Register();
		}

		NetworkAbsoluteURLDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Expression = "c";
		}

		Configuration_Network_AbsoluteURL result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Network_AbsoluteURL { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Network_AbsoluteURL Run(Window parent, NEVariables variables)
		{
			var dialog = new NetworkAbsoluteURLDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
