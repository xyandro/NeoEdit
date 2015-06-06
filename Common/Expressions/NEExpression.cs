using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Expressions.Parser;
using NeoEdit.Common.Transform;

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
			return EvaluateDict(null, values);
		}

		public object EvaluateDict(Dictionary<string, List<string>> dict, int row, params object[] values)
		{
			var rowDict = dict.ToDictionary(entry => entry.Key, entry => entry.Value.Count > row ? (object)entry.Value[row] : null);
			return EvaluateDict(rowDict, values);
		}

		public object EvaluateDict(Dictionary<string, object> dict, params object[] values)
		{
			return new ExpressionEvaluator(expression, dict, values).Visit(tree);
		}

		List<string> variables;
		public List<string> Variables { get { return variables = variables ?? VariableFinder.GetVariables(tree); } }

		[XMLConverter.ToXML]
		object ToXML() { return expression; }
		[XMLConverter.FromXML]
		static NEExpression FromXML(string expression) { return new NEExpression(expression); }
	}
}
