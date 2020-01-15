using System;
using Antlr4.Runtime.Misc;
using NeoEdit.Program.CommandLine.Parser;
using NeoEdit.Program.Parsing;

namespace NeoEdit.Program.CommandLine
{
	class CommandLineVisitor : CommandLineParserBaseVisitor<object>
	{
		public static CommandLineParams GetCommandLineParams(string input)
		{
			try
			{
				var tree = ParserHelper.Parse<CommandLineLexer, CommandLineParser, CommandLineParser.ExprContext>(input, parser => parser.expr(), true);
				var visitor = new CommandLineVisitor();
				visitor.Visit(tree);
				return visitor.clParams;
			}
			catch (ParserException ex) { throw new Exception($"Invalid command line at position {ex.Pos}: {ex.Msg}"); }
		}

		CommandLineParams clParams = new CommandLineParams();

		public override object VisitBackground([NotNull] CommandLineParser.BackgroundContext context)
		{
			clParams.Background = true;
			return base.VisitBackground(context);
		}

		public override object VisitDiff([NotNull] CommandLineParser.DiffContext context)
		{
			clParams.Diff = true;
			return base.VisitDiff(context);
		}

		public override object VisitFile([NotNull] CommandLineParser.FileContext context)
		{
			clParams.Files.Add(new CommandLineParams.File
			{
				FileName = context.filename.GetText(),
				DisplayName = context.display?.GetText(),
				Line = context.line == null ? 1 : int.Parse(context.line.Text),
				Column = context.column == null ? 1 : int.Parse(context.column.Text),
			});
			return base.VisitFile(context);
		}

		public override object VisitWait([NotNull] CommandLineParser.WaitContext context)
		{
			clParams.Wait = context.guid?.Text;
			return base.VisitWait(context);
		}
	}
}
