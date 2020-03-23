using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

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
