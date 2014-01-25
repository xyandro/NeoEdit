using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class RecordsToActionMenuItems : MarkupExtension, IMultiValueConverter
	{
		static RecordsToActionMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new RecordsToActionMenuItems();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var objs = value[0] as IEnumerable<object>;
			if (objs == null)
				return null;
			var records = objs.Cast<Record>().ToList();
			var actions = records.SelectMany(a => a.Actions).GroupBy(a => a).ToDictionary(a => a.Key, a => a.Count());
			return actions.Where(a => RecordAction.Get(a.Key).ValidNumArgs(a.Value)).Select(a => a.Key).ToList();
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
