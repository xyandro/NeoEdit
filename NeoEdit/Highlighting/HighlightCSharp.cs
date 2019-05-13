using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.Highlighting
{
	class HighlightCSharp : Highlight
	{
		static List<string> keyWords = new List<string> { "abstract", "add", "alias", "as", "ascending", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "namespace", "new", "null", "object", "operator", "orderby", "out", "out", "override", "params", "partial", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "where", "where", "while", "yield" };
		static Regex keyWordsRE = new Regex(string.Join("|", keyWords.Select(word => string.Format(@"\b{0}\b", word))));
		static Brush keywordsBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static string stringEsc = @"@""([^""]|"""")*""";
		static string stringReg = @"""([^\\""]|\\.)*""";
		static Regex stringRE = new Regex($"{stringEsc}|{stringReg}");
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));

		static Regex commentRE = new Regex("//.*?$");
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(96, 139, 78));

		static HighlightCSharp()
		{
			keywordsBrush.Freeze();
			stringBrush.Freeze();
			commentBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				[keyWordsRE] = keywordsBrush,
				[stringRE] = stringBrush,
				[commentRE] = commentBrush,
			};
		}
	}
}
