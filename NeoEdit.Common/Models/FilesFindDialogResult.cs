using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class FilesFindDialogResult
	{
		public string Text { get; set; }
		public bool IsExpression { get; set; }
		public bool AlignSelections { get; set; }
		public bool IsRegex { get; set; }
		public bool IsBinary { get; set; }
		public HashSet<Coder.CodePage> CodePages { get; set; }
		public bool MatchCase { get; set; }
	}
}
