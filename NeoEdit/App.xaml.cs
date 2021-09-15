using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.Editor;
using NeoEdit.UI;
using Newtonsoft.Json;

namespace NeoEdit
{
	partial class App
	{
		static string IPCName { get; } = $"NeoEdit-{Helpers.CurrentUser}-{{debe0282-0e9d-47fd-836c-60f500dbaeb5}}";
		static string ShutdownEventName { get; } = $"NeoEdit-Wait-{Helpers.CurrentUser}-{Guid.NewGuid()}";

		public static void RunProgram(CommandLineParams commandLineParams)
		{
			try
			{
				HandleWaitPID(commandLineParams);

				var masterPid = default(int?);
				if (!commandLineParams.Debug)
					masterPid = GetMasterPID();
				if (!masterPid.HasValue)
				{
					var app = new App();
					if (!commandLineParams.Debug)
					{
						SetMasterPID();
						app.SetupPipeWait();
					}
					app.neGlobalUI.HandleCommandLine(commandLineParams);
					app.Run();
					return;
				}

				var proc = Process.GetProcessById(masterPid.Value);
				Win32.AllowSetForegroundWindow(proc.Id);

				// Server already exists; connect and send command line
				var waitEvent = default(EventWaitHandle);
				if (commandLineParams.Wait != null)
				{
					commandLineParams.Wait = ShutdownEventName;
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
			catch (Exception ex) { NEWindowUI.ShowExceptionMessage(ex); }
		}

		private readonly NEGlobalUI neGlobalUI;
		static MemoryMappedFile masterPIDFile;

		static void HandleWaitPID(CommandLineParams commandLineParams)
		{
			if (commandLineParams.WaitPID != 0)
				Process.GetProcessById(commandLineParams.WaitPID)?.WaitForExit();
		}

		static int? GetMasterPID()
		{
			try
			{
				using (var file = MemoryMappedFile.OpenExisting(IPCName))
				using (var accessor = file.CreateViewAccessor())
					return accessor.ReadInt32(0);
			}
			catch { return null; }
		}

		static void SetMasterPID()
		{
			masterPIDFile = MemoryMappedFile.CreateNew(IPCName, 4);
			using (var accessor = masterPIDFile.CreateViewAccessor())
				accessor.Write(0, Process.GetCurrentProcess().Id);
		}

		void SetupPipeWait()
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

				neGlobalUI.HandleCommandLine(commandLineParams);

				SetupPipeWait();
			}, null);
		}

		public App()
		{
			neGlobalUI = new NEGlobalUI(new NEGlobal(), Dispatcher);

			new NEMenu(); // The first time it creates a menu it's slow, do it while the user isn't waiting
			Clipboarder.Initialize();
			Font.Reset();

			SetupJumpList();

			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			InitializeComponent();
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void SetupJumpList()
		{
			var runAsAdminTask = new JumpTask { Title = "Run as administrator", ApplicationPath = Helpers.GetEntryExe(), Arguments = "-admin" };

			var jumpList = new JumpList();
			jumpList.JumpItems.Add(runAsAdminTask);

			JumpList.SetJumpList(this, jumpList);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			NEWindowUI.ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}

		class Win32
		{
			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool AllowSetForegroundWindow(int dwProcessId);
		}
	}
}
