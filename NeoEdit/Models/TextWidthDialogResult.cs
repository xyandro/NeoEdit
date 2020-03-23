using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

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
