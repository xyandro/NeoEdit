using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.Transactional;

namespace NeoEdit.Editor
{
	public partial class Tab
	{
		void EnsureInTransaction()
		{
			if (state == null)
				throw new Exception("Must start transaction before editing data");
		}

		TabState saveTabState, tabState;

		public Tabs Tabs
		{
			get => tabState.tabs;
			set => tabState.tabs = value;
		}

		NEText Text
		{
			get => tabState.text;
			set
			{
				EnsureInTransaction();
				tabState.text = value;
			}
		}

		bool IsDiff
		{
			get => tabState.isDiff;
			set
			{
				EnsureInTransaction();
				tabState.isDiff = value;
			}
		}

		public Tab DiffTarget
		{
			get => tabState.diffTarget;
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
					DiffTarget.tabState.diffTarget = null;
					tabState.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					tabState.diffTarget = value;
					value.tabState.diffTarget = this;
					IsDiff = DiffTarget.IsDiff = true;
					CalculateDiff();
				}
			}
		}

		public int CurrentSelection
		{
			get => Math.Min(Math.Max(0, tabState.currentSelection), Selections.Count - 1);
			private set
			{
				EnsureInTransaction();
				tabState.currentSelection = value;
			}
		}

		public IReadOnlyList<Range> Selections
		{
			get => tabState.selections;
			set
			{
				EnsureInTransaction();
				tabState.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return tabState.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			tabState.regions[region - 1] = DeOverlap(regions);
		}

		readonly KeysAndValues[] newKeysAndValues = new KeysAndValues[Tabs.KeysAndValuesCount];
		public KeysAndValues GetKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (newKeysAndValues[kvIndex] == null)
				newKeysAndValues[kvIndex] = state.GetKeysAndValues(kvIndex, this);

			return newKeysAndValues[kvIndex];
		}

		readonly bool[] keysAndValuesSet = new bool[Tabs.KeysAndValuesCount];

		void SetKeysAndValues(int kvIndex, IReadOnlyList<string> values, bool matchCase = false)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			EnsureInTransaction();
			newKeysAndValues[kvIndex] = new KeysAndValues(values, kvIndex == 0, matchCase);
			keysAndValuesSet[kvIndex] = true;
		}

		public bool KeysAndValuesSet(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			return keysAndValuesSet[kvIndex];
		}

		public string DisplayName
		{
			get => tabState.displayName;
			private set
			{
				EnsureInTransaction();
				tabState.displayName = value;
			}
		}

		public string FileName
		{
			get => tabState.fileName;
			private set
			{
				EnsureInTransaction();
				tabState.fileName = value;
			}
		}

		public bool IsModified
		{
			get => tabState.isModified;
			private set
			{
				EnsureInTransaction();
				tabState.isModified = value;
			}
		}

		readonly List<(Tab tab, int? index)> newTabsToAdd = new List<(Tab tab, int? index)>();
		public IReadOnlyList<(Tab tab, int? index)> TabsToAdd => newTabsToAdd;
		void QueueAddTab(Tab tab, int? index = null)
		{
			EnsureInTransaction();
			newTabsToAdd.Add((tab, index));
		}

		Tuple<IReadOnlyList<string>, bool?> newClipboardData;
		public bool ClipboardDataSet { get; set; }
		public Tuple<IReadOnlyList<string>, bool?> ClipboardData
		{
			get
			{
				if (newClipboardData == null)
					newClipboardData = state.GetClipboardData(this);

				return newClipboardData;
			}

			set
			{
				EnsureInTransaction();
				newClipboardData = value;
				ClipboardDataSet = true;
			}
		}

		IReadOnlyList<string> Clipboard { get => ClipboardData.Item1; set => ClipboardData = Tuple.Create(value, default(bool?)); }
		IReadOnlyList<string> ClipboardCopy { set => ClipboardData = Tuple.Create(value, (bool?)false); }
		IReadOnlyList<string> ClipboardCut { set => ClipboardData = Tuple.Create(value, (bool?)true); }

		readonly List<string> newDragFiles = new List<string>();
		public IReadOnlyList<string> DragFiles => newDragFiles;

		public bool AutoRefresh
		{
			get => tabState.autoRefresh;
			private set
			{
				EnsureInTransaction();
				tabState.autoRefresh = value;
			}
		}

		public string DBName
		{
			get => tabState.dbName;
			private set
			{
				EnsureInTransaction();
				tabState.dbName = value;
			}
		}

		public ParserType ContentType
		{
			get => tabState.contentType;
			set
			{
				EnsureInTransaction();
				tabState.contentType = value;
			}
		}

		Coder.CodePage CodePage
		{
			get => tabState.codePage;
			set
			{
				EnsureInTransaction();
				tabState.codePage = value;
			}
		}

		public string AESKey
		{
			get => tabState.aesKey;
			private set
			{
				EnsureInTransaction();
				tabState.aesKey = value;
			}
		}

		public bool Compressed
		{
			get => tabState.compressed;
			private set
			{
				EnsureInTransaction();
				tabState.compressed = value;
			}
		}

		public bool DiffIgnoreWhitespace
		{
			get => tabState.diffIgnoreWhitespace;
			private set
			{
				EnsureInTransaction();
				tabState.diffIgnoreWhitespace = value;
			}
		}

		public bool DiffIgnoreCase
		{
			get => tabState.diffIgnoreCase;
			private set
			{
				EnsureInTransaction();
				tabState.diffIgnoreCase = value;
			}
		}

		public bool DiffIgnoreNumbers
		{
			get => tabState.diffIgnoreNumbers;
			private set
			{
				EnsureInTransaction();
				tabState.diffIgnoreNumbers = value;
			}
		}

		public bool DiffIgnoreLineEndings
		{
			get => tabState.diffIgnoreLineEndings;
			private set
			{
				EnsureInTransaction();
				tabState.diffIgnoreLineEndings = value;
			}
		}

		public string DiffIgnoreCharacters
		{
			get => tabState.diffIgnoreCharacters;
			private set
			{
				EnsureInTransaction();
				tabState.diffIgnoreCharacters = value;
			}
		}

		public bool KeepSelections
		{
			get => tabState.keepSelections;
			private set
			{
				EnsureInTransaction();
				tabState.keepSelections = value;
			}
		}

		public bool HighlightSyntax
		{
			get => tabState.highlightSyntax;
			private set
			{
				EnsureInTransaction();
				tabState.highlightSyntax = value;
			}
		}

		public bool StrictParsing
		{
			get => tabState.strictParsing;
			private set
			{
				EnsureInTransaction();
				tabState.strictParsing = value;
			}
		}

		public JumpByType JumpBy
		{
			get => tabState.jumpBy;
			private set
			{
				EnsureInTransaction();
				tabState.jumpBy = value;
			}
		}

		public DateTime LastActive
		{
			get => tabState.lastActive;
			set
			{
				EnsureInTransaction();
				tabState.lastActive = value;
			}
		}

		public bool ViewBinary
		{
			get => tabState.viewBinary;
			private set
			{
				EnsureInTransaction();
				tabState.viewBinary = value;
			}
		}

		public HashSet<Coder.CodePage> ViewBinaryCodePages
		{
			get => tabState.viewBinaryCodePages;
			private set
			{
				EnsureInTransaction();
				tabState.viewBinaryCodePages = value;
			}
		}

		public IReadOnlyList<HashSet<string>> ViewBinarySearches
		{
			get => tabState.viewBinarySearches;
			private set
			{
				EnsureInTransaction();
				tabState.viewBinarySearches = value;
			}
		}

		public int StartColumn
		{
			get => tabState.startColumn;
			set
			{
				EnsureInTransaction();
				tabState.startColumn = value;
			}
		}

		public int StartRow
		{
			get => tabState.startRow;
			set
			{
				EnsureInTransaction();
				tabState.startRow = value;
				if (DiffTarget != null)
				{
					Tabs.AddToTransaction(DiffTarget);
					DiffTarget.tabState.startRow = value;
				}
			}
		}

		public void BeginTransaction(EditorExecuteState state = null)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state ?? new EditorExecuteState(NECommand.None);
			saveTabState = tabState;
			tabState = tabState.Clone();
		}

		void ClearState()
		{
			EnsureInTransaction();

			newTabsToAdd.Clear();
			newClipboardData = null;
			ClipboardDataSet = false;
			for (var kvIndex = 0; kvIndex < Tabs.KeysAndValuesCount; ++kvIndex)
			{
				newKeysAndValues[kvIndex] = null;
				keysAndValuesSet[kvIndex] = false;
			}
			newDragFiles.Clear();
			state = null;
			saveTabState = null;
		}

		public void Rollback()
		{
			EnsureInTransaction();
			tabState = saveTabState;
			ClearState();
		}

		public void Commit()
		{
			EnsureInTransaction();
			ClearState();
		}
	}
}
