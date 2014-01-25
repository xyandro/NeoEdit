using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class ActionToHeader : MarkupExtension, IValueConverter
	{
		static ActionToHeader converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ActionToHeader();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return RecordAction.Get((RecordAction.ActionName)value).MenuHeader;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
