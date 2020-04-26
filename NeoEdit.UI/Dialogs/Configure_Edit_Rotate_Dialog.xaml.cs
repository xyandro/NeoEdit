using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Rotate_Dialog
	{
		[DepProp]
		public string Count { get { return UIHelper<Configure_Edit_Rotate_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Rotate_Dialog>.SetPropValue(this, value); } }

		public NEVariables Variables { get; }

		static Configure_Edit_Rotate_Dialog() { UIHelper<Configure_Edit_Rotate_Dialog>.Register(); }

		Configure_Edit_Rotate_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();
			Count = "1";
		}

		Configuration_Edit_Rotate result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			count.AddCurrentSuggestion();
			result = new Configuration_Edit_Rotate { Count = Count };
			DialogResult = true;
		}

		public static Configuration_Edit_Rotate Run(Window parent, NEVariables variables)
		{
			var dialog = new Configure_Edit_Rotate_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();

			return dialog.result;
		}
	}
}
