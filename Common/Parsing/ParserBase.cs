using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace NeoEdit.Parsing
{
	public class ParserBase
	{
		public int? start, end;
		public int Start { get { return start.Value; } set { start = value; } }
		public int End { get { return end.Value; } set { end = value; } }
		public int Length => End - Start;
		public bool HasLocation => (start.HasValue) && (end.HasValue);

		public ParserRuleContext LocationParserRule
		{
			set
			{
				int lStart, lEnd;
				value.GetBounds(out lStart, out lEnd);
				start = lStart;
				end = lEnd;
			}
		}

		public ITerminalNode LocationTerminalNode
		{
			set
			{
				int lStart, lEnd;
				value.GetBounds(out lStart, out lEnd);
				start = lStart;
				end = lEnd;
			}
		}
	}
}
