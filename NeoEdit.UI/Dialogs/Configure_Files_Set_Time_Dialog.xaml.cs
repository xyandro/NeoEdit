using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Set_Time_Dialog
	{
		[DepProp]
		public NEVariables Variables { get { return UIHelper<Configure_Files_Set_Time_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<Configure_Files_Set_Time_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Files_Set_Time_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Set_Time_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Set_Time_Dialog() { UIHelper<Configure_Files_Set_Time_Dialog>.Register(); }

		Configure_Files_Set_Time_Dialog(NEVariables variables, string expression)
		{
			InitializeComponent();

			Variables = variables;
			Expression = expression;
		}

		Configuration_Files_Set_Time result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Time { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_Files_Set_Time Run(Window parent, NEVariables variables, string expression)
		{
			var dialog = new Configure_Files_Set_Time_Dialog(variables, expression) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
