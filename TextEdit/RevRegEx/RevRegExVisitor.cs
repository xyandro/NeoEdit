using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using NeoEdit.TextEdit.RevRegEx.Parser;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExVisitor : RevRegExBaseVisitor<List<string>>
	{
		public static List<string> Parse(string input)
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

		public override List<string> VisitRevregex([NotNull] RevRegExParser.RevregexContext context) { return Visit(context.items()); }
		public override List<string> VisitParens([NotNull] RevRegExParser.ParensContext context) { return Visit(context.items()); }
		public override List<string> VisitItems([NotNull] RevRegExParser.ItemsContext context) { return context.item().SelectMany(item => Visit(item)).ToList(); }

		public override List<string> VisitRepeat([NotNull] RevRegExParser.RepeatContext context)
		{
			var data = Visit(context.item());
			var count = int.Parse(context.count.GetText());
			return Enumerable.Range(0, count).SelectMany(ctr => data).ToList();
		}

		public override List<string> VisitRange([NotNull] RevRegExParser.RangeContext context)
		{
			var chars = new List<char>();
			if (context.HYPHEN() != null)
				chars.Add('-');
			foreach (var rangeitem in context.rangeItem())
			{
				var item = Visit(rangeitem);
				chars.AddRange(item[0]);
			}
			return new List<string> { new string(chars.ToArray()) };
		}
		public override List<string> VisitChar([NotNull] RevRegExParser.CharContext context) { return new List<string> { context.charval.Text }; }
		public override List<string> VisitRangeChar([NotNull] RevRegExParser.RangeCharContext context) { return new List<string> { context.charval.Text }; }
		public override List<string> VisitRangeStartEnd([NotNull] RevRegExParser.RangeStartEndContext context)
		{
			var start = Visit(context.startchar)[0][0];
			var end = Visit(context.endchar)[0][0];
			if (start > end)
			{
				var tmp = start;
				start = end;
				end = tmp;
			}
			var chars = new List<char>();
			for (var ch = start; ch <= end; ++ch)
				chars.Add(ch);
			return new List<string> { new string(chars.ToArray()) };
		}
	}
}
