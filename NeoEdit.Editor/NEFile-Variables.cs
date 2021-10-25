using System;
using System.Collections.Generic;
using System.Data.Common;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		public INEFileData Data { get; private set; }
		NEFileData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
				{
					CreateResult();
					Data = Data.Next();
				}
				return Data as NEFileData;
			}
		}

		public void SetData(INEFileData data)
		{
			Data = data;

			Text.MoveToTextPoint(NETextPoint);
			EnsureVisible();
			SetIsModified();
			ClearResult();
			NEWindow?.CreateResult();
		}

		NETextPoint NETextPoint { get => Data.NETextPoint; set => EditableData.NETextPoint = value; }

		public IReadOnlyList<NERange> Selections
		{
			get => Data.Selections;
			set
			{
				if (AllowOverlappingSelections)
					EditableData.Selections = value;
				else
					EditableData.Selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<NERange> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return Data.Regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<NERange> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			EditableData.Regions[region - 1] = DeOverlap(regions);
			Data.Regions[region - 1] = DeOverlap(regions);
		}

		public bool AllowOverlappingSelections { get => Data.AllowOverlappingSelections; private set => EditableData.AllowOverlappingSelections = value; }

		void ClearNEFiles() => CreateResult().ClearNEFiles();
		void AddNEFile(NEFile neFile) => CreateResult().AddNEFile(neFile);
		void AddNewNEFile(NEFile neFile) => CreateResult().AddNewNEFile(neFile);

		public string DBName { get => Data.DBName; private set => EditableData.DBName = value; }
		public int CurrentSelection { get => Math.Min(Math.Max(0, Data.CurrentSelection), Selections.Count - 1); private set { EditableData.CurrentSelection = value; } }
		public string DisplayName { get => Data.DisplayName; private set => EditableData.DisplayName = value; }
		public string FileName { get => Data.FileName; private set => EditableData.FileName = value; }
		public bool AutoRefresh { get => Data.AutoRefresh; private set => EditableData.AutoRefresh = value; }
		public ParserType ContentType { get => Data.ContentType; set => EditableData.ContentType = value; }
		public Coder.CodePage CodePage { get => Data.CodePage; private set { EditableData.CodePage = value; SetIsModified(); } }
		public bool HasBOM { get => Data.HasBOM; private set { EditableData.HasBOM = value; SetIsModified(); } }
		public string AESKey { get => Data.AESKey; private set { EditableData.AESKey = value; SetIsModified(); } }
		public bool Compressed { get => Data.Compressed; private set { EditableData.Compressed = value; SetIsModified(); } }
		public bool DiffIgnoreWhitespace { get => Data.DiffIgnoreWhitespace; private set { EditableData.DiffIgnoreWhitespace = value; Text.ClearDiff(); } }
		public bool DiffIgnoreCase { get => Data.DiffIgnoreCase; private set { EditableData.DiffIgnoreCase = value; Text.ClearDiff(); } }
		public bool DiffIgnoreNumbers { get => Data.DiffIgnoreNumbers; private set { EditableData.DiffIgnoreNumbers = value; Text.ClearDiff(); } }
		public bool DiffIgnoreLineEndings { get => Data.DiffIgnoreLineEndings; private set { EditableData.DiffIgnoreLineEndings = value; Text.ClearDiff(); } }
		public HashSet<char> DiffIgnoreCharacters { get => Data.DiffIgnoreCharacters; private set { EditableData.DiffIgnoreCharacters = value; Text.ClearDiff(); } }
		public bool KeepSelections { get => Data.KeepSelections; private set => EditableData.KeepSelections = value; }
		public bool HighlightSyntax { get => Data.HighlightSyntax; private set => EditableData.HighlightSyntax = value; }
		public bool StrictParsing { get => Data.StrictParsing; private set => EditableData.StrictParsing = value; }
		public JumpByType JumpBy { get => Data.JumpBy; private set => EditableData.JumpBy = value; }
		public bool ViewBinary { get => Data.ViewBinary; private set => EditableData.ViewBinary = value; }
		public HashSet<Coder.CodePage> ViewBinaryCodePages { get => Data.ViewBinaryCodePages; private set => EditableData.ViewBinaryCodePages = value; }
		public IReadOnlyList<HashSet<string>> ViewBinarySearches { get => Data.ViewBinarySearches; private set => EditableData.ViewBinarySearches = value; }
		public NEFile DiffTarget
		{
			get => Data.DiffTarget;
			set
			{
				if (Data.DiffTarget != null)
				{
					Data.DiffTarget.NEWindow?.SetNeedsRender();
					Text.ClearDiff();
					Data.DiffTarget.Text.ClearDiff();
					Data.DiffTarget.EditableData.DiffTarget = null;
					EditableData.DiffTarget = null;
				}

				if (value != null)
				{
					value.EditableData.DiffTarget = null;
					EditableData.DiffTarget = value;
					value.EditableData.DiffTarget = this;
					Data.DiffTarget.NEWindow?.SetNeedsRender();
				}
			}
		}
		DbConnection DbConnection { get => Data.DbConnection; set => EditableData.DbConnection = value; }

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
			SetupWatcher();
		}

		public void Detach()
		{
			if (NEWindow == null)
				throw new Exception("File not attached");
			if (result != null)
				throw new Exception("Can't detach, file being modified");

			NEWindow = null;
			SetupWatcher();
		}
	}
}
