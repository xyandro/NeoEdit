using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class WidthDialog
	{
		public enum WidthType
		{
			None,
			Absolute,
			Relative,
			Multiple,
			Clipboard,
		}

		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		internal class Result
		{
			public WidthType Type { get; set; }
			public int Value { get; set; }
			public char PadChar { get; set; }
			public TextLocation Location { get; set; }
		}

		[DepProp]
		public WidthType Type { get { return UIHelper<WidthDialog>.GetPropValue<WidthType>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Value { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextLocation Location { get { return UIHelper<WidthDialog>.GetPropValue<TextLocation>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool NeedValue { get { return UIHelper<WidthDialog>.GetPropValue<bool>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }

		static WidthDialog()
		{
			UIHelper<WidthDialog>.Register();
			UIHelper<WidthDialog>.AddCallback(a => a.Type, (obj, o, n) => obj.SetValueParams());
		}

		readonly int startLength;
		WidthDialog(int startLength, char padChar, bool before)
		{
			InitializeComponent();

			this.startLength = startLength;
			this.padChar.GotFocus += (s, e) => this.padChar.SelectAll();

			Type = WidthType.Absolute;
			PadChar = new string(padChar, 1);
			Location = before ? TextLocation.End : TextLocation.Start;
		}

		void SetValueParams()
		{
			NeedValue = Type != WidthType.Clipboard;
			value.Minimum = Type == WidthType.Absolute ? 0 : Type == WidthType.Multiple ? 1 : int.MinValue;
			Value = Type == WidthType.Absolute ? startLength : Type == WidthType.Relative ? 0 : Type == WidthType.Multiple ? 1 : Value;
		}

		void NumericClick(object sender, RoutedEventArgs e)
		{
			PadChar = "0";
			Location = TextLocation.End;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			PadChar = " ";
			Location = TextLocation.Start;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Type = Type, Value = Value, PadChar = PadChar[0], Location = Location };
			DialogResult = true;
		}

		public static Result Run(int startLength, char padChar, bool before)
		{
			var dialog = new WidthDialog(startLength, padChar, before);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
