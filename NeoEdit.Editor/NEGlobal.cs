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
	public class NEGlobal : INEGlobal
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public NEGlobal()
		{
			data = new NEGlobalData();
			SetNEWindowDatas(new OrderedHashSet<NEWindowData>());
		}

		public NEGlobalData data { get; private set; }
		NEGlobalData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public void ResetData(NEGlobalData data)
		{
			var oldNEWindows = NEWindows;

			ResetResult();
			this.data = data;
			RecreateNEWindows();
			NEWindows.Except(oldNEWindows).ForEach(neWindow => neWindow.Attach());
			oldNEWindows.Except(NEWindows).ForEach(neWindow => neWindow.Detach());
			NEWindowDatas.ForEach(neWindowData => neWindowData.neWindow.ResetData(neWindowData));
		}

		IEnumerable<INEWindow> INEGlobal.NEWindows => NEWindows;

		public IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; private set; }

		public IReadOnlyOrderedHashSet<NEWindowData> NEWindowDatas
		{
			get => data.neWindowDatas;
			set
			{
				editableData.neWindowDatas = value;
				RecreateNEWindows();
			}
		}

		void RecreateNEWindows() => NEWindows = new OrderedHashSet<NEWindow>(data.neWindowDatas.Select(neWindowData => neWindowData.neWindow));

		public void SetNEWindowDatas(IEnumerable<NEWindowData> neWindowDatas) => NEWindowDatas = new OrderedHashSet<NEWindowData>(neWindowDatas);

		void ResetResult()
		{
			result = null;
		}

		public NEGlobalResult GetResult()
		{
			foreach (var neWindow in NEWindows)
			{
				var result = neWindow.GetResult();
				if (result == null)
					continue;

				if (result.Clipboard != null)
					CreateResult().SetClipboard(result.Clipboard);

				if (result.KeysAndValues != null)
					CreateResult().SetKeysAndValues(result.KeysAndValues);

				if (result.DragFiles != null)
					CreateResult().SetDragFiles(result.DragFiles);
			}

			IEnumerable<NEWindow> nextNEWindowsItr = NEWindows;
			if (result != null)
			{
				if (result.RemoveNEWindows != null)
					nextNEWindowsItr = nextNEWindowsItr.Except(result.RemoveNEWindows);
				if (result.AddNEWindows != null)
					nextNEWindowsItr = nextNEWindowsItr.Concat(result.AddNEWindows);
			}
			var nextNEWindowDatas = nextNEWindowsItr.Select(x => x.data).ToList();

			if (!NEWindowDatas.Matches(nextNEWindowDatas))
			{
				var oldNEWindows = NEWindows;
				SetNEWindowDatas(nextNEWindowDatas);
				NEWindows.Except(oldNEWindows).ForEach(neWindow => neWindow.Attach());
				oldNEWindows.Except(NEWindows).ForEach(neWindow => neWindow.Detach());
			}

			var ret = result;
			ResetResult();
			return ret;
		}

		NEGlobalResult result;
		NEGlobalResult CreateResult()
		{
			if (result == null)
				result = new NEGlobalResult();
			return result;
		}

		public void AddNEWindow(NEWindow neWindow) => CreateResult().AddNEWindow(neWindow);
		public void RemoveNEWindow(NEWindow neWindow) => CreateResult().RemoveNEWindow(neWindow);

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

		public void QueueActions(IEnumerable<ExecuteState> actions)
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
			var oldData = state.NEGlobal.data;

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

				var result = state.NEGlobal.GetResult();
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
			}
			catch
			{
				state.NEGlobal.ResetData(oldData);
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
