using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Models
{
	public class NumericMinMaxValuesDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
		public bool Min { get; set; }
		public bool Max { get; set; }
	}
}
