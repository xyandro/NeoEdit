using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
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

		Configuration_Numeric_Cycle result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Cycle { Minimum = Minimum, Maximum = Maximum, IncludeBeginning = IncludeBeginning };
			DialogResult = true;
		}

		public static Configuration_Numeric_Cycle Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericCycleDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
