using System;
using System.Collections.Generic;
using System.Data.Common;
using NeoEdit.Content;
using NeoEdit.Expressions;
using NeoEdit.Parsing;
using NeoEdit.Transform;

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
		DbConnection dbConnection { get; set; }
		string DBName { get; set; }
		List<string> GetSelectionStrings();
		void OpenTable(ITextEditor te, Table table, string name = null);
		string GetString(Range range);
		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		void CalculateDiff();
		Coder.CodePage CodePage { get; set; }
		bool DiffIgnoreCase { get; set; }
		string DiffIgnoreCharacters { get; set; }
		bool DiffIgnoreLineEndings { get; set; }
		bool DiffIgnoreNumbers { get; set; }
		bool DiffIgnoreWhitespace { get; set; }
		TextEditor DiffTarget { get; set; }
		IReadOnlyDictionary<int, RangeList> Regions { get; }
		int BeginOffset { get; }
		RangeList Bookmarks { get; }
		bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage);
		bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage);
		List<string> Clipboard { get; }
		int EndOffset { get; }
		List<T> GetFixedExpressionResults<T>(string expression);
		int GetNextWord(int offset);
		int GetPrevWord(int offset);
		List<T> GetVariableExpressionResults<T>(string expression);
		NEVariables GetVariables();
		WordSkipType GetWordSkipType(int line, int index);
		JumpByType JumpBy { get; set; }
		Dictionary<string, int> keysHash { get; }
		void ReplaceOneWithMany(List<string> strs, bool? addNewLines);
		RangeList Searches { get; }
		void SetBookmarks(List<Range> bookmarks);
		void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false);
		void SetClipboardStrings(IEnumerable<string> strs);
		void SetRegions(int region, List<Range> regions);
		void SetSearches(List<Range> searches);
		bool StringsAreFiles(List<string> strs);
		UndoRedo undoRedo { get; }
		bool IncludeInlineVariables { get; set; }
		string AESKey { get; set; }
		bool Compressed { get; set; }
		string DisplayName { get; set; }
		DragType doDrag { get; set; }
		DateTime fileLastWrite { get; set; }
		bool IsModified { get; }
		string LineEnding { get; }
		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, bool? modified = null, bool keepUndo = false);
		List<string> RelativeSelectedFiles();
		void Save(string fileName, bool copyOnly = false);
		void SetAutoRefresh(bool? value = null);
		void SetClipboardFile(string fileName, bool isCut = false);
		void SetClipboardString(string text);
		void SetFileName(string fileName);
	}
}
