using System.Collections.Generic;
using System.Data.Common;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public interface INEFileData
	{
		long NESerial { get; }
		NEFile NEFile { get; }

		NETextPoint NETextPoint { get; }
		IReadOnlyList<NERange> Selections { get; }
		IReadOnlyList<NERange>[] Regions { get; }
		bool AllowOverlappingSelections { get; }
		string DBName { get; }
		int CurrentSelection { get; }
		string DisplayName { get; }
		string FileName { get; }
		bool AutoRefresh { get; }
		ParserType ContentType { get; }
		Coder.CodePage CodePage { get; }
		bool HasBOM { get; }
		string AESKey { get; }
		bool Compressed { get; }
		bool DiffIgnoreWhitespace { get; }
		bool DiffIgnoreCase { get; }
		bool DiffIgnoreNumbers { get; }
		bool DiffIgnoreLineEndings { get; }
		HashSet<char> DiffIgnoreCharacters { get; }
		bool KeepSelections { get; }
		bool HighlightSyntax { get; }
		bool StrictParsing { get; }
		JumpByType JumpBy { get; }
		bool ViewBinary { get; }
		HashSet<Coder.CodePage> ViewBinaryCodePages { get; }
		IReadOnlyList<HashSet<string>> ViewBinarySearches { get; }
		NEFile DiffTarget { get; }
		DbConnection DbConnection { get; }

		INEFileData Next();
	}
}
