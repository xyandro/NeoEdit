using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Expressions.Parser;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	public class NEExpression
	{
		readonly string expression;
		readonly ExpressionParser.ExprContext tree;

		public NEExpression(string expression)
		{
			this.expression = expression;

			try { tree = ParserHelper.Parse<ExpressionLexer, ExpressionParser, ExpressionParser.ExprContext>(expression, parser => parser.expr(), true); }
			catch (Exception ex) { throw new Exception("Invalid expression", ex); }
		}

		internal object InternalEvaluate(NEVariables variables, int row, string unit) => new ExpressionEvaluator(expression, variables, row, unit).Visit(tree);

		public object Evaluate(NEVariables variables = null, string unit = null) => EvaluateList(variables, 1, unit)[0];
		public T Evaluate<T>(NEVariables variables = null, string unit = null) => EvaluateList<T>(variables, 1, unit)[0];

		public List<object> EvaluateList(NEVariables variables, int? rowCount = null, string unit = null) => EvaluateList<object>(variables, rowCount, unit);

		T ChangeType<T>(object value)
		{
			if (value == null)
				return default(T);
			if (typeof(T) == typeof(object))
				return (T)value;
			if (typeof(T) == typeof(string))
				return (T)(object)value.ToString();
			return (T)Convert.ChangeType(value, typeof(T));
		}

		public List<T> EvaluateList<T>(NEVariables variables, int? rowCount = null, string unit = null)
		{
			try
			{
				if (rowCount < 0)
					throw new ArgumentException($"{nameof(rowCount)} must be positive");
				var count = rowCount ?? variables.ResultCount(Variables);
				Func<int, T> action = row => ChangeType<T>(InternalEvaluate(variables, row, unit));
				if (count == 1)
					return new List<T> { action(0) };
				return Enumerable.Range(0, count).AsParallel().AsOrdered().Select(action).ToList();
			}
			catch (AggregateException ex) { throw ex.InnerException ?? ex; }
		}

		HashSet<string> variables;
		public HashSet<string> Variables => variables = variables ?? VariableFinder.GetVariables(tree);
	}
}
