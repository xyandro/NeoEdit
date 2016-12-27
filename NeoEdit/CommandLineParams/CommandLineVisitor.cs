﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.CommandLineParams.Parser;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;

namespace NeoEdit.CommandLineParams
{
	class CommandLineVisitor : CommandLineParamsParserBaseVisitor<object>
	{
		public static List<Param> GetCommandLineParams(string input)
		{
			try
			{
				var tree = ParserHelper.Parse<CommandLineParamsLexer, CommandLineParamsParser, CommandLineParamsParser.ExprContext>(input, parser => parser.expr(), true);
				return new CommandLineVisitor().Visit(tree) as List<Param>;
			}
			catch (ParserException ex) { throw new Exception($"Invalid command line at position {ex.Pos}: {ex.Msg}"); }
		}

		public override object VisitExpr(CommandLineParamsParser.ExprContext context) => context.parameter().Select(parameter => VisitParameter(parameter)).Where(param => param != null).Cast<Param>().ToList();
		public override object VisitAbout(CommandLineParamsParser.AboutContext context) => new AboutParam();
		public override object VisitDiff(CommandLineParamsParser.DiffContext context) => new DiffParam(context.texteditfile().Select(textEditFile => VisitTexteditfile(textEditFile) as TextEditParam.TextEditFile).Resize(2, null).ToList());
		public override object VisitDisk(CommandLineParamsParser.DiskContext context) => new DiskParam(context.file == null ? null : context.file.GetText());
		public override object VisitHandles(CommandLineParamsParser.HandlesContext context) => new HandlesParam(context.pid == null ? default(int?) : int.Parse(context.pid.Text));
		public override object VisitHexdump(CommandLineParamsParser.HexdumpContext context) => new HexDumpParam(context.param().Select(file => file.GetText()).ToList());
		public override object VisitHexedit(CommandLineParamsParser.HexeditContext context) => new HexEditParam(context.param().Select(file => file.GetText()).ToList());
		public override object VisitHexpid(CommandLineParamsParser.HexpidContext context) => new HexPidParam(context.NUMBER().Select(pid => int.Parse(pid.GetText())).ToList());
		public override object VisitImageedit(CommandLineParamsParser.ImageeditContext context) => new ImageEditParam(context.param().Select(file => file.GetText()).ToList());
		public override object VisitLicense(CommandLineParamsParser.LicenseContext context) => new LicenseParam();
		public override object VisitNetwork(CommandLineParamsParser.NetworkContext context) => new NetworkParam();
		public override object VisitProcesses(CommandLineParamsParser.ProcessesContext context) => new ProcessesParam(context.pid == null ? default(int?) : int.Parse(context.pid.Text));
		public override object VisitTextedit(CommandLineParamsParser.TexteditContext context) => new TextEditParam(context.texteditfile().Select(textEditFile => VisitTexteditfile(textEditFile) as TextEditParam.TextEditFile).ToList());
		public override object VisitTexteditfile(CommandLineParamsParser.TexteditfileContext context) => new TextEditParam.TextEditFile(context.file.GetText(), context.display?.GetText(), context.line == null ? default(int?) : int.Parse(context.line.Text), context.column == null ? default(int?) : int.Parse(context.column.Text));
		public override object VisitTextview(CommandLineParamsParser.TextviewContext context) => new TextViewParam(context.param().Select(file => file.GetText()).ToList());
		public override object VisitWait(CommandLineParamsParser.WaitContext context) => new WaitParam(context.guid?.Text);
	}
}
