﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	public partial class CryptorKeyDialog
	{
		[DepProp]
		public string PrivateKey { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PublicKey { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Password { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Salt { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<int> KeySizes { get { return UIHelper<CryptorKeyDialog>.GetPropValue<List<int>>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int KeySize { get { return UIHelper<CryptorKeyDialog>.GetPropValue<int>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }

		static CryptorKeyDialog() { UIHelper<CryptorKeyDialog>.Register(); }

		readonly Cryptor.Type type;
		readonly bool encrypt;
		CryptorKeyDialog(Cryptor.Type type, bool encrypt)
		{
			InitializeComponent();

			this.type = type;
			this.encrypt = encrypt;

			if (type.IsSymmetric())
			{
				asymmetricKeyGrid.Visibility = Visibility.Collapsed;
				symmetricPassword.Focus();
			}
			else
			{
				symmetricKeyGrid.Visibility = Visibility.Collapsed;
				asymmetricKeySize.Focus();
			}

			IEnumerable<int> keySizes;
			int defaultSize;
			Cryptor.GetKeySizeInfo(type, out keySizes, out defaultSize);
			KeySizes = keySizes.ToList();
			KeySize = defaultSize;

			Salt = "AWdSJ9hs72TXUUqaKpYIbU2v/YONdOxf";
		}

		void RandomizeSalt(object sender, RoutedEventArgs e)
		{
			if (new Message(this)
			{
				Title = "Please confirm",
				Text = "This value and the password together are required to generate the key.  Are you sure you want to change it?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			var bytes = new byte[24];
			new Random().NextBytes(bytes);
			Salt = Convert.ToBase64String(bytes);
		}

		string result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			GenerateKey(null, null);
			result = encrypt ? PublicKey : PrivateKey;
			if (string.IsNullOrEmpty(result))
				return;

			DialogResult = true;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			if (type.IsSymmetric())
			{
				if ((string.IsNullOrEmpty(Password)) || (string.IsNullOrEmpty(Salt)))
					return;
				PublicKey = PrivateKey = Cryptor.GetRfc2898Key(Password, Salt, KeySize);
			}
			else
			{
				if (string.IsNullOrEmpty(PrivateKey))
					PrivateKey = Cryptor.GenerateKey(type, KeySize);
				PublicKey = Cryptor.GetPublicKey(type, PrivateKey);
			}
		}

		public static string Run(Window owner, Cryptor.Type type, bool encrypt)
		{
			var dialog = new CryptorKeyDialog(type, encrypt) { Owner = owner };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}