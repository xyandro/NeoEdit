using System.Collections.Generic;

namespace NeoEdit.Program.Controls
{
	public class AnswerResult
	{
		readonly Dictionary<string, MessageOptions> answers = new Dictionary<string, MessageOptions>();

		public AnswerResult() { }

		public MessageOptions this[string str]
		{
			get => answers.ContainsKey(str) ? answers[str] : MessageOptions.None;
			set => answers[str] = value;
		}

		public void Clear() => answers.Clear();
	}
}
