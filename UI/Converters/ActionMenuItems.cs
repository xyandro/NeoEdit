﻿using System;
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
			if (!(value[1] is IEnumerable<Object>))
				return null;

			var parent = value[0] as Record;
			var children = (value[1] as IEnumerable<Object>).Cast<Record>().ToList();
			var clipboardCount = (int)value[2];
			var browser = value[3] as Browser;

			var actions = RecordAction.Actions(parent, children, clipboardCount);
			var ret = actions.Select(a => RecordAction.Get(a)).Select(a => new MenuItem { Header = a.MenuHeader, InputGestureText = a.GetInputGestureText() }).ToList();
			ret.ForEach(a => a.Click += browser.MenuItemActionClick);
			return ret;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
