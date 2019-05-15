namespace NeoEdit.Controls
{
	public class AnswerResult
	{
		public MessageOptions Answer { get; set; }

		public AnswerResult(MessageOptions answer = MessageOptions.None)
		{
			Answer = answer;
		}
	}
}
