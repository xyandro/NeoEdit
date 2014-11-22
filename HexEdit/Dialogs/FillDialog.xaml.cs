using System.Globalization;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs
{
	partial class FillDialog
	{
		[DepProp]
		public byte Fill { get { return UIHelper<FillDialog>.GetPropValue<byte>(this); } set { UIHelper<FillDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsHex { get { return UIHelper<FillDialog>.GetPropValue<bool>(this); } set { UIHelper<FillDialog>.SetPropValue(this, value); } }

		static FillDialog()
		{
			UIHelper<FillDialog>.Register();
			UIHelper<FillDialog>.AddCallback(a => a.IsHex, (obj, o, n) => obj.IsHexChanged());
		}

		FillDialog()
		{
			InitializeComponent();
			IsHex = true;
		}

		void IsHexChanged()
		{
			fill.FormatString = IsHex ? "x" : "";
			fill.ParsingNumberStyle = IsHex ? NumberStyles.HexNumber : NumberStyles.Integer;
			FocusManager.SetFocusedElement(this, fill);
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
