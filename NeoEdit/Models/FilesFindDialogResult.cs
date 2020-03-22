using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
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
