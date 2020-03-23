using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesOperationsSplitFileDialogResult
	{
		public string OutputTemplate { get; set; }
		public string ChunkSize { get; set; }
	}
}
