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

		internal object InternalEvaluate(NEVariables variables, int row, params object[] values) => new ExpressionEvaluator(expression, variables, row, values).Visit(tree);

		public object Evaluate(params object[] values) => EvaluateRow(null, values);
		public T Evaluate<T>(params object[] values) => EvaluateRow<T>(null, values);

		public object EvaluateRow(NEVariables variables, params object[] values) => EvaluateRows(variables, 1, values)[0];
		public T EvaluateRow<T>(NEVariables variables, params object[] values) => EvaluateRows<T>(variables, 1, values)[0];

		public List<object> EvaluateRows(NEVariables variables, int? rowCount = null, params object[] values) => EvaluateRows<object>(variables, rowCount, values);

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

		public List<T> EvaluateRows<T>(NEVariables variables, int? rowCount = null, params object[] values)
		{
			try
			{
				if (rowCount < 0)
					throw new ArgumentException($"{nameof(rowCount)} must be positive");
				var count = rowCount ?? variables.ResultCount(Variables);
				Func<int, T> action = row => ChangeType<T>(InternalEvaluate(variables, row, values));
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
