using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Network_AbsoluteURL_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Network_AbsoluteURL_Dialog>.GetPropValue<string>(this); } set { UIHelper<Network_AbsoluteURL_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Network_AbsoluteURL_Dialog()
		{
			UIHelper<Network_AbsoluteURL_Dialog>.Register();
		}

		Network_AbsoluteURL_Dialog(NEVariables variables)
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
			var dialog = new Network_AbsoluteURL_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
