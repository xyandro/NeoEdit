using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.MenuEdit.Dialogs
{
	partial class EditDataHashDialog
	{
		public class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public Hasher.Type HashType { get; set; }
			public byte[] HMACKey { get; set; }
		}

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

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { CodePage = CodePage, HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new EditDataHashDialog(codePage) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
