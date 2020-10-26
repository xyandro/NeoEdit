using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Editor.PreExecution;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		static PreExecutionStop PreExecute_Macro_Record_Quick(EditorExecuteState state, int quickNum)
		{
			if (state.Tabs.recordingMacro == null)
				PreExecute_Macro_Record_Record(state);
			else
				PreExecute_Macro_Record_StopRecording(state, QuickMacro(quickNum));

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Record_Record(EditorExecuteState state)
		{
			state.Tabs.EnsureNotRecording();
			state.Tabs.recordingMacro = new Macro();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Record_StopRecording(EditorExecuteState state, string fileName = null)
		{
			if (state.Tabs.recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = state.Tabs.recordingMacro;
			state.Tabs.recordingMacro = null;
			macro.Save(state.Tabs.TabsWindow, fileName, true);

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Append_Quick(EditorExecuteState state, int quickNum)
		{
			if (state.Tabs.recordingMacro == null)
				state.Tabs.recordingMacro = Macro.Load(state.Tabs.TabsWindow, QuickMacro(quickNum), true);
			else
				PreExecute_Macro_Record_StopRecording(state, QuickMacro(quickNum));

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Append_Append(EditorExecuteState state)
		{
			state.Tabs.EnsureNotRecording();
			state.Tabs.recordingMacro = Macro.Load(state.Tabs.TabsWindow);

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_Quick(EditorExecuteState state, int quickNum)
		{
			state.Tabs.PlayMacro(Macro.Load(state.Tabs.TabsWindow, QuickMacro(quickNum), true));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_Play(EditorExecuteState state)
		{
			state.Tabs.PlayMacro(Macro.Load(state.Tabs.TabsWindow));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_Repeat(EditorExecuteState state)
		{
			var result = state.Tabs.TabsWindow.RunMacroPlayRepeatDialog(() => Macro.ChooseMacro(state.Tabs.TabsWindow));

			var macro = Macro.Load(state.Tabs.TabsWindow, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((state.Tabs.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(state.Tabs.Focused.GetVariables()))
						return;

				state.Tabs.PlayMacro(macro, startNext);
			};
			startNext();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_PlayOnCopiedFiles(EditorExecuteState state)
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(state.Tabs.TabsWindow);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				state.Tabs.AddTab(new Tab(files.Dequeue()));
				state.Tabs.PlayMacro(macro, startNext);
			};
			startNext();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Open_Quick(EditorExecuteState state, int quickNum)
		{
			state.Tabs.AddTab(new Tab(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_TimeNextAction(EditorExecuteState state)
		{
			state.Tabs.timeNextAction = true;
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Visualize(EditorExecuteState state)
		{
			state.Tabs.MacroVisualize = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}
	}
}
