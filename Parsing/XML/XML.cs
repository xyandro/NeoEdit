using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace NeoEdit.Parsing
{
	static class XML
	{
		public static ParserNode Parse(string data)
		{
			var input = new AntlrInputStream(data);
			var lexer = new XMLLexer(input);
			var tokens = new CommonTokenStream(lexer);
			var parser = new XMLParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			XMLParser.DocumentContext tree;
			try
			{
				tree = parser.document();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.document();
			}

			return XMLVisitor.Parse(data, tree);
		}
	}
}
