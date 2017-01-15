using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FetchURLDialog
	{
		internal class Result
		{
			public string URL { get; set; }
			public string FileName { get; set; }
		}

		[DepProp]
		public string URL { get { return UIHelper<FetchURLDialog>.GetPropValue<string>(this); } set { UIHelper<FetchURLDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<FetchURLDialog>.GetPropValue<string>(this); } set { UIHelper<FetchURLDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static FetchURLDialog() { UIHelper<FetchURLDialog>.Register(); }

		FetchURLDialog(NEVariables variables)
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
			var dialog = new FetchURLDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
