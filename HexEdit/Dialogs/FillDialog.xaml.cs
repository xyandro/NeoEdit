using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs
{
	partial class FillDialog
	{
		[DepProp]
		public byte Fill { get { return UIHelper<FillDialog>.GetPropValue<byte>(this); } set { UIHelper<FillDialog>.SetPropValue(this, value); } }

		static FillDialog() { UIHelper<FillDialog>.Register(); }

		FillDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static byte? Run(Window parent)
		{
			var dialog = new FillDialog { Owner = parent };
			return dialog.ShowDialog() ? (byte?)dialog.Fill : null;
		}
	}
}
