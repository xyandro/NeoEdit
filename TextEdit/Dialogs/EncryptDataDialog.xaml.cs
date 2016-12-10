﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EncryptDataDialog
	{
		public class Result
		{
			public Coder.CodePage InputCodePage { get; set; }
			public Cryptor.Type CryptorType { get; set; }
			public string Key { get; set; }
			public Coder.CodePage OutputCodePage { get; set; }
		}

		[DepProp]
		Dictionary<Coder.CodePage, string> CodePages { get { return UIHelper<EncryptDataDialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<EncryptDataDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<EncryptDataDialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<EncryptDataDialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<EncryptDataDialog>.GetPropValue<string>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<EncryptDataDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EncryptDataDialog>.SetPropValue(this, value); } }

		static EncryptDataDialog() { UIHelper<EncryptDataDialog>.Register(); }

		readonly bool encrypt;
		EncryptDataDialog(Coder.CodePage _codePage, bool encrypt)
		{
			this.encrypt = encrypt;
			InitializeComponent();

			Title = encrypt ? "Encrypt Data" : "Decrypt Data";

			CodePages = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).ToList();

			CryptorType = Cryptor.Type.AES;
			InputCodePage = encrypt ? _codePage : Coder.CodePage.Hex;
			OutputCodePage = encrypt ? Coder.CodePage.Hex : _codePage;
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = CryptorKeyDialog.Run(this, CryptorType, encrypt);
			if (key != null)
				Key = key;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Result { InputCodePage = InputCodePage, CryptorType = CryptorType, Key = Key, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage, bool encrypt)
		{
			var dialog = new EncryptDataDialog(codePage, encrypt) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
