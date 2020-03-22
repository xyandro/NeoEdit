﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Models
{
	public class EditDataEncryptDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
