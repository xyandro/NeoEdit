using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesSetAttributesDialogResult
	{
		public Dictionary<FileAttributes, bool?> Attributes { get; set; }
	}
}
