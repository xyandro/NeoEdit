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
		public static List<Param> GetCommandLineParams()
		{
			var input = Environment.CommandLine;
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
		public override object VisitDbviewer(CommandLineParamsParser.DbviewerContext context) { return new DBViewerParam(); }
		public override object VisitGunzip(CommandLineParamsParser.GunzipContext context) { return new GUnZipParam(context.input.GetText(), context.output.GetText()); }
		public override object VisitGzip(CommandLineParamsParser.GzipContext context) { return new GZipParam(context.input.GetText(), context.output.GetText()); }
		public override object VisitSysteminfo(CommandLineParamsParser.SysteminfoContext context) { return new SystemInfoParam(); }

		public override object VisitConsolerunner(CommandLineParamsParser.ConsolerunnerContext context)
		{
			var param = new ConsoleRunnerParam();
			foreach (var file in context.param())
				param.AddParam(file.GetText());
			return param;
		}

		public override object VisitDisk(CommandLineParamsParser.DiskContext context)
		{
			var param = new DiskParam();
			if (context.file != null)
				param.Location = context.file.GetText();
			return param;
		}

		public override object VisitHandles(CommandLineParamsParser.HandlesContext context)
		{
			var param = new HandlesParam();
			if (context.pid != null)
				param.PID = int.Parse(context.pid.Text);
			return param;
		}

		public override object VisitHexdump(CommandLineParamsParser.HexdumpContext context)
		{
			var param = new HexDumpParam();
			foreach (var file in context.param())
				param.AddFile(file.GetText());
			return param;
		}

		public override object VisitHexedit(CommandLineParamsParser.HexeditContext context)
		{
			var param = new HexEditParam();
			foreach (var file in context.param())
				param.AddFile(file.GetText());
			return param;
		}

		public override object VisitHexpid(CommandLineParamsParser.HexpidContext context)
		{
			var param = new HexPidParam();
			foreach (var pid in context.NUMBER())
				param.AddPID(int.Parse(pid.GetText()));
			return param;
		}

		public override object VisitProcesses(CommandLineParamsParser.ProcessesContext context)
		{
			var param = new ProcessesParam();
			if (context.pid != null)
				param.PID = int.Parse(context.pid.Text);
			return param;
		}

		public override object VisitRegistry(CommandLineParamsParser.RegistryContext context)
		{
			var param = new RegistryParam();
			if (context.key != null)
				param.Key = context.key.GetText();
			return param;
		}

		public override object VisitTextedit(CommandLineParamsParser.TexteditContext context)
		{
			var param = new TextEditParam();
			foreach (var textEditFile in context.texteditfile())
				param.AddFile(VisitTexteditfile(textEditFile) as TextEditParam.TextEditFile);
			return param;
		}

		public override object VisitTexteditfile(CommandLineParamsParser.TexteditfileContext context)
		{
			var param = new TextEditParam.TextEditFile(context.file.GetText());
			if (context.line != null)
				param.Line = int.Parse(context.line.Text);
			if (context.column != null)
				param.Column = int.Parse(context.column.Text);
			return param;
		}

		public override object VisitTextview(CommandLineParamsParser.TextviewContext context)
		{
			var param = new TextViewParam();
			foreach (var file in context.param())
				param.AddFile(file.GetText());
			return param;
		}
	}
}
