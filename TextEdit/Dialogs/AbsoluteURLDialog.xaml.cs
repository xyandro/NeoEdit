using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class AbsoluteURLDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<AbsoluteURLDialog>.GetPropValue<string>(this); } set { UIHelper<AbsoluteURLDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static AbsoluteURLDialog()
		{
			UIHelper<AbsoluteURLDialog>.Register();
		}

		AbsoluteURLDialog(NEVariables variables)
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
			var dialog = new AbsoluteURLDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
