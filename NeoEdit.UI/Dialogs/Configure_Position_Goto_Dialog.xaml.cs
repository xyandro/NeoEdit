using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Position_Goto_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Position_Goto_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Position_Goto_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OpenFilesOnce { get { return UIHelper<Configure_Position_Goto_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Position_Goto_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Position_Goto_Dialog() { UIHelper<Configure_Position_Goto_Dialog>.Register(); }

		Configure_Position_Goto_Dialog(GotoType gotoType, int value, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Title = $"Go To {gotoType}";
			Expression = value.ToString();
			OpenFilesOnce = true;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Position_Goto result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Position_Goto { Expression = Expression, OpenFilesOnce = OpenFilesOnce };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Position_Goto Run(Window parent, GotoType gotoType, int startValue, NEVariables variables)
		{
			var dialog = new Configure_Position_Goto_Dialog(gotoType, startValue, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
