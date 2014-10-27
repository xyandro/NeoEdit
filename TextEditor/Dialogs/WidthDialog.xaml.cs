using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class WidthDialog
	{
		public enum PadLocation
		{
			Before,
			After,
			Both,
		}

		internal class Result
		{
			public int Length { get; set; }
			public char PadChar { get; set; }
			public PadLocation Location { get; set; }
		}

		[DepProp]
		public int Length { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MinValue { get { return UIHelper<WidthDialog>.GetPropValue<int>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PadChar { get { return UIHelper<WidthDialog>.GetPropValue<string>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }
		[DepProp]
		public PadLocation Location { get { return UIHelper<WidthDialog>.GetPropValue<PadLocation>(this); } set { UIHelper<WidthDialog>.SetPropValue(this, value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		WidthDialog(int minValue, char padChar, bool before)
		{
			InitializeComponent();

			this.padChar.GotFocus += (s, e) => this.padChar.SelectAll();

			Length = MinValue = minValue;
			PadChar = new string(padChar, 1);
			Location = before ? PadLocation.Before : PadLocation.After;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (PadChar.Length != 1)
				return;
			result = new Result { Length = Length, PadChar = PadChar[0], Location = Location };
			DialogResult = true;
		}

		public static Result Run(int minValue, char padChar, bool before)
		{
			var dialog = new WidthDialog(minValue, padChar, before);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
