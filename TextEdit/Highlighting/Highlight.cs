using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.TextEdit.Highlighting
{
	public class Highlight
	{
		public static HighlightType Get(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return HighlightType.None;
			switch (Path.GetExtension(filename).ToLowerInvariant())
			{
				case ".cs": return HighlightType.CSharp;
				case ".c":
				case ".cpp": return HighlightType.CPlusPlus;
				case ".htm":
				case ".html":
				case ".csproj":
				case ".vbproj":
				case ".vcxproj":
				case ".xml":
				case ".xaml": return HighlightType.Markup;
				default: return HighlightType.None;
			}
		}

		public static Highlight Get(HighlightType type)
		{
			switch (type)
			{
				case HighlightType.None: return new HighlightNone();
				case HighlightType.CSharp: return new HighlightCSharp();
				case HighlightType.CPlusPlus: return new HighlightCPlusPlus();
				case HighlightType.Markup: return new HighlightMarkup();
			}
			return null;
		}
		public virtual Dictionary<Regex, Brush> GetDictionary() => new Dictionary<Regex, Brush>();
	}
}
