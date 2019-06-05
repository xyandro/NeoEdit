using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Dialogs
{
	partial class PositionGotoDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public bool OpenFilesOnce { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<PositionGotoDialog>.GetPropValue<string>(this); } set { UIHelper<PositionGotoDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OpenFilesOnce { get { return UIHelper<PositionGotoDialog>.GetPropValue<bool>(this); } set { UIHelper<PositionGotoDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static PositionGotoDialog() { UIHelper<PositionGotoDialog>.Register(); }

		PositionGotoDialog(GotoType gotoType, int value, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Title = $"Go To {gotoType}";
			Expression = value.ToString();
			OpenFilesOnce = true;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression, OpenFilesOnce = OpenFilesOnce };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, GotoType gotoType, int startValue, NEVariables variables)
		{
			var dialog = new PositionGotoDialog(gotoType, startValue, variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
