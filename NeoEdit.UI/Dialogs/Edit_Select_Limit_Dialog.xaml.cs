using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Edit_Select_Limit_Dialog
	{
		[DepProp]
		public string FirstSelection { get { return UIHelper<Edit_Select_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Select_Limit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string EveryNth { get { return UIHelper<Edit_Select_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Select_Limit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string TakeCount { get { return UIHelper<Edit_Select_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Select_Limit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string NumSelections { get { return UIHelper<Edit_Select_Limit_Dialog>.GetPropValue<string>(this); } set { UIHelper<Edit_Select_Limit_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool JoinSelections { get { return UIHelper<Edit_Select_Limit_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Edit_Select_Limit_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Edit_Select_Limit_Dialog() { UIHelper<Edit_Select_Limit_Dialog>.Register(); }

		Edit_Select_Limit_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			FirstSelection = EveryNth = TakeCount = "1";
			NumSelections = "xn";
			JoinSelections = false;
		}

		Configuration_Edit_Select_Limit result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			firstSelection.AddCurrentSuggestion();
			everyNth.AddCurrentSuggestion();
			takeCount.AddCurrentSuggestion();
			numSelections.AddCurrentSuggestion();
			result = new Configuration_Edit_Select_Limit { FirstSelection = FirstSelection, EveryNth = EveryNth, TakeCount = TakeCount, NumSelections = NumSelections, JoinSelections = JoinSelections };
			DialogResult = true;
		}

		public static Configuration_Edit_Select_Limit Run(Window parent, NEVariables variables)
		{
			var dialog = new Edit_Select_Limit_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();

			return dialog.result;
		}
	}
}
