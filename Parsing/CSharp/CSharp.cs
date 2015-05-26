using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace NeoEdit
{
	namespace Parsing
	{
		public static class CSharp
		{
			public static CSharpNode Parse(string data)
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

				var visitor = new CSharpVisitor(data);
				visitor.Visit(tree);
				return visitor.Root;
			}
		}
	}
}
