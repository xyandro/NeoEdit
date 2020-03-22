using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
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

		NumericRandomNumberDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NumericRandomNumberDialogResult { MinValue = MinValue, MaxValue = MaxValue };
			DialogResult = true;
		}

		static public NumericRandomNumberDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericRandomNumberDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
