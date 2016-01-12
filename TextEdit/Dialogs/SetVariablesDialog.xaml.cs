using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SetVariablesDialog
	{
		internal class Result
		{
			public string VarName { get; set; }
		}


		[DepProp]
		public string VarName { get { return UIHelper<SetVariablesDialog>.GetPropValue<string>(this); } set { UIHelper<SetVariablesDialog>.SetPropValue(this, value); } }

		static SetVariablesDialog() { UIHelper<SetVariablesDialog>.Register(); }

		SetVariablesDialog(string varName)
		{
			InitializeComponent();
			VarName = varName;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrWhiteSpace(VarName))
				return;

			result = new Result { VarName = VarName };
			DialogResult = true;
		}

		static public Result Run(Window parent, string varName)
		{
			var dialog = new SetVariablesDialog(varName) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
