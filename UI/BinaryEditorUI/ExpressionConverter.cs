using System;
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

				var result = value[Int32.Parse(match.Groups[1].Value)].ToString();
				expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
			}

			var boolTerm = "True|False";
			var boolTermRE = new Regex(boolTerm);

			var term = "(" + boolTerm + ")";
			var termRE = new Regex("^" + term + "$");

			var operation = String.Format(@"{0}\s+(AND|OR)\s+{0}", term);
			var operationRE = new Regex(operation);

			var parens = String.Format(@"\(\s*{0}\s*\)", term);
			var parensRE = new Regex(parens);
			while (true)
			{
				Match match;
				
				match = operationRE.Match(expression);
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
						default: throw new Exception("Invalid op");
					}

					expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
					continue;
				}

				match = parensRE.Match(expression);
				if (match.Success)
				{
					var result = match.Groups[1].Value;
					expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
					continue;
				}

				if (!termRE.IsMatch(expression))
					throw new Exception("Unable to calculate");

				if (boolTermRE.IsMatch(expression))
					return Boolean.Parse(expression);

				return expression;
			}
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
