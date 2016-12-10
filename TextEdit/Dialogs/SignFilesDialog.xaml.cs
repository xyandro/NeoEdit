using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class SignFilesDialog
	{
		public class Result
		{
			public Cryptor.Type CryptorType { get; set; }
			public string Key { get; set; }
			public string Hash { get; set; }
		}

		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<SignFilesDialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<SignFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<SignFilesDialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<SignFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<SignFilesDialog>.GetPropValue<string>(this); } set { UIHelper<SignFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<string> Hashes { get { return UIHelper<SignFilesDialog>.GetPropValue<List<string>>(this); } set { UIHelper<SignFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Hash { get { return UIHelper<SignFilesDialog>.GetPropValue<string>(this); } set { UIHelper<SignFilesDialog>.SetPropValue(this, value); } }

		static SignFilesDialog()
		{
			UIHelper<SignFilesDialog>.Register();
			UIHelper<SignFilesDialog>.AddCallback(x => x.CryptorType, (obj, o, n) => obj.CryptorTypeUpdated());
		}

		SignFilesDialog()
		{
			InitializeComponent();
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).Where(type => type.SigningHashes().Any()).ToList();
			CryptorType = CryptorTypes.First();
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

			result = new Result { CryptorType = CryptorType, Key = Key, Hash = Hash };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new SignFilesDialog { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
