using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class FilesCreateFromExpressionsDialogResult
	{
		public string FileName { get; set; }
		public string Data { get; set; }
		public Coder.CodePage CodePage { get; set; }
	}
}
