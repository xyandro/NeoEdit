using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Parsing;
using NeoEdit.TextEdit.RevRegEx.Parser;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExVisitor : RevRegExParserBaseVisitor<RevRegExData>
	{
		public static RevRegExData Parse(string input)
		{
			var tree = ParserHelper.Parse<RevRegExLexer, RevRegExParser, RevRegExParser.RevregexContext>(input, parser => parser.revregex(), true);
			return new RevRegExVisitor().Visit(tree);
		}

		public override RevRegExData VisitRevregex(RevRegExParser.RevregexContext context) => Visit(context.items());
		public override RevRegExData VisitItems(RevRegExParser.ItemsContext context) => RevRegExDataJoin.Create(context.itemsList().Select(item => Visit(item)));
		public override RevRegExData VisitItemsList(RevRegExParser.ItemsListContext context) => RevRegExDataList.Create(context.item().Select(item => Visit(item)));
		public override RevRegExData VisitParens(RevRegExParser.ParensContext context) => Visit(context.items());
		public override RevRegExData VisitOptional(RevRegExParser.OptionalContext context) => new RevRegExDataRepeat(Visit(context.item()), 0, 1);
		public override RevRegExData VisitRepeat(RevRegExParser.RepeatContext context)
		{
			var max = int.Parse(context.maxcount.Text);
			var min = context.COMMA() == null ? max : context.mincount == null ? 0 : int.Parse(context.mincount.Text);
			return new RevRegExDataRepeat(Visit(context.item()), min, max);
		}
		public override RevRegExData VisitChar(RevRegExParser.CharContext context) => new RevRegExDataChar(context.val.Text.Last());
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
		public override RevRegExData VisitRangeChar(RevRegExParser.RangeCharContext context) => new RevRegExDataChar(context.val.Text.Last());
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
