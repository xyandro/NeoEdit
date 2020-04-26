using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericLimitDialog
	{
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

		Configuration_Numeric_Limit result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Limit { Minimum = Minimum, Maximum = Maximum };
			DialogResult = true;
		}

		public static Configuration_Numeric_Limit Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericLimitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
