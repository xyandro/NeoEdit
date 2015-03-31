using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class Expression
	{
		class EvaluationData
		{
			public List<object> values { get; private set; }
			public List<object> results { get; private set; }
			public Dictionary<string, List<string>> dict { get; private set; }
			public int row { get; private set; }

			public EvaluationData(List<object> values, Dictionary<string, List<string>> dict, int row)
			{
				this.values = values;
				this.dict = dict;
				this.row = row;
				results = new List<object>();
			}

			public void AddResult(object result)
			{
				results.Add(result);
			}

			public object LastResult()
			{
				return results.Last();
			}
		}

		class Operation
		{
			public class Term
			{
				public enum TermType
				{
					Value,
					Result,
					Term,
					Dictionary,
				}

				public TermType type;
				public int index;
				public string key;
				public object term;

				public object GetTerm(EvaluationData data)
				{
					switch (type)
					{
						case Term.TermType.Term: return term;
						case Term.TermType.Value: return data.values[index];
						case Term.TermType.Result: return data.results[index];
						case TermType.Dictionary: return (data.dict != null) && (data.dict.ContainsKey(key)) && (data.row < data.dict[key].Count) ? data.dict[key][data.row] : null;
					}

					throw new Exception("Invalid term");
				}

				public override string ToString()
				{
					return String.Format("{0}: {1}", type, type == TermType.Term ? term : index);
				}
			}

			public string operation { get; private set; }
			public List<Term> terms { get; private set; }

			public Operation(string _operation, params Term[] _terms)
			{
				operation = _operation;
				terms = _terms.ToList();
			}

			public T GetTerm<T>(int index, EvaluationData data)
			{
				object value = null;
				if ((terms.Count > index) && (terms[index] != null))
					value = terms[index].GetTerm(data);
				if ((value == null) && (typeof(T) == typeof(object)))
					return default(T);
				if ((value == null) && (typeof(T) == typeof(string)))
					value = "";
				if (value == null)
					throw new Exception("Value is NULL");
				if ((!(value is string)) && (typeof(T) == typeof(string)))
					value = value.ToString();
				if (value is T)
					return (T)value;
				return (T)Convert.ChangeType(value, typeof(T));
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
		readonly string expression;
		bool debug = false;

		public Expression(string expression, IEnumerable<string> vars = null)
		{
			this.expression = expression;
			List<object> internals;
			SimplifyAndPopulateInternals(ref expression, out internals, vars);
			GetEvaluation(expression, internals);
		}

		static readonly Regex simplifyTermRE = new Regex(@"\[(\d+)\]|'((?:[^']|'')*)'|(\d+(?:\.\d*)?(?:[eE]\d+)?)|\b(true|false)\b|\b(\w+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		void SimplifyAndPopulateInternals(ref string expression, out List<object> internals, IEnumerable<string> vars)
		{
			if (expression.StartsWith("!!"))
			{
				expression = expression.Substring(2);
				if (Debugger.IsAttached)
					Debugger.Break();
			}

			if (expression.StartsWith("!"))
			{
				expression = expression.Substring(1);
				debug = true;
			}

			internals = new List<object>();

			var matches = simplifyTermRE.Matches(expression).Cast<Match>().ToList();

			var result = "";
			var textAt = 0;
			foreach (var match in matches)
			{
				result += expression.Substring(textAt, match.Index - textAt);
				textAt = match.Index + match.Length;

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
				{
					var str = match.Groups[5].Value;
					if ((vars != null) && (vars.Contains(str)))
						result += "[d" + str + "]";
					else
						result += str;
				}
			}
			result += expression.Substring(textAt);
			expression = result;

			if (String.IsNullOrWhiteSpace(expression))
				expression = "&&";

			var exp = expression;
			if (binaryOperators.Any(a => a.Any(b => b == exp)))
				liveExp = expression;
		}

		static readonly string parenPlaceholder = "#PARENS#";
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

		static readonly List<string> functions = new List<string> { "Type", "ValidRE", "Eval", "Int", "Long", "FileName", "StrFormat" };
		static readonly Regex functionRE = new Regex(String.Format(@"\b({0})\s*{1}", String.Join("|", functions), parenPlaceholder));
		static readonly Regex functionArgsRE = new Regex(@"(\[[vird]\w+\])\s*($|,)");
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

			var index = term[1] == 'd' ? 0 : Int32.Parse(term.Substring(2, term.Length - 3));
			var key = term[1] == 'd' ? term.Substring(2, term.Length - 3) : null;
			switch (term[1])
			{
				case 'v': return new Operation.Term { type = Operation.Term.TermType.Value, index = index };
				case 'i': return new Operation.Term { type = Operation.Term.TermType.Term, term = internals[index] };
				case 'r': return new Operation.Term { type = Operation.Term.TermType.Result, index = index };
				case 'd': return new Operation.Term { type = Operation.Term.TermType.Dictionary, key = key };
			}

			throw new Exception("Invalid expression");
		}

		static readonly List<List<string>> binaryOperators = new List<List<string>>
		{
			new List<string>{ "." },
			new List<string>{ "^^", "root" },
			new List<string>{ "*", "/", "%" },
			new List<string>{ "+", "-", "t+" },
			new List<string>{ "<<", ">>" },
			new List<string>{ "IS" },
			new List<string>{ "<", "<=", ">", ">=", "t<", "t<=", "t>", "t>=", "ti<", "ti<=", "ti>", "ti>=" },
			new List<string>{ "=", "==", "!=", "t==", "t!=", "ti==", "ti!=" },
			new List<string>{ "&" },
			new List<string>{ "^" },
			new List<string>{ "|" },
			new List<string>{ "&&" },
			new List<string>{ "||" },
		};
		static readonly List<Regex> binaryOperatorREs = binaryOperators.Select(a => new Regex(String.Format(@"(\[[vird]\w+\])\s*({0})\s*(\[[vird]\w+\])", String.Join("|", a.Select(b => Regex.Escape(b)))))).ToList();
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

		static readonly Regex ternaryOperatorRE = new Regex(@"(\[[vird]\w+\])\s*\?\s*(\[[vird]\w+\])\s*:\s*(\[[vird]\w+\])");
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

		public List<string> DictValues()
		{
			return operations.SelectMany(op => op.terms).Where(term => term.type == Operation.Term.TermType.Dictionary).Select(term => term.key).Distinct().ToList();
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

		string StrFormat(List<Operation.Term> terms, EvaluationData data)
		{
			var termVals = terms.Select(term => term.GetTerm(data)).ToList();
			var format = Convert.ToString(termVals[0]);
			var args = termVals.Skip(1).Select(arg => arg ?? "").ToArray();
			return String.Format(format, args);
		}

		object LiveValue(List<object> values)
		{
			if (!values.Any())
				return "";
			var expression = String.Join(liveExp, Enumerable.Range(0, values.Count).Select(a => String.Format("[{0}]", a)));
			return new Expression(expression).Evaluate(values.ToArray());
		}

		object CheckUnset(object value)
		{
			return (value != null) && (value.GetType().FullName == "MS.Internal.NamedObject") && (value.ToString() == "{DependencyProperty.UnsetValue}") ? null : value;
		}

		public object Evaluate(params object[] values)
		{
			return EvaluateDict(null, 0, values);
		}

		public object EvaluateDict(Dictionary<string, List<string>> dict, int row, params object[] _values)
		{
			if ((debug) && (Debugger.IsAttached))
				Debugger.Break();

			var values = _values.Select(value => CheckUnset(value)).ToList();

			if (liveExp != null)
				return LiveValue(values);

			var data = new EvaluationData(values, dict, row);
			foreach (var op in operations)
			{
				switch (op.operation)
				{
					case "Type": data.AddResult(AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(op.GetTerm<string>(0, data))).FirstOrDefault(find => find != null)); break;
					case "ValidRE": data.AddResult(ValidRE(op.GetTerm<string>(0, data))); break;
					case "Eval": data.AddResult(Eval(op.GetTerm<string>(0, data))); break;
					case "Int": data.AddResult((int)op.GetTerm<double>(0, data)); break;
					case "Long": data.AddResult((long)op.GetTerm<double>(0, data)); break;
					case "FileName": data.AddResult(Path.GetFileName(op.GetTerm<string>(0, data))); break;
					case "StrFormat": data.AddResult(StrFormat(op.terms, data)); break;
					case ".": data.AddResult(GetDotOp(op.GetTerm<object>(0, data), op.GetTerm<string>(1, data))); break;
					case "^^": data.AddResult(Math.Pow(op.GetTerm<double>(0, data), op.GetTerm<double>(1, data))); break;
					case "root": data.AddResult(Math.Pow(op.GetTerm<double>(1, data), (1.0 / op.GetTerm<double>(0, data)))); break;
					case "*": data.AddResult(op.GetTerm<double>(0, data) * op.GetTerm<double>(1, data)); break;
					case "/": data.AddResult(op.GetTerm<double>(0, data) / op.GetTerm<double>(1, data)); break;
					case "%": data.AddResult(op.GetTerm<double>(0, data) % op.GetTerm<double>(1, data)); break;
					case "+": data.AddResult(op.GetTerm<double>(0, data) + op.GetTerm<double>(1, data)); break;
					case "-": data.AddResult(op.GetTerm<double>(0, data) - op.GetTerm<double>(1, data)); break;
					case "t+": data.AddResult(op.GetTerm<string>(0, data) + op.GetTerm<string>(1, data)); break;
					case "<<": data.AddResult(op.GetTerm<long>(0, data) << (int)op.GetTerm<long>(1, data)); break;
					case ">>": data.AddResult(op.GetTerm<long>(0, data) >> (int)op.GetTerm<long>(1, data)); break;
					case "IS": data.AddResult(IsType(op.GetTerm<object>(0, data), op.GetTerm<string>(1, data))); break;
					case "<": data.AddResult(op.GetTerm<double>(0, data) < op.GetTerm<double>(1, data)); break;
					case "<=": data.AddResult(op.GetTerm<double>(0, data) <= op.GetTerm<double>(1, data)); break;
					case ">": data.AddResult(op.GetTerm<double>(0, data) > op.GetTerm<double>(1, data)); break;
					case ">=": data.AddResult(op.GetTerm<double>(0, data) >= op.GetTerm<double>(1, data)); break;
					case "t<": data.AddResult(op.GetTerm<string>(0, data).CompareTo(op.GetTerm<string>(1, data)) < 0); break;
					case "t<=": data.AddResult(op.GetTerm<string>(0, data).CompareTo(op.GetTerm<string>(1, data)) <= 0); break;
					case "t>": data.AddResult(op.GetTerm<string>(0, data).CompareTo(op.GetTerm<string>(1, data)) > 0); break;
					case "t>=": data.AddResult(op.GetTerm<string>(0, data).CompareTo(op.GetTerm<string>(1, data)) >= 0); break;
					case "ti<": data.AddResult(op.GetTerm<string>(0, data).ToLower().CompareTo(op.GetTerm<string>(1, data).ToLower()) < 0); break;
					case "ti<=": data.AddResult(op.GetTerm<string>(0, data).ToLower().CompareTo(op.GetTerm<string>(1, data).ToLower()) <= 0); break;
					case "ti>": data.AddResult(op.GetTerm<string>(0, data).ToLower().CompareTo(op.GetTerm<string>(1, data).ToLower()) > 0); break;
					case "ti>=": data.AddResult(op.GetTerm<string>(0, data).ToLower().CompareTo(op.GetTerm<string>(1, data).ToLower()) >= 0); break;
					case "=":
					case "==": data.AddResult(op.GetTerm<object>(0, data) == op.GetTerm<object>(1, data)); break;
					case "t==": data.AddResult(op.GetTerm<string>(0, data) == op.GetTerm<string>(1, data)); break;
					case "!=": data.AddResult(op.GetTerm<object>(0, data) != op.GetTerm<object>(1, data)); break;
					case "t!=": data.AddResult(op.GetTerm<string>(0, data) != op.GetTerm<string>(1, data)); break;
					case "ti==": data.AddResult(op.GetTerm<string>(0, data).Equals(op.GetTerm<string>(1, data), StringComparison.OrdinalIgnoreCase)); break;
					case "ti!=": data.AddResult(!op.GetTerm<string>(0, data).Equals(op.GetTerm<string>(1, data), StringComparison.OrdinalIgnoreCase)); break;
					case "&": data.AddResult(op.GetTerm<long>(0, data) & op.GetTerm<long>(1, data)); break;
					case "^": data.AddResult(op.GetTerm<long>(0, data) ^ op.GetTerm<long>(1, data)); break;
					case "|": data.AddResult(op.GetTerm<long>(0, data) | op.GetTerm<long>(1, data)); break;
					case "&&": data.AddResult(op.GetTerm<bool>(0, data) && op.GetTerm<bool>(1, data)); break;
					case "||": data.AddResult(op.GetTerm<bool>(0, data) || op.GetTerm<bool>(1, data)); break;
					case "?:": data.AddResult(op.GetTerm<bool>(0, data) ? op.GetTerm<object>(1, data) : op.GetTerm<object>(2, data)); break;
					case "RETURN": data.AddResult(op.GetTerm<object>(0, data)); break;
					default:
						throw new Exception(String.Format("Invalid operation: {0}", op.operation));
				}
			}
			var result = data.LastResult();
			if (result is string)
			{
				bool value;
				if (bool.TryParse(result as string, out value))
					result = value;
			}
			return result;
		}
	}
}
