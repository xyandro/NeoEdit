using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericScaleDialog
	{
		[DepProp]
		public string PrevMin { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PrevMax { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMin { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NewMax { get { return UIHelper<NumericScaleDialog>.GetPropValue<string>(this); } set { UIHelper<NumericScaleDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static NumericScaleDialog() { UIHelper<NumericScaleDialog>.Register(); }

		NumericScaleDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			PrevMin = NewMin = "xmin";
			PrevMax = NewMax = "xmax";
		}

		Configuration_Numeric_Scale result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Scale { PrevMin = PrevMin, PrevMax = PrevMax, NewMin = NewMin, NewMax = NewMax };
			DialogResult = true;
		}

		public static Configuration_Numeric_Scale Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericScaleDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
