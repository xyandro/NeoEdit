using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NeoEdit.CommandLine;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.Editor;
using NeoEdit.UI;
using Newtonsoft.Json;

namespace NeoEdit
{
	class Program
	{
		const string IPCName = "NeoEdit-{debe0282-0e9d-47fd-836c-60f500dbaeb5}";
		const string ShutdownEventName = "NeoEdit-Wait-{0}";

		static MemoryMappedFile mmfile;

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool AllowSetForegroundWindow(int dwProcessId);

		[STAThread]
		static void Main()
		{
			var commandLine = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(str => $"\"{str}\""));
			var commandLineParams = CommandLineParams.Parse(commandLine);
			HandleWaitPID(commandLineParams);

			var masterPid = default(int?);
			if (!commandLineParams.Multi)
				masterPid = GetMasterPID();
			if (!masterPid.HasValue)
			{
				if (!commandLineParams.Multi)
				{
					SetMasterPID();
					SetupPipeWait();
				}
				App.Run(() => CreateTabs(commandLineParams));
				return;
			}

			var proc = Process.GetProcessById(masterPid.Value);
			AllowSetForegroundWindow(proc.Id);

			// Server already exists; connect and send command line
			var waitEvent = default(EventWaitHandle);
			if (commandLineParams.Wait != null)
			{
				commandLineParams.Wait = string.Format(ShutdownEventName, Guid.NewGuid());
				waitEvent = new EventWaitHandle(false, EventResetMode.ManualReset, commandLineParams.Wait);
			}

			var pipeClient = new NamedPipeClientStream(".", IPCName, PipeDirection.InOut);
			pipeClient.Connect();
			var buf = Coder.StringToBytes(JsonConvert.SerializeObject(commandLineParams), Coder.CodePage.UTF8);
			var size = BitConverter.GetBytes(buf.Length);
			pipeClient.Write(size, 0, size.Length);
			pipeClient.Write(buf, 0, buf.Length);

			while ((waitEvent?.WaitOne(1000) == false) && (!proc.HasExited)) { }
		}

		static void CreateTabs(CommandLineParams commandLineParams)
		{
			try
			{
				if (commandLineParams.Background)
					return;

				if (!commandLineParams.Files.Any())
				{
					new Tabs(true);
					return;
				}

				var shutdownData = string.IsNullOrWhiteSpace(commandLineParams.Wait) ? null : new ShutdownData(commandLineParams.Wait, commandLineParams.Files.Count);
				var tabs = default(Tabs);
				if (!commandLineParams.Diff)
					tabs = Tabs.Instances.OrderByDescending(x => x.LastActivated).FirstOrDefault();
				if (tabs == null)
					tabs = new Tabs();
				foreach (var file in commandLineParams.Files)
				{
					if ((file.Existing) && (Tabs.Instances.OrderByDescending(x => x.LastActivated).Select(x => x.GotoTab(file.FileName, file.Line, file.Column, file.Index)).FirstOrDefault(x => x)))
						continue;

					tabs.HandleCommand(new ExecuteState(NECommand.Internal_AddTab) { Configuration = new Tab(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData) });
				}

				//if (clParams.Diff)
				//{
				//	for (var ctr = 0; ctr + 1 < tabsWindow.Tabs.Count; ctr += 2)
				//	{
				//		tabsWindow.Tabs[ctr].DiffTarget = tabsWindow.Tabs[ctr + 1];
				//		if (tabsWindow.Tabs[ctr].ContentType == ParserType.None)
				//			tabsWindow.Tabs[ctr].ContentType = tabsWindow.Tabs[ctr + 1].ContentType;
				//		if (tabsWindow.Tabs[ctr + 1].ContentType == ParserType.None)
				//			tabsWindow.Tabs[ctr + 1].ContentType = tabsWindow.Tabs[ctr].ContentType;
				//	}
				//	tabsWindow.SetLayout(maxColumns: 2);
				//}
			}
			catch (Exception ex) { App.ShowExceptionMessage(ex); }
		}

		static void HandleWaitPID(CommandLineParams commandLineParams)
		{
			if (commandLineParams.WaitPID != 0)
				Process.GetProcessById(commandLineParams.WaitPID)?.WaitForExit();
		}

		static int? GetMasterPID()
		{
			try
			{
				using (var oldMMFile = MemoryMappedFile.OpenExisting(IPCName))
				using (var va = oldMMFile.CreateViewAccessor())
					return va.ReadInt32(0);
			}
			catch { return null; }
		}

		static void SetMasterPID()
		{
			mmfile = MemoryMappedFile.CreateNew(IPCName, 4);
			using (var va = mmfile.CreateViewAccessor())
				va.Write(0, Process.GetCurrentProcess().Id);
		}

		static void SetupPipeWait()
		{
			var pipe = new NamedPipeServerStream(IPCName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			pipe.BeginWaitForConnection(result =>
			{
				pipe.EndWaitForConnection(result);

				var buf = new byte[sizeof(int)];
				pipe.Read(buf, 0, buf.Length);
				var len = BitConverter.ToInt32(buf, 0);
				buf = new byte[len];
				pipe.Read(buf, 0, buf.Length);
				var commandLineParams = JsonConvert.DeserializeObject<CommandLineParams>(Coder.BytesToString(buf, Coder.CodePage.UTF8));

				App.Run(() => CreateTabs(commandLineParams));

				SetupPipeWait();
			}, null);
		}
	}
}
