using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class FilesHashDialogResult
	{
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
