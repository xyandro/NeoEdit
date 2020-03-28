using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.View;

namespace NeoEdit.Editor
{
	public partial class Tab
	{
		void EnsureInTransaction()
		{
			if (state == null)
				throw new Exception("Must start transaction before editing data");
		}

		Tabs oldTabs, newTabs;
		public Tabs Tabs
		{
			get => newTabs;
			set => newTabs = value;
		}

		NEText oldText, newText;
		NEText Text
		{
			get => newText;
			set
			{
				EnsureInTransaction();
				newText = value;
				newTextView = null;
				newMaxColumn = null;
			}
		}

		INEView oldTextView, newTextView;
		INEView TextView
		{
			get
			{
				if (newTextView == null)
				{
					if (ViewBinary)
						newTextView = new NEHexView(newText.Length, Tabs.TabColumns - 1);
					else
						newTextView = new NETextView(newText);
				}
				return newTextView;
			}
		}

		public void ResetView() => newTextView = null;

		int? oldMaxColumn, newMaxColumn;
		public int MaxColumn
		{
			get
			{
				if (!newMaxColumn.HasValue)
					newMaxColumn = TextView.GetMaxColumn(Text);
				return newMaxColumn.Value;
			}
		}

		int oldCurrentSelection, newCurrentSelection;
		public int CurrentSelection
		{
			get => Math.Min(Math.Max(0, newCurrentSelection), Selections.Count - 1);
			private set
			{
				EnsureInTransaction();
				newCurrentSelection = value;
			}
		}

		IReadOnlyList<Range> oldSelections, newSelections;
		public IReadOnlyList<Range> Selections
		{
			get => newSelections;
			set
			{
				EnsureInTransaction();
				newSelections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		readonly IReadOnlyList<Range>[] oldRegions = new IReadOnlyList<Range>[9];
		readonly IReadOnlyList<Range>[] newRegions = new IReadOnlyList<Range>[9];
		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return newRegions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			newRegions[region - 1] = DeOverlap(regions);
		}

		readonly KeysAndValues[] newKeysAndValues = new KeysAndValues[10];
		readonly KeysAndValues[] changedKeysAndValues = new KeysAndValues[10];
		KeysAndValues GetKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (newKeysAndValues[kvIndex] == null)
				newKeysAndValues[kvIndex] = state.GetKeysAndValues(kvIndex, this);

			return newKeysAndValues[kvIndex];
		}

		void SetKeysAndValues(int kvIndex, IReadOnlyList<string> values, bool matchCase = false)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			EnsureInTransaction();
			newKeysAndValues[kvIndex] = changedKeysAndValues[kvIndex] = new KeysAndValues(values, kvIndex == 0, matchCase);
		}

		public KeysAndValues GetChangedKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			return changedKeysAndValues[kvIndex];
		}

		string oldDisplayName, newDisplayName;
		public string DisplayName
		{
			get => newDisplayName;
			private set
			{
				EnsureInTransaction();
				newDisplayName = value;
			}
		}

		string oldFileName, newFileName;
		public string FileName
		{
			get => newFileName;
			private set
			{
				EnsureInTransaction();
				newFileName = value;
			}
		}

		bool oldIsModified, newIsModified;
		public bool IsModified
		{
			get => newIsModified;
			private set
			{
				EnsureInTransaction();
				newIsModified = value;
			}
		}

		Tuple<IReadOnlyList<string>, bool?> newClipboardData;
		Tuple<IReadOnlyList<string>, bool?> ClipboardData
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
				newClipboardData = ChangedClipboardData = value;
			}
		}

		IReadOnlyList<string> Clipboard { get => ClipboardData.Item1; set => ClipboardData = Tuple.Create(value, default(bool?)); }
		IReadOnlyList<string> ClipboardCopy { set => ClipboardData = Tuple.Create(value, (bool?)false); }
		IReadOnlyList<string> ClipboardCut { set => ClipboardData = Tuple.Create(value, (bool?)true); }

		public Tuple<IReadOnlyList<string>, bool?> ChangedClipboardData { get; private set; }

		public IReadOnlyList<string> ChangedDragFiles { get; private set; }

		bool oldAutoRefresh, newAutoRefresh;
		public bool AutoRefresh
		{
			get => newAutoRefresh;
			private set
			{
				EnsureInTransaction();
				newAutoRefresh = value;
			}
		}

		string oldDBName, newDBName;
		public string DBName
		{
			get => newDBName;
			private set
			{
				EnsureInTransaction();
				newDBName = value;
			}
		}

		ParserType oldContentType, newContentType;
		public ParserType ContentType
		{
			get => newContentType;
			private set
			{
				EnsureInTransaction();
				newContentType = value;
			}
		}

		Coder.CodePage oldCodePage, newCodePage;
		Coder.CodePage CodePage
		{
			get => newCodePage;
			set
			{
				EnsureInTransaction();
				newCodePage = value;
			}
		}

		string oldAESKey, newAESKey;
		public string AESKey
		{
			get => newAESKey;
			private set
			{
				EnsureInTransaction();
				newAESKey = value;
			}
		}

		bool oldCompressed, newCompressed;
		public bool Compressed
		{
			get => newCompressed;
			private set
			{
				EnsureInTransaction();
				newCompressed = value;
			}
		}

		string oldLineEnding, newLineEnding;
		string LineEnding
		{
			get => newLineEnding;
			set
			{
				EnsureInTransaction();
				newLineEnding = value;
			}
		}

		bool oldDiffIgnoreWhitespace, newDiffIgnoreWhitespace;
		public bool DiffIgnoreWhitespace
		{
			get => newDiffIgnoreWhitespace;
			private set
			{
				EnsureInTransaction();
				newDiffIgnoreWhitespace = value;
			}
		}

		bool oldDiffIgnoreCase, newDiffIgnoreCase;
		public bool DiffIgnoreCase
		{
			get => newDiffIgnoreCase;
			private set
			{
				EnsureInTransaction();
				newDiffIgnoreCase = value;
			}
		}

		bool oldDiffIgnoreNumbers, newDiffIgnoreNumbers;
		public bool DiffIgnoreNumbers
		{
			get => newDiffIgnoreNumbers;
			private set
			{
				EnsureInTransaction();
				newDiffIgnoreNumbers = value;
			}
		}

		bool oldDiffIgnoreLineEndings, newDiffIgnoreLineEndings;
		public bool DiffIgnoreLineEndings
		{
			get => newDiffIgnoreLineEndings;
			private set
			{
				EnsureInTransaction();
				newDiffIgnoreLineEndings = value;
			}
		}

		bool oldIsDiff, newIsDiff;
		bool IsDiff
		{
			get => newIsDiff;
			set
			{
				EnsureInTransaction();
				newIsDiff = value;
			}
		}

		bool oldDiffEncodingMismatch, newDiffEncodingMismatch;
		bool DiffEncodingMismatch
		{
			get => newDiffEncodingMismatch;
			set
			{
				EnsureInTransaction();
				newDiffEncodingMismatch = value;
			}
		}

		bool oldKeepSelections, newKeepSelections;
		public bool KeepSelections
		{
			get => newKeepSelections;
			private set
			{
				EnsureInTransaction();
				newKeepSelections = value;
			}
		}

		bool oldHighlightSyntax, newHighlightSyntax;
		public bool HighlightSyntax
		{
			get => newHighlightSyntax;
			private set
			{
				EnsureInTransaction();
				newHighlightSyntax = value;
			}
		}

		bool oldStrictParsing, newStrictParsing;
		public bool StrictParsing
		{
			get => newStrictParsing;
			private set
			{
				EnsureInTransaction();
				newStrictParsing = value;
			}
		}

		JumpByType oldJumpBy, newJumpBy;
		public JumpByType JumpBy
		{
			get => newJumpBy;
			private set
			{
				EnsureInTransaction();
				newJumpBy = value;
			}
		}

		DateTime oldLastActive, newLastActive;
		public DateTime LastActive
		{
			get => newLastActive;
			set
			{
				EnsureInTransaction();
				newLastActive = value;
			}
		}

		bool oldViewBinary, newViewBinary;
		public bool ViewBinary
		{
			get => newViewBinary;
			private set
			{
				EnsureInTransaction();
				newViewBinary = value;
				newTextView = null;
			}
		}

		HashSet<Coder.CodePage> oldViewBinaryCodePages, newViewBinaryCodePages;
		public HashSet<Coder.CodePage> ViewBinaryCodePages
		{
			get => newViewBinaryCodePages;
			private set
			{
				EnsureInTransaction();
				newViewBinaryCodePages = value;
			}
		}

		IReadOnlyList<HashSet<string>> oldViewBinarySearches, newViewBinarySearches;
		public IReadOnlyList<HashSet<string>> ViewBinarySearches
		{
			get => newViewBinarySearches;
			private set
			{
				EnsureInTransaction();
				newViewBinarySearches = value;
			}
		}

		int oldStartColumn, newStartColumn;
		public int StartColumn
		{
			get => newStartColumn;
			set
			{
				EnsureInTransaction();
				newStartColumn = value;
			}
		}

		int oldStartRow, newStartRow;
		public int StartRow
		{
			get => newStartRow;
			set
			{
				EnsureInTransaction();
				newStartRow = value;
			}
		}

		UndoRedo oldUndoRedo, newUndoRedo;

		public void BeginTransaction(ExecuteState state = null)
		{
			if (this.state != null)
				throw new Exception("Already in a transaction");
			this.state = state ?? new ExecuteState(NECommand.None);
			newClipboardData = ChangedClipboardData = null;
			for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
				newKeysAndValues[kvIndex] = changedKeysAndValues[kvIndex] = null;
			ChangedDragFiles = null;
		}

		public void Rollback()
		{
			EnsureInTransaction();

			newTabs = oldTabs;
			newText = oldText;
			newUndoRedo = oldUndoRedo;
			newTextView = oldTextView;
			newMaxColumn = oldMaxColumn;
			newCurrentSelection = oldCurrentSelection;
			newSelections = oldSelections;
			for (var ctr = 0; ctr < oldRegions.Length; ++ctr)
				newRegions[ctr] = oldRegions[ctr];
			newDisplayName = oldDisplayName;
			newFileName = oldFileName;
			newIsModified = oldIsModified;
			newAutoRefresh = oldAutoRefresh;
			newDBName = oldDBName;
			newContentType = oldContentType;
			newCodePage = oldCodePage;
			newAESKey = oldAESKey;
			newCompressed = oldCompressed;
			newLineEnding = oldLineEnding;
			newDiffIgnoreWhitespace = oldDiffIgnoreWhitespace;
			newDiffIgnoreCase = oldDiffIgnoreCase;
			newDiffIgnoreNumbers = oldDiffIgnoreNumbers;
			newDiffIgnoreLineEndings = oldDiffIgnoreLineEndings;
			newIsDiff = oldIsDiff;
			newDiffEncodingMismatch = oldDiffEncodingMismatch;
			newKeepSelections = oldKeepSelections;
			newHighlightSyntax = oldHighlightSyntax;
			newStrictParsing = oldStrictParsing;
			newJumpBy = oldJumpBy;
			newLastActive = oldLastActive;
			newViewBinary = oldViewBinary;
			newViewBinaryCodePages = oldViewBinaryCodePages;
			newViewBinarySearches = oldViewBinarySearches;
			newStartColumn = oldStartColumn;
			newStartRow = oldStartRow;

			state = null;
		}

		public void Commit()
		{
			EnsureInTransaction();

			oldTabs = newTabs;
			oldText = newText;
			oldUndoRedo = newUndoRedo;
			oldTextView = newTextView;
			oldMaxColumn = newMaxColumn;
			oldCurrentSelection = newCurrentSelection;
			oldSelections = newSelections;
			for (var ctr = 0; ctr < oldRegions.Length; ++ctr)
				oldRegions[ctr] = newRegions[ctr];
			oldDisplayName = newDisplayName;
			oldFileName = newFileName;
			oldIsModified = newIsModified;
			oldAutoRefresh = newAutoRefresh;
			oldDBName = newDBName;
			oldContentType = newContentType;
			oldCodePage = newCodePage;
			oldAESKey = newAESKey;
			oldCompressed = newCompressed;
			oldLineEnding = newLineEnding;
			oldDiffIgnoreWhitespace = newDiffIgnoreWhitespace;
			oldDiffIgnoreCase = newDiffIgnoreCase;
			oldDiffIgnoreNumbers = newDiffIgnoreNumbers;
			oldDiffIgnoreLineEndings = newDiffIgnoreLineEndings;
			oldIsDiff = newIsDiff;
			oldDiffEncodingMismatch = newDiffEncodingMismatch;
			oldKeepSelections = newKeepSelections;
			oldHighlightSyntax = newHighlightSyntax;
			oldStrictParsing = newStrictParsing;
			oldJumpBy = newJumpBy;
			oldLastActive = newLastActive;
			oldViewBinary = newViewBinary;
			oldViewBinaryCodePages = newViewBinaryCodePages;
			oldViewBinarySearches = newViewBinarySearches;
			oldStartColumn = newStartColumn;
			oldStartRow = newStartRow;

			state = null;
		}
	}
}
