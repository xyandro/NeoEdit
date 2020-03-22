using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class MacroPlayRepeatDialogResult
	{
		public enum RepeatTypeEnum
		{
			Number,
			Condition,
		}

		public string Macro { get; set; }
		public string Expression { get; set; }
		public RepeatTypeEnum RepeatType { get; set; }
	}
}
