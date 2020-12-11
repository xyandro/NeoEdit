using System;
using System.Linq;
using NeoEdit.CommandLine;

namespace NeoEdit
{
	class Program
	{
		[STAThread]
		static void Main(string[] args) => App.RunProgram(CommandLineVisitor.GetCommandLineParams(string.Join(" ", args.Select(str => $"\"{str}\""))));
	}
}
