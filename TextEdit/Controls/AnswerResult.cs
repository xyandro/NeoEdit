using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit.Controls
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
