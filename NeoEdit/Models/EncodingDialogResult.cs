using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class EncodingDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
	}
}
