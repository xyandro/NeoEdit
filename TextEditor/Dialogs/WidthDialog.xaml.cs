using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class WidthDialog : Window
	{
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
		public WidthDialog(int minValue, char padChar, bool before)
		{
			uiHelper = new UIHelper<WidthDialog>(this);
			InitializeComponent();

			Value = MinValue = minValue;
			PadChar = padChar;
			Before = before;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
