using System;
using System.Windows;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class MacroPlayRepeatDialog
	{
		public enum RepeatTypeEnum
		{
			Number,
			Condition,
		}

		internal class Result
		{
			public string Macro { get; set; }
			public string Expression { get; set; }
			public RepeatTypeEnum RepeatType { get; set; }
		}

		[DepProp]
		public string Macro { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public RepeatTypeEnum RepeatType { get { return UIHelper<MacroPlayRepeatDialog>.GetPropValue<RepeatTypeEnum>(this); } set { UIHelper<MacroPlayRepeatDialog>.SetPropValue(this, value); } }

		static MacroPlayRepeatDialog() { UIHelper<MacroPlayRepeatDialog>.Register(); }

		readonly Func<string> chooseMacro;
		MacroPlayRepeatDialog(Func<string> chooseMacro)
		{
			this.chooseMacro = chooseMacro;
			InitializeComponent();
		}

		void ChooseMacro(object sender, RoutedEventArgs e) => Macro = chooseMacro();

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((string.IsNullOrWhiteSpace(Macro)) || (string.IsNullOrWhiteSpace(Expression)))
				return;
			result = new Result { Macro = Macro, Expression = Expression, RepeatType = RepeatType };
			DialogResult = true;
		}

		public static Result Run(Window parent, Func<string> chooseMacro)
		{
			var dialog = new MacroPlayRepeatDialog(chooseMacro) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
