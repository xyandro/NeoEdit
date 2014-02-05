using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NeoEdit.UI.BinaryEditorUI
{
	class ExpressionConverter : IMultiValueConverter
	{
		static readonly string num = @"\[(\d+)\]";
		static readonly Regex numRE = new Regex(num);

		static readonly string term = "'((?:[^']|'')*)'";
		static readonly Regex termOnlyRE = new Regex("^" + term + "$");

		static readonly List<List<string>> binaryOperator = new List<List<string>>
		{
			new List<string>{ "AND" },
			new List<string>{ "OR" },
			new List<string>{ "==", "=i=" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperator.Select(a => new Regex(String.Format(@"{0}\s*({1})\s*{0}", term, String.Join("|", a)))).ToList();

		static readonly string ternaryOperator = String.Format(@"{0}\s*\?\s*{0}\s*:\s*{0}", term);
		static readonly Regex ternaryOperatorRE = new Regex(ternaryOperator);

		void ParseParameters(ref string expression, object[] value)
		{
			while (true)
			{
				var match = numRE.Match(expression);
				if (!match.Success)
					break;

				var val = value[Int32.Parse(match.Groups[1].Value)];
				var result = val == null ? "NULL" : val.ToString();
				expression = expression.Substring(0, match.Index) + "'" + result.Replace("'", "''") + "'" + expression.Substring(match.Index + match.Length);
			}
		}

		string ParseParens(ref string expression)
		{
			bool inStr = false;
			int expressionStart = -1;
			int numParens = 0;
			for (var ctr = 0; ctr < expression.Length; ctr++)
			{
				switch (expression[ctr])
				{
					case '\'': inStr = !inStr; break;
					case '(':
						if (!inStr)
						{
							if (expressionStart == -1)
								expressionStart = ctr;
							++numParens;
						}
						break;
					case ')':
						if (!inStr)
						{
							if (numParens <= 0)
								throw new Exception("Invalid expression");
							if (--numParens == 0)
							{
								var subQuery = expression.Substring(expressionStart + 1, ctr - expressionStart - 1);
								expression = expression.Substring(0, expressionStart) + "#VALUE#" + expression.Substring(ctr + 1);
								return subQuery;
							}
						}
						break;
				}
			}
			return null;
		}

		public bool DoBinaryOperation(ref string expression)
		{
			foreach (var binaryOperatorRE in binaryOperatorREs)
			{
				var match = binaryOperatorRE.Match(expression);
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
						case "=i=": result = (term1.Equals(term2, StringComparison.OrdinalIgnoreCase)).ToString(); break;
						default: throw new Exception("Invalid op");
					}

					expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
					return true;
				}
			}

			return false;
		}

		public bool DoTernaryOperation(ref string expression)
		{
			var match = ternaryOperatorRE.Match(expression);
			if (!match.Success)
				return false;

			var test = match.Groups[1].Value;
			var result = "";
			if (Boolean.Parse(test))
				result = match.Groups[2].Value;
			else
				result = match.Groups[3].Value;

			expression = expression.Substring(0, match.Index) + "'" + result + "'" + expression.Substring(match.Index + match.Length);
			return true;
		}

		static readonly Regex boolRE = new Regex("^True|False$");
		static readonly Regex intRE = new Regex(@"^\d+$");

		object GetReturnValue(string expression, Type targetType)
		{
			if (boolRE.IsMatch(expression))
			{
				var val = Boolean.Parse(expression);
				if (targetType == typeof(Visibility))
					return val ? Visibility.Visible : Visibility.Collapsed;
				return val;
			}

			if (intRE.IsMatch(expression))
			{
				var val = Int32.Parse(expression);
				if (targetType == typeof(Thickness))
					return new Thickness(val);
				return val;
			}

			if (targetType == typeof(Brush))
				return typeof(Brushes).GetProperty(expression).GetValue(null);

			return expression;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var expression = parameter as string;

			ParseParameters(ref expression, value);

			var expressionStack = new Stack<string>();

			while (true)
			{
				var subExpression = ParseParens(ref expression);
				if (subExpression != null)
				{
					expressionStack.Push(expression);
					expression = subExpression;
					continue;
				}

				if (DoBinaryOperation(ref expression))
					continue;

				if (DoTernaryOperation(ref expression))
					continue;

				var match = termOnlyRE.Match(expression);
				if (!match.Success)
					throw new Exception("Unable to calculate");

				// If we're inside ()
				if (expressionStack.Count != 0)
				{
					expression = expressionStack.Pop().Replace("#VALUE#", expression);
					continue;
				}

				expression = match.Groups[1].Value.Replace("''", "'");

				return GetReturnValue(expression, targetType);
			}
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
