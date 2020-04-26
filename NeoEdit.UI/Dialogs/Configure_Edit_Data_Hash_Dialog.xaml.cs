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
	partial class Configure_Edit_Data_Hash_Dialog
	{
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<Configure_Edit_Data_Hash_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Hash_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Hasher.Type HashType { get { return UIHelper<Configure_Edit_Data_Hash_Dialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<Configure_Edit_Data_Hash_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] HMACKey { get { return UIHelper<Configure_Edit_Data_Hash_Dialog>.GetPropValue<byte[]>(this); } set { UIHelper<Configure_Edit_Data_Hash_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Data_Hash_Dialog() { UIHelper<Configure_Edit_Data_Hash_Dialog>.Register(); }

		Configure_Edit_Data_Hash_Dialog(Coder.CodePage _codePage)
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();

			CodePage = _codePage;
			HashType = Hasher.Type.SHA1;
		}

		Configuration_Edit_Data_Hash result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Data_Hash { CodePage = CodePage, HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Configuration_Edit_Data_Hash Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new Configure_Edit_Data_Hash_Dialog(codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
