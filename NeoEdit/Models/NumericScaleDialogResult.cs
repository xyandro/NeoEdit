using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class NumericScaleDialogResult
	{
		public string PrevMin { get; set; }
		public string PrevMax { get; set; }
		public string NewMin { get; set; }
		public string NewMax { get; set; }
	}
}
