using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEGlobal : INEGlobal
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public NEGlobal()
		{
			Data = new NEGlobalData();
			NEWindowDatas = new OrderedHashSet<NEWindowData>();
		}

		public bool HandlesKey(ModifierKeys modifiers, Key key)
		{
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Escape:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
				case Key.Home:
				case Key.End:
				case Key.PageUp:
				case Key.PageDown:
				case Key.Tab:
				case Key.Enter:
					return true;
			}

			return false;
		}

		readonly Stack<ExecuteState> actionStack = new Stack<ExecuteState>();
		ExecuteState lastAction;
		bool timeNextAction;
		NEGlobalData undoGlobalData, redoGlobalData;

		public void QueueActions(IEnumerable<ExecuteState> actions)
		{
			lock (actionStack)
				actions.Reverse().ForEach(actionStack.Push);
		}

		public void HandleCommand(INEWindow neWindow, ExecuteState executeState, Func<bool> skipDraw)
		{
			switch (executeState.Command)
			{
				case NECommand.Edit_Undo_Global:
					if (undoGlobalData != null)
					{
						redoGlobalData = Data;
						SetData(undoGlobalData);
						undoGlobalData = null;
						NEWindows.ForEach(x => x.RenderNEWindowUI());
					}
					return;
				case NECommand.Edit_Redo_Global:
					if (redoGlobalData != null)
					{
						undoGlobalData = Data;
						SetData(redoGlobalData);
						redoGlobalData = null;
						NEWindows.ForEach(x => x.RenderNEWindowUI());
					}
					return;
			}

			lock (actionStack)
				actionStack.Push(executeState);
			RunCommands(neWindow as NEWindow, skipDraw);
			CheckExit(executeState);
		}

		void RunCommands(NEWindow neWindow, Func<bool> skipDraw)
		{
			var oldData = Data;
			try
			{
				var actionCount = -2; // -1 because it preincrements, and -1 so it doesn't count the first step (which could queue a macro)

				while (true)
				{
					++actionCount;

					int actionStackCount;
					ExecuteState action;
					lock (actionStack)
					{
						actionStackCount = actionStack.Count;
						if (actionStackCount == 0)
							break;
						action = actionStack.Pop();
					}
					var total = actionStackCount + actionCount;
					if (total > 0)
					{
						if (state.NEWindow.MacroVisualize)
							state.NEWindow.RenderNEWindowUI();
						state.NEWindow.neWindowUI.SetMacroProgress((double)actionCount / total);
					}

					if (action.Command == NECommand.Macro_RepeatLastAction)
					{
						if (lastAction == null)
							throw new Exception("No last action available");
						action = lastAction;
					}

					NESerialTracker.MoveNext();
					EditorExecuteState.SetState(this, neWindow, action);

					RunCommand();
					if (state.MacroInclude)
						lastAction = new ExecuteState(state);
				}

				if (Data != oldData)
				{
					undoGlobalData = oldData;
					redoGlobalData = null;
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException))
				{
					state.NEWindow?.RenderNEWindowUI();
					state.NEWindow.neWindowUI?.ShowExceptionMessage(ex);
				}
				lock (actionStack)
					actionStack.Clear();
			}
			finally
			{
				if (!skipDraw())
					state.NEWindow?.RenderNEWindowUI();
				state.NEWindow?.neWindowUI?.SetMacroProgress(null);
				EditorExecuteState.ClearState();
			}
		}

		void RunCommand()
		{
			var oldData = state.NEGlobal.Data;

			try
			{
				var elapsed = 0L;
				switch (state.Command)
				{
					case NECommand.Internal_CommandLine: NEFile.PreExecute_Internal_CommandLine(); break;
					case NECommand.Help_TimeNextAction: timeNextAction = true; break;
					default: elapsed = state.NEWindow.RunCommand(); break;
				}

				if (timeNextAction)
				{
					timeNextAction = false;
					state.NEWindow.neWindowUI.RunDialog_ShowMessage("Timer", $"Elapsed time: {elapsed:n} ms", MessageOptions.Ok, MessageOptions.None, MessageOptions.None);
				}

				var result = GetResult();
				if (result != null)
				{
					if (result.Clipboard != null)
						NEClipboard.Current = result.Clipboard;

					if (result.KeysAndValues != null)
						for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
							if (result.KeysAndValues[kvIndex] != null)
								NEWindow.keysAndValues[kvIndex] = result.KeysAndValues[kvIndex];

					if (result.DragFiles?.Any() == true)
					{
						var nonExisting = result.DragFiles.Where(x => !File.Exists(x)).ToList();
						if (nonExisting.Any())
							throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
						// TODO: Make these files actually do something
						//Focused.DragFiles = fileNames;
					}
				}

				UpdateAttachments();
			}
			catch
			{
				state.NEGlobal.SetData(oldData);
				throw;
			}
			finally
			{
				state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(null);
			}
		}

		void CheckExit(ExecuteState state)
		{
			if (NEWindows.Any())
				return;

			if ((!Settings.DontExitOnClose) || ((state.Configuration as Configuration_File_Exit)?.WindowClosed != true))
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

		public bool StopTasks()
		{
			lock (actionStack)
				if (actionStack.Any())
				{
					actionStack.Clear();
					return true;
				}
			if (TaskRunner.Cancel())
				return true;
			return false;
		}

		public bool KillTasks()
		{
			lock (actionStack)
				actionStack.Clear();
			TaskRunner.ForceCancel();
			return true;
		}
	}
}
