using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;

namespace NeoEdit.UI
{
	public static class NEGlobalUI
	{
		public static Dispatcher Dispatcher { get; set; }
		public static INEGlobal neGlobal { get; set; }

		static readonly ActionRunner actionRunner = new ActionRunner();

		public static bool HandlesKey(ModifierKeys modifiers, Key key) => neGlobal.HandlesKey(modifiers, key);

		public static void HandleCommand(ExecuteState state) => HandleCommand(null, null, state);

		public static void HandleCommand(INEWindow neWindow, INEWindowUI neWindowUI, ExecuteState state)
		{
			actionRunner.Add(moreQueued =>
			{
				Dispatcher.Invoke(() => Clipboarder.GetSystem());

				neGlobal.HandleCommand(neWindow, neWindowUI, state, moreQueued);

				Dispatcher.Invoke(() =>
				{
					Clipboarder.SetSystem();
					AssignWindows();
					CheckExit(state);
				});
			});
		}

		static readonly List<NEWindowUI> neWindowUIs = new List<NEWindowUI>();
		static void AssignWindows()
		{
			var createdNEWindows = neGlobal.NEWindows.Except(neWindowUIs.Select(x => x.NEWindow)).ToList();
			foreach (var neWindow in createdNEWindows)
			{
				var neWindowUI = new NEWindowUI(neWindow);
				neWindowUI.SetForeground();
				neWindowUIs.Add(neWindowUI);
			}

			var deletedNEWindowUIs = neWindowUIs.Where(neWindowUI => !neGlobal.NEWindows.Contains(neWindowUI.NEWindow)).ToList();
			foreach (var neWindowUI in deletedNEWindowUIs)
			{
				neWindowUIs.Remove(neWindowUI);
				neWindowUI.CloseWindow();
			}
		}

		static void CheckExit(ExecuteState state)
		{
			if (neWindowUIs.Any())
				return;

			if (((state.Configuration as Configuration_File_Exit)?.WindowClosed != true) || (!Settings.DontExitOnClose))
				Environment.Exit(0);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// Restart if memory usage is more than 1/2 GB
			var process = Process.GetCurrentProcess();
			if (process.PrivateMemorySize64 > (1 << 29))
			{
				Process.Start(Environment.GetCommandLineArgs()[0], $"-background -waitpid={process.Id}");
				Environment.Exit(0);
			}
		}

		public static bool StopTasks()
		{
			var result = false;
			if (actionRunner.CancelActive())
				result = true;
			if (neGlobal.StopTasks())
				result = true;
			return result;
		}

		public static bool KillTasks()
		{
			actionRunner.CancelActive();
			neGlobal.KillTasks();
			return true;
		}
	}
}
