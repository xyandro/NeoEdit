using System.Collections.Generic;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.CPlusPlus.Parser;

namespace NeoEdit.TextEdit.Content.CPlusPlus
{
	class CPlusPlusVisitor : CPlusPlusParserBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var tree = ParserHelper.Parse<CPlusPlusLexer, CPlusPlusParser, CPlusPlusParser.CplusplusContext>(input, parser => parser.cplusplus());
			var visitor = new CPlusPlusVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		readonly ParserNode Root;
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		CPlusPlusVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = "Root", Start = 0, End = input.Length });
		}
	}
}
