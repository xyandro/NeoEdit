using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		public NEFileData data { get; private set; }
		NEFileData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public void ResetData(NEFileData data)
		{
			result = null;
			this.data = data;
		}

		NEText Text
		{
			get => data.text;
			set => editableData.text = value;
		}

		UndoRedo UndoRedo
		{
			get => data.undoRedo;
			set => editableData.undoRedo = value;
		}

		bool IsDiff
		{
			get => data.isDiff;
			set => editableData.isDiff = value;
		}

		public NEFile DiffTarget
		{
			get => data.diffTarget;
			set
			{
				IsDiff = false;

				if (DiffTarget != null)
				{
					DiffTarget.IsDiff = false;

					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.editableData.diffTarget = null;
					editableData.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					editableData.diffTarget = value;
					value.editableData.diffTarget = this;
					IsDiff = DiffTarget.IsDiff = true;
					CalculateDiff();
				}
			}
		}

		public int CurrentSelection
		{
			get => Math.Min(Math.Max(0, data.currentSelection), Selections.Count - 1);
			private set => editableData.currentSelection = value;
		}

		public IReadOnlyList<Range> Selections
		{
			get => data.selections;
			set
			{
				editableData.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return data.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			data.regions[region - 1] = DeOverlap(regions);
		}

		NEFileResult result;
		NEFileResult CreateResult()
		{
			if (result == null)
				result = new NEFileResult();
			return result;
		}

		readonly KeysAndValues[] keysAndValues = new KeysAndValues[10];
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

			var newKeysAndValues = new KeysAndValues(values, kvIndex == 0, matchCase);
			keysAndValues[kvIndex] = newKeysAndValues;
			CreateResult().SetKeysAndValues(kvIndex, newKeysAndValues);
		}

		public string DisplayName
		{
			get => data.displayName;
			private set => editableData.displayName = value;
		}

		public string FileName
		{
			get => data.fileName;
			private set => editableData.fileName = value;
		}

		public bool IsModified
		{
			get => data.isModified;
			private set => editableData.isModified = value;
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
			get => data.autoRefresh;
			private set => editableData.autoRefresh = value;
		}

		public string DBName
		{
			get => data.dbName;
			private set => editableData.dbName = value;
		}

		public ParserType ContentType
		{
			get => data.contentType;
			set => editableData.contentType = value;
		}

		Coder.CodePage CodePage
		{
			get => data.codePage;
			set => editableData.codePage = value;
		}

		public string AESKey
		{
			get => data.aesKey;
			private set => editableData.aesKey = value;
		}

		public bool Compressed
		{
			get => data.compressed;
			private set => editableData.compressed = value;
		}

		public bool DiffIgnoreWhitespace
		{
			get => data.diffIgnoreWhitespace;
			private set => editableData.diffIgnoreWhitespace = value;
		}

		public bool DiffIgnoreCase
		{
			get => data.diffIgnoreCase;
			private set => editableData.diffIgnoreCase = value;
		}

		public bool DiffIgnoreNumbers
		{
			get => data.diffIgnoreNumbers;
			private set => editableData.diffIgnoreNumbers = value;
		}

		public bool DiffIgnoreLineEndings
		{
			get => data.diffIgnoreLineEndings;
			private set => editableData.diffIgnoreLineEndings = value;
		}

		public string DiffIgnoreCharacters
		{
			get => data.diffIgnoreCharacters;
			private set => editableData.diffIgnoreCharacters = value;
		}

		public bool KeepSelections
		{
			get => data.keepSelections;
			private set => editableData.keepSelections = value;
		}

		public bool HighlightSyntax
		{
			get => data.highlightSyntax;
			private set => editableData.highlightSyntax = value;
		}

		public bool StrictParsing
		{
			get => data.strictParsing;
			private set => editableData.strictParsing = value;
		}

		public JumpByType JumpBy
		{
			get => data.jumpBy;
			private set => editableData.jumpBy = value;
		}

		public DateTime LastActive { get; set; }

		public bool ViewBinary
		{
			get => data.viewBinary;
			private set => editableData.viewBinary = value;
		}

		public HashSet<Coder.CodePage> ViewBinaryCodePages
		{
			get => data.viewBinaryCodePages;
			private set => editableData.viewBinaryCodePages = value;
		}

		public IReadOnlyList<HashSet<string>> ViewBinarySearches
		{
			get => data.viewBinarySearches;
			private set => editableData.viewBinarySearches = value;
		}

		public int StartColumn
		{
			get => data.startColumn;
			set => editableData.startColumn = value;
		}

		public int StartRow
		{
			get => data.startRow;
			set
			{
				editableData.startRow = value;
				if (DiffTarget != null)
					DiffTarget.editableData.startRow = value;
			}
		}

		public NEFileResult GetResult()
		{
			clipboardData = null;
			for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
				keysAndValues[kvIndex] = null;

			var ret = result;
			result = null;
			return ret;
		}
	}
}
