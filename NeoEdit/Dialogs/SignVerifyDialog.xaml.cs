using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Transform;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class SignVerifyDialog
	{
		public class Result
		{
			public string Key { get; set; }
			public string Hash { get; set; }
			public string Signature { get; set; }
		}

		[DepProp]
		public string Key { get { return UIHelper<SignVerifyDialog>.GetPropValue<string>(this); } set { UIHelper<SignVerifyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Hashes { get { return UIHelper<SignVerifyDialog>.GetPropValue<List<string>>(this); } set { UIHelper<SignVerifyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Hash { get { return UIHelper<SignVerifyDialog>.GetPropValue<string>(this); } set { UIHelper<SignVerifyDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Signature { get { return UIHelper<SignVerifyDialog>.GetPropValue<string>(this); } set { UIHelper<SignVerifyDialog>.SetPropValue(this, value); } }

		static SignVerifyDialog() { UIHelper<SignVerifyDialog>.Register(); }

		readonly Cryptor.Type type;
		readonly bool sign;
		SignVerifyDialog(Cryptor.Type type, bool sign)
		{
			this.type = type;
			this.sign = sign;

			InitializeComponent();

			Hashes = type.SigningHashes().ToList();
			Hash = Hashes.FirstOrDefault();

			if (sign)
				signatureLabel.Visibility = signature.Visibility = Visibility.Collapsed;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			var key = CryptorKeyDialog.Run(this, type, !sign);
			if (key != null)
				Key = key;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Key))
				return;

			result = new Result { Key = Key, Hash = Hash, Signature = Signature };
			DialogResult = true;
		}

		public static Result Run(Window owner, Cryptor.Type type, bool sign)
		{
			var dialog = new SignVerifyDialog(type, sign) { Owner = owner };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}
