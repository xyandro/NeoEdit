using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesSignDialog
	{
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<FilesSignDialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<FilesSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<FilesSignDialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<FilesSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<FilesSignDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<string> Hashes { get { return UIHelper<FilesSignDialog>.GetPropValue<List<string>>(this); } set { UIHelper<FilesSignDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Hash { get { return UIHelper<FilesSignDialog>.GetPropValue<string>(this); } set { UIHelper<FilesSignDialog>.SetPropValue(this, value); } }

		static FilesSignDialog()
		{
			UIHelper<FilesSignDialog>.Register();
			UIHelper<FilesSignDialog>.AddCallback(x => x.CryptorType, (obj, o, n) => obj.CryptorTypeUpdated());
		}

		FilesSignDialog()
		{
			InitializeComponent();
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).Where(type => type.SigningHashes().Any()).ToList();
			CryptorType = CryptorTypes.First();
		}

		void CryptorTypeUpdated()
		{
			Hashes = Cryptor.SigningHashes(CryptorType).ToList();
			Hash = Hashes.FirstOrDefault();
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = CryptorKeyDialog.Run(this, CryptorType, false);
			if (key != null)
				Key = key;
		}

		FilesSignDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new FilesSignDialogResult { CryptorType = CryptorType, Key = Key, Hash = Hash };
			DialogResult = true;
		}

		public static FilesSignDialogResult Run(Window parent)
		{
			var dialog = new FilesSignDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
