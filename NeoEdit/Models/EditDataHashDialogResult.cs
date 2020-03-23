using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class EditDataHashDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
