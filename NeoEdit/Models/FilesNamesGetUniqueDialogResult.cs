using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesNamesGetUniqueDialogResult
	{
		public string Format { get; set; }
		public bool CheckExisting { get; set; }
		public bool RenameAll { get; set; }
		public bool UseGUIDs { get; set; }
	}
}
