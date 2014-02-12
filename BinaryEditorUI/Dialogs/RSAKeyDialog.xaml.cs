using System;
using System.Collections.Generic;
using System.Windows;

namespace NeoEdit.BinaryEditorUI.Dialogs
{
	public partial class RSAKeyDialog : Window
	{
		bool isPublic;
		public bool Public
		{
			get { return isPublic; }
			set
			{
				isPublic = value;
				keyLabel.Content = String.Format("{0} key (XML):", value ? "Public" : "Private");
			}
		}
		bool getHash;
		public bool GetHash
		{
			get { return getHash; }
			set
			{
				getHash = value;
				hashPanel.Visibility = getHash ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		bool getSignature;
		public bool GetSignature
		{
			get { return getSignature; }
			set
			{
				getSignature = value;
				signaturePanel.Visibility = getSignature ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		bool canGenerate;
		public bool CanGenerate
		{
			get { return canGenerate; }
			set
			{
				canGenerate = value;
				generateBox.Visibility = canGenerate ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public string Key { get; private set; }
		public string Hash { get; private set; }
		public string Signature { get; private set; }

		public RSAKeyDialog(bool _public)
		{
			InitializeComponent();

			Public = _public;
			GetHash = GetSignature = CanGenerate = false;

			IEnumerable<int> keySizes;
			int defaultSize;
			Crypto.GetRSAKeySizeInfo(out keySizes, out defaultSize);
			foreach (var size in keySizes)
			{
				if (size == defaultSize)
					keySize.SelectedIndex = keySize.Items.Count;
				keySize.Items.Add(size);
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(key.Text))
				return;
			if ((signaturePanel.IsVisible) && (String.IsNullOrEmpty(signature.Text)))
				return;

			Key = key.Text;
			Hash = hash.Text;
			Signature = signature.Text;

			DialogResult = true;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(privateKey.Text))
				privateKey.Text = Crypto.CreateRSAPrivateKey(Int32.Parse(keySize.Text));
			publicKey.Text = Crypto.GetRSAPublicKey(privateKey.Text);
			key.Text = isPublic ? publicKey.Text : privateKey.Text;
		}
	}
}
