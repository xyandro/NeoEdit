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
		static string QuickMacro(int num) => $"QuickMacro{num}.xml";

		static bool PreExecute_Macro_Play_Quick(int quickNum)
		{
			state.NEWindow.PlayMacro(Macro.Load(state.NEWindowUI, QuickMacro(quickNum), true));
			return true;
		}

		static bool PreExecute_Macro_Play_Play()
		{
			state.NEWindow.PlayMacro(Macro.Load(state.NEWindowUI));
			return true;
		}

		static bool PreExecute_Macro_Play_Repeat()
		{
			var result = state.NEWindowUI.RunDialog_PreExecute_Macro_Play_Repeat(() => Macro.ChooseMacro(state.NEWindowUI));

			var macro = Macro.Load(state.NEWindowUI, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((state.NEWindow.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(state.NEWindow.Focused.GetVariables()))
						return;

				state.NEWindow.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(state.NEWindowUI);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				state.NEWindow.AddNewFile(new NEFile(files.Dequeue()));
				state.NEWindow.PlayMacro(macro, startNext);
			};
			startNext();

			return true;
		}

		static bool PreExecute_Macro_Record_Quick(int quickNum)
		{
			if (state.NEWindow.recordingMacro == null)
				PreExecute_Macro_Record_Record();
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Record_Record()
		{
			state.NEWindow.EnsureNotRecording();
			state.NEWindow.recordingMacro = new Macro();

			return true;
		}

		static bool PreExecute_Macro_Record_StopRecording(string fileName = null)
		{
			if (state.NEWindow.recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = state.NEWindow.recordingMacro;
			state.NEWindow.recordingMacro = null;
			macro.Save(state.NEWindowUI, fileName, true);

			return true;
		}

		static bool PreExecute_Macro_Append_Quick(int quickNum)
		{
			if (state.NEWindow.recordingMacro == null)
				state.NEWindow.recordingMacro = Macro.Load(state.NEWindowUI, QuickMacro(quickNum), true);
			else
				PreExecute_Macro_Record_StopRecording(QuickMacro(quickNum));

			return true;
		}

		static bool PreExecute_Macro_Append_Append()
		{
			state.NEWindow.EnsureNotRecording();
			state.NEWindow.recordingMacro = Macro.Load(state.NEWindowUI);

			return true;
		}

		static bool PreExecute_Macro_Open_Quick(int quickNum)
		{
			state.NEWindow.AddNewFile(new NEFile(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
			return true;
		}

		static bool PreExecute_Macro_Visualize()
		{
			state.NEWindow.MacroVisualize = state.MultiStatus != true;
			return true;
		}
	}
}
