using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.TextEdit
{
	public class Highlighting
	{
		public enum HighlightingType
		{
			None,
			CSharp,
			CPlusPlus,
		}

		public static HighlightingType Get(string filename)
		{
			if (String.IsNullOrEmpty(filename))
				return HighlightingType.None;
			switch (Path.GetExtension(filename).ToLowerInvariant())
			{
				case ".cs": return HighlightingType.CSharp;
				case ".c":
				case ".cpp": return HighlightingType.CPlusPlus;
				default: return HighlightingType.None;
			}
		}

		public static Highlighting Get(HighlightingType type)
		{
			switch (type)
			{
				case HighlightingType.None: return new HighlightingNone();
				case HighlightingType.CSharp: return new HighlightingCSharp();
				case HighlightingType.CPlusPlus: return new HighlightingCPlusPlus();
			}
			return null;
		}
		public virtual Dictionary<Regex, Brush> GetDictionary() { return new Dictionary<Regex, Brush>(); }
	}

	class HighlightingNone : Highlighting { }

	class HighlightingCSharp : Highlighting
	{
		static List<string> keyWords = new List<string> { "abstract", "add", "alias", "as", "ascending", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "namespace", "new", "null", "object", "operator", "orderby", "out", "out", "override", "params", "partial", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "where", "where", "while", "yield" };
		static Regex keyWordsRE = new Regex(String.Join("|", keyWords.Select(word => String.Format(@"\b{0}\b", word))));
		static Brush keywordsBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static string stringEsc = @"@""([^""]|"""")*""";
		static string stringReg = @"""([^\\""]|\\.)*""";
		static Regex stringRE = new Regex(stringEsc + "|" + stringReg);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));

		static Regex commentRE = new Regex("//.*?$");
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(96, 139, 78));

		static HighlightingCSharp()
		{
			keywordsBrush.Freeze();
			stringBrush.Freeze();
			commentBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				{ keyWordsRE, keywordsBrush },
				{ stringRE, stringBrush },
				{ commentRE, commentBrush },
			};
		}
	}

	class HighlightingCPlusPlus : Highlighting
	{
		static List<string> keyWords = new List<string> { "__abstract", "__alignof", "__asm", "__assume", "__based", "__box", "__cdecl", "__declspec", "__delegate", "__event", "__except", "__fastcall", "__finally", "__forceinline", "__gc", "__hook", "__identifier", "__if_exists", "__if_not_exists", "__inline", "__int16", "__int32", "__int64", "__int8", "__interface", "__leave", "__m128", "__m128d", "__m128i", "__m64", "__multiple_inheritance", "__nogc", "__noop", "__pin", "__property", "__raise", "__sealed", "__single_inheritance", "__stdcall", "__super", "__thiscall", "__try", "__try_cast", "__unaligned", "__unhook", "__uuidof", "__value", "__virtual_inheritance", "__w64", "__wchar_t", "abstract", "alignas", "alignof", "and", "and_eq", "array", "asm", "auto", "bitand", "bitor", "bool", "break", "case", "catch", "char", "char16_t", "char32_t", "class", "compl", "const", "const_cast", "constexpr", "continue", "decltype", "default", "delegate", "delete", "deprecated", "dllexport", "dllimport", "do", "double", "dynamic_cast", "else", "enum", "event", "explicit", "export", "extern", "false", "finally", "float", "for", "for each", "friend", "friend_as", "gcnew", "generic", "goto", "if", "in", "initonly", "inline", "int", "interface", "interior_ptr", "literal", "long", "mutable", "naked", "namespace", "new", "noexcept", "noinline", "noreturn", "not", "not_eq", "nothrow", "novtable", "nullptr", "operator", "or", "or_eq", "private", "property", "protected", "public", "ref", "register", "reinterpret_cast", "return", "safecast", "sealed", "selectany", "short", "signed", "sizeof", "static", "static_assert", "static_cast", "struct", "switch", "template", "this", "thread", "thread_local", "throw", "true", "try", "typedef", "typeid", "typename", "typeof", "union", "unsigned", "using", "uuid", "value", "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq" };
		static Regex keyWordsRE = new Regex(String.Join("|", keyWords.Select(word => String.Format(@"\b{0}\b", word))));
		static Brush keywordsBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static List<string> directives = new List<string> { "define", "elif", "else", "endif", "error", "if", "ifdef", "ifndef", "include", "line", "pragma", "undef" };
		static Regex directivesRE = new Regex(String.Join("|", directives.Select(word => String.Format(@"\#{0}\b", word))));
		static Brush directivesBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static Regex stringRE = new Regex(@"""([^\\""]*|\\.)*""");
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));

		static Regex commentRE = new Regex("//.*?$");
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(96, 139, 78));

		static HighlightingCPlusPlus()
		{
			keywordsBrush.Freeze();
			directivesBrush.Freeze();
			stringBrush.Freeze();
			commentBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				{ keyWordsRE, keywordsBrush },
				{ directivesRE, directivesBrush },
				{ stringRE, stringBrush },
				{ commentRE, commentBrush },
			};
		}
	}
}
