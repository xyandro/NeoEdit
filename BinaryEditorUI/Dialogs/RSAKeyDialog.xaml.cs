using System;
using System.Windows;

namespace NeoEdit.BinaryEditorUI.Dialogs
{
	public partial class RSAKeyDialog : Window
	{
		public string Key { get; private set; }

		readonly bool pub;
		RSAKeyDialog(bool _pub)
		{
			pub = _pub;
			InitializeComponent();
			keyLabel.Content = String.Format("{0} key (XML):", _pub ? "Public" : "Private");
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(key.Text))
				return;

			Key = key.Text;
			DialogResult = true;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(privateKey.Text))
				privateKey.Text = Crypto.CreateRSAPrivateKey(Int32.Parse(keySize.Text));
			publicKey.Text = Crypto.GetRSAPublicKey(privateKey.Text);
			key.Text = pub ? publicKey.Text : privateKey.Text;
		}

		public static string Run(bool pub)
		{
			var RSAKeyDialog = new RSAKeyDialog(pub);
			if (RSAKeyDialog.ShowDialog() == false)
				return null;
			return RSAKeyDialog.Key;
		}
	}
}
