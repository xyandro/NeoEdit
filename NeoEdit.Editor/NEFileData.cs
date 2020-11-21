using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public class NEFileData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEFile neFile;

		public NEText text;
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
		public NEFile diffTarget;

		public NEFileData undo;
		public NEFileData redo;

		public NEFileData(NEFile neFile)
		{
			this.neFile = neFile;
			regions = new IReadOnlyList<Range>[9];
		}

		public NEFileData(NEFileData neFileData)
		{
			text = neFileData.text;
			currentSelection = neFileData.currentSelection;
			selections = neFileData.selections;
			regions = neFileData.regions.ToArray();
			displayName = neFileData.displayName;
			fileName = neFileData.fileName;
			isModified = neFileData.isModified;
			autoRefresh = neFileData.autoRefresh;
			dbName = neFileData.dbName;
			contentType = neFileData.contentType;
			codePage = neFileData.codePage;
			aesKey = neFileData.aesKey;
			compressed = neFileData.compressed;
			diffIgnoreWhitespace = neFileData.diffIgnoreWhitespace;
			diffIgnoreCase = neFileData.diffIgnoreCase;
			diffIgnoreNumbers = neFileData.diffIgnoreNumbers;
			diffIgnoreLineEndings = neFileData.diffIgnoreLineEndings;
			diffIgnoreCharacters = neFileData.diffIgnoreCharacters;
			keepSelections = neFileData.keepSelections;
			highlightSyntax = neFileData.highlightSyntax;
			strictParsing = neFileData.strictParsing;
			jumpBy = neFileData.jumpBy;
			viewBinary = neFileData.viewBinary;
			viewBinaryCodePages = neFileData.viewBinaryCodePages;
			viewBinarySearches = neFileData.viewBinarySearches;
			startColumn = neFileData.startColumn;
			startRow = neFileData.startRow;
			isDiff = neFileData.isDiff;
			diffTarget = neFileData.diffTarget;

			undo = neFileData;
			neFileData.redo = this;
		}

		public override string ToString() => NESerial.ToString();
	}
}
