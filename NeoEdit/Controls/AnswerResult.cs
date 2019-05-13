using NeoEdit.Dialogs;

namespace NeoEdit.Controls
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
