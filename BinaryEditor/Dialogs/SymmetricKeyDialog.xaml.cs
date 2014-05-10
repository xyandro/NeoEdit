using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.BinaryEditor.Dialogs
{
	public partial class SymmetricKeyDialog : Window
	{
		Crypto.Type type;
		public Crypto.Type Type
		{
			get { return type; }
			set
			{
				type = value;
				keySize.Items.Clear();
				IEnumerable<int> keySizes;
				int defaultSize;
				Crypto.GetKeySizeInfo(type, out keySizes, out defaultSize);
				foreach (var size in keySizes)
				{
					if (size == defaultSize)
						keySize.SelectedIndex = keySize.Items.Count;
					keySize.Items.Add(size);
				}
			}
		}

		public string Key { get; private set; }

		public SymmetricKeyDialog()
		{
			InitializeComponent();

			Type = Crypto.Type.AES;
			salt.Text = "AWdSJ9hs72TXUUqaKpYIbU2v/YONdOxf";
		}

		void RandomizeSalt(object sender, RoutedEventArgs e)
		{
			if (new Message
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
			salt.Text = Convert.ToBase64String(bytes);
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

			key.Text = Crypto.GetRfc2898Key(password.Text, salt.Text, Int32.Parse(keySize.Text));
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			Generate();
		}
	}
}
