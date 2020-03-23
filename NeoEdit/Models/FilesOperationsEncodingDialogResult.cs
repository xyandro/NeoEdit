using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesOperationsEncodingDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
