using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.TextEditor
{
	class Highlighting
	{
		public enum HighlightingType
		{
			None,
			CSharp,
		}

		public static Highlighting Get(HighlightingType type)
		{
			switch (type)
			{
				case HighlightingType.None: return new HighlightingNone();
				case HighlightingType.CSharp: return new HighlightingCSharp();
			}
			return null;
		}
		public virtual Dictionary<Regex, Brush> GetDictionary() { return new Dictionary<Regex, Brush>(); }
	}

	class HighlightingNone : Highlighting { }

	class HighlightingCSharp : Highlighting
	{
		static List<string> keyWords = new List<string> { "abstract", "add", "alias", "as", "ascending", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "namespace", "new", "null", "object", "operator", "orderby", "out", "out", "override", "params", "partial", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "where", "where", "while", "yield" };
		static string keyWordsStr = String.Join("|", keyWords.Select(word => String.Format(@"\b{0}\b", word)));
		static Regex keyWordsRE = new Regex(keyWordsStr);
		static Brush keywordsBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static string stringEsc = @"@""([^""]|"""")*""";
		static string stringReg = @"""([^\\""]*|\\.)*""";
		static Regex stringRE = new Regex(stringEsc + "|" + stringReg);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				{ keyWordsRE, keywordsBrush },
				{ stringRE, stringBrush },
			};
		}
	}
}
