﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Models
{
	public class EncodingDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
	}
}
