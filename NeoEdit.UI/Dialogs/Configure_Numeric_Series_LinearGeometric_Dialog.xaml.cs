using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Numeric_Series_LinearGeometric_Dialog
	{
		[DepProp]
		public string StartExpression { get { return UIHelper<Configure_Numeric_Series_LinearGeometric_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Series_LinearGeometric_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string IncrementExpression { get { return UIHelper<Configure_Numeric_Series_LinearGeometric_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Series_LinearGeometric_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Numeric_Series_LinearGeometric_Dialog() { UIHelper<Configure_Numeric_Series_LinearGeometric_Dialog>.Register(); }

		Configure_Numeric_Series_LinearGeometric_Dialog(bool linear, NEVariables variables)
		{
			Variables = variables;

			InitializeComponent();
			if (linear)
			{
				StartExpression = "linestart";
				IncrementExpression = "lineincrement";
			}
			else
			{
				StartExpression = "geostart";
				IncrementExpression = "geoincrement";
				Title = "Geometric Series";
				incrementLabel.Content = "_Multiplier";
			}
		}

		Configuration_Numeric_Series_LinearGeometric result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Series_LinearGeometric { StartExpression = StartExpression, IncrementExpression = IncrementExpression };
			DialogResult = true;
		}

		public static Configuration_Numeric_Series_LinearGeometric Run(Window parent, bool linear, NEVariables variables)
		{
			var dialog = new Configure_Numeric_Series_LinearGeometric_Dialog(linear, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
