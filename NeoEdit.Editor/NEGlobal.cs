﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEGlobal : INEGlobal
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public bool MacroVisualize { get; set; }

		public NEGlobal()
		{
			Data = new NEGlobalData();
			NEWindowDatas = new OrderedHashSet<INEWindowData>();
		}

		public bool HandlesKey(Modifiers modifiers, string key)
		{
			switch (key)
			{
				case "Back":
				case "Delete":
				case "Escape":
				case "Left":
				case "Right":
				case "Up":
				case "Down":
				case "Home":
				case "End":
				case "PageUp":
				case "PageDown":
				case "Tab":
				case "Enter":
					return true;
			}

			return false;
		}

		readonly Stack<ExecuteState> actionStack = new Stack<ExecuteState>();
		ExecuteState lastAction;
		bool timeNextAction;
		INEGlobalData undoGlobalData;

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
		}

		void RunCommands(NEWindow neWindow, Func<bool> skipDraw)
		{
			neWindow?.SetNeedsRender();
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

					if (action.Command != NECommand.Internal_Activate)
						INEGlobalUI.DragFiles = null;

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
					{
						lastAction = new ExecuteState(state);
						if (recordingMacro != null)
							recordingMacro.AddAction(lastAction);
					}
				}

				if ((Data != oldData) && ((state.Command != NECommand.Internal_CommandLine) || (NEWindows.Count != 1) || (NEWindows[0].NEFiles.Count != 1) || (!NEWindows[0].NEFiles[0].Empty())))
					undoGlobalData = oldData;
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException))
				{
					NEWindows.ForEach(x => x.RenderNEWindowUI());
					if (state?.NEWindow?.neWindowUI == null)
						INEWindowUI.ShowExceptionMessageStatic(ex);
					else
						state.NEWindow.neWindowUI.ShowExceptionMessage(ex);
				}
				lock (actionStack)
					actionStack.Clear();
			}
			finally
			{
				CheckExit();
				if (!skipDraw())
					NEWindows.ForEach(x => x.RenderNEWindowUI());
				EditorExecuteState.ClearState();
			}
		}

		void RunCommand()
		{
			var oldData = Data;

			try
			{
				Configure();

				var showTime = timeNextAction;
				timeNextAction = false;
				Stopwatch sw = Stopwatch.StartNew();

				state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(0);
				if (state.PreExecution != PreExecution_TaskFinished.Singleton)
					TaskRunner.Run(PreExecute, percent => state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(percent));
				if (state.PreExecution != PreExecution_TaskFinished.Singleton)
					TaskRunner.Run(Execute, percent => state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(percent));
				if (state.PreExecution != PreExecution_TaskFinished.Singleton)
					TaskRunner.Run(PostExecute, percent => state.NEWindow?.neWindowUI?.SetTaskRunnerProgress(percent));

				state.NEWindow?.CalculateDiffs();

				Debug.WriteLine($"{state.Command} elapsed time: {sw.ElapsedMilliseconds:n0} ms");
				if (showTime)
					state.NEWindow.neWindowUI.RunDialog_ShowMessage("Timer", $"{state.Command} elapsed time: {sw.ElapsedMilliseconds:n0} ms", MessageOptions.Ok, MessageOptions.None, MessageOptions.None);

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
						INEGlobalUI.DragFiles = result.DragFiles.ToArray();
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
				NEWindows.ForEach(neWindow => neWindow.neWindowUI?.SetMacroProgress(null));
				NEWindows.ForEach(neWindow => neWindow.neWindowUI?.SetTaskRunnerProgress(null));
			}
		}

		void Configure()
		{
			if (state.Configuration != null)
				return;

			if (state.NEWindow != null)
				state.NEWindow.Configure();
		}

		#region PreExecute
		void PreExecute()
		{
			switch (state.Command)
			{
				case NECommand.File_Advanced_DontExitOnClose: PreExecute__File_Advanced_DontExitOnClose(); break;
				default: state.NEWindow?.PreExecute(); break;
			}
		}
		#endregion

		#region Execute
		void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_CommandLine: Execute__Internal_CommandLine(); break;
				case NECommand.Edit_Undo_Global: Execute__Edit_Undo_Global(); break;
				case NECommand.Macro_Play_Quick_1: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(1); break;
				case NECommand.Macro_Play_Quick_2: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(2); break;
				case NECommand.Macro_Play_Quick_3: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(3); break;
				case NECommand.Macro_Play_Quick_4: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(4); break;
				case NECommand.Macro_Play_Quick_5: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(5); break;
				case NECommand.Macro_Play_Quick_6: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(6); break;
				case NECommand.Macro_Play_Quick_7: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(7); break;
				case NECommand.Macro_Play_Quick_8: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(8); break;
				case NECommand.Macro_Play_Quick_9: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(9); break;
				case NECommand.Macro_Play_Quick_10: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(10); break;
				case NECommand.Macro_Play_Quick_11: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(11); break;
				case NECommand.Macro_Play_Quick_12: Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(12); break;
				case NECommand.Macro_Play_Play: Execute__Macro_Play_Play(); break;
				case NECommand.Macro_Play_Repeat: Execute__Macro_Play_Repeat(); break;
				case NECommand.Macro_Play_PlayOnCopiedFiles: Execute__Macro_Play_PlayOnCopiedFiles(); break;
				case NECommand.Macro_Record_Quick_1: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(1); break;
				case NECommand.Macro_Record_Quick_2: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(2); break;
				case NECommand.Macro_Record_Quick_3: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(3); break;
				case NECommand.Macro_Record_Quick_4: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(4); break;
				case NECommand.Macro_Record_Quick_5: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(5); break;
				case NECommand.Macro_Record_Quick_6: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(6); break;
				case NECommand.Macro_Record_Quick_7: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(7); break;
				case NECommand.Macro_Record_Quick_8: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(8); break;
				case NECommand.Macro_Record_Quick_9: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(9); break;
				case NECommand.Macro_Record_Quick_10: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(10); break;
				case NECommand.Macro_Record_Quick_11: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(11); break;
				case NECommand.Macro_Record_Quick_12: Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(12); break;
				case NECommand.Macro_Record_Record: Execute__Macro_Record_Record(); break;
				case NECommand.Macro_Record_StopRecording: Execute__Macro_Record_StopRecording(); break;
				case NECommand.Macro_Append_Quick_1: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(1); break;
				case NECommand.Macro_Append_Quick_2: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(2); break;
				case NECommand.Macro_Append_Quick_3: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(3); break;
				case NECommand.Macro_Append_Quick_4: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(4); break;
				case NECommand.Macro_Append_Quick_5: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(5); break;
				case NECommand.Macro_Append_Quick_6: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(6); break;
				case NECommand.Macro_Append_Quick_7: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(7); break;
				case NECommand.Macro_Append_Quick_8: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(8); break;
				case NECommand.Macro_Append_Quick_9: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(9); break;
				case NECommand.Macro_Append_Quick_10: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(10); break;
				case NECommand.Macro_Append_Quick_11: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(11); break;
				case NECommand.Macro_Append_Quick_12: Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(12); break;
				case NECommand.Macro_Append_Append: Execute__Macro_Append_Append(); break;
				case NECommand.Macro_Open_Quick_1: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(1); break;
				case NECommand.Macro_Open_Quick_2: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(2); break;
				case NECommand.Macro_Open_Quick_3: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(3); break;
				case NECommand.Macro_Open_Quick_4: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(4); break;
				case NECommand.Macro_Open_Quick_5: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(5); break;
				case NECommand.Macro_Open_Quick_6: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(6); break;
				case NECommand.Macro_Open_Quick_7: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(7); break;
				case NECommand.Macro_Open_Quick_8: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(8); break;
				case NECommand.Macro_Open_Quick_9: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(9); break;
				case NECommand.Macro_Open_Quick_10: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(10); break;
				case NECommand.Macro_Open_Quick_11: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(11); break;
				case NECommand.Macro_Open_Quick_12: Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(12); break;
				case NECommand.Macro_Visualize: Execute__Macro_Visualize(); break;
				case NECommand.Help_Advanced_TimeNextAction: Execute__Help_Advanced_TimeNextAction(); break;
				default: state.NEWindow?.Execute(); break;
			}
		}
		#endregion

		#region PostExecute
		void PostExecute() => state.NEWindow?.PostExecute();
		#endregion

		Timer exitTimer;
		void CancelExit()
		{
			if (exitTimer == null)
				return;

			exitTimer.Stop();
			exitTimer.Dispose();
			exitTimer = null;
		}

		void ScheduleExit(TimeSpan delay, int pid)
		{
			exitTimer = new Timer((int)delay.TotalMilliseconds);
			exitTimer.Elapsed += (s, e) =>
			{
				CancelExit();

				Process.Start(Environment.GetCommandLineArgs()[0], $"-background -waitpid={pid}");
				Environment.Exit(0);
			};
			exitTimer.Start();
		}

		void CheckExit()
		{
			CancelExit();

			if (NEWindows.Any())
				return;

			if ((!Settings.DontExitOnClose) || ((state.Configuration as Configuration_File_Exit)?.ShouldExit ?? false))
				Environment.Exit(0);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// Restart if memory usage is more than 1/2 GB (but give user time in case they do something else)
			var process = Process.GetCurrentProcess();
			if (process.PrivateMemorySize64 > (1 << 29))
				ScheduleExit(TimeSpan.FromSeconds(30), process.Id);
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
