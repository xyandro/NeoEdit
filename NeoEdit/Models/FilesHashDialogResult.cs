using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesHashDialogResult
	{
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
