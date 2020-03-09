using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValue
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<ViewValue>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }
		[DepProp]
		public IList<byte> Data { get { return UIHelper<ViewValue>.GetPropValue<IList<byte>>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasSel { get { return UIHelper<ViewValue>.GetPropValue<bool>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }

		static ViewValue() => UIHelper<ViewValue>.Register();

		public ViewValue() => InitializeComponent();

		public string GetValue()
		{
			if (Data == null)
				return null;

			var data = Data;
			if (!Coder.IsStr(CodePage))
			{
				var size = Coder.BytesRequired(CodePage);
				if ((!HasSel) && (data.Count > size))
				{
					var newData = new byte[size];
					Array.Copy(data as byte[], newData, size);
					data = newData;
				}
				if (data.Count != size)
					return null;
			}

			return Coder.TryBytesToString(data as byte[], CodePage);
		}

		void OnClick(object sender, MouseButtonEventArgs e)
		{
			if ((Coder.IsStr(CodePage)) && (!HasSel))
				return;

			var value = GetValue();
			if (value == null)
				return;

			byte[] newBytes;
			while (true)
			{
				value = ViewValuesEditValueDialog.Run(UIHelper.FindParent<Window>(this), value);
				if (value == null)
					return;

				newBytes = Coder.TryStringToBytes(value, CodePage);
				if (newBytes != null)
					break;
			}

			int? size = null;
			if (!Coder.IsStr(CodePage))
				size = Coder.BytesRequired(CodePage);

			UIHelper.FindParent<TextEditor>(this).UpdateViewValue(newBytes, size);
		}
	}

	class ViewValueConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Font.RemoveSpecialChars((values[0] as ViewValue)?.GetValue() ?? "");

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
