using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.Program.Converters
{
	public class BoolToVisibleCollapsedConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value as bool?) == System.Convert.ToBoolean(parameter ?? true) ? Visibility.Visible : Visibility.Collapsed;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
