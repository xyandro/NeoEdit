using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.Parsing.TCSV.Parser;

namespace NeoEdit.TextEdit.Parsing.TCSV
{
	static class TCSVEntry
	{
		public static ParserNode ParseCSV(string data)
		{
			var input = new AntlrInputStream(data);
			var lexer = new CSVLexer(input);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CSVParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			CSVParser.DocContext tree;
			try
			{
				tree = parser.doc();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.doc();
			}

			return CSVVisitor.Parse(data, tree);
		}

		public static ParserNode ParseTSV(string data)
		{
			var input = new AntlrInputStream(data);
			var lexer = new TSVLexer(input);
			var tokens = new CommonTokenStream(lexer);
			var parser = new TSVParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			TSVParser.DocContext tree;
			try
			{
				tree = parser.doc();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.doc();
			}

			return TSVVisitor.Parse(data, tree);
		}
	}
}
