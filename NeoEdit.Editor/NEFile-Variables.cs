using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		public NEFileData Data { get; private set; }
		NEFileData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
				{
					CreateResult();
					Data = new NEFileData(Data);
				}
				return Data;
			}
		}

		public void SetData(NEFileData data)
		{
			ClearResult();
			Data = data;
			SetModifiedFlag();
		}

		NEText Text { get => Data.text; set => EditableData.text = value; }
		bool IsDiff { get => Data.isDiff; set => EditableData.isDiff = value; }
		public int CurrentSelection { get => Math.Min(Math.Max(0, Data.currentSelection), Selections.Count - 1); private set => EditableData.currentSelection = value; }
		public string DisplayName { get => Data.displayName; private set => EditableData.displayName = value; }
		public string FileName { get => Data.fileName; private set => EditableData.fileName = value; }
		public bool AutoRefresh { get => Data.autoRefresh; private set => EditableData.autoRefresh = value; }
		public string DBName { get => Data.dbName; private set => EditableData.dbName = value; }
		public ParserType ContentType { get => Data.contentType; set => EditableData.contentType = value; }
		Coder.CodePage CodePage { get => Data.codePage; set => EditableData.codePage = value; }
		public string AESKey { get => Data.aesKey; private set => EditableData.aesKey = value; }
		public bool Compressed { get => Data.compressed; private set => EditableData.compressed = value; }
		public bool DiffIgnoreWhitespace { get => Data.diffIgnoreWhitespace; private set => EditableData.diffIgnoreWhitespace = value; }
		public bool DiffIgnoreCase { get => Data.diffIgnoreCase; private set => EditableData.diffIgnoreCase = value; }
		public bool DiffIgnoreNumbers { get => Data.diffIgnoreNumbers; private set => EditableData.diffIgnoreNumbers = value; }
		public bool DiffIgnoreLineEndings { get => Data.diffIgnoreLineEndings; private set => EditableData.diffIgnoreLineEndings = value; }
		public string DiffIgnoreCharacters { get => Data.diffIgnoreCharacters; private set => EditableData.diffIgnoreCharacters = value; }
		public bool KeepSelections { get => Data.keepSelections; private set => EditableData.keepSelections = value; }
		public bool HighlightSyntax { get => Data.highlightSyntax; private set => EditableData.highlightSyntax = value; }
		public bool StrictParsing { get => Data.strictParsing; private set => EditableData.strictParsing = value; }
		public JumpByType JumpBy { get => Data.jumpBy; private set => EditableData.jumpBy = value; }
		public bool ViewBinary { get => Data.viewBinary; private set => EditableData.viewBinary = value; }
		public HashSet<Coder.CodePage> ViewBinaryCodePages { get => Data.viewBinaryCodePages; private set => EditableData.viewBinaryCodePages = value; }
		public IReadOnlyList<HashSet<string>> ViewBinarySearches { get => Data.viewBinarySearches; private set => EditableData.viewBinarySearches = value; }

		public NEFile DiffTarget
		{
			get => Data.diffTarget;
			set
			{
				IsDiff = false;

				if (DiffTarget != null)
				{
					DiffTarget.IsDiff = false;

					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.EditableData.diffTarget = null;
					EditableData.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					EditableData.diffTarget = value;
					value.EditableData.diffTarget = this;
					IsDiff = DiffTarget.IsDiff = true;
					CalculateDiff();
				}
			}
		}

		public IReadOnlyList<Range> Selections
		{
			get => Data.selections;
			set
			{
				EditableData.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public int StartRow
		{
			get => Data.startRow;
			set
			{
				EditableData.startRow = value;
				if (DiffTarget != null)
					DiffTarget.EditableData.startRow = value;
			}
		}
		public int StartColumn { get => Data.startColumn; set => EditableData.startColumn = value; }

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return Data.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			EditableData.regions[region - 1] = DeOverlap(regions);
			Data.regions[region - 1] = DeOverlap(regions);
		}

		void ClearNEFiles() => CreateResult().ClearNEFiles();
		void AddNEFile(NEFile neFile) => CreateResult().AddNEFile(neFile);
		void AddNewNEFile(NEFile neFile) => CreateResult().AddNewNEFile(neFile);

		Tuple<IReadOnlyList<string>, bool?> clipboardData;
		Tuple<IReadOnlyList<string>, bool?> ClipboardData
		{
			get
			{
				if (clipboardData == null)
					clipboardData = state.GetClipboardData(this);

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

		readonly KeysAndValues[] keysAndValues = new KeysAndValues[10];
		KeysAndValues GetKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (keysAndValues[kvIndex] == null)
				keysAndValues[kvIndex] = state.GetKeysAndValues(kvIndex, this);

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

		void AddDragFile(string fileName) => CreateResult().AddDragFile(fileName);

		NEFileResult result;
		NEFileResult CreateResult()
		{
			if (result == null)
			{
				NEWindow?.CreateResult();
				result = new NEFileResult(this);
			}
			return result;
		}

		public NEFileResult GetResult()
		{
			if (result == null)
				return null;

			var ret = result;
			ClearResult();
			return ret;
		}

		void ClearResult()
		{
			clipboardData = null;
			for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
				keysAndValues[kvIndex] = null;
			result = null;
		}

		public NEWindow NEWindow { get; private set; }
		public void Attach(NEWindow neWindow)
		{
			if (NEWindow != null)
				throw new Exception("File already attached");
			if (result != null)
				throw new Exception("Can't attach, file being modified");

			NEWindow = neWindow;
			SetAutoRefresh();
		}

		public void Detach()
		{
			if (NEWindow == null)
				throw new Exception("File not attached");
			if (result != null)
				throw new Exception("Can't detach, file being modified");

			NEWindow = null;
			SetAutoRefresh();
		}
	}
}
