using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.Dialogs
{
	partial class EditDataEncryptDialog
	{
		public class Result
		{
			public Coder.CodePage InputCodePage { get; set; }
			public Cryptor.Type CryptorType { get; set; }
			public string Key { get; set; }
			public Coder.CodePage OutputCodePage { get; set; }
		}

		[DepProp]
		Dictionary<Coder.CodePage, string> CodePages { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage InputCodePage { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }
		[DepProp]
		List<Cryptor.Type> CryptorTypes { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<List<Cryptor.Type>>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }
		[DepProp]
		Cryptor.Type CryptorType { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<Cryptor.Type>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Key { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<string>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }
		[DepProp]
		Coder.CodePage OutputCodePage { get { return UIHelper<EditDataEncryptDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditDataEncryptDialog>.SetPropValue(this, value); } }

		static EditDataEncryptDialog() { UIHelper<EditDataEncryptDialog>.Register(); }

		readonly bool encrypt;
		EditDataEncryptDialog(Coder.CodePage _codePage, bool encrypt)
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
			var key = CryptorKeyDialog.Run(this, CryptorType, encrypt);
			if (key != null)
				Key = key;
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Key == null)
				return;

			result = new Result { InputCodePage = InputCodePage, CryptorType = CryptorType, Key = Key, OutputCodePage = OutputCodePage };
			DialogResult = true;
		}

		public static Result Run(Window parent, Coder.CodePage codePage, bool encrypt)
		{
			var dialog = new EditDataEncryptDialog(codePage, encrypt) { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}

	class CryptorFormatDescriptionConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Cryptor.GetFormatDescription((Cryptor.Type)value);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
