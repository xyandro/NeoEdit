using System.Collections.Generic;
using NeoEdit.Content;
using NeoEdit.Parsing;

namespace NeoEdit
{
	public interface ITextEditor
	{
		Parser.ParserType ContentType { get; set; }
		CacheValue previousData { get; }
		Parser.ParserType previousType { get; set; }
		ParserNode previousRoot { get; set; }
		TextData Data { get; }
		bool HighlightSyntax { get; set; }
		bool StrictParsing { get; set; }
		void SetSelections(List<Range> selections, bool deOverlap = true);
		RangeList Selections { get; }
		string FileName { get; set; }
		Range FullRange { get; }
		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		bool KeepSelections { get; set; }
		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		Tabs TabsParent { get; }
		Range MoveCursor(Range range, int cursor, bool selecting);
	}
}
