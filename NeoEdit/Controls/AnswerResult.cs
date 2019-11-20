using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program.Controls
{
	public class AnswerResult
	{
		public bool Canceled => answers.Any(pair => pair.Value.HasFlag(MessageOptions.Cancel));

		readonly Dictionary<string, MessageOptions> answers = new Dictionary<string, MessageOptions>();

		public AnswerResult() { }

		public MessageOptions this[string str]
		{
			get => answers.ContainsKey(str) ? answers[str] : MessageOptions.None;
			set => answers[str] = value;
		}
	}
}
