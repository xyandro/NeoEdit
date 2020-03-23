using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class NumericMinMaxValuesDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
		public bool Min { get; set; }
		public bool Max { get; set; }
	}
}
