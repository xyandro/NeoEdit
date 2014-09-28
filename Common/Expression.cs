using System;
using System.Collections.Generic;
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

				public override string ToString()
				{
					return String.Format("{0}: {1}", type, type == TermType.Term ? term : index);
				}
			}

			public string operation;
			public Term term1, term2, term3;

			public override string ToString()
			{
				var str = operation;
				if (term1 != null)
					str += ", " + term1.ToString();
				if (term2 != null)
					str += ", " + term2.ToString();
				if (term3 != null)
					str += ", " + term3.ToString();
				return str;
			}
		}

		List<Operation> operations = new List<Operation>();
		string liveExp = null;

		static readonly Regex simplifyTermRE = new Regex(@"\[(\d+)\]|'((?:[^']|'')*)'|(\d+(?:\.\d*)?(?:[eE]\d+)?)|(true|false)|\b([xy])\b", RegexOptions.IgnoreCase);

		static readonly List<string> functions = new List<string> { "Type", "ValidRE", "Eval", "Int" };
		static readonly Regex functionRE = new Regex(String.Format(@"\b({0}):(\[[vir]\d+\])", String.Join("|", functions)));

		static readonly List<List<string>> binaryOperators = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "*", "/", "%" },
			new List<string>{ "+", "-", "t+" },
			new List<string>{ "IS" },
			new List<string>{ "<", "<=", ">", ">=", "t<", "t<=", "t>", "t>=", "ti<", "ti<=", "ti>", "ti>=" },
			new List<string>{ "==", "!=", "t==", "t!=", "ti==", "ti!=" },
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

		bool EvalFunctions(ref string expression, List<object> internals, ref int results)
		{
			var match = functionRE.Match(expression);
			if (!match.Success)
				return false;

			var term1 = GetTerm(match.Groups[2].Value, internals);
			operations.Add(new Operation { operation = match.Groups[1].Value, term1 = term1 });
			expression = expression.Substring(0, match.Index) + "[r" + results++ + "]" + expression.Substring(match.Index + match.Length);
			return true;
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
					operations.Add(new Operation { operation = match.Groups[2].Value, term1 = term1, term2 = term2 });
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

			var term1 = GetTerm(match.Groups[1].Value, internals);
			var term2 = GetTerm(match.Groups[2].Value, internals);
			var term3 = GetTerm(match.Groups[3].Value, internals);
			operations.Add(new Operation { operation = "?:", term1 = term1, term2 = term2, term3 = term3 });
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

				if (EvalFunctions(ref expression, internals, ref results))
					continue;

				if (DoBinaryOperation(ref expression, internals, ref results))
					continue;

				if (DoTernaryOperation(ref expression, internals, ref results))
					continue;

				if (ExitParens(ref expression, expressionStack))
					continue;

				operations.Add(new Operation { operation = "=", term1 = GetTerm(expression, internals) });
				break;
			}
		}

		object GetObject(Operation.Term term, object[] values, List<object> results)
		{
			switch (term.type)
			{
				case Operation.Term.TermType.Term: return term.term;
				case Operation.Term.TermType.Value: return values[term.index];
				case Operation.Term.TermType.Result: return results[term.index];
			}

			throw new Exception("Invalid term");
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
				return (double)(int)value;
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
				var term1 = op.term1 == null ? null : GetObject(op.term1, values, results);
				var term2 = op.term2 == null ? null : GetObject(op.term2, values, results);
				var term3 = op.term3 == null ? null : GetObject(op.term3, values, results);

				switch (op.operation)
				{
					case "Type": results.Add(AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(GetString(term1))).FirstOrDefault(find => find != null)); break;
					case "ValidRE": results.Add(ValidRE(GetString(term1))); break;
					case "Eval": results.Add(Eval(GetString(term1))); break;
					case "Int": results.Add(Math.Floor(GetDouble(term1))); break;
					case ".": results.Add(GetDotOp(term1, GetString(term2))); break;
					case "*": results.Add(GetDouble(term1) * GetDouble(term2)); break;
					case "/": results.Add(GetDouble(term1) / GetDouble(term2)); break;
					case "%": results.Add(GetDouble(term1) % GetDouble(term2)); break;
					case "+": results.Add(GetDouble(term1) + GetDouble(term2)); break;
					case "-": results.Add(GetDouble(term1) - GetDouble(term2)); break;
					case "t+": results.Add(GetString(term1) + GetString(term2)); break;
					case "IS": results.Add(IsType(term1, GetString(term2))); break;
					case "<": results.Add(GetDouble(term1) < GetDouble(term2)); break;
					case "<=": results.Add(GetDouble(term1) <= GetDouble(term2)); break;
					case ">": results.Add(GetDouble(term1) > GetDouble(term2)); break;
					case ">=": results.Add(GetDouble(term1) >= GetDouble(term2)); break;
					case "t<": results.Add(GetString(term1).CompareTo(GetString(term2)) < 0); break;
					case "t<=": results.Add(GetString(term1).CompareTo(GetString(term2)) <= 0); break;
					case "t>": results.Add(GetString(term1).CompareTo(GetString(term2)) > 0); break;
					case "t>=": results.Add(GetString(term1).CompareTo(GetString(term2)) >= 0); break;
					case "ti<": results.Add(GetString(term1).ToLower().CompareTo(GetString(term2).ToLower()) < 0); break;
					case "ti<=": results.Add(GetString(term1).ToLower().CompareTo(GetString(term2).ToLower()) <= 0); break;
					case "ti>": results.Add(GetString(term1).ToLower().CompareTo(GetString(term2).ToLower()) > 0); break;
					case "ti>=": results.Add(GetString(term1).ToLower().CompareTo(GetString(term2).ToLower()) >= 0); break;
					case "==":
					case "t==": results.Add(GetString(term1) == GetString(term2)); break;
					case "!=":
					case "t!=": results.Add(GetString(term1) != GetString(term2)); break;
					case "ti==": results.Add(GetString(term1).Equals(GetString(term2), StringComparison.OrdinalIgnoreCase)); break;
					case "ti!=": results.Add(!GetString(term1).Equals(GetString(term2), StringComparison.OrdinalIgnoreCase)); break;
					case "&&": results.Add(GetBoolean(term1) && GetBoolean(term2)); break;
					case "||": results.Add(GetBoolean(term1) || GetBoolean(term2)); break;
					case "?:": results.Add(GetBoolean(term1) ? term2 : term3); break;
					case "=": return term1;
					default:
						throw new Exception(String.Format("Invalid operation: {0}", op.operation));
				}
			}
			throw new Exception("Error: No value returned");
		}
	}
}
