using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class WidthDialog
	{
		internal class Result
		{
			public int Value { get; set; }
			public char PadChar { get; set; }
			public bool Before { get; set; }
		}

		[DepProp]
		public int Value { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MinValue { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Before { get { return UIHelper<WidthDialog>.GetPropValue<bool>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		WidthDialog(int minValue, char padChar, bool before)
		{
			InitializeComponent();

			this.padChar.GotFocus += (s, e) => this.padChar.SelectAll();

			Value = MinValue = minValue;
			PadChar = new string(padChar, 1);
			if (before)
				this.before.IsChecked = true;
			else
				this.after.IsChecked = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Value = Value, PadChar = PadChar[0], Before = Before };
			DialogResult = true;
		}

		public static Result Run(int minValue, char padChar, bool before)
		{
			var dialog = new WidthDialog(minValue, padChar, before);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
