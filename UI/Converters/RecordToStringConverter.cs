﻿using NeoEdit.Records;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.UI.Converters
{
	class RecordToStringConverter : MarkupExtension, IMultiValueConverter
	{
		static RecordToStringConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new RecordToStringConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var propertyValue = (value[0] as Record)[(Record.Property)value[1]];
			if (propertyValue == null)
				return null;

			if (propertyValue.GetType().IsIntegerType())
				return String.Format("{0:n0}", propertyValue);
			if (propertyValue is DateTime)
				return ((DateTime)propertyValue).ToLocalTime().ToString();
			return propertyValue.ToString();
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
