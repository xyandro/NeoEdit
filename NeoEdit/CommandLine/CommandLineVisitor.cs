﻿using System;
using System.IO;
using Antlr4.Runtime.Misc;
using NeoEdit.CommandLine.Parser;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;

namespace NeoEdit.CommandLine
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

		public override object VisitAdmin([NotNull] CommandLineParser.AdminContext context)
		{
			clParams.Admin = true;
			return base.VisitAdmin(context);
		}

		public override object VisitBackground([NotNull] CommandLineParser.BackgroundContext context)
		{
			clParams.Background = true;
			return base.VisitBackground(context);
		}

		public override object VisitDebug([NotNull] CommandLineParser.DebugContext context)
		{
			clParams.Debug = true;
			return base.VisitDebug(context);
		}

		public override object VisitExisting([NotNull] CommandLineParser.ExistingContext context)
		{
			clParams.Existing = true;
			return base.VisitExisting(context);
		}

		public override object VisitDiff([NotNull] CommandLineParser.DiffContext context)
		{
			clParams.Diff = true;
			return base.VisitDiff(context);
		}

		public override object VisitFile([NotNull] CommandLineParser.FileContext context)
		{
			var fileName = Path.GetFullPath(context.filename.GetText());
			if (!File.Exists(fileName))
				throw new Exception($"File doesn't exist: {fileName}");

			fileName = Path.GetFullPath(fileName);
			clParams.Files.Add(new CommandLineParams.File
			{
				FileName = fileName,
				DisplayName = context.display?.GetText(),
				Line = context.line == null ? default(int?) : int.Parse(context.line.Text),
				Column = context.column == null ? default(int?) : int.Parse(context.column.Text),
				Index = context.index == null ? default(int?) : int.Parse(context.index.Text),
			});
			return base.VisitFile(context);
		}

		public override object VisitWait([NotNull] CommandLineParser.WaitContext context)
		{
			clParams.Wait = "";
			return base.VisitWait(context);
		}

		public override object VisitWaitpid([NotNull] CommandLineParser.WaitpidContext context)
		{
			clParams.WaitPID = int.Parse(context.pid.Text);
			return base.VisitWaitpid(context);
		}
	}
}
