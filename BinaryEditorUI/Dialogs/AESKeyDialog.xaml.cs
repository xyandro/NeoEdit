using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace NeoEdit.BinaryEditorUI.Dialogs
{
	public partial class AESKeyDialog : Window
	{
		public string Key { get; private set; }

		AESKeyDialog()
		{
			InitializeComponent();
			salt.Text = "j8g+rM6ouNIvQptlGiDnXo9KvEI89WLXre+FbaRHzp0=";
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Generate();
			if (String.IsNullOrEmpty(key.Text))
				return;

			Key = key.Text;
			DialogResult = true;
		}

		void Generate()
		{
			if ((String.IsNullOrEmpty(password.Text)) || (String.IsNullOrEmpty(salt.Text)))
				return;

			using (var byteGenerator = new Rfc2898DeriveBytes(password.Text, Encoding.ASCII.GetBytes(salt.Text)))
			using (var aesAlg = new RijndaelManaged { KeySize = Int32.Parse(keySize.Text) })
				key.Text = Convert.ToBase64String(byteGenerator.GetBytes(aesAlg.KeySize / 8));
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			Generate();
		}

		public static string Run()
		{
			var aesKeyDialog = new AESKeyDialog();
			if (aesKeyDialog.ShowDialog() == false)
				return null;
			return aesKeyDialog.Key;
		}
	}
}
