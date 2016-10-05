using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class OnDropDialog
	{
		[DepProp]
		public Dictionary<string, object> FormatDict { get { return UIHelper<OnDropDialog>.GetPropValue<Dictionary<string, object>>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public object Format { get { return UIHelper<OnDropDialog>.GetPropValue<object>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Visibility DataVisibility { get { return UIHelper<OnDropDialog>.GetPropValue<Visibility>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<OnDropDialog>.GetPropValue<string>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<OnDropDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Result { get { return UIHelper<OnDropDialog>.GetPropValue<List<string>>(this); } set { UIHelper<OnDropDialog>.SetPropValue(this, value); } }

		static OnDropDialog()
		{
			UIHelper<OnDropDialog>.Register();
			UIHelper<OnDropDialog>.AddCallback(x => x.Format, (obj, o, n) => obj.SetData());
			UIHelper<OnDropDialog>.AddCallback(x => x.CodePage, (obj, o, n) => obj.SetData());
		}

		readonly IDataObject dataObj;
		OnDropDialog(IDataObject dataObj)
		{
			this.dataObj = dataObj;

			InitializeComponent();

			FormatDict = dataObj.GetFormats().OrderBy().TrySelect(format => new { format = format, value = dataObj.GetData(format) }, null).Where(obj => obj?.value != null).ToDictionary(obj => obj.format, obj => obj.value);
			var first = new List<string> { "FileDrop", "FileContents", "UnicodeText" }.Where(format => FormatDict.ContainsKey(format)).DefaultIfEmpty(FormatDict.Keys.First()).First();
			Format = FormatDict[first];

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			CodePage = Coder.CodePage.AutoUnicode;
		}

		void SetData()
		{
			DataVisibility = Visibility.Collapsed;
			Data = null;
			if (Format == null)
				Result = new List<string>();
			else if (Format is IEnumerable<string>)
				Result = (Format as IEnumerable<string>).ToList();
			else if (Format is MemoryStream)
			{
				DataVisibility = Visibility.Visible;
				var bytes = (Format as MemoryStream).ToArray();
				Data = new string(bytes.Select(b => (char)b).Select(c => (char.IsControl(c)) || (c == 0xad) ? '·' : c).ToArray());
				var list = new List<string>();
				var str = Coder.TryBytesToString(bytes, CodePage);
				if (str != null)
					list.Add(str);
				Result = list;
			}
			else
				Result = new List<string> { Format.ToString() };
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static List<string> Run(Window parent, IDataObject dataObj)
		{
			var dialog = new OnDropDialog(dataObj) { Owner = parent };
			return dialog.ShowDialog() ? dialog.Result : null;
		}
	}

	class OnDropDialogResultConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is List<string>)
				return string.Join("\n", value as List<string>);
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
