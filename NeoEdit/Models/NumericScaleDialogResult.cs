using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

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
