using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Data_Sign_Dialog
	{
		[DepProp]
		Dictionary<Coder.CodePage, string> CodePages { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		List<string> Hashes { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<List<string>>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Hash { get { return UIHelper<Configure_Edit_Data_Sign_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Data_Sign_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Data_Sign_Dialog()
		{
			UIHelper<Configure_Edit_Data_Sign_Dialog>.Register();
			UIHelper<Configure_Edit_Data_Sign_Dialog>.AddCallback(x => x.CryptorType, (obj, o, n) => obj.CryptorTypeUpdated());
		}

		Configure_Edit_Data_Sign_Dialog(Coder.CodePage codePage)
		{
			InitializeComponent();

			CodePages = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).Where(type => type.SigningHashes().Any()).ToList();

			CryptorType = CryptorTypes.First();
			CodePage = codePage;
		}

		void CryptorTypeUpdated()
		{
			Hashes = Cryptor.SigningHashes(CryptorType).ToList();
			Hash = Hashes.FirstOrDefault();
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = Configure_File_Encrypt_Dialog.Run(this, CryptorType, false)?.Key;
			if (key != null)
				Key = key;
		}

		Configuration_Edit_Data_Sign result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Configuration_Edit_Data_Sign { CodePage = CodePage, CryptorType = CryptorType, Key = Key, Hash = Hash };
			DialogResult = true;
		}

		public static Configuration_Edit_Data_Sign Run(Window parent, Coder.CodePage codePage)
		{
			var dialog = new Configure_Edit_Data_Sign_Dialog(codePage) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
