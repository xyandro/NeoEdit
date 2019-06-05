using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Dialogs
{
	partial class SelectLimitDialog
	{
		public class Result
		{
			public string FirstSelection { get; set; }
			public string EveryNth { get; set; }
			public string TakeCount { get; set; }
			public string NumSelections { get; set; }
			public bool JoinSelections { get; set; }
		}

		[DepProp]
		public string FirstSelection { get { return UIHelper<SelectLimitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string EveryNth { get { return UIHelper<SelectLimitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TakeCount { get { return UIHelper<SelectLimitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NumSelections { get { return UIHelper<SelectLimitDialog>.GetPropValue<string>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool JoinSelections { get { return UIHelper<SelectLimitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static SelectLimitDialog() { UIHelper<SelectLimitDialog>.Register(); }

		SelectLimitDialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			FirstSelection = EveryNth = TakeCount = "1";
			NumSelections = "xn";
			JoinSelections = false;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			firstSelection.AddCurrentSuggestion();
			everyNth.AddCurrentSuggestion();
			takeCount.AddCurrentSuggestion();
			numSelections.AddCurrentSuggestion();
			result = new Result { FirstSelection = FirstSelection, EveryNth = EveryNth, TakeCount = TakeCount, NumSelections = NumSelections, JoinSelections = JoinSelections };
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new SelectLimitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
