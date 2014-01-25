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
			var records = value[0] as IEnumerable<Record>;
			if (records == null)
				return null;

			var actions = records.SelectMany(a => a.Actions).Distinct().ToList();
			actions = actions.Where(a => RecordAction.Get(a).ValidNumArgs((int)value[1])).ToList();
			return actions;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
