using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class NumericCycleDialogResult
	{
		public string Minimum { get; set; }
		public string Maximum { get; set; }
		public bool IncludeBeginning { get; set; }
	}
}
