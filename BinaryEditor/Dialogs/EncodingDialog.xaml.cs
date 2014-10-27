using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor.Dialogs
{
	internal partial class EncodingDialog
	{
		[DepProp]
		StrCoder.CodePage CodePage { get { return UIHelper<EncodingDialog>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string DetectedStr { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }

		readonly StrCoder.CodePage Detected;

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		class Tester
		{
			public string Display { get; set; }
			public StrCoder.CodePage CodePage { get; set; }
		}

		EncodingDialog(StrCoder.CodePage _CodePage, StrCoder.CodePage _Detected)
		{
			InitializeComponent();

			codePage.ItemsSource = StrCoder.GetEncodingTypes();
			codePage.SelectedValuePath = "Item1";
			codePage.DisplayMemberPath = "Item2";

			CodePage = _CodePage;
			Detected = _Detected;
			DetectedStr = StrCoder.GetDescription(Detected);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static StrCoder.CodePage? Run(StrCoder.CodePage codePage, StrCoder.CodePage detected)
		{
			var dialog = new EncodingDialog(codePage, detected);
			return dialog.ShowDialog() == true ? (StrCoder.CodePage?)dialog.CodePage : null;
		}

		void SetDetected(object sender, RoutedEventArgs e)
		{
			CodePage = Detected;
		}
	}
}
