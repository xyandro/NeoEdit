using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Dialogs
{
	partial class NumericScaleDialog
	{
		public class Result
		{
			public string PrevMin { get; set; }
			public string PrevMax { get; set; }
			public string NewMin { get; set; }
			public string NewMax { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { PrevMin = PrevMin, PrevMax = PrevMax, NewMin = NewMin, NewMax = NewMax };
			DialogResult = true;
		}

		static public Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericScaleDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
