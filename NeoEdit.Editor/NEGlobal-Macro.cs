using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;

namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		static string GetQuickMacroName(int num) => $"QuickMacro{num}.xml";

		public Macro recordingMacro;
		public void PlayMacro(Macro macro, Action action = null) => QueueActions(macro.Actions);

		public void EnsureNotRecording()
		{
			if (recordingMacro != null)
				throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Execute__Macro_Play_Quick_1__Macro_Play_Quick_2__Macro_Play_Quick_3__Macro_Play_Quick_4__Macro_Play_Quick_5__Macro_Play_Quick_6__Macro_Play_Quick_7__Macro_Play_Quick_8__Macro_Play_Quick_9__Macro_Play_Quick_10__Macro_Play_Quick_11__Macro_Play_Quick_12(int quickNum) => PlayMacro(Macro.Load(state.NEWindow.neWindowUI, GetQuickMacroName(quickNum), true));

		void Execute__Macro_Play_Play() => PlayMacro(Macro.Load(state.NEWindow.neWindowUI));

		void Execute__Macro_Play_Repeat()
		{
			var result = state.NEWindow.neWindowUI.RunDialog_PreExecute_Macro_Play_Repeat(() => Macro.ChooseMacro(state.NEWindow.neWindowUI));

			var macro = Macro.Load(state.NEWindow.neWindowUI, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.EvaluateOne<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((state.NEWindow.Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.EvaluateOne<bool>(state.NEWindow.Focused.GetVariables()))
						return;

				PlayMacro(macro, startNext);
			};
			startNext();
		}

		void Execute__Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(state.NEWindow.neWindowUI);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				state.NEWindow.AddNewNEFile(new NEFile(files.Dequeue()));
				PlayMacro(macro, startNext);
			};
			startNext();
		}

		void Execute__Macro_Record_Quick_1__Macro_Record_Quick_2__Macro_Record_Quick_3__Macro_Record_Quick_4__Macro_Record_Quick_5__Macro_Record_Quick_6__Macro_Record_Quick_7__Macro_Record_Quick_8__Macro_Record_Quick_9__Macro_Record_Quick_10__Macro_Record_Quick_11__Macro_Record_Quick_12(int quickNum)
		{
			if (recordingMacro == null)
				Execute__Macro_Record_Record();
			else
				Execute__Macro_Record_StopRecording(GetQuickMacroName(quickNum));
		}

		void Execute__Macro_Record_Record()
		{
			EnsureNotRecording();
			recordingMacro = new Macro();
		}

		void Execute__Macro_Record_StopRecording(string fileName = null)
		{
			if (recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = recordingMacro;
			recordingMacro = null;
			macro.Save(state.NEWindow.neWindowUI, fileName, true);
		}

		void Execute__Macro_Append_Quick_1__Macro_Append_Quick_2__Macro_Append_Quick_3__Macro_Append_Quick_4__Macro_Append_Quick_5__Macro_Append_Quick_6__Macro_Append_Quick_7__Macro_Append_Quick_8__Macro_Append_Quick_9__Macro_Append_Quick_10__Macro_Append_Quick_11__Macro_Append_Quick_12(int quickNum)
		{
			if (recordingMacro == null)
				recordingMacro = Macro.Load(state.NEWindow.neWindowUI, GetQuickMacroName(quickNum), true);
			else
				Execute__Macro_Record_StopRecording(GetQuickMacroName(quickNum));
		}

		void Execute__Macro_Append_Append()
		{
			EnsureNotRecording();
			recordingMacro = Macro.Load(state.NEWindow.neWindowUI);
		}

		void Execute__Macro_Open_Quick_1__Macro_Open_Quick_2__Macro_Open_Quick_3__Macro_Open_Quick_4__Macro_Open_Quick_5__Macro_Open_Quick_6__Macro_Open_Quick_7__Macro_Open_Quick_8__Macro_Open_Quick_9__Macro_Open_Quick_10__Macro_Open_Quick_11__Macro_Open_Quick_12(int quickNum) => state.NEWindow.AddNewNEFile(new NEFile(Path.Combine(Macro.MacroDirectory, GetQuickMacroName(quickNum))));

		void Execute__Macro_Visualize() => MacroVisualize = state.MultiStatus != true;
	}
}
