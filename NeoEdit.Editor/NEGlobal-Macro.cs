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

		void Execute_Macro_Play_Quick(int quickNum) => PlayMacro(Macro.Load(state.NEWindow.neWindowUI, GetQuickMacroName(quickNum), true));

		void Execute_Macro_Play_Play() => PlayMacro(Macro.Load(state.NEWindow.neWindowUI));

		void Execute_Macro_Play_Repeat()
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

		void Execute_Macro_Play_PlayOnCopiedFiles()
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

		void Execute_Macro_Record_Quick(int quickNum)
		{
			if (recordingMacro == null)
				Execute_Macro_Record_Record();
			else
				Execute_Macro_Record_StopRecording(GetQuickMacroName(quickNum));
		}

		void Execute_Macro_Record_Record()
		{
			EnsureNotRecording();
			recordingMacro = new Macro();
		}

		void Execute_Macro_Record_StopRecording(string fileName = null)
		{
			if (recordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = recordingMacro;
			recordingMacro = null;
			macro.Save(state.NEWindow.neWindowUI, fileName, true);
		}

		void Execute_Macro_Append_Quick(int quickNum)
		{
			if (recordingMacro == null)
				recordingMacro = Macro.Load(state.NEWindow.neWindowUI, GetQuickMacroName(quickNum), true);
			else
				Execute_Macro_Record_StopRecording(GetQuickMacroName(quickNum));
		}

		void Execute_Macro_Append_Append()
		{
			EnsureNotRecording();
			recordingMacro = Macro.Load(state.NEWindow.neWindowUI);
		}

		void Execute_Macro_Open_Quick(int quickNum) => state.NEWindow.AddNewNEFile(new NEFile(Path.Combine(Macro.MacroDirectory, GetQuickMacroName(quickNum))));

		void Execute_Macro_Visualize() => MacroVisualize = state.MultiStatus != true;
	}
}
