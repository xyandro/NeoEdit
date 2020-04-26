using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class SelectLimitDialog
	{
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

		SelectLimitDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			firstSelection.AddCurrentSuggestion();
			everyNth.AddCurrentSuggestion();
			takeCount.AddCurrentSuggestion();
			numSelections.AddCurrentSuggestion();
			result = new SelectLimitDialogResult { FirstSelection = FirstSelection, EveryNth = EveryNth, TakeCount = TakeCount, NumSelections = NumSelections, JoinSelections = JoinSelections };
			DialogResult = true;
		}

		public static SelectLimitDialogResult Run(Window parent, NEVariables variables)
		{
			var dialog = new SelectLimitDialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();

			return dialog.result;
		}
	}
}
