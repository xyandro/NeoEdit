using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Dialogs
{
	partial class CryptorKeyDialog
	{
		[DepProp]
		public bool Encrypt { get { return UIHelper<CryptorKeyDialog>.GetPropValue<bool>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PrivateKey { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string PublicKey { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Salt { get { return UIHelper<CryptorKeyDialog>.GetPropValue<string>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<int> KeySizes { get { return UIHelper<CryptorKeyDialog>.GetPropValue<List<int>>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int KeySize { get { return UIHelper<CryptorKeyDialog>.GetPropValue<int>(this); } set { UIHelper<CryptorKeyDialog>.SetPropValue(this, value); } }

		static CryptorKeyDialog() { UIHelper<CryptorKeyDialog>.Register(); }

		readonly Cryptor.Type type;
		CryptorKeyDialog(Cryptor.Type type, bool encrypt)
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
			if (new Message(this)
			{
				Title = "Please confirm",
				Text = "This value and the password together are required to generate the key. Are you sure you want to change it?",
				Options = MessageOptions.YesNo,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.No,
			}.Show() != MessageOptions.Yes)
				return;

			var bytes = new byte[24];
			new Random().NextBytes(bytes);
			Salt = Convert.ToBase64String(bytes);
		}

		string result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			GenerateKey(null, null);
			result = Encrypt ? PublicKey : PrivateKey;
			if (string.IsNullOrEmpty(result))
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
				{
					new Message(this)
					{
						Title = "Password mismatch",
						Text = "Passwords must match.",
						Options = MessageOptions.Ok,
					}.Show();
					return;
				}
				PublicKey = PrivateKey = Cryptor.GetRfc2898Key(symmetricPassword.Password, Salt, KeySize);
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
