using System.Windows;
using NeoEdit.Transform;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	public partial class EncryptDialog
	{
		public class Result
		{
			public string Key { get; set; }
		}

		[DepProp]
		public string Key { get { return UIHelper<EncryptDialog>.GetPropValue<string>(this); } set { UIHelper<EncryptDialog>.SetPropValue(this, value); } }

		static EncryptDialog() { UIHelper<EncryptDialog>.Register(); }

		readonly Cryptor.Type type;
		readonly bool encrypt;
		EncryptDialog(Cryptor.Type type, bool encrypt)
		{
			InitializeComponent();

			this.type = type;
			this.encrypt = encrypt;
		}

		void GenerateKey(object sender, RoutedEventArgs e)
		{
			var key = CryptorKeyDialog.Run(this, type, encrypt);
			if (key != null)
				Key = key;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Key))
				return;

			result = new Result { Key = Key };
			DialogResult = true;
		}

		public static Result Run(Window owner, Cryptor.Type type, bool encrypt)
		{
			var dialog = new EncryptDialog(type, encrypt) { Owner = owner };
			if (!dialog.ShowDialog())
				return null;
			return dialog.result;
		}
	}
}
