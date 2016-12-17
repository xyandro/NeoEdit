using System.Windows;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Misc;

namespace NeoEdit.GUI.Dialogs
{
	partial class FontSizeDialog
	{
		[DepProp]
		public double TextFontSize { get { return UIHelper<FontSizeDialog>.GetPropValue<double>(this); } set { UIHelper<FontSizeDialog>.SetPropValue(this, value); } }

		static FontSizeDialog() { UIHelper<FontSizeDialog>.Register(); }

		FontSizeDialog()
		{
			InitializeComponent();
			TextFontSize = Font.FontSize;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Font.FontSize = TextFontSize;
			DialogResult = true;
		}

		public static void Run(Window parent) => new FontSizeDialog() { Owner = parent }.ShowDialog();
	}
}
