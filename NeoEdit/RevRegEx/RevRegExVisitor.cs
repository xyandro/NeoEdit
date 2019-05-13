using System.Collections.Generic;
using System.Linq;
using NeoEdit.Parsing;
using NeoEdit.RevRegEx.Parser;

namespace NeoEdit.RevRegEx
{
	class RevRegExVisitor : RevRegExParserBaseVisitor<RevRegExData>
	{
		public static RevRegExData Parse(string input, int infiniteCount)
		{
			var tree = ParserHelper.Parse<RevRegExLexer, RevRegExParser, RevRegExParser.RevregexContext>(input, parser => parser.revregex(), true);
			return new RevRegExVisitor(infiniteCount).Visit(tree);
		}

		readonly int infiniteCount;
		public RevRegExVisitor(int infiniteCount)
		{
			this.infiniteCount = infiniteCount;
		}

		public override RevRegExData VisitRevregex(RevRegExParser.RevregexContext context) => Visit(context.items());
		public override RevRegExData VisitItems(RevRegExParser.ItemsContext context) => RevRegExDataJoin.Create(context.itemsList().Select(item => Visit(item)).ToList());
		public override RevRegExData VisitItemsList(RevRegExParser.ItemsListContext context) => RevRegExDataList.Create(context.item().Select(item => Visit(item)).ToList());
		public override RevRegExData VisitParens(RevRegExParser.ParensContext context) => Visit(context.items());
		public override RevRegExData VisitRepeat(RevRegExParser.RepeatContext context)
		{
			var min = 0;
			var max = infiniteCount;
			if (context.question != null)
				max = 1;
			else if (context.asterisk != null)
			{ }
			else if (context.plus != null)
				min = 1;
			else if (context.count != null)
				min = max = int.Parse(context.count.Text);
			else
			{
				min = context.mincount == null ? min : int.Parse(context.mincount.Text);
				max = context.maxcount == null ? max : int.Parse(context.maxcount.Text);
			}
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
