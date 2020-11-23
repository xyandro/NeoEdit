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

		public bool MacroVisualize { get; set; } = true;

		public NEGlobal()
		{
			Data = new NEGlobalData();
			NEWindowDatas = new OrderedHashSet<INEWindowData>();
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
		INEGlobalData undoGlobalData, redoGlobalData;

		void QueueActions(IEnumerable<ExecuteState> actions)
		{
			lock (actionStack)
				actions.Reverse().ForEach(actionStack.Push);
		}

		public void HandleCommand(INEWindow neWindow, ExecuteState executeState, Func<bool> skipDraw)
		{
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
						if (MacroVisualize)
							NEWindows.ForEach(x => x.RenderNEWindowUI());
						state.NEWindow?.neWindowUI?.SetMacroProgress((double)actionCount / total);
					}

					if (action.Command == NECommand.Macro_RepeatLastAction)
					{
						if (lastAction == null)
							throw new Exception("No last action available");
						action = lastAction;
					}

					NESerialTracker.MoveNext();
					EditorExecuteState.SetState(this, neWindow, action);
					if (state.NEWindow != null)
					{
						state.ClipboardDataMapFunc = state.NEWindow.GetClipboardDataMap;
						state.KeysAndValuesFunc = state.NEWindow.GetKeysAndValuesMap;
					}

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
					NEWindows.ForEach(x => x.RenderNEWindowUI());
					state.NEWindow.neWindowUI?.ShowExceptionMessage(ex);
				}
				lock (actionStack)
					actionStack.Clear();
			}
			finally
			{
				if (!skipDraw())
					NEWindows.ForEach(x => x.RenderNEWindowUI());
				state.NEWindow?.neWindowUI?.SetMacroProgress(null);
				EditorExecuteState.ClearState();
			}
		}

		void RunCommand()
		{
			var oldData = Data;

			try
			{
				Configure();

				Stopwatch sw = null;
				if (timeNextAction)
				{
					timeNextAction = false;
					sw = Stopwatch.StartNew();
				}

				state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(0);
				if (!TaskRunner.Run(PreExecute, percent => state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(percent)))
					TaskRunner.Run(Execute, percent => state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(percent));

				if (sw != null)
					state.NEWindow.neWindowUI.RunDialog_ShowMessage("Timer", $"Elapsed time: {sw.ElapsedMilliseconds:n} ms", MessageOptions.Ok, MessageOptions.None, MessageOptions.None);

				if ((recordingMacro != null) && (state.MacroInclude))
					recordingMacro.AddAction(new ExecuteState(state));

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
				SetData(oldData);
				throw;
			}
			finally
			{
				state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(null);
				state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(null);
			}
		}

		void Configure()
		{
			if (state.Configuration != null)
				return;

			if (state.NEWindow != null)
				state.NEWindow.Configure();
		}

		bool PreExecute() => state.NEWindow?.PreExecute() ?? false;

		void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_CommandLine: Execute_Internal_CommandLine(); return;
				case NECommand.Edit_Undo_Global: Execute_Edit_Undo_Global(); return;
				case NECommand.Edit_Redo_Global: Execute_Edit_Redo_Global(); return;
				case NECommand.Macro_Play_Quick_1: Execute_Macro_Play_Quick(1); return;
				case NECommand.Macro_Play_Quick_2: Execute_Macro_Play_Quick(2); return;
				case NECommand.Macro_Play_Quick_3: Execute_Macro_Play_Quick(3); return;
				case NECommand.Macro_Play_Quick_4: Execute_Macro_Play_Quick(4); return;
				case NECommand.Macro_Play_Quick_5: Execute_Macro_Play_Quick(5); return;
				case NECommand.Macro_Play_Quick_6: Execute_Macro_Play_Quick(6); return;
				case NECommand.Macro_Play_Quick_7: Execute_Macro_Play_Quick(7); return;
				case NECommand.Macro_Play_Quick_8: Execute_Macro_Play_Quick(8); return;
				case NECommand.Macro_Play_Quick_9: Execute_Macro_Play_Quick(9); return;
				case NECommand.Macro_Play_Quick_10: Execute_Macro_Play_Quick(10); return;
				case NECommand.Macro_Play_Quick_11: Execute_Macro_Play_Quick(11); return;
				case NECommand.Macro_Play_Quick_12: Execute_Macro_Play_Quick(12); return;
				case NECommand.Macro_Play_Play: Execute_Macro_Play_Play(); return;
				case NECommand.Macro_Play_Repeat: Execute_Macro_Play_Repeat(); return;
				case NECommand.Macro_Play_PlayOnCopiedFiles: Execute_Macro_Play_PlayOnCopiedFiles(); return;
				case NECommand.Macro_Record_Quick_1: Execute_Macro_Record_Quick(1); return;
				case NECommand.Macro_Record_Quick_2: Execute_Macro_Record_Quick(2); return;
				case NECommand.Macro_Record_Quick_3: Execute_Macro_Record_Quick(3); return;
				case NECommand.Macro_Record_Quick_4: Execute_Macro_Record_Quick(4); return;
				case NECommand.Macro_Record_Quick_5: Execute_Macro_Record_Quick(5); return;
				case NECommand.Macro_Record_Quick_6: Execute_Macro_Record_Quick(6); return;
				case NECommand.Macro_Record_Quick_7: Execute_Macro_Record_Quick(7); return;
				case NECommand.Macro_Record_Quick_8: Execute_Macro_Record_Quick(8); return;
				case NECommand.Macro_Record_Quick_9: Execute_Macro_Record_Quick(9); return;
				case NECommand.Macro_Record_Quick_10: Execute_Macro_Record_Quick(10); return;
				case NECommand.Macro_Record_Quick_11: Execute_Macro_Record_Quick(11); return;
				case NECommand.Macro_Record_Quick_12: Execute_Macro_Record_Quick(12); return;
				case NECommand.Macro_Record_Record: Execute_Macro_Record_Record(); return;
				case NECommand.Macro_Record_StopRecording: Execute_Macro_Record_StopRecording(); return;
				case NECommand.Macro_Append_Quick_1: Execute_Macro_Append_Quick(1); return;
				case NECommand.Macro_Append_Quick_2: Execute_Macro_Append_Quick(2); return;
				case NECommand.Macro_Append_Quick_3: Execute_Macro_Append_Quick(3); return;
				case NECommand.Macro_Append_Quick_4: Execute_Macro_Append_Quick(4); return;
				case NECommand.Macro_Append_Quick_5: Execute_Macro_Append_Quick(5); return;
				case NECommand.Macro_Append_Quick_6: Execute_Macro_Append_Quick(6); return;
				case NECommand.Macro_Append_Quick_7: Execute_Macro_Append_Quick(7); return;
				case NECommand.Macro_Append_Quick_8: Execute_Macro_Append_Quick(8); return;
				case NECommand.Macro_Append_Quick_9: Execute_Macro_Append_Quick(9); return;
				case NECommand.Macro_Append_Quick_10: Execute_Macro_Append_Quick(10); return;
				case NECommand.Macro_Append_Quick_11: Execute_Macro_Append_Quick(11); return;
				case NECommand.Macro_Append_Quick_12: Execute_Macro_Append_Quick(12); return;
				case NECommand.Macro_Append_Append: Execute_Macro_Append_Append(); return;
				case NECommand.Macro_Open_Quick_1: Execute_Macro_Open_Quick(1); return;
				case NECommand.Macro_Open_Quick_2: Execute_Macro_Open_Quick(2); return;
				case NECommand.Macro_Open_Quick_3: Execute_Macro_Open_Quick(3); return;
				case NECommand.Macro_Open_Quick_4: Execute_Macro_Open_Quick(4); return;
				case NECommand.Macro_Open_Quick_5: Execute_Macro_Open_Quick(5); return;
				case NECommand.Macro_Open_Quick_6: Execute_Macro_Open_Quick(6); return;
				case NECommand.Macro_Open_Quick_7: Execute_Macro_Open_Quick(7); return;
				case NECommand.Macro_Open_Quick_8: Execute_Macro_Open_Quick(8); return;
				case NECommand.Macro_Open_Quick_9: Execute_Macro_Open_Quick(9); return;
				case NECommand.Macro_Open_Quick_10: Execute_Macro_Open_Quick(10); return;
				case NECommand.Macro_Open_Quick_11: Execute_Macro_Open_Quick(11); return;
				case NECommand.Macro_Open_Quick_12: Execute_Macro_Open_Quick(12); return;
				case NECommand.Macro_Visualize: Execute_Macro_Visualize(); return;
				case NECommand.Help_Advanced_TimeNextAction: Execute_Help_Advanced_TimeNextAction(); return;
				default: state.NEWindow?.Execute(); return;
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
