using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class Tabs
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		static void ValidateNoCurrentMacro(ITabs tabs)
		{
			if (tabs.RecordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		static public void Command_Macro_Record_Quick(ITabs tabs, int quickNum)
		{
			if (tabs.RecordingMacro == null)
				Command_Macro_Record_Record(tabs);
			else
				Command_Macro_Record_StopRecording(tabs, QuickMacro(quickNum));
		}

		static public void Command_Macro_Record_Record(ITabs tabs)
		{
			ValidateNoCurrentMacro(tabs);
			tabs.RecordingMacro = new Macro();
		}

		static public void Command_Macro_Record_StopRecording(ITabs tabs, string fileName = null)
		{
			if (tabs.RecordingMacro == null)
			{
				new Message(tabs.WindowParent)
				{
					Title = "Error",
					Text = $"Cannot stop recording; recording not in progess.",
					Options = MessageOptions.Ok,
				}.Show();
				return;
			}

			var macro = tabs.RecordingMacro;
			tabs.RecordingMacro = null;
			macro.Save(fileName, true);
		}

		static public void Command_Macro_Append_Quick(ITabs tabs, int quickNum)
		{
			if (tabs.RecordingMacro == null)
				tabs.RecordingMacro = Macro.Load(QuickMacro(quickNum), true);
			else
				Command_Macro_Record_StopRecording(tabs, QuickMacro(quickNum));
		}

		static public void Command_Macro_Append_Append(ITabs tabs)
		{
			ValidateNoCurrentMacro(tabs);
			tabs.RecordingMacro = Macro.Load();
		}

		static public void Command_Macro_Play_Quick(ITabs tabs, int quickNum) => Macro.Load(QuickMacro(quickNum), true).Play(tabs, playing => tabs.MacroPlaying = playing);

		static public void Command_Macro_Play_Play(ITabs tabs) => Macro.Load().Play(tabs, playing => tabs.MacroPlaying = playing);

		static public void Command_Macro_Play_Repeat(ITabs tabs)
		{
			var result = MacroPlayRepeatDialog.Run(tabs.WindowParent, Macro.ChooseMacro);
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
				if ((tabs.TopMost == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(tabs.TopMost.GetVariables()))
						return;

				macro.Play(tabs, playing => tabs.MacroPlaying = playing, startNext);
			};
			startNext();
		}

		static public void Command_Macro_Play_PlayOnCopiedFiles(ITabs tabs)
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load();
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				tabs.Add(files.Dequeue());
				macro.Play(tabs, playing => tabs.MacroPlaying = playing, startNext);
			};
			startNext();
		}

		static public void Command_Macro_Open_Quick(ITabs tabs, int quickNum) => tabs.Add(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum)));
	}
}
