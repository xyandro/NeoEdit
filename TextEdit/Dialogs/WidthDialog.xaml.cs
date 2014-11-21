using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class WidthDialog
	{
		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		internal class Result
		{
			public int Length { get; set; }
			public bool ClipboardValue { get; set; }
			public char PadChar { get; set; }
			public TextLocation Location { get; set; }
		}

		[DepProp]
		public int Length { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ClipboardValue { get { return UIHelper<WidthDialog>.GetPropValue<bool>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int StartLength { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextLocation Location { get { return UIHelper<WidthDialog>.GetPropValue<TextLocation>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		WidthDialog(int startLength, char padChar, bool before)
		{
			InitializeComponent();

			this.padChar.GotFocus += (s, e) => this.padChar.SelectAll();

			Length = StartLength = startLength;
			ClipboardValue = false;
			PadChar = new string(padChar, 1);
			Location = before ? TextLocation.End : TextLocation.Start;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Length = Length, ClipboardValue = ClipboardValue, PadChar = PadChar[0], Location = Location };
			DialogResult = true;
		}

		public static Result Run(int startLength, char padChar, bool before)
		{
			var dialog = new WidthDialog(startLength, padChar, before);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
