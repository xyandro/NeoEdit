using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class GotoDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<GotoDialog>.GetPropValue<string>(this); } set { UIHelper<GotoDialog>.SetPropValue(this, value); } }
		public Dictionary<string, List<object>> ExpressionData { get; }

		static GotoDialog() { UIHelper<GotoDialog>.Register(); }

		GotoDialog(GotoType gotoType, int value, Dictionary<string, List<object>> expressionData)
		{
			ExpressionData = expressionData;
			InitializeComponent();

			Title = $"Go To {gotoType}";
			Expression = value.ToString();
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression };
			DialogResult = true;
		}

		public static Result Run(Window parent, GotoType gotoType, int startValue, Dictionary<string, List<object>> expressionData)
		{
			var dialog = new GotoDialog(gotoType, startValue, expressionData) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
