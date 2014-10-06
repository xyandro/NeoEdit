using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class Expression
	{
		class Operation
		{
			public class Term
			{
				public enum TermType
				{
					Value,
					Result,
					Term,
				}

				public TermType type;
				public int index;
				public object term;

				public object GetTerm(object[] values, List<object> results)
				{
					switch (type)
					{
						case Term.TermType.Term: return term;
						case Term.TermType.Value: return values[index];
						case Term.TermType.Result: return results[index];
					}

					throw new Exception("Invalid term");
				}

				public override string ToString()
				{
					return String.Format("{0}: {1}", type, type == TermType.Term ? term : index);
				}
			}

			public string operation { get; private set; }
			public Term[] terms { get; private set; }

			public Operation(string _operation, params Term[] _terms)
			{
				operation = _operation;
				terms = _terms;
			}

			public object GetTerm(int index, object[] values, List<object> results)
			{
				if ((terms.Length <= index) || (terms[index] == null))
					return null;
				return terms[index].GetTerm(values, results);
			}

			public override string ToString()
			{
				var str = operation;
				foreach (var term in terms)
					str += ", " + term.ToString();
				return str;
			}
		}

		List<Operation> operations = new List<Operation>();
		string liveExp = null;

		static readonly Regex simplifyTermRE = new Regex(@"\[(\d+)\]|'((?:[^']|'')*)'|(\d+(?:\.\d*)?(?:[eE]\d+)?)|(true|false)|\b([xy])\b", RegexOptions.IgnoreCase);

		static readonly string parenPlaceholder = "#PARENS#";

		static readonly List<string> functions = new List<string> { "Type", "ValidRE", "Eval", "Int", "Long", "FileName", "StrFormat" };
		static readonly Regex functionRE = new Regex(String.Format(@"\b({0})\s*{1}", String.Join("|", functions), parenPlaceholder));
		static readonly Regex functionArgsRE = new Regex(@"(\[[vir]\d+\])\s*($|,)");

		static readonly List<List<string>> binaryOperators = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "*", "/", "%" },
			new List<string>{ "+", "-", "t+" },
			new List<string>{ "IS" },
			new List<string>{ "<", "<=", ">", ">=", "t<", "t<=", "t>", "t>=", "ti<", "ti<=", "ti>", "ti>=" },
			new List<string>{ "=", "==", "!=", "t==", "t!=", "ti==", "ti!=" },
			new List<string>{ "&&" },
			new List<string>{ "||" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperators.Select(a => new Regex(String.Format(@"(\[[vir]\d+\])\s*({0})\s*(\[[vir]\d+\])", String.Join("|", a.Select(b => Regex.Escape(b)))))).ToList();

		static readonly Regex ternaryOperatorRE = new Regex(@"(\[[vir]\d+\])\s*\?\s*(\[[vir]\d+\])\s*:\s*(\[[vir]\d+\])");

		public Expression(string expression)
		{
			List<object> internals;
			SimplifyAndPopulateInternals(ref expression, out internals);
			GetEvaluation(expression, internals);
		}

		void SimplifyAndPopulateInternals(ref string expression, out List<object> internals)
		{
			if (expression.StartsWith("!"))
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
				expression = expression.Substring(1);
			}

			internals = new List<object>();

			var result = "";
			while (true)
			{
				var match = simplifyTermRE.Match(expression);
				if (!match.Success)
				{
					result += expression;
					break;
				}

				result += expression.Substring(0, match.Index);
				expression = expression.Substring(match.Index + match.Length);

				if (match.Groups[1].Success)
					result += "[v" + match.Value.Substring(1, match.Length - 2) + "]";
				else if (match.Groups[2].Success)
				{
					result += "[i" + internals.Count + "]";
					internals.Add(match.Groups[2].Value);
				}
				else if (match.Groups[3].Success)
				{
					result += "[i" + internals.Count + "]";
					internals.Add(Double.Parse(match.Groups[3].Value));
				}
				else if (match.Groups[4].Success)
				{
					result += "[i" + internals.Count + "]";
					internals.Add(Boolean.Parse(match.Groups[4].Value));
				}
				else if (match.Groups[5].Success)
					result += "[v" + (match.Value.ToLower()[0] - 'x') + "]";
			}
			expression = result;

			if (String.IsNullOrWhiteSpace(expression))
				expression = "&&";

			var exp = expression;
			if (binaryOperators.Any(a => a.Any(b => b == exp)))
				liveExp = expression;
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
								expression = expression.Substring(0, expressionStart) + parenPlaceholder + expression.Substring(ctr + 1);
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

		bool ExitParensAndEvalFunctions(ref string expression, Stack<string> expressionStack, List<object> internals, ref int results)
		{
			if (expressionStack.Count == 0)
				return false;

			var enclosing = expressionStack.Pop();
			var match = functionRE.Match(enclosing);
			if (!match.Success)
			{
				expression = enclosing.Replace(parenPlaceholder, expression);
				return true;
			}

			var function = match.Groups[1].Value;
			var args = new List<string>();
			while (expression.Length != 0)
			{
				var argsMatch = functionArgsRE.Match(expression);
				if (!argsMatch.Success)
					throw new Exception("Failed to get function arguments");

				args.Add(argsMatch.Groups[1].Value);
				expression = expression.Substring(argsMatch.Index + argsMatch.Length).Trim();
			}

			operations.Add(new Operation(function, args.Select(arg => GetTerm(arg, internals)).ToArray()));
			expression = enclosing.Substring(0, match.Index) + "[r" + results++ + "]" + enclosing.Substring(match.Index + match.Length);
			return true;
		}

		Operation.Term GetTerm(string term, List<object> internals)
		{
			if ((!term.StartsWith("[")) || (!term.EndsWith("]")))
				throw new Exception("Invalid expression");

			var index = Int32.Parse(term.Substring(2, term.Length - 3));
			switch (term[1])
			{
				case 'v': return new Operation.Term { type = Operation.Term.TermType.Value, index = index };
				case 'i': return new Operation.Term { type = Operation.Term.TermType.Term, term = internals[index] };
				case 'r': return new Operation.Term { type = Operation.Term.TermType.Result, index = index };
			}

			throw new Exception("Invalid expression");
		}

		bool DoBinaryOperation(ref string expression, List<object> internals, ref int results)
		{
			foreach (var binaryOperatorRE in binaryOperatorREs)
			{
				var match = binaryOperatorRE.Match(expression);
				if (match.Success)
				{
					var term1 = GetTerm(match.Groups[1].Value, internals);
					var term2 = GetTerm(match.Groups[3].Value, internals);
					operations.Add(new Operation(match.Groups[2].Value, term1, term2));
					expression = expression.Substring(0, match.Index) + "[r" + results++ + "]" + expression.Substring(match.Index + match.Length);
					return true;
				}
			}

			return false;
		}

		bool DoTernaryOperation(ref string expression, List<object> internals, ref int results)
		{
			var match = ternaryOperatorRE.Match(expression);
			if (!match.Success)
				return false;

			var condition = GetTerm(match.Groups[1].Value, internals);
			var trueValue = GetTerm(match.Groups[2].Value, internals);
			var falseValue = GetTerm(match.Groups[3].Value, internals);
			operations.Add(new Operation("?:", condition, trueValue, falseValue));
			expression = expression.Substring(0, match.Index) + "[r" + results++ + "]" + expression.Substring(match.Index + match.Length);
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

		void GetEvaluation(string expression, List<object> internals)
		{
			if (liveExp != null)
				return;

			var expressionStack = new Stack<string>();
			var results = 0;
			while (true)
			{
				expression = expression.Trim();

				if (EnterParens(ref expression, expressionStack))
					continue;

				if (DoBinaryOperation(ref expression, internals, ref results))
					continue;

				if (DoTernaryOperation(ref expression, internals, ref results))
					continue;

				if (ExitParensAndEvalFunctions(ref expression, expressionStack, internals, ref results))
					continue;

				operations.Add(new Operation("RETURN", GetTerm(expression, internals)));
				break;
			}
		}

		double GetDouble(object value)
		{
			if (value == null)
				throw new Exception("Value is NULL");
			if (value is double)
				return (double)value;
			if (value is int)
				return (double)(int)value;
			if (value is byte)
				return (double)(byte)value;
			if (value is float)
				return (double)(float)value;
			if (value is short)
				return (double)(short)value;
			if (value is long)
				return (double)(long)value;
			return double.Parse(value.ToString());
		}

		bool GetBoolean(object value)
		{
			if (value == null)
				throw new Exception("Value is NULL");
			if (value is bool)
				return (bool)value;
			return bool.Parse(value.ToString());
		}

		string GetString(object value)
		{
			if (value == null)
				return "";
			if (value is string)
				return (string)value;
			return value.ToString();
		}

		bool IsType(object obj, string type)
		{
			if (obj == null)
				return false;
			return obj.GetType().Name == type;
		}

		object GetDotOp(object obj, string fieldName)
		{
			if (obj == null)
				return null;
			var field = obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field == null)
				return null;
			return field.GetValue(obj);
		}

		bool ValidRE(string re)
		{
			try { new Regex(re); return true; }
			catch { return false; }
		}

		object Eval(string expression)
		{
			return new Expression(expression).Evaluate();
		}

		string StrFormat(Operation.Term[] terms, object[] values, List<object> results)
		{
			var termVals = terms.Select(term => term.GetTerm(values, results)).ToList();
			var format = GetString(termVals[0]);
			var args = termVals.Skip(1).ToArray();
			return String.Format(format, args);
		}

		object LiveValue(object[] values)
		{
			var expression = String.Join(liveExp, Enumerable.Range(0, values.Length).Select(a => String.Format("[{0}]", a)));
			return new Expression(expression).Evaluate(values);
		}

		public object Evaluate(params object[] values)
		{
			if (liveExp != null)
				return LiveValue(values);

			var results = new List<object>();
			foreach (var op in operations)
			{
				switch (op.operation)
				{
					case "Type": results.Add(AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(GetString(op.GetTerm(0, values, results)))).FirstOrDefault(find => find != null)); break;
					case "ValidRE": results.Add(ValidRE(GetString(op.GetTerm(0, values, results)))); break;
					case "Eval": results.Add(Eval(GetString(op.GetTerm(0, values, results)))); break;
					case "Int": results.Add((int)GetDouble(op.GetTerm(0, values, results))); break;
					case "Long": results.Add((long)GetDouble(op.GetTerm(0, values, results))); break;
					case "FileName": results.Add(Path.GetFileName(GetString(op.GetTerm(0, values, results)))); break;
					case "StrFormat": results.Add(StrFormat(op.terms, values, results)); break;
					case ".": results.Add(GetDotOp(op.GetTerm(0, values, results), GetString(op.GetTerm(1, values, results)))); break;
					case "*": results.Add(GetDouble(op.GetTerm(0, values, results)) * GetDouble(op.GetTerm(1, values, results))); break;
					case "/": results.Add(GetDouble(op.GetTerm(0, values, results)) / GetDouble(op.GetTerm(1, values, results))); break;
					case "%": results.Add(GetDouble(op.GetTerm(0, values, results)) % GetDouble(op.GetTerm(1, values, results))); break;
					case "+": results.Add(GetDouble(op.GetTerm(0, values, results)) + GetDouble(op.GetTerm(1, values, results))); break;
					case "-": results.Add(GetDouble(op.GetTerm(0, values, results)) - GetDouble(op.GetTerm(1, values, results))); break;
					case "t+": results.Add(GetString(op.GetTerm(0, values, results)) + GetString(op.GetTerm(1, values, results))); break;
					case "IS": results.Add(IsType(op.GetTerm(0, values, results), GetString(op.GetTerm(1, values, results)))); break;
					case "<": results.Add(GetDouble(op.GetTerm(0, values, results)) < GetDouble(op.GetTerm(1, values, results))); break;
					case "<=": results.Add(GetDouble(op.GetTerm(0, values, results)) <= GetDouble(op.GetTerm(1, values, results))); break;
					case ">": results.Add(GetDouble(op.GetTerm(0, values, results)) > GetDouble(op.GetTerm(1, values, results))); break;
					case ">=": results.Add(GetDouble(op.GetTerm(0, values, results)) >= GetDouble(op.GetTerm(1, values, results))); break;
					case "t<": results.Add(GetString(op.GetTerm(0, values, results)).CompareTo(GetString(op.GetTerm(1, values, results))) < 0); break;
					case "t<=": results.Add(GetString(op.GetTerm(0, values, results)).CompareTo(GetString(op.GetTerm(1, values, results))) <= 0); break;
					case "t>": results.Add(GetString(op.GetTerm(0, values, results)).CompareTo(GetString(op.GetTerm(1, values, results))) > 0); break;
					case "t>=": results.Add(GetString(op.GetTerm(0, values, results)).CompareTo(GetString(op.GetTerm(1, values, results))) >= 0); break;
					case "ti<": results.Add(GetString(op.GetTerm(0, values, results)).ToLower().CompareTo(GetString(op.GetTerm(1, values, results)).ToLower()) < 0); break;
					case "ti<=": results.Add(GetString(op.GetTerm(0, values, results)).ToLower().CompareTo(GetString(op.GetTerm(1, values, results)).ToLower()) <= 0); break;
					case "ti>": results.Add(GetString(op.GetTerm(0, values, results)).ToLower().CompareTo(GetString(op.GetTerm(1, values, results)).ToLower()) > 0); break;
					case "ti>=": results.Add(GetString(op.GetTerm(0, values, results)).ToLower().CompareTo(GetString(op.GetTerm(1, values, results)).ToLower()) >= 0); break;
					case "=":
					case "==":
					case "t==": results.Add(GetString(op.GetTerm(0, values, results)) == GetString(op.GetTerm(1, values, results))); break;
					case "!=":
					case "t!=": results.Add(GetString(op.GetTerm(0, values, results)) != GetString(op.GetTerm(1, values, results))); break;
					case "ti==": results.Add(GetString(op.GetTerm(0, values, results)).Equals(GetString(op.GetTerm(1, values, results)), StringComparison.OrdinalIgnoreCase)); break;
					case "ti!=": results.Add(!GetString(op.GetTerm(0, values, results)).Equals(GetString(op.GetTerm(1, values, results)), StringComparison.OrdinalIgnoreCase)); break;
					case "&&": results.Add(GetBoolean(op.GetTerm(0, values, results)) && GetBoolean(op.GetTerm(1, values, results))); break;
					case "||": results.Add(GetBoolean(op.GetTerm(0, values, results)) || GetBoolean(op.GetTerm(1, values, results))); break;
					case "?:": results.Add(GetBoolean(op.GetTerm(0, values, results)) ? op.GetTerm(1, values, results) : op.GetTerm(2, values, results)); break;
					case "RETURN": results.Add(op.GetTerm(0, values, results)); break;
					default:
						throw new Exception(String.Format("Invalid operation: {0}", op.operation));
				}
			}
			return results.Last();
		}
	}
}
