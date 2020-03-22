using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class MacroPlayRepeatDialog
	{
		[DepProp]
		public string Macro { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public MacroPlayRepeatDialogResult.RepeatTypeEnum RepeatType { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<MacroPlayRepeatDialogResult.RepeatTypeEnum>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }

		static MacroPlayRepeatDialog() { UIHelper<MacroPlayRepeatDialog>.Register(); }

		readonly Func<string> chooseMacro;
		MacroPlayRepeatDialog(Func<string> chooseMacro)
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
			var dialog = new MacroPlayRepeatDialog(chooseMacro) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
