using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericRandomNumberDialog
	{
		[DepProp]
		public string MinValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<string>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string MaxValue { get { return UIHelper<NumericRandomNumberDialog>.GetPropValue<string>(this); } set { UIHelper<NumericRandomNumberDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NumericRandomNumberDialog() { UIHelper<NumericRandomNumberDialog>.Register(); }

		NumericRandomNumberDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			MinValue = "1";
			MaxValue = "1000";
		}

		Configuration_Numeric_RandomNumber result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_RandomNumber { MinValue = MinValue, MaxValue = MaxValue };
			DialogResult = true;
		}

		public static Configuration_Numeric_RandomNumber Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericRandomNumberDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
