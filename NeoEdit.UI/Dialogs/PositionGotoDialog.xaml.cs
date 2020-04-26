using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class PositionGotoDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<PositionGotoDialog>.GetPropValue<string>(this); } set { UIHelper<PositionGotoDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OpenFilesOnce { get { return UIHelper<PositionGotoDialog>.GetPropValue<bool>(this); } set { UIHelper<PositionGotoDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static PositionGotoDialog() { UIHelper<PositionGotoDialog>.Register(); }

		PositionGotoDialog(GotoType gotoType, int value, NEVariables variables)
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
			var dialog = new PositionGotoDialog(gotoType, startValue, variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
