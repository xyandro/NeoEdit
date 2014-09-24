using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class Expression
	{
		static readonly string num = @"\d+(\.\d*)?([eE]\d+)?";
		static readonly Regex numRE = new Regex(num);

		static readonly string term = @"('(?:[^']|'')*'|\[NULL\]|\[\d+\])";
		static readonly Regex termRE = new Regex(term);
		static readonly string termParts = @"'((?:[^']|'')*)'|\[(NULL)\]|\[(\d+)\]";
		static readonly Regex termPartsRE = new Regex("^" + termParts + "$");

		static readonly List<string> functions = new List<string> { "Type", "ValidRE" };
		static readonly string functionExp = String.Format(@"\b({0}):({1})", String.Join("|", functions), term);
		static readonly Regex functionRE = new Regex(functionExp);

		static readonly List<List<string>> binaryOperator = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "*", "/", "%" },
			new List<string>{ "+", "-", "t+" },
			new List<string>{ "IS" },
			new List<string>{ "AND" },
			new List<string>{ "OR" },
			new List<string>{ "==", "=i=", "!=", "!i=" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperator.Select(a => new Regex(String.Format(@"{0}\s*({1})\s*{0}", term, String.Join("|", a.Select(b => Regex.Escape(b)))))).ToList();

		static readonly string ternaryOperator = String.Format(@"{0}\s*\?\s*{0}\s*:\s*{0}", term);
		static readonly Regex ternaryOperatorRE = new Regex(ternaryOperator);

		readonly string expression;
		public Expression(string _expression)
		{
			expression = _expression;
		}

		const string placeHolder = "##MATCH##";
		string GetExpression(string expression, List<object> value)
		{
			if ((expression.Length > 0) && (expression[0] == '!'))
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
				expression = expression.Substring(1);
			}

			var matches = termRE.Matches(expression).Cast<Match>().Select(match => match.Groups[1].Value).ToList();
			expression = termRE.Replace(expression, placeHolder);
			expression = numRE.Replace(expression, match => "'" + match.Value + "'");

			while (matches.Count != 0)
			{
				var idx = expression.IndexOf(placeHolder);
				if (idx == -1)
					throw new ArgumentException();
				expression = expression.Substring(0, idx) + matches[0] + expression.Substring(idx + placeHolder.Length);
				matches.RemoveAt(0);
			}

			if (String.IsNullOrWhiteSpace(expression))
				expression = "AND";

			if (binaryOperator.Any(a => a.Any(b => b == expression)))
				expression = String.Join(expression, Enumerable.Range(0, value.Count).Select(a => String.Format("[{0}]", a)));

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

		bool EvalFunctions(ref string expression, List<object> value)
		{
			var match = functionRE.Match(expression);
			if (!match.Success)
				return false;

			var function = match.Groups[1].Value;
			var term = GetTerm(match.Groups[2].Value, value);
			var termStr = (term ?? "").ToString();

			var result = "[NULL]";

			switch (function)
			{
				case "Type":
					var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(termStr)).FirstOrDefault(find => find != null);
					if (type != null)
					{
						result = "[" + value.Count + "]";
						value.Add(type);
					}
					break;
				case "ValidRE":
					try { new Regex(termStr); result = "'True'"; }
					catch { result = "'False'"; }
					break;
			}

			expression = expression.Substring(0, match.Index) + result + expression.Substring(match.Index + match.Length);
			return true;
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
						case "*": result = "'" + (Double.Parse(term1Str) * Double.Parse(term2Str)).ToString() + "'"; break;
						case "/": result = "'" + (Double.Parse(term1Str) / Double.Parse(term2Str)).ToString() + "'"; break;
						case "%": result = "'" + (Double.Parse(term1Str) % Double.Parse(term2Str)).ToString() + "'"; break;
						case "+": result = "'" + (Double.Parse(term1Str) + Double.Parse(term2Str)).ToString() + "'"; break;
						case "-": result = "'" + (Double.Parse(term1Str) - Double.Parse(term2Str)).ToString() + "'"; break;
						case "t+": result = "'" + term1Str + term2Str + "'"; break;
						case "==": result = "'" + (term1Str == term2Str).ToString() + "'"; break;
						case "=i=": result = "'" + (term1Str.Equals(term2Str, StringComparison.OrdinalIgnoreCase)).ToString() + "'"; break;
						case "!=": result = "'" + (term1Str != term2Str).ToString() + "'"; break;
						case "!i=": result = "'" + (!term1Str.Equals(term2Str, StringComparison.OrdinalIgnoreCase)).ToString() + "'"; break;
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

		public object Evaluate(params object[] values)
		{
			var value = values.ToList();
			var expression = GetExpression(this.expression, value);

			var expressionStack = new Stack<string>();
			while (true)
			{
				if (EnterParens(ref expression, expressionStack))
					continue;

				if (EvalFunctions(ref expression, value))
					continue;

				if (DoBinaryOperation(ref expression, value))
					continue;

				if (DoTernaryOperation(ref expression, value))
					continue;

				var result = GetTerm(expression, value);

				if (ExitParens(ref expression, expressionStack))
					continue;

				return result;
			}
		}
	}
}
