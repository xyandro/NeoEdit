using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericCycleDialog
	{
		[DepProp]
		public string Minimum { get { return UIHelper<NumericCycleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericCycleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Maximum { get { return UIHelper<NumericCycleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericCycleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeBeginning { get { return UIHelper<NumericCycleDialog>.GetPropValue<bool>(this); } set { UIHelper<NumericCycleDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NumericCycleDialog() { UIHelper<NumericCycleDialog>.Register(); }

		NumericCycleDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			IncludeBeginning = true;
		}

		NumericCycleDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new NumericCycleDialogResult { Minimum = Minimum, Maximum = Maximum, IncludeBeginning = IncludeBeginning };
			DialogResult = true;
		}

		public static NumericCycleDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericCycleDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
