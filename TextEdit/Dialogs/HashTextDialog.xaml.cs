using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class HashTextDialog
	{
		public class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public Hasher.Type HashType { get; set; }
			public byte[] Key { get; set; }
		}

		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<HashTextDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<HashTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		Hasher.Type HashType { get { return UIHelper<HashTextDialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<HashTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] Key { get { return UIHelper<HashTextDialog>.GetPropValue<byte[]>(this); } set { UIHelper<HashTextDialog>.SetPropValue(this, value); } }
		[DepProp]
		bool NeedsKey { get { return UIHelper<HashTextDialog>.GetPropValue<bool>(this); } set { UIHelper<HashTextDialog>.SetPropValue(this, value); } }

		static HashTextDialog()
		{
			UIHelper<HashTextDialog>.Register();
			UIHelper<HashTextDialog>.AddCallback(a => a.HashType, (obj, o, n) => obj.NeedsKey = Hasher.NeedsKey(obj.HashType));
		}

		HashTextDialog(Coder.CodePage _codePage)
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetStringCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();

			CodePage = _codePage;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { CodePage = CodePage, HashType = HashType, Key = Key };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new HashTextDialog(codePage) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
