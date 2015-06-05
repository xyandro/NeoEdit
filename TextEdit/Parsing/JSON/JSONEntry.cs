using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.Parsing.JSON.Parser;

namespace NeoEdit.TextEdit.Parsing.JSON
{
	static class JSONEntry
	{
		public static ParserNode Parse(string data)
		{
			var input = new AntlrInputStream(data);
			var lexer = new JSONLexer(input);
			var tokens = new CommonTokenStream(lexer);
			var parser = new JSONParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			JSONParser.JsonContext tree;
			try
			{
				tree = parser.json();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.json();
			}

			return JSONVisitor.Parse(data, tree);
		}
	}
}
