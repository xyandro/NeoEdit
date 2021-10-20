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
	partial class Files_EncryptDecrypt_Dialog
	{
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<Files_EncryptDecrypt_Dialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<Files_EncryptDecrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<Files_EncryptDecrypt_Dialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<Files_EncryptDecrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<Files_EncryptDecrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_EncryptDecrypt_Dialog>.SetPropValue(this, value); } }

		static Files_EncryptDecrypt_Dialog() { UIHelper<Files_EncryptDecrypt_Dialog>.Register(); }

		readonly bool encrypt;
		Files_EncryptDecrypt_Dialog(bool encrypt)
		{
			this.encrypt = encrypt;
			InitializeComponent();

			Title = encrypt ? "Encrypt Files" : "Decrypt Files";

			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).ToList();
			CryptorType = Cryptor.Type.AES;
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			try
			{
				var key = File_Advanced_Encrypt_Dialog.Run(this, CryptorType, encrypt)?.Key;
				if (key != null)
					Key = key;
			}
			catch (OperationCanceledException) { }
		}

		Configuration_Files_EncryptDecrypt result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Configuration_Files_EncryptDecrypt { CryptorType = CryptorType, Key = Key };
			DialogResult = true;
		}

		public static Configuration_Files_EncryptDecrypt Run(Window parent, bool encrypt)
		{
			var dialog = new Files_EncryptDecrypt_Dialog(encrypt) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
