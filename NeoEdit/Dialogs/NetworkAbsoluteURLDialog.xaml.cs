using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Dialogs
{
	partial class NetworkAbsoluteURLDialog
	{
		public class Result
		{
			public string Expression { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NetworkAbsoluteURLDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
