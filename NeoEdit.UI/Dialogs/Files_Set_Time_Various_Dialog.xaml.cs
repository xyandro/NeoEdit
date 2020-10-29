using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Set_Time_Various_Dialog
	{
		[DepProp]
		public NEVariables Variables { get { return UIHelper<Files_Set_Time_Various_Dialog>.GetPropValue<NEVariables>(this); } set { UIHelper<Files_Set_Time_Various_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<Files_Set_Time_Various_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Set_Time_Various_Dialog>.SetPropValue(this, value); } }

		static Files_Set_Time_Various_Dialog() { UIHelper<Files_Set_Time_Various_Dialog>.Register(); }

		Files_Set_Time_Various_Dialog(NEVariables variables, string expression)
		{
			InitializeComponent();

			Variables = variables;
			Expression = expression;
		}

		Configuration_Files_Set_Time_Various result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Time_Various { Expression = Expression };
			DialogResult = true;
		}

		public static Configuration_Files_Set_Time_Various Run(Window parent, NEVariables variables, string expression)
		{
			var dialog = new Files_Set_Time_Various_Dialog(variables, expression) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
