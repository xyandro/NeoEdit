using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using NeoEdit.Content;
using NeoEdit.Expressions;
using NeoEdit.Parsing;
using NeoEdit.Transform;

namespace NeoEdit
{
	public interface ITextEditor
	{
		TextData Data { get; }
		string DisplayName { get; set; }
		string FileName { get; set; }
		bool IsModified { get; }
		Parser.ParserType ContentType { get; set; }
		Coder.CodePage CodePage { get; set; }
		string AESKey { get; set; }
		bool Compressed { get; set; }
		string LineEnding { get; }
		bool DiffIgnoreWhitespace { get; set; }
		bool DiffIgnoreCase { get; set; }
		bool DiffIgnoreNumbers { get; set; }
		bool DiffIgnoreLineEndings { get; set; }
		Tabs TabsParent { get; }
		int CurrentSelection { get; set; }
		List<string> Clipboard { get; }
		TextEditor DiffTarget { get; set; }
		RangeList Selections { get; }
		void SetSelections(List<Range> selections, bool deOverlap = true);
		RangeList Searches { get; }
		void SetSearches(List<Range> searches);
		RangeList Bookmarks { get; }
		void SetBookmarks(List<Range> bookmarks);
		IReadOnlyDictionary<int, RangeList> Regions { get; }
		void SetRegions(int region, List<Range> regions);
		UndoRedo undoRedo { get; }
		DateTime fileLastWrite { get; set; }
		DragType doDrag { get; set; }
		string DiffIgnoreCharacters { get; set; }
		int BeginOffset { get; }
		int EndOffset { get; }
		Range BeginRange { get; }
		Range FullRange { get; }
		string AllText { get; }
		void CalculateDiff();
		void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false);
		List<T> GetFixedExpressionResults<T>(string expression);
		WordSkipType GetWordSkipType(int line, int index);
		int GetNextWord(int offset);
		int GetPrevWord(int offset);
		List<string> GetSelectionStrings();
		string GetString(Range range);
		List<T> GetVariableExpressionResults<T>(string expression);
		NEVariables GetVariables();
		Range MoveCursor(Range range, int cursor, bool selecting);
		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, bool? modified = null, bool keepUndo = false);
		List<string> RelativeSelectedFiles();
		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		void ReplaceOneWithMany(List<string> strs, bool? addNewLines);
		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false);
		void Save(string fileName, bool copyOnly = false);
		void SetClipboardFile(string fileName, bool isCut = false);
		void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false);
		void SetClipboardString(string text);
		void SetClipboardStrings(IEnumerable<string> strs);
		void SetFileName(string fileName);
		void SetAutoRefresh(bool? value = null);
		bool StringsAreFiles(List<string> strs);
		bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage);
		bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage);
		void OpenTable(Table table, string name = null);
		ObservableCollection<ObservableCollection<string>> KeysAndValues { get; }
		Dictionary<string, int> keysHash { get; }
		void CalculateKeysHash(bool caseSensitive);
		bool KeepSelections { get; set; }
		bool HighlightSyntax { get; set; }
		bool StrictParsing { get; set; }
		CacheValue previousData { get; }
		Parser.ParserType previousType { get; set; }
		ParserNode previousRoot { get; set; }
		string DBName { get; set; }
		DbConnection dbConnection { get; set; }
		JumpByType JumpBy { get; set; }
		bool IncludeInlineVariables { get; set; }
		string savedBitmapText { get; set; }
		System.Drawing.Bitmap savedBitmap { get; set; }
	}
}
