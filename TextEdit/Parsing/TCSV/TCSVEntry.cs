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

			CSVParser.CsvContext tree;
			try
			{
				tree = parser.csv();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.csv();
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

			TSVParser.TsvContext tree;
			try
			{
				tree = parser.tsv();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.tsv();
			}

			return TSVVisitor.Parse(data, tree);
		}
	}
}
