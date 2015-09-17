using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.CommandLineParams.Parser;

namespace NeoEdit.CommandLineParams
{
	class CommandLineVisitor : CommandLineParamsParserBaseVisitor<object>
	{
		public static List<Param> GetCommandLineParams(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new CommandLineParamsLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new CommandLineParamsParser(tokens);
			parser.ErrorHandler = new BailErrorStrategy();
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			CommandLineParamsParser.ExprContext tree;
			try
			{
				tree = parser.expr();
			}
			catch
			{
				try
				{
					tokens.Reset();
					parser.Reset();
					parser.Interpreter.PredictionMode = PredictionMode.Ll;
					tree = parser.expr();
				}
				catch { throw new Exception("Invalid command line"); }
			}

			var visitor = new CommandLineVisitor();
			return visitor.Visit(tree) as List<Param>;
		}

		public override object VisitExpr(CommandLineParamsParser.ExprContext context) { return context.parameter().Select(parameter => VisitParameter(parameter)).Where(param => param != null).Cast<Param>().ToList(); }
		public override object VisitAbout(CommandLineParamsParser.AboutContext context) { return new AboutParam(); }
		public override object VisitConsole(CommandLineParamsParser.ConsoleContext context) { return new ConsoleParam(); }
		public override object VisitConsolerunner(CommandLineParamsParser.ConsolerunnerContext context) { return new ConsoleRunnerParam(context.param().Select(param => param.GetText()).ToArray()); }
		public override object VisitDiff(CommandLineParamsParser.DiffContext context) { return new DiffParam(context.file1 == null ? null : context.file1.GetText(), context.file2 == null ? null : context.file2.GetText()); }
		public override object VisitDisk(CommandLineParamsParser.DiskContext context) { return new DiskParam(context.file == null ? null : context.file.GetText()); }
		public override object VisitGunzip(CommandLineParamsParser.GunzipContext context) { return new GUnZipParam(context.input.GetText(), context.output.GetText()); }
		public override object VisitGzip(CommandLineParamsParser.GzipContext context) { return new GZipParam(context.input.GetText(), context.output.GetText()); }
		public override object VisitHandles(CommandLineParamsParser.HandlesContext context) { return new HandlesParam(context.pid == null ? default(int?) : int.Parse(context.pid.Text)); }
		public override object VisitHexdump(CommandLineParamsParser.HexdumpContext context) { return new HexDumpParam(context.param().Select(file => file.GetText()).ToList()); }
		public override object VisitHexedit(CommandLineParamsParser.HexeditContext context) { return new HexEditParam(context.param().Select(file => file.GetText()).ToList()); }
		public override object VisitHexpid(CommandLineParamsParser.HexpidContext context) { return new HexPidParam(context.NUMBER().Select(pid => int.Parse(pid.GetText())).ToList()); }
		public override object VisitProcesses(CommandLineParamsParser.ProcessesContext context) { return new ProcessesParam(context.pid == null ? default(int?) : int.Parse(context.pid.Text)); }
		public override object VisitRegistry(CommandLineParamsParser.RegistryContext context) { return new RegistryParam(context.key == null ? null : context.key.GetText()); }
		public override object VisitSysteminfo(CommandLineParamsParser.SysteminfoContext context) { return new SystemInfoParam(); }
		public override object VisitTextedit(CommandLineParamsParser.TexteditContext context) { return new TextEditParam(context.texteditfile().Select(textEditFile => VisitTexteditfile(textEditFile) as TextEditParam.TextEditFile).ToList()); }
		public override object VisitTexteditfile(CommandLineParamsParser.TexteditfileContext context) { return new TextEditParam.TextEditFile(context.file.GetText(), context.line == null ? default(int?) : int.Parse(context.line.Text), context.column == null ? default(int?) : int.Parse(context.column.Text)); }
		public override object VisitTextview(CommandLineParamsParser.TextviewContext context) { return new TextViewParam(context.param().Select(file => file.GetText()).ToList()); }
	}
}
