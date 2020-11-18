using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		NEFileData saveFileData, fileData;

		NEText Text
		{
			get => fileData.text;
			set
			{
				EnsureInTransaction();
				fileData.text = value;
			}
		}

		UndoRedo UndoRedo
		{
			get => fileData.undoRedo;
			set
			{
				EnsureInTransaction();
				fileData.undoRedo = value;
			}
		}

		bool IsDiff
		{
			get => fileData.isDiff;
			set
			{
				EnsureInTransaction();
				fileData.isDiff = value;
			}
		}

		public NEFile DiffTarget
		{
			get => fileData.diffTarget;
			set
			{
				EnsureInTransaction();
				if (value != null)
					value.EnsureInTransaction();

				IsDiff = false;

				if (DiffTarget != null)
				{
					DiffTarget.IsDiff = false;

					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.fileData.diffTarget = null;
					fileData.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					fileData.diffTarget = value;
					value.fileData.diffTarget = this;
					IsDiff = DiffTarget.IsDiff = true;
					CalculateDiff();
				}
			}
		}

		public int CurrentSelection
		{
			get => Math.Min(Math.Max(0, fileData.currentSelection), Selections.Count - 1);
			private set
			{
				EnsureInTransaction();
				fileData.currentSelection = value;
			}
		}

		public IReadOnlyList<Range> Selections
		{
			get => fileData.selections;
			set
			{
				EnsureInTransaction();
				fileData.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return fileData.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			fileData.regions[region - 1] = DeOverlap(regions);
		}

		public NEFileResult result { get; private set; }
		NEFileResult CreateResult()
		{
			if (result == null)
				result = new NEFileResult();
			return result;
		}

		public void ClearResult() => result = null;

		readonly KeysAndValues[] keysAndValues = new KeysAndValues[NEFiles.KeysAndValuesCount];
		KeysAndValues GetKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (keysAndValues[kvIndex] == null)
				keysAndValues[kvIndex] = EditorExecuteState.CurrentState.GetKeysAndValues(kvIndex, this);

			return keysAndValues[kvIndex];
		}

		void SetKeysAndValues(int kvIndex, IReadOnlyList<string> values, bool matchCase = false)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			EnsureInTransaction();
			var newKeysAndValues = new KeysAndValues(values, kvIndex == 0, matchCase);
			keysAndValues[kvIndex] = newKeysAndValues;
			CreateResult().SetKeysAndValues(kvIndex, newKeysAndValues);
		}

		public string DisplayName
		{
			get => fileData.displayName;
			private set
			{
				EnsureInTransaction();
				fileData.displayName = value;
			}
		}

		public string FileName
		{
			get => fileData.fileName;
			private set
			{
				EnsureInTransaction();
				fileData.fileName = value;
			}
		}

		public bool IsModified
		{
			get => fileData.isModified;
			private set
			{
				EnsureInTransaction();
				fileData.isModified = value;
			}
		}

		void ClearFiles() => CreateResult().ClearFiles();
		void AddFile(NEFile neFile) => CreateResult().AddFile(neFile);
		void AddNewFile(NEFile neFile) => CreateResult().AddNewFile(neFile);

		Tuple<IReadOnlyList<string>, bool?> clipboardData;
		Tuple<IReadOnlyList<string>, bool?> ClipboardData
		{
			get
			{
				if (clipboardData == null)
					clipboardData = EditorExecuteState.CurrentState.GetClipboardData(this);

				return clipboardData;
			}

			set
			{
				EnsureInTransaction();
				clipboardData = value;
				CreateResult().SetClipboard(value);
			}
		}

		IReadOnlyList<string> Clipboard { get => ClipboardData.Item1; set => ClipboardData = Tuple.Create(value, default(bool?)); }
		IReadOnlyList<string> ClipboardCopy { set => ClipboardData = Tuple.Create(value, (bool?)false); }
		IReadOnlyList<string> ClipboardCut { set => ClipboardData = Tuple.Create(value, (bool?)true); }

		void AddDragFile(string fileName) => CreateResult().AddDragFile(fileName);

		public bool AutoRefresh
		{
			get => fileData.autoRefresh;
			private set
			{
				EnsureInTransaction();
				fileData.autoRefresh = value;
			}
		}

		public string DBName
		{
			get => fileData.dbName;
			private set
			{
				EnsureInTransaction();
				fileData.dbName = value;
			}
		}

		public ParserType ContentType
		{
			get => fileData.contentType;
			set
			{
				EnsureInTransaction();
				fileData.contentType = value;
			}
		}

		Coder.CodePage CodePage
		{
			get => fileData.codePage;
			set
			{
				EnsureInTransaction();
				fileData.codePage = value;
			}
		}

		public string AESKey
		{
			get => fileData.aesKey;
			private set
			{
				EnsureInTransaction();
				fileData.aesKey = value;
			}
		}

		public bool Compressed
		{
			get => fileData.compressed;
			private set
			{
				EnsureInTransaction();
				fileData.compressed = value;
			}
		}

		public bool DiffIgnoreWhitespace
		{
			get => fileData.diffIgnoreWhitespace;
			private set
			{
				EnsureInTransaction();
				fileData.diffIgnoreWhitespace = value;
			}
		}

		public bool DiffIgnoreCase
		{
			get => fileData.diffIgnoreCase;
			private set
			{
				EnsureInTransaction();
				fileData.diffIgnoreCase = value;
			}
		}

		public bool DiffIgnoreNumbers
		{
			get => fileData.diffIgnoreNumbers;
			private set
			{
				EnsureInTransaction();
				fileData.diffIgnoreNumbers = value;
			}
		}

		public bool DiffIgnoreLineEndings
		{
			get => fileData.diffIgnoreLineEndings;
			private set
			{
				EnsureInTransaction();
				fileData.diffIgnoreLineEndings = value;
			}
		}

		public string DiffIgnoreCharacters
		{
			get => fileData.diffIgnoreCharacters;
			private set
			{
				EnsureInTransaction();
				fileData.diffIgnoreCharacters = value;
			}
		}

		public bool KeepSelections
		{
			get => fileData.keepSelections;
			private set
			{
				EnsureInTransaction();
				fileData.keepSelections = value;
			}
		}

		public bool HighlightSyntax
		{
			get => fileData.highlightSyntax;
			private set
			{
				EnsureInTransaction();
				fileData.highlightSyntax = value;
			}
		}

		public bool StrictParsing
		{
			get => fileData.strictParsing;
			private set
			{
				EnsureInTransaction();
				fileData.strictParsing = value;
			}
		}

		public JumpByType JumpBy
		{
			get => fileData.jumpBy;
			private set
			{
				EnsureInTransaction();
				fileData.jumpBy = value;
			}
		}

		public DateTime LastActive { get; set; }

		public bool ViewBinary
		{
			get => fileData.viewBinary;
			private set
			{
				EnsureInTransaction();
				fileData.viewBinary = value;
			}
		}

		public HashSet<Coder.CodePage> ViewBinaryCodePages
		{
			get => fileData.viewBinaryCodePages;
			private set
			{
				EnsureInTransaction();
				fileData.viewBinaryCodePages = value;
			}
		}

		public IReadOnlyList<HashSet<string>> ViewBinarySearches
		{
			get => fileData.viewBinarySearches;
			private set
			{
				EnsureInTransaction();
				fileData.viewBinarySearches = value;
			}
		}

		public int StartColumn
		{
			get => fileData.startColumn;
			set
			{
				EnsureInTransaction();
				fileData.startColumn = value;
			}
		}

		public int StartRow
		{
			get => fileData.startRow;
			set
			{
				EnsureInTransaction();
				fileData.startRow = value;
				if (DiffTarget != null)
				{
					EditorExecuteState.CurrentState.NEFiles.AddToTransaction(DiffTarget);
					DiffTarget.fileData.startRow = value;
				}
			}
		}

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			saveFileData = fileData;
			fileData = fileData.Clone();
		}

		void ClearState()
		{
			EnsureInTransaction();

			clipboardData = null;
			for (var kvIndex = 0; kvIndex < NEFiles.KeysAndValuesCount; ++kvIndex)
				keysAndValues[kvIndex] = null;
			inTransaction = false;
			saveFileData = null;
		}

		public void Rollback()
		{
			EnsureInTransaction();
			fileData = saveFileData;
			ClearState();
			result = null;
		}

		public void Commit()
		{
			EnsureInTransaction();
			ClearState();
		}
	}
}
