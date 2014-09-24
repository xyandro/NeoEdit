using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	public partial class WidthDialog : Window
	{
		[DepProp]
		public int WidthNum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinWidthNum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public char PadChar { get { return uiHelper.GetPropValue<char>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Before { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		readonly UIHelper<WidthDialog> uiHelper;
		public WidthDialog()
		{
			uiHelper = new UIHelper<WidthDialog>(this);
			InitializeComponent();

			Loaded += (s, e) => WidthNum = MinWidthNum;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
