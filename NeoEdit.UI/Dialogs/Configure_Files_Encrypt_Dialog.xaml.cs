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
	partial class Configure_Files_Encrypt_Dialog
	{
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<Configure_Files_Encrypt_Dialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<Configure_Files_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<Configure_Files_Encrypt_Dialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<Configure_Files_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<Configure_Files_Encrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Encrypt_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Encrypt_Dialog() { UIHelper<Configure_Files_Encrypt_Dialog>.Register(); }

		readonly bool encrypt;
		Configure_Files_Encrypt_Dialog(bool encrypt)
		{
			this.encrypt = encrypt;
			InitializeComponent();

			Title = encrypt ? "Encrypt Files" : "Decrypt Files";

			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).ToList();
			CryptorType = Cryptor.Type.AES;
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = Configure_File_Encrypt_Dialog.Run(this, CryptorType, encrypt)?.Key;
			if (key != null)
				Key = key;
		}

		Configuration_Files_Encrypt result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Configuration_Files_Encrypt { CryptorType = CryptorType, Key = Key };
			DialogResult = true;
		}

		public static Configuration_Files_Encrypt Run(Window parent, bool encrypt)
		{
			var dialog = new Configure_Files_Encrypt_Dialog(encrypt) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
