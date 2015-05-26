using System;
using Antlr4.Runtime;

namespace NeoEdit.Parsing
{
	static class Helpers
	{
		public static void GetBounds(this ParserRuleContext ctx, out int start, out int end)
		{
			start = ctx.Start.StartIndex;
			end = Math.Max(start, ctx.Stop == null ? 0 : ctx.Stop.StopIndex + 1);
		}

		public static string GetText(this ParserRuleContext ctx, string input)
		{
			if (input == null)
				return null;
			int start, end;
			ctx.GetBounds(out start, out end);
			return input.Substring(start, end - start);
		}
	}
}
