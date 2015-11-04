using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class RepeatDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
			public bool SelectRepetitions { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<RepeatDialog>.GetPropValue<string>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SelectRepetitions { get { return UIHelper<RepeatDialog>.GetPropValue<bool>(this); } set { UIHelper<RepeatDialog>.SetPropValue(this, value); } }
		public Dictionary<string, List<object>> ExpressionData { get; }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		RepeatDialog(bool selectRepetitions, Dictionary<string, List<object>> expressionData)
		{
			ExpressionData = expressionData;
			InitializeComponent();

			Expression = "1";
			SelectRepetitions = selectRepetitions;
		}

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression, SelectRepetitions = SelectRepetitions };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool selectRepetitions, Dictionary<string, List<object>> expressionData)
		{
			var dialog = new RepeatDialog(selectRepetitions, expressionData) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
