using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using NeoEdit.Content;

namespace NeoEdit.Highlighting
{
	public class Highlight
	{
		public static Highlight Get(Parser.ParserType type)
		{
			switch (type)
			{
				case Parser.ParserType.Columns: case Parser.ParserType.ExactColumns: return new HighlightColumns();
				case Parser.ParserType.CPlusPlus: return new HighlightCPlusPlus();
				case Parser.ParserType.CSharp: return new HighlightCSharp();
				case Parser.ParserType.HTML: case Parser.ParserType.XML: return new HighlightMarkup();
				case Parser.ParserType.JSON: return new HighlightJSON();
				case Parser.ParserType.SQL: return new HighlightSQL();
				default: return null;
			}
		}
		public virtual Dictionary<Regex, Brush> GetDictionary() => new Dictionary<Regex, Brush>();
	}
}
