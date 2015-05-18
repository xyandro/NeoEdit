using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Common;
using NeoEdit.TextEdit.Dialogs;
using HtmlAgilityPack;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		void TidyHTML()
		{
			// Validates too
			var allRange = new Range(BeginOffset(), EndOffset());
			var str = GetString(allRange);
			str = Win32.Interop.HTMLTidy(str);
			var doc = new HtmlDocument();
			doc.LoadHtml(str);
			Replace(new List<Range> { allRange }, new List<string> { doc.DocumentNode.OuterHtml });
		}

		void ValidateHTML()
		{
			var allRange = new Range(BeginOffset(), EndOffset());
			var doc = new HtmlDocument();
			doc.LoadHtml(GetString(allRange));
			Replace(new List<Range> { allRange }, new List<string> { doc.DocumentNode.OuterHtml });
		}
	}
}
