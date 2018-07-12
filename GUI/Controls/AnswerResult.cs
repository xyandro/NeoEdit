﻿using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class AnswerResult
	{
		public Message.OptionsEnum Answer { get; set; }

		public AnswerResult(Message.OptionsEnum answer = Message.OptionsEnum.None)
		{
			Answer = answer;
		}
	}
}