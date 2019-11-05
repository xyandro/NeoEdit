using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NeoEdit.Program.Converters
{
	public class ActiveTabBackgroundConverter : IMultiValueConverter
	{
		static readonly Brush FocusedWindowBrush = new SolidColorBrush(Color.FromRgb(23, 81, 156));
		static readonly Brush ActiveWindowBrush = new SolidColorBrush(Color.FromRgb(14, 50, 96));
		static readonly Brush InactiveWindowBrush = Brushes.Transparent;

		static ActiveTabBackgroundConverter()
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
