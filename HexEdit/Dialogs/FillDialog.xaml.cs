using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs
{
	partial class FillDialog
	{
		[DepProp]
		public byte Fill { get { return UIHelper<FillDialog>.GetPropValue(() => this.Fill); } set { UIHelper<FillDialog>.SetPropValue(() => this.Fill, value); } }

		static FillDialog() { UIHelper<FillDialog>.Register(); }

		FillDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static byte? Run()
		{
			var dialog = new FillDialog();
			return dialog.ShowDialog() == true ? (byte?)dialog.Fill : null;
		}
	}
}
