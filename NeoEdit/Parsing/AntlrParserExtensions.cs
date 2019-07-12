using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace NeoEdit.Program.Parsing
{
	public static class AntlrParserExtensions
	{
		public static void GetBounds(this ParserRuleContext ctx, out int start, out int end)
		{
			start = ctx.Start.StartIndex;
			end = Math.Max(start, (ctx.Stop?.StopIndex + 1) ?? 0);
		}

		public static void GetBounds(this ITerminalNode token, out int start, out int end) => token.Symbol.GetBounds(out start, out end);

		public static void GetBounds(this IToken token, out int start, out int end)
		{
			start = token.StartIndex;
			end = token.StopIndex + 1;
		}

		public static string GetText(this ParserRuleContext ctx, string input)
		{
			if (input == null)
				return null;
			int start, end;
			ctx.GetBounds(out start, out end);
			return input.Substring(start, end - start);
		}

		public static string GetText(this ITerminalNode token, string input)
		{
			if (input == null)
				return null;
			int start, end;
			token.GetBounds(out start, out end);
			return input.Substring(start, end - start);
		}

		public static T Cast<T>(this object input) where T : class => input as T;

		public static ResultType ParentAs<ResultType>(this ParserRuleContext input) where ResultType : ParserRuleContext
		{
			if ((input == null) || (input.Parent == null))
				return default(ResultType);
			return input.Parent as ResultType;
		}

		public static ResultType Take<InputType, ResultType>(this InputType input, Func<InputType, ResultType> func) where InputType : ParserRuleContext => input == null ? default(ResultType) : func(input);

		public static ResultType[] Take<InputType, ResultType>(this InputType[] input, Func<InputType, ResultType> func) where InputType : ParserRuleContext
		{
			if (input == null)
				return new ResultType[0];

			return input.Select(item => func(item)).ToArray();
		}

		public static void Do<InputType>(this InputType input, Action<InputType> action) where InputType : IParseTree
		{
			if (input == null)
				return;

			action(input);
		}

		public static void Do<InputType>(this InputType[] input, Action<InputType> action) where InputType : IParseTree
		{
			if (input == null)
				return;

			foreach (var item in input)
				action(item);
		}
	}
}
