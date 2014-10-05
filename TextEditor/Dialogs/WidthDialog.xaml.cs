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
		public int Value { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public char PadChar { get { return uiHelper.GetPropValue<char>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Before { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		readonly UIHelper<WidthDialog> uiHelper;
		WidthDialog(int minValue, char padChar, bool before)
		{
			uiHelper = new UIHelper<WidthDialog>(this);
			InitializeComponent();

			Value = MinValue = minValue;
			PadChar = padChar;
			Before = before;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Value = Value, PadChar = PadChar, Before = Before };
			DialogResult = true;
		}

		public static Result Run(int minValue, char padChar, bool before)
		{
			var dialog = new WidthDialog(minValue, padChar, before);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
