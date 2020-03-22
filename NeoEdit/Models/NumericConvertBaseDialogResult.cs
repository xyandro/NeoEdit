using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class NumericConvertBaseDialogResult
	{
		public Dictionary<char, int> InputSet { get; set; }
		public Dictionary<int, char> OutputSet { get; set; }
	}
}
