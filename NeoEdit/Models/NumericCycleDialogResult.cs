using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class NumericCycleDialogResult
	{
		public string Minimum { get; set; }
		public string Maximum { get; set; }
		public bool IncludeBeginning { get; set; }
	}
}
