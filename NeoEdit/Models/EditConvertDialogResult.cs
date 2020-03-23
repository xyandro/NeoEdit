using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

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
