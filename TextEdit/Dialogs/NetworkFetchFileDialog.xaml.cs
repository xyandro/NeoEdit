using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class NetworkFetchFileDialog
	{
		internal class Result
		{
			public string URL { get; set; }
			public string FileName { get; set; }
		}

		[DepProp]
		public string URL { get { return UIHelper<NetworkFetchFileDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchFileDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<NetworkFetchFileDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchFileDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NetworkFetchFileDialog() { UIHelper<NetworkFetchFileDialog>.Register(); }

		NetworkFetchFileDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			URL = FileName = "x";
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { URL = URL, FileName = FileName };
			url.AddCurrentSuggestion();
			fileName.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NetworkFetchFileDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
