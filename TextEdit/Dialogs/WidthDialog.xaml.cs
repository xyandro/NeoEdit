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
			Minimum,
			Maximum,
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
		public WidthType Type { get { return UIHelper<WidthDialog>.GetPropValue(() => this.Type); } set { UIHelper<WidthDialog>.SetPropValue(() => this.Type, value); } }
		[DepProp]
		public int Value { get { return UIHelper<WidthDialog>.GetPropValue(() => this.Value); } set { UIHelper<WidthDialog>.SetPropValue(() => this.Value, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue(() => this.PadChar); } set { UIHelper<WidthDialog>.SetPropValue(() => this.PadChar, value); } }
		[DepProp]
		public TextLocation Location { get { return UIHelper<WidthDialog>.GetPropValue(() => this.Location); } set { UIHelper<WidthDialog>.SetPropValue(() => this.Location, value); } }
		[DepProp]
		public bool NeedValue { get { return UIHelper<WidthDialog>.GetPropValue(() => this.NeedValue); } set { UIHelper<WidthDialog>.SetPropValue(() => this.NeedValue, value); } }
		[DepProp]
		public bool IsSelect { get { return UIHelper<WidthDialog>.GetPropValue(() => this.IsSelect); } set { UIHelper<WidthDialog>.SetPropValue(() => this.IsSelect, value); } }

		static WidthDialog()
		{
			UIHelper<WidthDialog>.Register();
			UIHelper<WidthDialog>.AddCallback(a => a.Type, (obj, o, n) => obj.SetValueParams());
		}

		readonly int minLength, maxLength;
		WidthDialog(int minLength, int maxLength, bool numeric, bool isSelect)
		{
			InitializeComponent();

			this.minLength = minLength;
			this.maxLength = maxLength;
			padChar.GotFocus += (s, e) => padChar.SelectAll();

			IsSelect = isSelect;

			Type = WidthType.Absolute;
			if (numeric)
				NumericClick(null, null);
			else
				StringClick(null, null);
		}

		void SetValueParams()
		{
			NeedValue = Type != WidthType.Clipboard;
			value.Minimum = Type == WidthType.Multiple ? 1 : Type == WidthType.Relative ? int.MinValue : 0;
			Value = Type == WidthType.Multiple ? 1 : Type == WidthType.Relative ? 0 : Type == WidthType.Minimum ? minLength : maxLength;
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

		public static Result Run(Window parent, int minLength, int maxLength, bool numeric, bool isSelect)
		{
			var dialog = new WidthDialog(minLength, maxLength, numeric, isSelect) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
