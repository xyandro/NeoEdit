using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.Parsing.CSharp.Parser;

namespace NeoEdit.TextEdit.Parsing.CSharp
{
	static class CSharpEntry
	{
		public static ParserNode Parse(string data)
		{
			var input = new AntlrInputStream(data);
			var lexer = new CSharp4Lexer(input);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CSharp4Parser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			CSharp4Parser.Compilation_unitContext tree;
			try
			{
				tree = parser.compilation_unit();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.compilation_unit();
			}

			return CSharpVisitor.Parse(data, tree);
		}
	}
}
