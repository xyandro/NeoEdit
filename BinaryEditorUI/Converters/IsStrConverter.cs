﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.GUI.Data;

namespace NeoEdit.GUI.BinaryEditorUI.Converters
{
	class IsStrConverter : MarkupExtension, IValueConverter
	{
		static IsStrConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new IsStrConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Coder.Type)value).IsStr();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
