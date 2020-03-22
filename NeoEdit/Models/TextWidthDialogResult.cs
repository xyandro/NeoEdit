using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class TextWidthDialogResult
	{
		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		public string Expression { get; set; }
		public char PadChar { get; set; }
		public TextLocation Location { get; set; }
	}
}
