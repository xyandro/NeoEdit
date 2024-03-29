﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class File_Advanced_Encrypt_Dialog
	{
		[DepProp]
		public bool Encrypt { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<bool>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PrivateKey { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PublicKey { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Salt { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<int> KeySizes { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<List<int>>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int KeySize { get { return UIHelper<File_Advanced_Encrypt_Dialog>.GetPropValue<int>(this); } set { UIHelper<File_Advanced_Encrypt_Dialog>.SetPropValue(this, value); } }

		static File_Advanced_Encrypt_Dialog() { UIHelper<File_Advanced_Encrypt_Dialog>.Register(); }

		readonly Cryptor.Type type;
		File_Advanced_Encrypt_Dialog(Cryptor.Type type, bool encrypt)
		{
			InitializeComponent();

			this.type = type;
			Encrypt = encrypt;

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
			if (!new Message(this)
			{
				Title = "Please confirm",
				Text = "This value and the password together are required to generate the key. Are you sure you want to change it?",
				Options = MessageOptions.YesNo,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.No,
			}.Show().HasFlag(MessageOptions.Yes))
				return;

			var bytes = new byte[24];
			new Random().NextBytes(bytes);
			Salt = Convert.ToBase64String(bytes);
		}

		Configuration_File_Advanced_Encrypt result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			GenerateKey(null, null);
			result = new Configuration_File_Advanced_Encrypt();
			result.Key = Encrypt ? PublicKey : PrivateKey;
			if (string.IsNullOrEmpty(result.Key))
				return;

			DialogResult = true;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			if (type.IsSymmetric())
			{
				if ((string.IsNullOrEmpty(symmetricPassword.Password)) || (string.IsNullOrEmpty(Salt)))
					return;
				if ((Encrypt) && (symmetricPassword.Password != symmetricConfirm.Password))
					throw new Exception("Passwords must match.");
				PublicKey = PrivateKey = Cryptor.GetRfc2898Key(symmetricPassword.Password, Salt, KeySize);
			}
			else
			{
				if (string.IsNullOrEmpty(PrivateKey))
					PrivateKey = Cryptor.GenerateKey(type, KeySize);
				PublicKey = Cryptor.GetPublicKey(type, PrivateKey);
			}
		}

		public static Configuration_File_Advanced_Encrypt Run(Window parent, Cryptor.Type type, bool encrypt)
		{
			var dialog = new File_Advanced_Encrypt_Dialog(type, encrypt) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
