using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class FilesOperationsSplitFileDialogResult
	{
		public string OutputTemplate { get; set; }
		public string ChunkSize { get; set; }
	}
}
