using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
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

		NetworkAbsoluteURLDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NetworkAbsoluteURLDialogResult { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static NetworkAbsoluteURLDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new NetworkAbsoluteURLDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
