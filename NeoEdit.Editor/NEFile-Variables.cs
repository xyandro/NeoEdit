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

		NEFileData saveFileState, fileState;

		public NEFiles NEFiles
		{
			get => fileState.neFiles;
			set => fileState.neFiles = value;
		}

		NEText Text
		{
			get => fileState.text;
			set
			{
				EnsureInTransaction();
				fileState.text = value;
			}
		}

		UndoRedo UndoRedo
		{
			get => fileState.undoRedo;
			set
			{
				EnsureInTransaction();
				fileState.undoRedo = value;
			}
		}

		bool IsDiff
		{
			get => fileState.isDiff;
			set
			{
				EnsureInTransaction();
				fileState.isDiff = value;
			}
		}

		public NEFile DiffTarget
		{
			get => fileState.diffTarget;
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
					DiffTarget.fileState.diffTarget = null;
					fileState.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					fileState.diffTarget = value;
					value.fileState.diffTarget = this;
					IsDiff = DiffTarget.IsDiff = true;
					CalculateDiff();
				}
			}
		}

		public int CurrentSelection
		{
			get => Math.Min(Math.Max(0, fileState.currentSelection), Selections.Count - 1);
			private set
			{
				EnsureInTransaction();
				fileState.currentSelection = value;
			}
		}

		public IReadOnlyList<Range> Selections
		{
			get => fileState.selections;
			set
			{
				EnsureInTransaction();
				fileState.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return fileState.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			fileState.regions[region - 1] = DeOverlap(regions);
		}

		public NEFileResult result { get; private set; }
		NEFileResult CreateResult()
		{
			if (result == null)
				result = new NEFileResult();
			return result;
		}

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
			get => fileState.displayName;
			private set
			{
				EnsureInTransaction();
				fileState.displayName = value;
			}
		}

		public string FileName
		{
			get => fileState.fileName;
			private set
			{
				EnsureInTransaction();
				fileState.fileName = value;
			}
		}

		public bool IsModified
		{
			get => fileState.isModified;
			private set
			{
				EnsureInTransaction();
				fileState.isModified = value;
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
			get => fileState.autoRefresh;
			private set
			{
				EnsureInTransaction();
				fileState.autoRefresh = value;
			}
		}

		public string DBName
		{
			get => fileState.dbName;
			private set
			{
				EnsureInTransaction();
				fileState.dbName = value;
			}
		}

		public ParserType ContentType
		{
			get => fileState.contentType;
			set
			{
				EnsureInTransaction();
				fileState.contentType = value;
			}
		}

		Coder.CodePage CodePage
		{
			get => fileState.codePage;
			set
			{
				EnsureInTransaction();
				fileState.codePage = value;
			}
		}

		public string AESKey
		{
			get => fileState.aesKey;
			private set
			{
				EnsureInTransaction();
				fileState.aesKey = value;
			}
		}

		public bool Compressed
		{
			get => fileState.compressed;
			private set
			{
				EnsureInTransaction();
				fileState.compressed = value;
			}
		}

		public bool DiffIgnoreWhitespace
		{
			get => fileState.diffIgnoreWhitespace;
			private set
			{
				EnsureInTransaction();
				fileState.diffIgnoreWhitespace = value;
			}
		}

		public bool DiffIgnoreCase
		{
			get => fileState.diffIgnoreCase;
			private set
			{
				EnsureInTransaction();
				fileState.diffIgnoreCase = value;
			}
		}

		public bool DiffIgnoreNumbers
		{
			get => fileState.diffIgnoreNumbers;
			private set
			{
				EnsureInTransaction();
				fileState.diffIgnoreNumbers = value;
			}
		}

		public bool DiffIgnoreLineEndings
		{
			get => fileState.diffIgnoreLineEndings;
			private set
			{
				EnsureInTransaction();
				fileState.diffIgnoreLineEndings = value;
			}
		}

		public string DiffIgnoreCharacters
		{
			get => fileState.diffIgnoreCharacters;
			private set
			{
				EnsureInTransaction();
				fileState.diffIgnoreCharacters = value;
			}
		}

		public bool KeepSelections
		{
			get => fileState.keepSelections;
			private set
			{
				EnsureInTransaction();
				fileState.keepSelections = value;
			}
		}

		public bool HighlightSyntax
		{
			get => fileState.highlightSyntax;
			private set
			{
				EnsureInTransaction();
				fileState.highlightSyntax = value;
			}
		}

		public bool StrictParsing
		{
			get => fileState.strictParsing;
			private set
			{
				EnsureInTransaction();
				fileState.strictParsing = value;
			}
		}

		public JumpByType JumpBy
		{
			get => fileState.jumpBy;
			private set
			{
				EnsureInTransaction();
				fileState.jumpBy = value;
			}
		}

		public DateTime LastActive { get; set; }

		public bool ViewBinary
		{
			get => fileState.viewBinary;
			private set
			{
				EnsureInTransaction();
				fileState.viewBinary = value;
			}
		}

		public HashSet<Coder.CodePage> ViewBinaryCodePages
		{
			get => fileState.viewBinaryCodePages;
			private set
			{
				EnsureInTransaction();
				fileState.viewBinaryCodePages = value;
			}
		}

		public IReadOnlyList<HashSet<string>> ViewBinarySearches
		{
			get => fileState.viewBinarySearches;
			private set
			{
				EnsureInTransaction();
				fileState.viewBinarySearches = value;
			}
		}

		public int StartColumn
		{
			get => fileState.startColumn;
			set
			{
				EnsureInTransaction();
				fileState.startColumn = value;
			}
		}

		public int StartRow
		{
			get => fileState.startRow;
			set
			{
				EnsureInTransaction();
				fileState.startRow = value;
				if (DiffTarget != null)
				{
					NEFiles.AddToTransaction(DiffTarget);
					DiffTarget.fileState.startRow = value;
				}
			}
		}

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			saveFileState = fileState;
			fileState = fileState.Clone();
		}

		void ClearState()
		{
			EnsureInTransaction();

			clipboardData = null;
			for (var kvIndex = 0; kvIndex < NEFiles.KeysAndValuesCount; ++kvIndex)
				keysAndValues[kvIndex] = null;
			inTransaction = false;
			saveFileState = null;
			result = null;
		}

		public void Rollback()
		{
			EnsureInTransaction();
			fileState = saveFileState;
			ClearState();
		}

		public void Commit()
		{
			EnsureInTransaction();
			ClearState();
		}
	}
}
