using System.Windows;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Window_Font_Size_Dialog
	{
		[DepProp]
		public double TextFontSize { get { return UIHelper<Window_Font_Size_Dialog>.GetPropValue<double>(this); } set { UIHelper<Window_Font_Size_Dialog>.SetPropValue(this, value); } }

		static Window_Font_Size_Dialog() { UIHelper<Window_Font_Size_Dialog>.Register(); }

		Window_Font_Size_Dialog()
		{
			InitializeComponent();
			TextFontSize = Font.FontSize;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Settings.FontSize = TextFontSize;
			Font.Reset();
			DialogResult = true;
		}

		public static void Run(Window parent) => new Window_Font_Size_Dialog() { Owner = parent }.ShowDialog();
	}
}
