using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Program.Content;
using NeoEdit.Program.Controls;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Program.Models
{
	public class ContentAttributeDialogResult
	{
		public string Attribute { get; set; }
		public Regex Regex { get; set; }
		public bool Invert { get; set; }
	}
}
