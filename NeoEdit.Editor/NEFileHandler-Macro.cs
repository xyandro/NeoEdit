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
	partial class NEFileHandler
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		static bool PreExecute_Macro_Play_Quick(int quickNum)
		{
			EditorExecuteState.CurrentState.NEFiles.PlayMacro(Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow, QuickMacro(quickNum), true));
			return true;
		}

		static bool PreExecute_Macro_Play_Play()
		{
			EditorExecuteState.CurrentState.NEFiles.PlayMacro(Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow));
			return true;
		}

		static bool PreExecute_Macro_Play_Repeat()
		{
			var result = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_PreExecute_Macro_Play_Repeat(() => Macro.ChooseMacro(EditorExecuteState.CurrentState.NEFiles.FilesWindow));

			var macro = Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((EditorExecuteState.CurrentState.NEFiles.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables()))
						return;

				EditorExecuteState.CurrentState.NEFiles.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				EditorExecuteState.CurrentState.NEFiles.AddNewFile(new NEFileHandler(files.Dequeue()));
				EditorExecuteState.CurrentState.NEFiles.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Record_Quick(int quickNum)
		{
			if (EditorExecuteState.CurrentState.NEFiles.recordingMacro == null)
				PreExecute_Macro_Record_Record();
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Record_Record()
		{
			EditorExecuteState.CurrentState.NEFiles.EnsureNotRecording();
			EditorExecuteState.CurrentState.NEFiles.recordingMacro = new Macro();

			return true;
		}

		static bool PreExecute_Macro_Record_StopRecording(string fileName = null)
		{
			if (EditorExecuteState.CurrentState.NEFiles.recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = EditorExecuteState.CurrentState.NEFiles.recordingMacro;
			EditorExecuteState.CurrentState.NEFiles.recordingMacro = null;
			macro.Save(EditorExecuteState.CurrentState.NEFiles.FilesWindow, fileName, true);

			return true;
		}

		static bool PreExecute_Macro_Append_Quick(int quickNum)
		{
			if (EditorExecuteState.CurrentState.NEFiles.recordingMacro == null)
				EditorExecuteState.CurrentState.NEFiles.recordingMacro = Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow, QuickMacro(quickNum), true);
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Append_Append()
		{
			EditorExecuteState.CurrentState.NEFiles.EnsureNotRecording();
			EditorExecuteState.CurrentState.NEFiles.recordingMacro = Macro.Load(EditorExecuteState.CurrentState.NEFiles.FilesWindow);

			return true;
		}

		static bool PreExecute_Macro_Open_Quick(int quickNum)
		{
			EditorExecuteState.CurrentState.NEFiles.AddNewFile(new NEFileHandler(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
			return true;
		}

		static bool PreExecute_Macro_Visualize()
		{
			EditorExecuteState.CurrentState.NEFiles.MacroVisualize = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}
	}
}
