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

		public object EvaluateInterpret(params string[] values)
		{
			return EvaluateDictInterpret(null, values);
		}

		public object EvaluateDictInterpret(Dictionary<string, List<string>> dict, int row, params string[] values)
		{
			var rowDict = dict.ToDictionary(entry => entry.Key, entry => entry.Value.Count > row ? entry.Value[row] : null);
			return EvaluateDictInterpret(rowDict, values);
		}

		object InterpretValue(string str)
		{
			bool boolVal;
			if (bool.TryParse(str, out boolVal))
				return boolVal;

			long longVal;
			if (long.TryParse(str, out longVal))
				return longVal;

			double doubleVal;
			if (double.TryParse(str, out doubleVal))
				return doubleVal;

			return str;
		}

		public object EvaluateDictInterpret(Dictionary<string, string> dict, params string[] values)
		{
			var objDict = dict.ToDictionary(pair => pair.Key, pair => InterpretValue(pair.Value));
			var objValues = values.Select(value => InterpretValue(value)).ToArray();
			return new ExpressionEvaluator(expression, objDict, objValues).Visit(tree);
		}

		List<string> variables;
		public List<string> Variables { get { return variables = variables ?? VariableFinder.GetVariables(tree); } }

		[XMLConverter.ToXML]
		object ToXML() { return expression; }
		[XMLConverter.FromXML]
		static NEExpression FromXML(string expression) { return new NEExpression(expression); }
	}
}
