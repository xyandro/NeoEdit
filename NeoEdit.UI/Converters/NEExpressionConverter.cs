﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.Common.Expressions;

namespace NeoEdit.UI.Converters
{
	public class NEExpressionConverter : MarkupExtension, IMultiValueConverter, IValueConverter
	{
		static NEExpressionConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new NEExpressionConverter();
			return converter;
		}

		static Dictionary<string, NEExpression> expressionUsed = new Dictionary<string, NEExpression>();
		NEExpression GetExpression(string expression)
		{
			if (expression == null)
				expression = "";
			if (!expressionUsed.ContainsKey(expression))
				expressionUsed[expression] = new NEExpression(expression);
			return expressionUsed[expression];
		}

		delegate bool TryParseHandler<T>(string value, out T result);
		void TryStringConversion<T>(ref object obj, TryParseHandler<T> tryParse)
		{
			if (!(obj is string))
				return;

			T value;
			if (!tryParse(obj as string, out value))
				return;

			obj = value;
		}

		object GetResult(object result, Type targetType)
		{
			if (result == null)
				return null;

			if (targetType == typeof(object))
				return result;
			if (targetType == typeof(string))
				return result.ToString();
			if (targetType == typeof(bool))
			{
				TryStringConversion<bool>(ref result, bool.TryParse);
				return result as bool?;
			}
			if (targetType == typeof(Visibility))
			{
				TryStringConversion<bool>(ref result, bool.TryParse);
				if ((result is bool val))
					return val ? Visibility.Visible : Visibility.Collapsed;

				if (result is string s)
					if (Enum.TryParse<Visibility>(s, out var visibility))
						return visibility;

				return null;
			}
			if ((targetType == typeof(double)) || (targetType == typeof(Thickness)))
			{
				TryStringConversion<double>(ref result, double.TryParse);
				try
				{
					var val = System.Convert.ToDouble(result);
					if (targetType == typeof(Thickness))
						return new Thickness(val);
					return val;
				}
				catch { return null; }
			}
			if (targetType == typeof(Brush))
			{
				if (result is string s)
					result = new BrushConverter().ConvertFrom(s);
				if (!(result is Brush))
					return null;
				return result;
			}
			if (targetType == typeof(GridLength))
			{
				if ((result is string) && ((result as string).Equals("Auto", StringComparison.OrdinalIgnoreCase)))
					return GridLength.Auto;

				TryStringConversion<bool>(ref result, bool.TryParse);
				if (result is bool)
					return (bool)result ? GridLength.Auto : new GridLength(0);

				TryStringConversion<double>(ref result, double.TryParse);
				try
				{
					var val = System.Convert.ToDouble(result);
					return new GridLength(val);
				}
				catch { return null; }
			}
			if (targetType.FullName == "System.Collections.IEnumerable")
			{
				if (result is Type)
				{
					var type = result as Type;
					if (typeof(Enum).IsAssignableFrom(type))
						return Enum.GetValues(type);
				}
			}
			if (targetType == typeof(long))
			{
				TryStringConversion<long>(ref result, long.TryParse);
				if ((result is string) && ((result as string).StartsWith("0x")))
				{
					long value;
					if (long.TryParse((result as string).Substring(2), NumberStyles.HexNumber, null, out value))
						result = value;
				}
				try { return System.Convert.ToInt64(result); }
				catch { return null; }
			}

			return result;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var values = value.Select(val => val == DependencyProperty.UnsetValue ? null : val).ToArray();
			var result = GetExpression(parameter as string).EvaluateOne<object>(NEVariables.FromConstants(values));
			return GetResult(result, targetType);
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == DependencyProperty.UnsetValue)
				value = null;
			var result = GetExpression(parameter as string).EvaluateOne<object>(NEVariables.FromConstants(value));
			return GetResult(result, targetType);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
