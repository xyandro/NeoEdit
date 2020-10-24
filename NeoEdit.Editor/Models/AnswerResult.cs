using System.Collections.Generic;
using NeoEdit.Common.Enums;

namespace NeoEdit.Editor.Models
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
