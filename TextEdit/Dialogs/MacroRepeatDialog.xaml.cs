using System;
using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class MacroRepeatDialog
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
		public string Macro { get { return UIHelper<MacroRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Expression { get { return UIHelper<MacroRepeatDialog>.GetPropValue<string>(this); } set { UIHelper<MacroRepeatDialog>.SetPropValue(this, value); } }
		[DepProp]
		public RepeatTypeEnum RepeatType { get { return UIHelper<MacroRepeatDialog>.GetPropValue<RepeatTypeEnum>(this); } set { UIHelper<MacroRepeatDialog>.SetPropValue(this, value); } }

		static MacroRepeatDialog() { UIHelper<MacroRepeatDialog>.Register(); }

		readonly Func<string> chooseMacro;
		MacroRepeatDialog(Func<string> chooseMacro)
		{
			this.chooseMacro = chooseMacro;
			InitializeComponent();
		}

		void ChooseMacro(object sender, RoutedEventArgs e) => Macro = chooseMacro();

		private void ExpressionHelp(object sender, RoutedEventArgs e) => ExpressionHelpDialog.Display();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((String.IsNullOrWhiteSpace(Macro)) || (String.IsNullOrWhiteSpace(Expression)))
				return;
			result = new Result { Macro = Macro, Expression = Expression, RepeatType = RepeatType };
			DialogResult = true;
		}

		public static Result Run(Window parent, Func<string> chooseMacro)
		{
			var dialog = new MacroRepeatDialog(chooseMacro) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
