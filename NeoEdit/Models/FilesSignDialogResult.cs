using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class FilesSignDialogResult
	{
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
