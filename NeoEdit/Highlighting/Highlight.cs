using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using NeoEdit.Common.Enums;

namespace NeoEdit.Program.Highlighting
{
	public class Highlight
	{
		public static Highlight Get(ParserType type)
		{
			switch (type)
			{
				case ParserType.Columns: case ParserType.ExactColumns: return new HighlightColumns();
				case ParserType.CPlusPlus: return new HighlightCPlusPlus();
				case ParserType.CSharp: return new HighlightCSharp();
				case ParserType.HTML: case ParserType.XML: return new HighlightMarkup();
				case ParserType.JSON: return new HighlightJSON();
				case ParserType.SQL: return new HighlightSQL();
				default: return null;
			}
		}
		public virtual Dictionary<Regex, Brush> GetDictionary() => new Dictionary<Regex, Brush>();
	}
}
