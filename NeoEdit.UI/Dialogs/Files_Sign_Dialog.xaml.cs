using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Sign_Dialog
	{
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<Files_Sign_Dialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<Files_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<Files_Sign_Dialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<Files_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<Files_Sign_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		List<string> Hashes { get { return UIHelper<Files_Sign_Dialog>.GetPropValue<List<string>>(this); } set { UIHelper<Files_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Hash { get { return UIHelper<Files_Sign_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Sign_Dialog>.SetPropValue(this, value); } }

		static Files_Sign_Dialog()
		{
			UIHelper<Files_Sign_Dialog>.Register();
			UIHelper<Files_Sign_Dialog>.AddCallback(x => x.CryptorType, (obj, o, n) => obj.CryptorTypeUpdated());
		}

		Files_Sign_Dialog()
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
			var key = File_Advanced_Encrypt_Dialog.Run(this, CryptorType, false)?.Key;
			if (key != null)
				Key = key;
		}

		Configuration_Files_Sign result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Configuration_Files_Sign { CryptorType = CryptorType, Key = Key, Hash = Hash };
			DialogResult = true;
		}

		public static Configuration_Files_Sign Run(Window parent)
		{
			var dialog = new Files_Sign_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
