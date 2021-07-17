using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NeoEdit.Common.Expressions.Parser;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	public class NEExpression
	{
		public string Expression { get; }
		public List<NEVariableUse> VariableUses { get; }
		public string DefaultUnit { get; set; }

		readonly ExpressionParser.ExprContext tree;

		public NEExpression(string expression)
		{
			Expression = expression;

			try { tree = ParserHelper.Parse<ExpressionLexer, ExpressionParser, ExpressionParser.ExprContext>(expression, parser => parser.expr(), true); }
			catch (Exception ex) { throw new Exception($"Invalid expression: {expression}", ex); }

			VariableUses = NEExpressionVariableFinder.GetVariables(tree);
		}

		T ChangeType<T>(object value)
		{
			if (value == null)
				return default(T);
			if (typeof(T) == typeof(object))
				return (T)value;
			if (typeof(T) == typeof(string))
				return (T)(object)value.ToString();
			if ((typeof(T) == typeof(double)) && (value is BigInteger v))
				return (T)(object)(double)v;
			return (T)Convert.ChangeType(value, typeof(T));
		}

		public T EvaluateOne<T>(NEVariables variables = null) => Evaluate<T>(variables, 0, 1, 1)[0];

		public List<T> Evaluate<T>(NEVariables variables = null, int? rowCount = null)
		{
			var count = rowCount ?? variables.RowCount(VariableUses);
			return Evaluate<T>(variables, 0, count, count);
		}

		public List<T> Evaluate<T>(NEVariables variables, int startRow, int count, int rowCount)
		{
			if (rowCount < 0)
				throw new ArgumentException($"{nameof(rowCount)} invalid ({rowCount})");
			if ((startRow < 0) || (startRow > rowCount))
				throw new ArgumentException($"{nameof(startRow)} invalid (0 <= {startRow} <= {rowCount})");
			if ((count < 0) || (startRow + count > rowCount))
				throw new ArgumentException($"{nameof(count)} invalid (0 <= {count} <= {rowCount})");

			if (rowCount != variables.RowCount(VariableUses, rowCount))
				throw new Exception("Invalid row count");

			try { return Enumerable.Range(0, count).Select(row => ChangeType<T>(new NEExpressionEvaluator(Expression, variables, row, rowCount, DefaultUnit).Visit(tree))).ToList(); }
			catch (AggregateException ex) { throw ex.InnerException ?? ex; }
		}

		public override string ToString() => Expression;
	}
}
