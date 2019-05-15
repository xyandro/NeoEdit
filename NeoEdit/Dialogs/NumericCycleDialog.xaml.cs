using System.Windows;
using NeoEdit.Expressions;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class NumericCycleDialog
	{
		public class Result
		{
			public string Minimum { get; set; }
			public string Maximum { get; set; }
			public bool IncludeBeginning { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Minimum = Minimum, Maximum = Maximum, IncludeBeginning = IncludeBeginning };
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new NumericCycleDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
