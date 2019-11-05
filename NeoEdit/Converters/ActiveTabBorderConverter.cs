using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NeoEdit.Program.Converters
{
	public class ActiveTabBorderConverter : IMultiValueConverter
	{
		static readonly Brush FocusedWindowBrush = new SolidColorBrush(Color.FromRgb(31, 113, 216));
		static readonly Brush ActiveWindowBrush = new SolidColorBrush(Color.FromRgb(28, 101, 193));
		static readonly Brush InactiveWindowBrush = Brushes.Transparent;

		static ActiveTabBorderConverter()
		{
			FocusedWindowBrush.Freeze();
			ActiveWindowBrush.Freeze();
			InactiveWindowBrush.Freeze();
		}

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
