using System;
using System.Diagnostics;
using System.Linq;
using NeoEdit.CommandLine;
using NeoEdit.Common;

namespace NeoEdit
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var commandLine = string.Join(" ", args.Select(str => $"\"{str}\""));
			var commandLineParams = CommandLineVisitor.GetCommandLineParams(commandLine);
			if ((commandLineParams.Admin) && (!Helpers.IsAdministrator))
			{
				new Process()
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Helpers.GetEntryExe(),
						Arguments = commandLine,
						UseShellExecute = true,
						Verb = "runas",
					},
				}.Start();
				return;
			}

			App.RunProgram(commandLineParams);
		}
	}
}
