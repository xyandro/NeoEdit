﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditor
	{
		bool inTransaction = false;
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		NEText oldText, newText;
		public NEText Text
		{
			get => newText;
			private set
			{
				EnsureInTransaction();
				newText = value;
				TextView = new NETextView(newText);
				MaxColumn = Enumerable.Range(0, TextView.NumLines).AsParallel().Max(line => GetLineColumnsLength(line));
			}
		}

		NETextView oldTextView, newTextView;
		public NETextView TextView
		{
			get => newTextView;
			set
			{
				EnsureInTransaction();
				newTextView = value;
			}
		}

		int oldMaxColumn, newMaxColumn;
		public int MaxColumn
		{
			get => newMaxColumn;
			set
			{
				EnsureInTransaction();
				newMaxColumn = value;
			}
		}

		int oldCurrentSelection, newCurrentSelection;
		public int CurrentSelection
		{
			get => newCurrentSelection;
			set
			{
				EnsureInTransaction();
				newCurrentSelection = Math.Max(0, Math.Min(value, Selections.Count - 1));
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

		public void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			newRegions[region - 1] = DeOverlap(regions);
		}

		string oldDisplayName, newDisplayName;
		public string DisplayName
		{
			get => newDisplayName;
			set
			{
				EnsureInTransaction();
				newDisplayName = value;
			}
		}

		string oldFileName, newFileName;
		public string FileName
		{
			get => newFileName;
			set
			{
				EnsureInTransaction();
				newFileName = value;
			}
		}

		bool oldIsModified, newIsModified;
		public bool IsModified
		{
			get => newIsModified;
			set
			{
				EnsureInTransaction();
				newIsModified = value;
			}
		}

		IReadOnlyList<string> oldClipboard, newClipboard;
		public IReadOnlyList<string> Clipboard
		{
			get
			{
				if (newClipboard == null)
					oldClipboard = newClipboard = commandState.GetClipboard(this);

				return newClipboard;
			}

			set
			{
				EnsureInTransaction();
				newClipboard = value;
			}
		}

		public Tuple<IReadOnlyList<string>, bool?> ChangedClipboard => (oldClipboard == newClipboard) ? default : Tuple.Create(newClipboard, newClipboardIsCut);

		bool? oldClipboardIsCut, newClipboardIsCut;
		public bool? ClipboardIsCut
		{
			get => newClipboardIsCut;
			set
			{
				EnsureInTransaction();
				newClipboardIsCut = value;
			}
		}

		bool oldAutoRefresh, newAutoRefresh;
		public bool AutoRefresh
		{
			get => newAutoRefresh;
			set
			{
				EnsureInTransaction();
				newAutoRefresh = value;
			}
		}

		string oldDBName, newDBName;
		public string DBName
		{
			get => newDBName;
			set
			{
				EnsureInTransaction();
				newDBName = value;
			}
		}

		ParserType oldContentType, newContentType;
		public ParserType ContentType
		{
			get => newContentType;
			set
			{
				EnsureInTransaction();
				newContentType = value;
			}
		}

		Coder.CodePage oldCodePage, newCodePage;
		public Coder.CodePage CodePage
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
			set
			{
				EnsureInTransaction();
				newAESKey = value;
			}
		}

		bool oldCompressed, newCompressed;
		public bool Compressed
		{
			get => newCompressed;
			set
			{
				EnsureInTransaction();
				newCompressed = value;
			}
		}

		string oldLineEnding, newLineEnding;
		public string LineEnding
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
			set
			{
				EnsureInTransaction();
				newDiffIgnoreWhitespace = value;
			}
		}

		bool oldDiffIgnoreCase, newDiffIgnoreCase;
		public bool DiffIgnoreCase
		{
			get => newDiffIgnoreCase;
			set
			{
				EnsureInTransaction();
				newDiffIgnoreCase = value;
			}
		}

		bool oldDiffIgnoreNumbers, newDiffIgnoreNumbers;
		public bool DiffIgnoreNumbers
		{
			get => newDiffIgnoreNumbers;
			set
			{
				EnsureInTransaction();
				newDiffIgnoreNumbers = value;
			}
		}

		bool oldDiffIgnoreLineEndings, newDiffIgnoreLineEndings;
		public bool DiffIgnoreLineEndings
		{
			get => newDiffIgnoreLineEndings;
			set
			{
				EnsureInTransaction();
				newDiffIgnoreLineEndings = value;
			}
		}

		bool oldIsDiff, newIsDiff;
		public bool IsDiff
		{
			get => newIsDiff;
			set
			{
				EnsureInTransaction();
				newIsDiff = value;
			}
		}

		bool oldDiffEncodingMismatch, newDiffEncodingMismatch;
		public bool DiffEncodingMismatch
		{
			get => newDiffEncodingMismatch;
			set
			{
				EnsureInTransaction();
				newDiffEncodingMismatch = value;
			}
		}

		int oldTextEditorOrder, newTextEditorOrder;
		public int TextEditorOrder
		{
			get => newTextEditorOrder;
			set
			{
				EnsureInTransaction();
				newTextEditorOrder = value;
			}
		}

		bool oldKeepSelections, newKeepSelections;
		public bool KeepSelections
		{
			get => newKeepSelections;
			set
			{
				EnsureInTransaction();
				newKeepSelections = value;
			}
		}

		bool oldHighlightSyntax, newHighlightSyntax;
		public bool HighlightSyntax
		{
			get => newHighlightSyntax;
			set
			{
				EnsureInTransaction();
				newHighlightSyntax = value;
			}
		}

		bool oldStrictParsing, newStrictParsing;
		public bool StrictParsing
		{
			get => newStrictParsing;
			set
			{
				EnsureInTransaction();
				newStrictParsing = value;
			}
		}

		JumpByType oldJumpBy, newJumpBy;
		public JumpByType JumpBy
		{
			get => newJumpBy;
			set
			{
				EnsureInTransaction();
				newJumpBy = value;
			}
		}

		bool oldViewValues, newViewValues;
		public bool ViewValues
		{
			get => newViewValues;
			set
			{
				EnsureInTransaction();
				newViewValues = value;
			}
		}

		IList<byte> oldViewValuesData, newViewValuesData;
		public IList<byte> ViewValuesData
		{
			get => newViewValuesData;
			set
			{
				EnsureInTransaction();
				newViewValuesData = value;
			}
		}

		bool oldViewValuesHasSel, newViewValuesHasSel;
		public bool ViewValuesHasSel
		{
			get => newViewValuesHasSel;
			set
			{
				EnsureInTransaction();
				newViewValuesHasSel = value;
			}
		}

		double oldXScrollValue, newXScrollValue;
		double XScrollValue
		{
			get => newXScrollValue;
			set
			{
				EnsureInTransaction();
				newXScrollValue = value;
			}
		}

		double oldYScrollValue, newYScrollValue;
		double YScrollValue
		{
			get => newYScrollValue;
			set
			{
				EnsureInTransaction();
				newYScrollValue = value;
			}
		}

		double oldXScrollViewport, newXScrollViewport;
		double XScrollViewport
		{
			get => newXScrollViewport;
			set
			{
				EnsureInTransaction();
				newXScrollViewport = value;
			}
		}

		double oldYScrollViewport, newYScrollViewport;
		double YScrollViewport
		{
			get => newYScrollViewport;
			set
			{
				EnsureInTransaction();
				newYScrollViewport = value;
			}
		}

		int XScrollViewportFloor => (int)Math.Floor(XScrollViewport);
		int XScrollViewportCeiling => (int)Math.Ceiling(XScrollViewport);
		int YScrollViewportFloor => (int)Math.Floor(YScrollViewport);
		int YScrollViewportCeiling => (int)Math.Ceiling(YScrollViewport);

		UndoRedo oldUndoRedo, newUndoRedo;

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			oldClipboard = newClipboard = null;
			oldClipboardIsCut = newClipboardIsCut = null;
		}

		public void Rollback()
		{
			EnsureInTransaction();

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
			newClipboard = oldClipboard;
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
			newTextEditorOrder = oldTextEditorOrder;
			newKeepSelections = oldKeepSelections;
			newHighlightSyntax = oldHighlightSyntax;
			newStrictParsing = oldStrictParsing;
			newJumpBy = oldJumpBy;
			newViewValues = oldViewValues;
			newViewValuesData = oldViewValuesData;
			newViewValuesHasSel = oldViewValuesHasSel;
			newXScrollValue = oldXScrollValue;
			newYScrollValue = oldYScrollValue;
			newXScrollViewport = oldXScrollViewport;
			newYScrollViewport = oldYScrollViewport;

			inTransaction = false;
		}

		public void Commit()
		{
			EnsureInTransaction();
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
			oldClipboard = newClipboard;
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
			oldTextEditorOrder = newTextEditorOrder;
			oldKeepSelections = newKeepSelections;
			oldHighlightSyntax = newHighlightSyntax;
			oldStrictParsing = newStrictParsing;
			oldJumpBy = newJumpBy;
			oldViewValues = newViewValues;
			oldViewValuesData = newViewValuesData;
			oldViewValuesHasSel = newViewValuesHasSel;
			oldXScrollValue = newXScrollValue;
			oldYScrollValue = newYScrollValue;
			oldXScrollViewport = newXScrollViewport;
			oldYScrollViewport = newYScrollViewport;

			inTransaction = false;
		}
	}
}