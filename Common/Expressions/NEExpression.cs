using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
{
	public class NEExpression
	{
		readonly string expression;
		readonly ExpressionParser.ExprContext tree;
		public NEExpression(string expression)
		{
			this.expression = expression;

			try
			{
				var input = new AntlrInputStream(expression);
				var lexer = new ExpressionLexer(input);

				var tokens = new CommonTokenStream(lexer);
				var parser = new ExpressionParser(tokens);
				parser.ErrorHandler = new BailErrorStrategy();

				parser.Interpreter.PredictionMode = PredictionMode.Sll;

				try
				{
					tree = parser.expr();
				}
				catch (RecognitionException)
				{
					tokens.Reset();
					parser.Reset();
					parser.Interpreter.PredictionMode = PredictionMode.Ll;
					tree = parser.expr();
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Invalid expression", ex);
			}
		}

		internal ExpressionResult InternalEvaluate(NEVariables variables, int row, params object[] values) => new ExpressionEvaluator(expression, variables, row, values).Visit(tree);

		public object Evaluate(params object[] values) => EvaluateRow(null, values);
		public T Evaluate<T>(params object[] values) => EvaluateRow<T>(null, values);

		public object EvaluateRow(NEVariables variables, params object[] values) => EvaluateRows(variables, 1, values)[0];
		public T EvaluateRow<T>(NEVariables variables, params object[] values) => EvaluateRows<T>(variables, 1, values)[0];

		public List<object> EvaluateRows(NEVariables variables, int? rowCount = null, params object[] values)
		{
			if (rowCount < 0)
				throw new ArgumentException($"{nameof(rowCount)} must be positive");

			if (!Variables.Any())
				return Enumerable.Repeat(InternalEvaluate(null, 0, values).GetResult(), rowCount ?? 1).ToList();

			variables.Prepare(this, rowCount);
			return Enumerable.Range(0, variables.RowCount).AsParallel().AsOrdered().Select(row => InternalEvaluate(variables, row, values).GetResult()).ToList();
		}

		public List<T> EvaluateRows<T>(NEVariables variables, int? rowCount = null, params object[] values)
		{
			if (rowCount < 0)
				throw new ArgumentException($"{nameof(rowCount)} must be positive");

			if (!Variables.Any())
				return Enumerable.Repeat((T)Convert.ChangeType(InternalEvaluate(null, 0, values).GetResult(), typeof(T)), rowCount ?? 1).ToList();

			variables.Prepare(this, rowCount);
			return Enumerable.Range(0, variables.RowCount).AsParallel().AsOrdered().Select(row => (T)Convert.ChangeType(InternalEvaluate(variables, row, values).GetResult(), typeof(T))).ToList();
		}

		HashSet<string> variables;
		public HashSet<string> Variables => variables = variables ?? VariableFinder.GetVariables(tree);
	}
}
