using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.Transform;

namespace NeoEdit.Controls
{
	partial class ViewValue
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<ViewValue>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }
		[DepProp]
		public IList<byte> Data { get { return UIHelper<ViewValue>.GetPropValue<IList<byte>>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasSel { get { return UIHelper<ViewValue>.GetPropValue<bool>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }
		[DepProp]
		public string FindValue { get { return UIHelper<ViewValue>.GetPropValue<string>(this); } set { UIHelper<ViewValue>.SetPropValue(this, value); } }

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
	}

	class ViewValueConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var viewValue = values[0] as ViewValue;
			if (viewValue == null)
				return null;

			var value = viewValue.GetValue();
			if (targetType == typeof(string))
				return Font.RemoveSpecialChars(value);

			if (targetType == typeof(Brush))
				if ((value != null) && (viewValue.FindValue != null) && (value.Equals(viewValue.FindValue, StringComparison.OrdinalIgnoreCase)))
					return Brushes.LightBlue;
				else
					return Brushes.Transparent;

			throw new Exception("Invalid converter");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
