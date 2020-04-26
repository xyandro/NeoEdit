using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Numeric_Limit_Dialog
	{
		[DepProp]
		public string Minimum { get { return UIHelper<Configure_Numeric_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Limit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Maximum { get { return UIHelper<Configure_Numeric_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Limit_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Numeric_Limit_Dialog() { UIHelper<Configure_Numeric_Limit_Dialog>.Register(); }

		Configure_Numeric_Limit_Dialog(NEVariables variables)
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
			var dialog = new Configure_Numeric_Limit_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
