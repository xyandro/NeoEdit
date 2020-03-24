using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditFindFindDialogResult
	{
		public enum ResultType
		{
			None,
			CopyCount,
			FindNext,
			FindAll,
		}

		public string Text { get; set; }
		public bool IsExpression { get; set; }
		public bool AlignSelections { get; set; }
		public bool IsBoolean { get; set; }
		public bool IsRegex { get; set; }
		public bool RegexGroups { get; set; }
		public bool IsBinary { get; set; }
		public HashSet<Coder.CodePage> CodePages { get; set; }
		public bool WholeWords { get; set; }
		public bool MatchCase { get; set; }
		public bool SelectionOnly { get; set; }
		public bool EntireSelection { get; set; }
		public bool KeepMatching { get; set; }
		public bool RemoveMatching { get; set; }
		public ResultType Type { get; set; }
	}
}
