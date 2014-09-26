using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class Expression
	{
		static readonly Regex expressionRE = new Regex(@"\[!(\d+)\]");

		static readonly Regex evalTermRE = new Regex(@"\[(\d+)\]|'((?:[^']|'')*)'|(\d+(?:\.\d*)?(?:[eE]\d+)?)|(true|false)", RegexOptions.IgnoreCase);

		static readonly List<string> functions = new List<string> { "Type", "ValidRE" };
		static readonly Regex functionRE = new Regex(String.Format(@"\b({0}):(\[\d+\])", String.Join("|", functions)));

		static readonly List<List<string>> binaryOperators = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "*", "/", "%" },
			new List<string>{ "+", "-", "t+" },
			new List<string>{ "IS" },
			new List<string>{ "&&" },
			new List<string>{ "||" },
			new List<string>{ "==", "=i=", "!=", "!i=" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperators.Select(a => new Regex(String.Format(@"(\[\d+\])\s*({0})\s*(\[\d+\])", String.Join("|", a.Select(b => Regex.Escape(b)))))).ToList();

		static readonly Regex ternaryOperatorRE = new Regex(@"(\[\d+\])\s*\?\s*(\[\d+\])\s*:\s*(\[\d+\])");

		readonly string expression;
		public Expression(string _expression)
		{
			expression = _expression;
		}

		string GetExpression(string expression, List<object> value)
		{
			if (expression.StartsWith("!"))
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
				expression = expression.Substring(1);
			}

			var result = "";
			while (true)
			{
				var match = expressionRE.Match(expression);
				if (!match.Success)
				{
					result += expression;
					break;
				}

				result += expression.Substring(0, match.Index);
				expression = expression.Substring(match.Index + match.Length);

				result += value[Int32.Parse(match.Groups[1].Value)];
			}
			expression = result;

			result = "";
			while (true)
			{
				var match = evalTermRE.Match(expression);
				if (!match.Success)
				{
					result += expression;
					break;
				}

				result += expression.Substring(0, match.Index);
				expression = expression.Substring(match.Index + match.Length);

				if (match.Groups[1].Success)
					result += match.Value;
				else if (match.Groups[2].Success)
				{
					result += "[" + value.Count + "]";
					value.Add(match.Groups[2].Value);
				}
				else if (match.Groups[3].Success)
				{
					result += "[" + value.Count + "]";
					value.Add(Double.Parse(match.Groups[3].Value));
				}
				else if (match.Groups[4].Success)
				{
					result += "[" + value.Count + "]";
					value.Add(Boolean.Parse(match.Groups[4].Value));
				}
			}
			expression = result;

			if (String.IsNullOrWhiteSpace(expression))
				expression = "&&";

			if (binaryOperators.Any(a => a.Any(b => b == expression)))
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
			return value[Int32.Parse(term.Substring(1, term.Length - 2))];
		}

		bool EvalFunctions(ref string expression, List<object> value)
		{
			var match = functionRE.Match(expression);
			if (!match.Success)
				return false;

			var function = match.Groups[1].Value;
			var term = GetTerm(match.Groups[2].Value, value);
			var termStr = (term ?? "").ToString();

			object result = null;

			switch (function)
			{
				case "Type":
					result = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(termStr)).FirstOrDefault(find => find != null);
					break;
				case "ValidRE":
					try { new Regex(termStr); result = true; }
					catch { result = false; }
					break;
			}

			expression = expression.Substring(0, match.Index) + "[" + value.Count + "]" + expression.Substring(match.Index + match.Length);
			value.Add(result);

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
					object result = null;

					var term1Str = (term1 ?? "").ToString();
					var term2Str = (term2 ?? "").ToString();

					switch (op)
					{
						case ".":
							{
								if (term1 == null)
									break;
								var field = term1.GetType().GetProperty(term2Str, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								if (field == null)
									break;
								result = field.GetValue(term1);
								break;
							}
						case "IS": result = (term1 != null) && (term1.GetType().Name == term2Str); break;
						case "&&": result = Boolean.Parse(term1Str) && Boolean.Parse(term2Str); break;
						case "||": result = Boolean.Parse(term1Str) || Boolean.Parse(term2Str); break;
						case "*": result = Double.Parse(term1Str) * Double.Parse(term2Str); break;
						case "/": result = Double.Parse(term1Str) / Double.Parse(term2Str); break;
						case "%": result = Double.Parse(term1Str) % Double.Parse(term2Str); break;
						case "+": result = Double.Parse(term1Str) + Double.Parse(term2Str); break;
						case "-": result = Double.Parse(term1Str) - Double.Parse(term2Str); break;
						case "t+": result = term1Str + term2Str; break;
						case "==": result = term1Str == term2Str; break;
						case "=i=": result = term1Str.Equals(term2Str, StringComparison.OrdinalIgnoreCase); break;
						case "!=": result = term1Str != term2Str; break;
						case "!i=": result = !term1Str.Equals(term2Str, StringComparison.OrdinalIgnoreCase); break;
						default: throw new Exception("Invalid op");
					}

					expression = expression.Substring(0, match.Index) + "[" + value.Count + "]" + expression.Substring(match.Index + match.Length);
					value.Add(result);
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
			if (((test is string) && (Boolean.Parse(test as string))) || ((test is bool) && ((bool)test)))
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
				expression = expression.Trim();

				if (EnterParens(ref expression, expressionStack))
					continue;

				if (EvalFunctions(ref expression, value))
					continue;

				if (DoBinaryOperation(ref expression, value))
					continue;

				if (DoTernaryOperation(ref expression, value))
					continue;

				if (ExitParens(ref expression, expressionStack))
					continue;

				var result = GetTerm(expression, value);

				return result;
			}
		}
	}
}
