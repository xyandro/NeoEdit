using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class EditConvertDialogResult
	{
		public Coder.CodePage InputType { get; set; }
		public bool InputBOM { get; set; }
		public Coder.CodePage OutputType { get; set; }
		public bool OutputBOM { get; set; }
	}
}
