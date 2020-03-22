using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class EditFindRegexReplaceDialogResult
	{
		public string Text { get; set; }
		public string Replace { get; set; }
		public bool WholeWords { get; set; }
		public bool MatchCase { get; set; }
		public bool SelectionOnly { get; set; }
		public bool EntireSelection { get; set; }
	}
}
