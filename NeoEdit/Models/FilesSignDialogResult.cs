﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class FilesSignDialogResult
	{
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
