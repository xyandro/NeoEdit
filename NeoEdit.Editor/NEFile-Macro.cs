using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		static bool PreExecute_Macro_Play_Quick(int quickNum)
		{
			EditorExecuteState.CurrentState.NEWindow.PlayMacro(Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow, QuickMacro(quickNum), true));
			return true;
		}

		static bool PreExecute_Macro_Play_Play()
		{
			EditorExecuteState.CurrentState.NEWindow.PlayMacro(Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow));
			return true;
		}

		static bool PreExecute_Macro_Play_Repeat()
		{
			var result = EditorExecuteState.CurrentState.NEWindow.FilesWindow.RunDialog_PreExecute_Macro_Play_Repeat(() => Macro.ChooseMacro(EditorExecuteState.CurrentState.NEWindow.FilesWindow));

			var macro = Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((EditorExecuteState.CurrentState.NEWindow.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(EditorExecuteState.CurrentState.NEWindow.Focused.GetVariables()))
						return;

				EditorExecuteState.CurrentState.NEWindow.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				EditorExecuteState.CurrentState.NEWindow.AddNewFile(new NEFile(files.Dequeue()));
				EditorExecuteState.CurrentState.NEWindow.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Record_Quick(int quickNum)
		{
			if (EditorExecuteState.CurrentState.NEWindow.recordingMacro == null)
				PreExecute_Macro_Record_Record();
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Record_Record()
		{
			EditorExecuteState.CurrentState.NEWindow.EnsureNotRecording();
			EditorExecuteState.CurrentState.NEWindow.recordingMacro = new Macro();

			return true;
		}

		static bool PreExecute_Macro_Record_StopRecording(string fileName = null)
		{
			if (EditorExecuteState.CurrentState.NEWindow.recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = EditorExecuteState.CurrentState.NEWindow.recordingMacro;
			EditorExecuteState.CurrentState.NEWindow.recordingMacro = null;
			macro.Save(EditorExecuteState.CurrentState.NEWindow.FilesWindow, fileName, true);

			return true;
		}

		static bool PreExecute_Macro_Append_Quick(int quickNum)
		{
			if (EditorExecuteState.CurrentState.NEWindow.recordingMacro == null)
				EditorExecuteState.CurrentState.NEWindow.recordingMacro = Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow, QuickMacro(quickNum), true);
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Append_Append()
		{
			EditorExecuteState.CurrentState.NEWindow.EnsureNotRecording();
			EditorExecuteState.CurrentState.NEWindow.recordingMacro = Macro.Load(EditorExecuteState.CurrentState.NEWindow.FilesWindow);

			return true;
		}

		static bool PreExecute_Macro_Open_Quick(int quickNum)
		{
			EditorExecuteState.CurrentState.NEWindow.AddNewFile(new NEFile(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
			return true;
		}

		static bool PreExecute_Macro_Visualize()
		{
			EditorExecuteState.CurrentState.NEWindow.MacroVisualize = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}
	}
}
