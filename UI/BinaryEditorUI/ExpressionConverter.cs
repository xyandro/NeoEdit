using System;
using System.Windows;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace NeoEdit.UI.BinaryEditorUI
{
	class ExpressionConverter : IMultiValueConverter
	{
		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var expression = parameter as string;

			var numRE = new Regex(@"\[(\d+)\]");
			while (true)
			{
				var match = numRE.Match(expression);
				if (!match.Success)
					break;

				var val = value[Int32.Parse(match.Groups[1].Value)];
				var result = val == null ? "NULL" : val.ToString();
				expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
			}

			var term = "'([^']*)'";
			var termRE = new Regex("^" + term + "$");

			var binaryOperation = String.Format(@"{0}\s*(AND|OR|==)\s*{0}", term);
			var binaryOperationRE = new Regex(binaryOperation);

			var trinaryOperation = String.Format(@"{0}\s*\?\s*{0}\s*:\s*{0}", term);
			var trinaryOperationRE = new Regex(trinaryOperation);

			var parens = String.Format(@"\(\s*{0}\s*\)", term);
			var parensRE = new Regex(parens);
			while (true)
			{
				Match match;

				match = parensRE.Match(expression);
				if (match.Success)
				{
					var result = match.Groups[1].Value;
					expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
					continue;
				}

				match = binaryOperationRE.Match(expression);
				if (match.Success)
				{
					var term1 = match.Groups[1].Value;
					var op = match.Groups[2].Value;
					var term2 = match.Groups[3].Value;
					var result = "";

					switch (op)
					{
						case "AND": result = (Boolean.Parse(term1) && Boolean.Parse(term2)).ToString(); break;
						case "OR": result = (Boolean.Parse(term1) || Boolean.Parse(term2)).ToString(); break;
						case "==": result = (term1 == term2).ToString(); break;
						default: throw new Exception("Invalid op");
					}

					expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
					continue;
				}

				match = trinaryOperationRE.Match(expression);
				if (match.Success)
				{
					var test = match.Groups[1].Value;
					var result = "";
					if (Boolean.Parse(test))
						result = match.Groups[2].Value;
					else
						result = match.Groups[3].Value;

					expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
					continue;
				}

				match = termRE.Match(expression);
				if (!match.Success)
					throw new Exception("Unable to calculate");
				expression = match.Groups[1].Value;

				if (new Regex("^True|False$").IsMatch(expression))
				{
					var val = Boolean.Parse(expression);
					if (targetType == typeof(Visibility))
						return val ? Visibility.Visible : Visibility.Collapsed;
					return val;
				}

				if (new Regex(@"^\d+$").IsMatch(expression))
				{
					var val = Int32.Parse(expression);
					if (targetType == typeof(Thickness))
						return new Thickness(val);
					return val;
				}

				return expression;
			}
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
