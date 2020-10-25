using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Data_Encrypt_Dialog
	{
		[DepProp]
		Dictionary<Coder.CodePage, string> CodePages { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<Configure_Edit_Data_Encrypt_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Data_Encrypt_Dialog() { UIHelper<Configure_Edit_Data_Encrypt_Dialog>.Register(); }

		readonly bool encrypt;
		Configure_Edit_Data_Encrypt_Dialog(Coder.CodePage _codePage, bool encrypt)
		{
			this.encrypt = encrypt;
			InitializeComponent();

			Title = encrypt ? "Encrypt Data" : "Decrypt Data";

			CodePages = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			CryptorTypes = Enum.GetValues(typeof(Cryptor.Type)).Cast<Cryptor.Type>().Where(type => type != Cryptor.Type.None).ToList();

			CryptorType = Cryptor.Type.AES;
			InputCodePage = encrypt ? _codePage : Coder.CodePage.Hex;
			OutputCodePage = encrypt ? Coder.CodePage.Hex : _codePage;
		}

		void OnGenerate(object sender, RoutedEventArgs e)
		{
			var key = Configure_File_Encrypt_Dialog.Run(this, CryptorType, encrypt)?.Key;
			if (key != null)
				Key = key;
		}

		Configuration_Edit_Data_Encrypt result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Configuration_Edit_Data_Encrypt { InputCodePage = InputCodePage, CryptorType = CryptorType, Key = Key, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Configuration_Edit_Data_Encrypt Run(Window parent, Coder.CodePage codePage, bool encrypt)
		{
			var dialog = new Configure_Edit_Data_Encrypt_Dialog(codePage, encrypt) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}

	class CryptorFormatDescriptionConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Cryptor.GetFormatDescription((Cryptor.Type)value);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
