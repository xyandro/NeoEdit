using System;
using System.Collections.Generic;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class TextSelectWholeBoundedWordDialogResult
	{
		public HashSet<char> Chars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
