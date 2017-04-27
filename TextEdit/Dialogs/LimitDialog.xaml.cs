using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class LimitDialog
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
		public string FirstSelection { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string EveryNth { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TakeCount { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NumSelections { get { return UIHelper<LimitDialog>.GetPropValue<string>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool JoinSelections { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool WithinRegions { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int numSelections, NEVariables variables)
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
			var dialog = new LimitDialog(numSelections, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return dialog.result;
		}
	}
}
