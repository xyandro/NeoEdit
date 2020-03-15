using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericLimitDialog
	{
		public class Result
		{
			public string Minimum { get; set; }
			public string Maximum { get; set; }
		}

		[DepProp]
		public string Minimum { get { return UIHelper<NumericLimitDialog>.GetPropValue<string>(this); } set { UIHelper<NumericLimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Maximum { get { return UIHelper<NumericLimitDialog>.GetPropValue<string>(this); } set { UIHelper<NumericLimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NumericLimitDialog() { UIHelper<NumericLimitDialog>.Register(); }

		NumericLimitDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Minimum = "xmin";
			Maximum = "xmax";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Minimum = Minimum, Maximum = Maximum };
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericLimitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
