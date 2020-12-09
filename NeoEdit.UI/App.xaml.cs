using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.UI.CommandLine;
using NeoEdit.UI.Dialogs;
using Newtonsoft.Json;

namespace NeoEdit.UI
{
	partial class App
	{
		const int DrawFrequency = 5;

		readonly INEGlobal neGlobal;

		public static void RunProgram(Func<INEGlobal> getNEGlobal, string commandLine)
		{
			try { RunCommandLine(getNEGlobal, commandLine); }
			catch (Exception ex) { NEWindowUI.ShowExceptionMessage(ex); }
		}

		static void RunCommandLine(Func<INEGlobal> getNEGlobal, string commandLine)
		{
			var commandLineParams = CommandLineVisitor.GetCommandLineParams(commandLine);
			HandleWaitPID(commandLineParams);

			var masterPid = default(int?);
			if (!commandLineParams.Multi)
				masterPid = GetMasterPID();
			if (!masterPid.HasValue)
			{
				var app = new App(getNEGlobal());
				if (!commandLineParams.Multi)
				{
					SetMasterPID();
					app.SetupPipeWait();
				}
				app.HandleCommandLine(commandLineParams);
				app.Run();
				return;
			}

			var proc = Process.GetProcessById(masterPid.Value);
			Win32.AllowSetForegroundWindow(proc.Id);

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

		const string IPCName = "NeoEdit-{debe0282-0e9d-47fd-836c-60f500dbaeb5}";
		const string ShutdownEventName = "NeoEdit-Wait-{0}";

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

				HandleCommandLine(commandLineParams);

				SetupPipeWait();
			}, null);
		}

		static readonly BlockingCollection<(INEWindow, ExecuteState)> states = new BlockingCollection<(INEWindow, ExecuteState)>();

		static int numSkipped = 0;

		void RunThread()
		{
			while (true)
			{
				try
				{
					(var neWindow, var state) = states.Take();
					Dispatcher.Invoke(() => Clipboarder.GetSystem());
					neGlobal.HandleCommand(neWindow, state, SkipDraw);
					Dispatcher.Invoke(() => Clipboarder.SetSystem());
				}
				catch { }
			}
		}

		static bool SkipDraw()
		{
			if (states.Count == 0)
			{
				numSkipped = 0;
				return false;
			}

			++numSkipped;
			if (numSkipped == DrawFrequency)
			{
				numSkipped = 0;
				return false;
			}

			return true;
		}

		public bool HandlesKey(ModifierKeys modifiers, Key key) => neGlobal.HandlesKey(modifiers, key);

		void HandleCommandLine(CommandLineParams commandLineParams)
		{
			if (Dispatcher?.CheckAccess() == false)
				Dispatcher.Invoke(() => HandleCommandLine(commandLineParams));
			else
				HandleCommand(new ExecuteState(NECommand.Internal_CommandLine) { Configuration = new Configuration_Internal_CommandLine { CommandLineParams = commandLineParams } });
		}

		public static void HandleCommand(ExecuteState state) => HandleCommand(null, state);

		public static void HandleCommand(INEWindow neWindow, ExecuteState state) => states.Add((neWindow, state));

		public bool StopTasks()
		{
			var result = false;
			if (CancelActive())
				result = true;
			if (neGlobal.StopTasks())
				result = true;
			return result;
		}

		public bool KillTasks()
		{
			CancelActive();
			neGlobal.KillTasks();
			return true;
		}

		static bool CancelActive()
		{
			var result = false;
			while (states.TryTake(out var _))
				result = true;
			return result;
		}

		public App(INEGlobal neGlobal)
		{
			this.neGlobal = neGlobal;

			new NEMenu(); // The first time it creates a menu it's slow, do it while the user isn't waiting
			Clipboarder.Initialize();
			Font.Reset();

			INEWindowUIStatic.CreateNEWindowUI = neWindow => Dispatcher.Invoke(() => new NEWindowUI(neWindow, this));
			INEWindowUIStatic.GetDecryptKey = type => Dispatcher.Invoke(() => File_Advanced_Encrypt_Dialog.Run(null, type, false).Key);

			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			InitializeComponent();
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
			new Thread(RunThread) { Name = nameof(App) }.Start();
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
