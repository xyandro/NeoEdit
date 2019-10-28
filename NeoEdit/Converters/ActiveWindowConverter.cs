using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace NeoEdit.Program.Converters
{
	public class ActiveWindowConverter : MarkupExtension, IMultiValueConverter
	{
		static readonly Brush FocusedWindowBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush ActiveWindowBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
		static readonly Brush InactiveWindowBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));

		static ActiveWindowConverter()
		{
			FocusedWindowBrush.Freeze();
			ActiveWindowBrush.Freeze();
			InactiveWindowBrush.Freeze();
		}

		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var textEditor = values[0] as TextEditor;
			var focused = values[1] as TextEditor;
			var activeWindows = values[2] as HashSet<TextEditor>;
			if (textEditor == focused)
				return FocusedWindowBrush;
			if (activeWindows.Contains(textEditor))
				return ActiveWindowBrush;
			return InactiveWindowBrush;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
