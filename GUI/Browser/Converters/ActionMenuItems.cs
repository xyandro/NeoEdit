using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.GUI.Browser.Converters
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
			if (value[0] == null)
				return null;
			if (!(value[1] is IEnumerable<Object>))
				return null;

			var parent = value[0] as Record;
			var children = (value[1] as IEnumerable<Object>).Cast<GUIRecord>().Select(record => record.record).ToList();
			var browser = value[2] as BrowserWindow;

			var actions = RecordAction.Actions(parent, children);
			var ret = actions.Select(a => RecordAction.Get(a)).Select(a => new MenuItem { Header = a.MenuHeader, InputGestureText = a.GetInputGestureText() }).ToList();
			ret.ForEach(a => a.Click += browser.MenuItemActionClick);
			return ret;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
