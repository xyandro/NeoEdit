using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Models;
using NeoEdit.UI.Dialogs;

namespace NeoEdit.UI
{
	public class NEGlobalUI
	{
		const int DrawFrequency = 5;

		readonly INEGlobal neGlobal;
		readonly Dispatcher dispatcher;

		static readonly BlockingCollection<(INEWindow, ExecuteState)> states = new BlockingCollection<(INEWindow, ExecuteState)>();

		static int numSkipped = 0;

		void RunThread()
		{
			while (true)
			{
				try
				{
					(var neWindow, var state) = states.Take();
					dispatcher.Invoke(() => Clipboarder.GetSystem());
					neGlobal.HandleCommand(neWindow, state, SkipDraw);
					dispatcher.Invoke(() => Clipboarder.SetSystem());
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

		public bool HandlesKey(ModifierKeys modifiers, Key key) => neGlobal.HandlesKey(modifiers.ToModifiers(), key.FromKey());

		public void HandleCommandLine(CommandLineParams commandLineParams)
		{
			if (dispatcher?.CheckAccess() == false)
				dispatcher.Invoke(() => HandleCommandLine(commandLineParams));
			else
				HandleCommand(new ExecuteState(NECommand.Internal_CommandLine, Keyboard.Modifiers.ToModifiers()) { Configuration = new Configuration_Internal_CommandLine { CommandLineParams = commandLineParams } });
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

		public NEGlobalUI(INEGlobal neGlobal, Dispatcher dispatcher)
		{
			this.neGlobal = neGlobal;
			this.dispatcher = dispatcher;

			new NEMenu(); // The first time it creates a menu it's slow, do it while the user isn't waiting
			Clipboarder.Initialize();
			Font.Reset();

			INEWindowUI.CreateNEWindowUIStatic = neWindow => dispatcher.Invoke(() => new NEWindowUI(neWindow, this));
			INEWindowUI.GetDecryptKeyStatic = type => dispatcher.Invoke(() => File_Advanced_Encrypt_Dialog.Run(null, type, false).Key);
			INEWindowUI.ShowExceptionMessageStatic = ex => dispatcher.Invoke(() => NEWindowUI.ShowExceptionMessage(ex));
			INEWindowUI.ShellIntegrateStatic = integrate => ShellIntegrate(integrate);

			new Thread(RunThread) { Name = nameof(NEGlobalUI) }.Start();
		}

		static void ShellIntegrate(bool integrate)
		{
			using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);
			using var starKey = baseKey.OpenSubKey("*");
			using var shellKey = starKey.OpenSubKey("shell", true);
			if (integrate)
			{
				using var neoEditKey = shellKey.CreateSubKey("Open with NeoEdit");
				using var commandKey = neoEditKey.CreateSubKey("command");
				commandKey.SetValue("", $@"""{Helpers.GetEntryExe()}"" ""%1""");
			}
			else
				shellKey.DeleteSubKeyTree("Open with NeoEdit");
		}
	}
}
