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

			var input = new AntlrInputStream(expression);
			var lexer = new ExpressionLexer(input);

			var tokens = new CommonTokenStream(lexer);
			var parser = new ExpressionParser(tokens);
			parser.ErrorHandler = new BailErrorStrategy();

			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			try
			{
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

		public object Evaluate(params object[] values)
		{
			return Evaluate(default(Dictionary<string, object>), values);
		}

		public T Evaluate<T>(params object[] values)
		{
			return (T)Convert.ChangeType(Evaluate(values), typeof(T));
		}

		public object EvaluateRow(Dictionary<string, List<object>> dict, int row, params object[] values)
		{
			var rowDict = dict.ToDictionary(entry => entry.Key, entry => entry.Value.Count > row ? entry.Value[row] : null);
			return Evaluate(rowDict, values);
		}

		public T EvaluateRow<T>(Dictionary<string, List<object>> dict, int row, params object[] values)
		{
			return (T)Convert.ChangeType(EvaluateRow(dict, row, values), typeof(T));
		}

		public object Evaluate(Dictionary<string, object> dict, params object[] values)
		{
			return InternalEvaluate(dict, values).GetResult();
		}

		internal ExpressionResult InternalEvaluate(Dictionary<string, object> dict, params object[] values)
		{
			return new ExpressionEvaluator(expression, dict, values).Visit(tree);
		}

		public List<object> Evaluate(Dictionary<string, List<object>> dict, params object[] values)
		{
			var count = 1;
			if (Variables.Any())
				count = Variables.Max(var => dict[var].Count);
			return Enumerable.Range(0, count).Select(row => EvaluateRow(dict, row, values)).ToList();
		}

		public List<T> Evaluate<T>(Dictionary<string, List<object>> dict, params object[] values)
		{
			return Evaluate(dict, values).Select(val => (T)System.Convert.ChangeType(val, typeof(T))).ToList();
		}

		HashSet<string> variables;
		public HashSet<string> Variables { get { return variables = variables ?? VariableFinder.GetVariables(tree); } }
	}
}
