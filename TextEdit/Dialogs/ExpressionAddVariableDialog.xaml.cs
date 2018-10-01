using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ExpressionAddVariableDialog
	{
		internal class Result
		{
			public string VarName { get; set; }
			public string Expression { get; set; }
		}


		[DepProp]
		public string VarName { get { return UIHelper<ExpressionAddVariableDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionAddVariableDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<ExpressionAddVariableDialog>.GetPropValue<string>(this); } set { UIHelper<ExpressionAddVariableDialog>.SetPropValue(this, value); } }

		static ExpressionAddVariableDialog() { UIHelper<ExpressionAddVariableDialog>.Register(); }

		ExpressionAddVariableDialog()
		{
			InitializeComponent();
			VarName = "";
			Expression = "0";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((string.IsNullOrWhiteSpace(VarName)) || (string.IsNullOrWhiteSpace(Expression)))
				return;

			result = new Result { VarName = VarName, Expression = Expression };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new ExpressionAddVariableDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
