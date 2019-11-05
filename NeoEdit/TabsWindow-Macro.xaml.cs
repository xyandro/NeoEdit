using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.NEClipboards;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		static string QuickMacro(int num) => $"QuickText{num}.xml";

		void ValidateNoCurrentMacro()
		{
			if (RecordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Command_Macro_Record_Quick(int quickNum)
		{
			if (RecordingMacro == null)
				Command_Macro_Record_Record();
			else
				Command_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Command_Macro_Record_Record()
		{
			ValidateNoCurrentMacro();
			RecordingMacro = new Macro();
		}

		void Command_Macro_Record_StopRecording(string fileName = null)
		{
			if (RecordingMacro == null)
			{
				new Message(this)
				{
					Title = "Error",
					Text = $"Cannot stop recording; recording not in progess.",
					Options = MessageOptions.Ok,
				}.Show();
				return;
			}

			var macro = RecordingMacro;
			RecordingMacro = null;
			macro.Save(fileName, true);
		}

		void Command_Macro_Append_Quick(int quickNum)
		{
			if (RecordingMacro == null)
				RecordingMacro = Macro.Load(QuickMacro(quickNum), true);
			else
				Command_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Command_Macro_Append_Append()
		{
			ValidateNoCurrentMacro();
			RecordingMacro = Macro.Load();
		}

		void Command_Macro_Play_Quick(int quickNum) => Macro.Load(QuickMacro(quickNum), true).Play(this, playing => MacroPlaying = playing);

		void Command_Macro_Play_Play() => Macro.Load().Play(this, playing => MacroPlaying = playing);

		void Command_Macro_Play_Repeat()
		{
			var result = MacroPlayRepeatDialog.Run(this, Macro.ChooseMacro);
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

				macro.Play(this, playing => MacroPlaying = playing, startNext);
			};
			startNext();
		}

		void Command_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load();
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				AddTextEditor(new TextEditor(files.Dequeue()));
				macro.Play(this, playing => MacroPlaying = playing, startNext);
			};
			startNext();
		}

		void Command_Macro_Open_Quick(int quickNum) => AddTextEditor(new TextEditor(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));
	}
}
