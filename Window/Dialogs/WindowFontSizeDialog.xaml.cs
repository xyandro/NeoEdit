using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Misc;

namespace NeoEdit.Dialogs
{
	partial class WindowFontSizeDialog
	{
		[DepProp]
		public double TextFontSize { get { return UIHelper<WindowFontSizeDialog>.GetPropValue<double>(this); } set { UIHelper<WindowFontSizeDialog>.SetPropValue(this, value); } }

		static WindowFontSizeDialog() { UIHelper<WindowFontSizeDialog>.Register(); }

		WindowFontSizeDialog()
		{
			InitializeComponent();
			TextFontSize = Font.FontSize;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Font.FontSize = TextFontSize;
			DialogResult = true;
		}

		public static void Run(Window parent) => new WindowFontSizeDialog() { Owner = parent }.ShowDialog();
	}
}
