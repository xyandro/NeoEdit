using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class EditDataCompressDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Compressor.Type CompressorType { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
