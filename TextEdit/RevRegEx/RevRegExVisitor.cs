using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.RevRegEx.Parser;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExVisitor : RevRegExParserBaseVisitor<RevRegExData>
	{
		public static RevRegExData Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new RevRegExLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);

			var parser = new RevRegExParser(tokens);
			parser.ErrorHandler = new BailErrorStrategy();
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			RevRegExParser.RevregexContext tree;
			try
			{
				tree = parser.revregex();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.revregex();
			}

			return new RevRegExVisitor().Visit(tree);
		}

		public override RevRegExData VisitRevregex(RevRegExParser.RevregexContext context) { return Visit(context.items()); }
		public override RevRegExData VisitItems(RevRegExParser.ItemsContext context) { return RevRegExDataJoin.Create(context.itemsList().Select(item => Visit(item))); }
		public override RevRegExData VisitItemsList(RevRegExParser.ItemsListContext context) { return RevRegExDataList.Create(context.item().Select(item => Visit(item))); }
		public override RevRegExData VisitParens(RevRegExParser.ParensContext context) { return Visit(context.items()); }
		public override RevRegExData VisitRepeat(RevRegExParser.RepeatContext context)
		{
			var max = int.Parse(context.maxcount.Text);
			var min = context.COMMA() == null ? max : context.mincount == null ? 0 : int.Parse(context.mincount.Text);
			return new RevRegExDataRepeat(Visit(context.item()), min, max);
		}
		public override RevRegExData VisitChar(RevRegExParser.CharContext context) { return new RevRegExDataChar(context.val.Text.Last()); }
		public override RevRegExData VisitRange(RevRegExParser.RangeContext context)
		{
			var list = new List<RevRegExDataChar>();
			if (context.HYPHEN() != null)
				list.Add(new RevRegExDataChar('-'));
			foreach (var item in context.rangeItem().Select(item => Visit(item)))
			{
				if (item is RevRegExDataChar)
					list.Add(item as RevRegExDataChar);
				if (item is RevRegExDataJoin)
					list.AddRange((item as RevRegExDataJoin).List.Cast<RevRegExDataChar>());
			}
			return RevRegExDataJoin.Create(list);
		}
		public override RevRegExData VisitRangeChar(RevRegExParser.RangeCharContext context) { return new RevRegExDataChar(context.val.Text.Last()); }
		public override RevRegExData VisitRangeStartEnd(RevRegExParser.RangeStartEndContext context)
		{
			var start = (Visit(context.startchar) as RevRegExDataChar).Value;
			var end = (Visit(context.endchar) as RevRegExDataChar).Value;
			if (start > end)
			{
				var tmp = start;
				start = end;
				end = tmp;
			}
			var chars = new List<RevRegExDataChar>();
			for (var ch = start; ch <= end; ++ch)
				chars.Add(new RevRegExDataChar(ch));
			return RevRegExDataJoin.Create(chars);
		}
	}
}
