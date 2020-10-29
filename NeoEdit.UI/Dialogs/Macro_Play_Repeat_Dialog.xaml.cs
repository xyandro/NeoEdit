using System;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Macro_Play_Repeat_Dialog
	{
		[DepProp]
		public string Macro { get { return UIHelper<Macro_Play_Repeat_Dialog>.GetPropValue<string>(this); } set { UIHelper<Macro_Play_Repeat_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<Macro_Play_Repeat_Dialog>.GetPropValue<string>(this); } set { UIHelper<Macro_Play_Repeat_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public MacroPlayRepeatDialogResult.RepeatTypeEnum RepeatType { get { return UIHelper<Macro_Play_Repeat_Dialog>.GetPropValue<MacroPlayRepeatDialogResult.RepeatTypeEnum>(this); } set { UIHelper<Macro_Play_Repeat_Dialog>.SetPropValue(this, value); } }

		static Macro_Play_Repeat_Dialog() { UIHelper<Macro_Play_Repeat_Dialog>.Register(); }

		readonly Func<string> chooseMacro;
		Macro_Play_Repeat_Dialog(Func<string> chooseMacro)
		{
			this.chooseMacro = chooseMacro;
			InitializeComponent();
		}

		void ChooseMacro(object sender, RoutedEventArgs e) => Macro = chooseMacro();

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		MacroPlayRepeatDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((string.IsNullOrWhiteSpace(Macro)) || (string.IsNullOrWhiteSpace(Expression)))
				return;
			result = new MacroPlayRepeatDialogResult { Macro = Macro, Expression = Expression, RepeatType = RepeatType };
			DialogResult = true;
		}

		public static MacroPlayRepeatDialogResult Run(Window parent, Func<string> chooseMacro)
		{
			var dialog = new Macro_Play_Repeat_Dialog(chooseMacro) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
