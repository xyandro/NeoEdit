using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Position_Goto_Various_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Position_Goto_Various_Dialog>.GetPropValue<string>(this); } set { UIHelper<Position_Goto_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OpenFilesOnce { get { return UIHelper<Position_Goto_Various_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Position_Goto_Various_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Position_Goto_Various_Dialog() { UIHelper<Position_Goto_Various_Dialog>.Register(); }

		Position_Goto_Various_Dialog(GotoType gotoType, int value, NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			Title = $"Go To {gotoType}";
			Expression = value.ToString();
			OpenFilesOnce = true;
		}

		void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display(Variables);

		Configuration_Position_Goto_Various result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Position_Goto_Various { Expression = Expression, OpenFilesOnce = OpenFilesOnce };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Position_Goto_Various Run(Window parent, GotoType gotoType, int startValue, NEVariables variables)
		{
			var dialog = new Position_Goto_Various_Dialog(gotoType, startValue, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
