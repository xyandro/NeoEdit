using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Numeric_Cycle_Dialog
	{
		[DepProp]
		public string Minimum { get { return UIHelper<Configure_Numeric_Cycle_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Cycle_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Maximum { get { return UIHelper<Configure_Numeric_Cycle_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Cycle_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IncludeBeginning { get { return UIHelper<Configure_Numeric_Cycle_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Numeric_Cycle_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Numeric_Cycle_Dialog() { UIHelper<Configure_Numeric_Cycle_Dialog>.Register(); }

		Configure_Numeric_Cycle_Dialog(NEVariables variables)
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
			var dialog = new Configure_Numeric_Cycle_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
