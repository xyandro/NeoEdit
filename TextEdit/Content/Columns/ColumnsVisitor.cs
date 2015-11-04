using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.Content.Columns.Parser;

namespace NeoEdit.TextEdit.Content.Columns
{
	class ColumnsVisitor : ColumnsBaseVisitor<ParserNode>
	{
		public class ColumnsErrorListener : IAntlrErrorListener<IToken>
		{
			public void SyntaxError(IRecognizer recognizer, IToken token, int line, int pos, string msg, RecognitionException e) { throw new Exception($"Error: Token mistmatch at position {token.StartIndex}"); }
		}

		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new ColumnsLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new ColumnsParser(tokens);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new ColumnsErrorListener());
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			ColumnsParser.ColumnsContext tree;
			try
			{
				tree = parser.columns();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.columns();
			}

			return new ColumnsVisitor().Visit(tree);
		}

		const string ROOT = "Root";
		const string LINE = "Line";
		const string ITEM = "Item";

		ParserNode GetNode(ParserRuleContext context, string type, IEnumerable<ParserRuleContext> nodes)
		{
			var node = new ParserNode { Type = type, LocationParserRule = context };
			if (nodes != null)
				foreach (var child in nodes)
					Visit(child).Parent = node;
			return node;
		}

		public override ParserNode VisitColumns(ColumnsParser.ColumnsContext context) => GetNode(context, ROOT, context.line());
		public override ParserNode VisitLine(ColumnsParser.LineContext context) => Visit(context.items());
		public override ParserNode VisitItems(ColumnsParser.ItemsContext context) => GetNode(context, LINE, context.itemws());
		public override ParserNode VisitItemws(ColumnsParser.ItemwsContext context) => Visit(context.item());
		public override ParserNode VisitItem(ColumnsParser.ItemContext context) => GetNode(context, ITEM, null);
	}
}
