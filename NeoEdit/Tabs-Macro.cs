using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.NEClipboards;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		void ValidateNoCurrentMacro()
		{
			if (RecordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Execute_Macro_Record_Quick(int quickNum)
		{
			if (RecordingMacro == null)
				Execute_Macro_Record_Record();
			else
				Execute_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Execute_Macro_Record_Record()
		{
			ValidateNoCurrentMacro();
			RecordingMacro = new Macro();
		}

		void Execute_Macro_Record_StopRecording(string fileName = null)
		{
			if (RecordingMacro == null)
				throw new Exception($"Cannot stop recording; recording not in progess.");

			var macro = RecordingMacro;
			RecordingMacro = null;
			macro.Save(fileName, true);
		}

		void Execute_Macro_Append_Quick(int quickNum)
		{
			if (RecordingMacro == null)
				RecordingMacro = Macro.Load(QuickMacro(quickNum), true);
			else
				Execute_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Execute_Macro_Append_Append()
		{
			ValidateNoCurrentMacro();
			RecordingMacro = Macro.Load();
		}

		void Execute_Macro_Play_Quick(int quickNum) => Macro.Load(QuickMacro(quickNum), true).Play(state.TabsWindow, playing => MacroPlaying = playing);

		void Execute_Macro_Play_Play() => Macro.Load().Play(state.TabsWindow, playing => MacroPlaying = playing);

		void Execute_Macro_Play_Repeat()
		{
			var result = MacroPlayRepeatDialog.Run(state.TabsWindow, Macro.ChooseMacro);
			if (result == null)
				return;

			var macro = Macro.Load(result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((Focused == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(Focused.GetVariables()))
						return;

				macro.Play(state.TabsWindow, playing => MacroPlaying = playing, startNext);
			};
			startNext();
		}

		void Execute_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load();
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				AddTab(new Tab(files.Dequeue()));
				macro.Play(state.TabsWindow, playing => MacroPlaying = playing, startNext);
			};
			startNext();
		}

		void Execute_Macro_Open_Quick(int quickNum) => AddTab(new Tab(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));

		void Execute_Macro_TimeNextAction() => timeNextAction = true;
	}
}
