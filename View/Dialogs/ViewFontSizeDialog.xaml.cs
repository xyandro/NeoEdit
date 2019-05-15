using System.Windows;
using NeoEdit.Controls;
using NeoEdit.Misc;

namespace NeoEdit.Dialogs
{
	partial class ViewFontSizeDialog
	{
		[DepProp]
		public double TextFontSize { get { return UIHelper<ViewFontSizeDialog>.GetPropValue<double>(this); } set { UIHelper<ViewFontSizeDialog>.SetPropValue(this, value); } }

		static ViewFontSizeDialog() { UIHelper<ViewFontSizeDialog>.Register(); }

		ViewFontSizeDialog()
		{
			InitializeComponent();
			TextFontSize = Font.FontSize;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Font.FontSize = TextFontSize;
			DialogResult = true;
		}

		public static void Run(Window parent) => new ViewFontSizeDialog() { Owner = parent }.ShowDialog();
	}
}
