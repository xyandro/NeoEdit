using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.TextEdit.Controls;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EditDataSignDialog
	{
		public class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public Cryptor.Type CryptorType { get; set; }
			public string Key { get; set; }
			public string Hash { get; set; }
		}

		[DepProp]
		Dictionary<Coder.CodePage, string> CodePages { get { return UIHelper<EditDataSignDialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<EditDataSignDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<EditDataSignDialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<EditDataSignDialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<EditDataSignDialog>.GetPropValue<string>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<string> Hashes { get { return UIHelper<EditDataSignDialog>.GetPropValue<List<string>>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Hash { get { return UIHelper<EditDataSignDialog>.GetPropValue<string>(this); } set { UIHelper<EditDataSignDialog>.SetPropValue(this, value); } }

		static EditDataSignDialog()
		{
			UIHelper<EditDataSignDialog>.Register();
			UIHelper<EditDataSignDialog>.AddCallback(x => x.CryptorType, (obj, o, n) => obj.CryptorTypeUpdated());
		}

		EditDataSignDialog(Coder.CodePage codePage)
		{
			InitializeComponent();

			CodePages = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).Where(type => type.SigningHashes().Any()).ToList();

			CryptorType = CryptorTypes.First();
			CodePage = codePage;
		}

		void CryptorTypeUpdated()
		{
			Hashes = Cryptor.SigningHashes(CryptorType).ToList();
			Hash = Hashes.FirstOrDefault();
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = CryptorKeyDialog.Run(this, CryptorType, false);
			if (key != null)
				Key = key;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Result { CodePage = CodePage, CryptorType = CryptorType, Key = Key, Hash = Hash };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new EditDataSignDialog(codePage) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
