using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common.Expressions;

namespace NeoEdit.GUI.Converters
{
	public class ExpressionResultListConverter : MarkupExtension, IMultiValueConverter
	{
		static ExpressionResultListConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ExpressionResultListConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.Length < 2)
				throw new ArgumentNullException();

			var expression = value[0] as string;
			var expressionData = value[1] as Dictionary<string, List<object>>;
			if ((expression == null) || (expressionData == null))
				return DependencyProperty.UnsetValue;

			try { return String.Join(", ", new NEExpression(expression).Evaluate(expressionData)); }
			catch { return DependencyProperty.UnsetValue; }
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
