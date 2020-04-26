using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Numeric_Floor_Dialog
	{
		[DepProp]
		public string BaseValue { get { return UIHelper<Configure_Numeric_Floor_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Floor_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Interval { get { return UIHelper<Configure_Numeric_Floor_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Numeric_Floor_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Numeric_Floor_Dialog() { UIHelper<Configure_Numeric_Floor_Dialog>.Register(); }

		Configure_Numeric_Floor_Dialog(string title, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Title = title;
			BaseValue = "0";
			Interval = "1";
		}

		Configuration_Numeric_Floor result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Floor { BaseValue = BaseValue, Interval = Interval };
			baseValue.AddCurrentSuggestion();
			interval.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Numeric_Floor Run(Window parent, string title, NEVariables variables)
		{
			var dialog = new Configure_Numeric_Floor_Dialog(title, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
