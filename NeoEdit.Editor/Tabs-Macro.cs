using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		Macro playingMacro, recordingMacro;
		Action playingMacroNextAction;
		void PlayMacro(Macro macro, Action action = null)
		{
			playingMacro = macro;
			playingMacroNextAction = action;
		}

		static string QuickMacro(int num) => $"QuickText{num}.xml";

		void EnsureNotRecording()
		{
			if (recordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Execute_Macro_Record_Quick(int quickNum)
		{
			if (recordingMacro == null)
				Execute_Macro_Record_Record();
			else
				Execute_Macro_Record_StopRecording(QuickMacro(quickNum));
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
			macro.Save(TabsWindow, fileName, true);
		}

		void Execute_Macro_Append_Quick(int quickNum)
		{
			if (recordingMacro == null)
				recordingMacro = Macro.Load(TabsWindow, QuickMacro(quickNum), true);
			else
				Execute_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Execute_Macro_Append_Append()
		{
			EnsureNotRecording();
			recordingMacro = Macro.Load(TabsWindow);
		}

		void Execute_Macro_Play_Quick(int quickNum) => PlayMacro(Macro.Load(TabsWindow, QuickMacro(quickNum), true));

		void Execute_Macro_Play_Play() => PlayMacro(Macro.Load(TabsWindow));

		void Execute_Macro_Play_Repeat()
		{
			var result = TabsWindow.RunMacroPlayRepeatDialog(() => Macro.ChooseMacro(TabsWindow));

			var macro = Macro.Load(TabsWindow, result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialogResult.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(Focused.GetVariables()))
						return;

				PlayMacro(macro, startNext);
			};
			startNext();
		}

		void Execute_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load(TabsWindow);
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				AddTab(new Tab(files.Dequeue()));
				PlayMacro(macro, startNext);
			};
			startNext();
		}

		void Execute_Macro_Open_Quick(int quickNum) => AddTab(new Tab(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));

		void Execute_Macro_TimeNextAction() => timeNextAction = true;

		void Execute_Macro_Visualize(bool? multiStatus) => MacroVisualize = multiStatus != true;
	}
}
