using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;
using NeoEdit.UI.Windows;

namespace NeoEdit.UI.Converters
{
	class ActionMenuItems : MarkupExtension, IMultiValueConverter
	{
		static ActionMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ActionMenuItems();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var objs = value[0] as IEnumerable<object>;
			var browser = value[1] as Browser;
			if (objs == null)
				return null;

			var records = objs.Cast<Record>().ToList();
			var actions = records.SelectMany(a => a.Actions).GroupBy(a => a).ToDictionary(a => a.Key, a => a.Count());
			var actionsList = actions.Where(a => RecordAction.Get(a.Key).ValidNumArgs(a.Value)).Select(a => a.Key).ToList();
			var ret = actionsList.Select(a => new MenuItem { Header = RecordAction.Get(a).MenuHeader }).ToList();
			ret.ForEach(a => a.Click += browser.MenuItemActionClick);
			return ret;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
