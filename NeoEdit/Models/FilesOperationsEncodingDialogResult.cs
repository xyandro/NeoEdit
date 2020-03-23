using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Models
{
	public class FilesOperationsEncodingDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
