using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace NeoEdit.Common
{
	class ExpressionConverter : MarkupExtension, IMultiValueConverter, IValueConverter
	{
		static ExpressionConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ExpressionConverter();
			return converter;
		}

		static readonly string hexExp = @"#([0-9a-fA-F]{1,2})#";
		static readonly Regex hexExpRE = new Regex(hexExp);

		static readonly string term = @"('(?:[^']|'')*'|\[NULL\]|\[\d+\])";
		static readonly string termParts = @"'((?:[^']|'')*)'|\[(NULL)\]|\[(\d+)\]";
		static readonly Regex termPartsRE = new Regex("^" + termParts + "$");

		static readonly List<List<string>> binaryOperator = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "IS" },
			new List<string>{ "AND" },
			new List<string>{ "OR" },
			new List<string>{ "==", "=i=" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperator.Select(a => new Regex(String.Format(@"{0}\s*({1})\s*{0}", term, String.Join("|", a.Select(b => b.Replace(".", @"\.")))))).ToList();

		static readonly string ternaryOperator = String.Format(@"{0}\s*\?\s*{0}\s*:\s*{0}", term);
		static readonly Regex ternaryOperatorRE = new Regex(ternaryOperator);

		string GetExpression(string expression)
		{
			while (true)
			{
				var match = hexExpRE.Match(expression);
				if (!match.Success)
					break;

				expression = expression.Replace(match.Groups[0].Value, ((char)Byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber)).ToString());
			}

			return expression;
		}

		bool EnterParens(ref string expression, Stack<string> expressionStack)
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
								expressionStack.Push(expression);
								expression = subQuery;
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		bool ExitParens(ref string expression, Stack<string> expressionStack)
		{
			if (expressionStack.Count == 0)
				return false;

			expression = expressionStack.Pop().Replace("#VALUE#", expression);
			return true;
		}

		object GetTerm(string term, List<object> value)
		{
			var match = termPartsRE.Match(term);
			if (match.Success)
			{
				if (match.Groups[1].Success)
					return match.Groups[1].Value.Replace("''", "'");
				if (match.Groups[2].Success)
					return null;
				if (match.Groups[3].Success)
					return value[Int32.Parse(match.Groups[3].Value)];
			}

			throw new Exception("Invalid term");
		}

		bool DoBinaryOperation(ref string expression, List<object> value)
		{
			foreach (var binaryOperatorRE in binaryOperatorREs)
			{
				var match = binaryOperatorRE.Match(expression);
				if (match.Success)
				{
					var term1 = GetTerm(match.Groups[1].Value, value);
					var op = match.Groups[2].Value;
					var term2 = GetTerm(match.Groups[3].Value, value);
					var result = "";

					var term1Str = (term1 ?? "").ToString();
					var term2Str = (term2 ?? "").ToString();

					switch (op)
					{
						case ".":
							{
								result = "[NULL]";
								if (term1 == null)
									break;
								var field = term1.GetType().GetProperty(term2Str, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								if (field == null)
									break;
								term1 = field.GetValue(term1);
								if (term1 == null)
									break;

								result = "[" + value.Count + "]";
								value.Add(term1);
								break;
							}
						case "IS":
							result = "'" + ((term1 != null) && (term1.GetType().Name == term2Str)).ToString() + "'";
							break;
						case "AND": result = "'" + (Boolean.Parse(term1Str) && Boolean.Parse(term2Str)).ToString() + "'"; break;
						case "OR": result = "'" + (Boolean.Parse(term1Str) || Boolean.Parse(term2Str)).ToString() + "'"; break;
						case "==": result = "'" + (term1Str == term2Str).ToString() + "'"; break;
						case "=i=": result = "'" + (term1Str.Equals(term2Str, StringComparison.OrdinalIgnoreCase)).ToString() + "'"; break;
						default: throw new Exception("Invalid op");
					}

					expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
					return true;
				}
			}

			return false;
		}

		bool DoTernaryOperation(ref string expression, List<object> value)
		{
			var match = ternaryOperatorRE.Match(expression);
			if (!match.Success)
				return false;

			var test = GetTerm(match.Groups[1].Value, value);
			var result = "";
			if ((test is string) && (Boolean.Parse(test as string)))
				result = match.Groups[2].Value;
			else
				result = match.Groups[3].Value;

			expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
			return true;
		}

		static readonly Regex boolRE = new Regex("^True|False$");
		static readonly Regex intRE = new Regex(@"^\d+$");

		object GetReturnValue(object result, Type targetType)
		{
			if (!(result is string))
				return result;

			var expression = result as string;
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

		object Evaluate(string expression, List<object> value, Type targetType)
		{
			var expressionStack = new Stack<string>();

			while (true)
			{
				if (EnterParens(ref expression, expressionStack))
					continue;

				if (DoBinaryOperation(ref expression, value))
					continue;

				if (DoTernaryOperation(ref expression, value))
					continue;

				var result = GetTerm(expression, value);

				if (ExitParens(ref expression, expressionStack))
					continue;

				return GetReturnValue(result, targetType);
			}
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			return Evaluate(GetExpression(parameter as string), value.ToList(), targetType);
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Evaluate(GetExpression(parameter as string), new List<object> { value }, targetType);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
