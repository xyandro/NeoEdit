using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Numeric_Various_Dialog
	{
		[DepProp]
		public string BaseValue { get { return UIHelper<Numeric_Various_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Interval { get { return UIHelper<Numeric_Various_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_Various_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Numeric_Various_Dialog() { UIHelper<Numeric_Various_Dialog>.Register(); }

		Numeric_Various_Dialog(string title, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Title = title;
			BaseValue = "0";
			Interval = "1";
		}

		Configuration_Numeric_Various result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_Various { BaseValue = BaseValue, Interval = Interval };
			baseValue.AddCurrentSuggestion();
			interval.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Numeric_Various Run(Window parent, string title, NEVariables variables)
		{
			var dialog = new Numeric_Various_Dialog(title, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
