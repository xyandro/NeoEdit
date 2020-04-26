using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class EditDataHashDialog
	{
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<EditDataHashDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataHashDialog>.SetPropValue(this, value); } }
		[DepProp]
		Hasher.Type HashType { get { return UIHelper<EditDataHashDialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<EditDataHashDialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] HMACKey { get { return UIHelper<EditDataHashDialog>.GetPropValue<byte[]>(this); } set { UIHelper<EditDataHashDialog>.SetPropValue(this, value); } }

		static EditDataHashDialog() { UIHelper<EditDataHashDialog>.Register(); }

		EditDataHashDialog(Coder.CodePage _codePage)
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();

			CodePage = _codePage;
			HashType = Hasher.Type.SHA1;
		}

		EditDataHashDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EditDataHashDialogResult { CodePage = CodePage, HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static EditDataHashDialogResult Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new EditDataHashDialog(codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
