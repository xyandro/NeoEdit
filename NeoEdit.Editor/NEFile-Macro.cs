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
	partial class NEFile
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		static PreExecutionStop PreExecute_Macro_Play_Quick(EditorExecuteState state, int quickNum)
		{
			state.NEFiles.PlayMacro(Macro.Load(state.NEFiles.FilesWindow, QuickMacro(quickNum), true));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_Play(EditorExecuteState state)
		{
			state.NEFiles.PlayMacro(Macro.Load(state.NEFiles.FilesWindow));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_Repeat(EditorExecuteState state)
		{
			var result = state.NEFiles.FilesWindow.RunDialog_PreExecute_Macro_Play_Repeat(() => Macro.ChooseMacro(state.NEFiles.FilesWindow));

			var macro = Macro.Load(state.NEFiles.FilesWindow, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((state.NEFiles.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(state.NEFiles.Focused.GetVariables()))
						return;

				state.NEFiles.PlayMacro(macro, startNext);
			};
			startNext();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Play_PlayOnCopiedFiles(EditorExecuteState state)
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(state.NEFiles.FilesWindow);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				state.NEFiles.AddFile(new NEFile(files.Dequeue()));
				state.NEFiles.PlayMacro(macro, startNext);
			};
			startNext();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Record_Quick(EditorExecuteState state, int quickNum)
		{
			if (state.NEFiles.recordingMacro == null)
				PreExecute_Macro_Record_Record(state);
			else
				PreExecute_Macro_Record_StopRecording(state, QuickMacro(quickNum));

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Record_Record(EditorExecuteState state)
		{
			state.NEFiles.EnsureNotRecording();
			state.NEFiles.recordingMacro = new Macro();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Record_StopRecording(EditorExecuteState state, string fileName = null)
		{
			if (state.NEFiles.recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = state.NEFiles.recordingMacro;
			state.NEFiles.recordingMacro = null;
			macro.Save(state.NEFiles.FilesWindow, fileName, true);

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Append_Quick(EditorExecuteState state, int quickNum)
		{
			if (state.NEFiles.recordingMacro == null)
				state.NEFiles.recordingMacro = Macro.Load(state.NEFiles.FilesWindow, QuickMacro(quickNum), true);
			else
				PreExecute_Macro_Record_StopRecording(state, QuickMacro(quickNum));

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Append_Append(EditorExecuteState state)
		{
			state.NEFiles.EnsureNotRecording();
			state.NEFiles.recordingMacro = Macro.Load(state.NEFiles.FilesWindow);

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Open_Quick(EditorExecuteState state, int quickNum)
		{
			state.NEFiles.AddFile(new NEFile(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_Macro_Visualize(EditorExecuteState state)
		{
			state.NEFiles.MacroVisualize = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}
	}
}
