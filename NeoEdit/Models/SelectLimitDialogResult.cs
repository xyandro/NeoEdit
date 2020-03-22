using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class SelectLimitDialogResult
	{
		public string FirstSelection { get; set; }
		public string EveryNth { get; set; }
		public string TakeCount { get; set; }
		public string NumSelections { get; set; }
		public bool JoinSelections { get; set; }
	}
}
