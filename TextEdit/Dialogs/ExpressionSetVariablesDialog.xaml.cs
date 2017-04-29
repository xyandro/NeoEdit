using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ExpressionSetVariablesDialog
	{
		internal class Result
		{
			public string VarName { get; set; }
		}


		[DepProp]
		public string VarName { get { return UIHelper<ExpressionSetVariablesDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionSetVariablesDialog>.SetPropValue(this, value); } }

		static ExpressionSetVariablesDialog() { UIHelper<ExpressionSetVariablesDialog>.Register(); }

		ExpressionSetVariablesDialog(string varName)
		{
			InitializeComponent();
			VarName = varName;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VarName))
				return;

			result = new Result { VarName = VarName };
			DialogResult = true;
		}

		static public Result Run(Window parent, string varName)
		{
			var dialog = new ExpressionSetVariablesDialog(varName) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
