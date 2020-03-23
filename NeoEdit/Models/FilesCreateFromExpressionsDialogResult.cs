using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesCreateFromExpressionsDialogResult
	{
		public string FileName { get; set; }
		public string Data { get; set; }
		public Coder.CodePage CodePage { get; set; }
	}
}
