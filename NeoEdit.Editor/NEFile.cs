using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	class NEFile
	{
		public NEFilesHandler neFiles;
		public NEText text;
		public UndoRedo undoRedo;
		public int currentSelection;
		public IReadOnlyList<Range> selections;
		public IReadOnlyList<Range>[] regions;
		public string displayName;
		public string fileName;
		public bool isModified;
		public bool autoRefresh;
		public string dbName;
		public ParserType contentType;
		public Coder.CodePage codePage;
		public string aesKey;
		public bool compressed;
		public bool diffIgnoreWhitespace;
		public bool diffIgnoreCase;
		public bool diffIgnoreNumbers;
		public bool diffIgnoreLineEndings;
		public string diffIgnoreCharacters;
		public bool keepSelections;
		public bool highlightSyntax;
		public bool strictParsing;
		public JumpByType jumpBy;
		public bool viewBinary;
		public HashSet<Coder.CodePage> viewBinaryCodePages;
		public IReadOnlyList<HashSet<string>> viewBinarySearches;
		public int startColumn;
		public int startRow;
		public bool isDiff;
		public NEFileHandler diffTarget;

		public NEFile()
		{
			regions = new IReadOnlyList<Range>[9];
		}

		public NEFile Clone()
		{
			var fileState = new NEFile
			{
				neFiles = neFiles,
				text = text,
				undoRedo = undoRedo,
				currentSelection = currentSelection,
				selections = selections,
				displayName = displayName,
				fileName = fileName,
				isModified = isModified,
				autoRefresh = autoRefresh,
				dbName = dbName,
				contentType = contentType,
				codePage = codePage,
				aesKey = aesKey,
				compressed = compressed,
				diffIgnoreWhitespace = diffIgnoreWhitespace,
				diffIgnoreCase = diffIgnoreCase,
				diffIgnoreNumbers = diffIgnoreNumbers,
				diffIgnoreLineEndings = diffIgnoreLineEndings,
				diffIgnoreCharacters = diffIgnoreCharacters,
				keepSelections = keepSelections,
				highlightSyntax = highlightSyntax,
				strictParsing = strictParsing,
				jumpBy = jumpBy,
				viewBinary = viewBinary,
				viewBinaryCodePages = viewBinaryCodePages,
				viewBinarySearches = viewBinarySearches,
				startColumn = startColumn,
				startRow = startRow,
				isDiff = isDiff,
				diffTarget = diffTarget,
			};

			regions.CopyTo(fileState.regions, 0);

			return fileState;
		}
	}
}
