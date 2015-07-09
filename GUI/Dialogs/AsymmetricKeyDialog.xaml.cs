using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Dialogs
{
	public partial class AsymmetricKeyDialog
	{
		public class Result
		{
			public string Key { get; set; }
			public string Hash { get; set; }
			public string Signature { get; set; }
		}

		readonly bool IsPublic;
		readonly Crypto.Type Type;
		AsymmetricKeyDialog(Crypto.Type type, bool isPublic, bool canGenerate, bool getHash, bool getSignature)
		{
			InitializeComponent();

			Type = type;
			IsPublic = isPublic;

			IEnumerable<int> keySizes;
			int defaultSize;
			Crypto.GetKeySizeInfo(type, out keySizes, out defaultSize);
			keySize.Items.Clear();
			foreach (var size in keySizes)
			{
				if (size == defaultSize)
					keySize.SelectedIndex = keySize.Items.Count;
				keySize.Items.Add(size);
			}

			hash.Items.Clear();
			var hashes = type.SigningHashes();
			foreach (var item in hashes)
				hash.Items.Add(item);
			hash.SelectedIndex = 0;

			keyLabel.Content = String.Format("{0} key (XML):", IsPublic ? "Public" : "Private");
			generateBox.Visibility = canGenerate ? Visibility.Visible : Visibility.Collapsed;
			hashPanel.Visibility = getHash ? Visibility.Visible : Visibility.Collapsed;
			signaturePanel.Visibility = getSignature ? Visibility.Visible : Visibility.Collapsed;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(key.Text))
				return;
			if ((signaturePanel.IsVisible) && (String.IsNullOrEmpty(signature.Text)))
				return;

			result = new Result { Key = key.Text, Hash = hash.Text, Signature = signature.Text };
			DialogResult = true;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(privateKey.Text))
				privateKey.Text = Crypto.GenerateKey(Type, Int32.Parse(keySize.Text));
			publicKey.Text = Crypto.GetPublicKey(Type, privateKey.Text);
			key.Text = IsPublic ? publicKey.Text : privateKey.Text;
		}

		public static Result Run(Window owner, Crypto.Type type, bool isPublic = true, bool canGenerate = false, bool getHash = false, bool getSignature = false)
		{
			var dialog = new AsymmetricKeyDialog(type, isPublic, getHash, canGenerate, getSignature) { Owner = owner };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}
