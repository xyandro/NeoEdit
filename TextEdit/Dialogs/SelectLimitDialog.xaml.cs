using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SelectLimitDialog
	{
		internal class Result
		{
			public string FirstSelection { get; set; }
			public string EveryNth { get; set; }
			public string TakeCount { get; set; }
			public string NumSelections { get; set; }
			public bool JoinSelections { get; set; }
			public bool WithinRegions { get; set; }
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
		[DepProp]
		public bool WithinRegions { get { return UIHelper<SelectLimitDialog>.GetPropValue<bool>(this); } set { UIHelper<SelectLimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static SelectLimitDialog() { UIHelper<SelectLimitDialog>.Register(); }

		SelectLimitDialog(int numSelections, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			FirstSelection = EveryNth = TakeCount = "1";
			NumSelections = numSelections.ToString();
			JoinSelections = WithinRegions = false;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			firstSelection.AddCurrentSuggestion();
			everyNth.AddCurrentSuggestion();
			takeCount.AddCurrentSuggestion();
			numSelections.AddCurrentSuggestion();
			result = new Result { FirstSelection = FirstSelection, EveryNth = EveryNth, TakeCount = TakeCount, NumSelections = NumSelections, JoinSelections = JoinSelections, WithinRegions = WithinRegions };
			DialogResult = true;
		}

		public static Result Run(Window parent, int numSelections, NEVariables variables)
		{
			var dialog = new SelectLimitDialog(numSelections, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
