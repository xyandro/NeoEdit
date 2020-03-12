using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditorData
	{
		NEText oldText, newText;
		public NEText Text
		{
			get => newText;
			private set
			{
				newText = value;
				TextView = new NETextView(newText);
				MaxColumn = Enumerable.Range(0, TextView.NumLines).AsParallel().Max(line => GetLineColumnsLength(line));
			}
		}

		NETextView oldTextView, newTextView;
		public NETextView TextView
		{
			get => newTextView;
			set => newTextView = value;
		}

		int oldMaxColumn, newMaxColumn;
		public int MaxColumn
		{
			get => newMaxColumn;
			set => newMaxColumn = value;
		}

		int oldCurrentSelection, newCurrentSelection;
		public int CurrentSelection
		{
			get => newCurrentSelection;
			set => newCurrentSelection = Math.Max(0, Math.Min(value, Selections.Count - 1));
		}

		List<Range> oldSelections, newSelections;
		public List<Range> Selections
		{
			get => newSelections;
			set
			{
				newSelections = DeOverlap(value);
				CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			}
		}

		readonly List<Range>[] oldRegions = new List<Range>[9];
		readonly List<Range>[] newRegions = new List<Range>[9];
		public List<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return newRegions[region - 1];
		}

		public void SetRegions(int region, List<Range> oldRegions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			newRegions[region - 1] = DeOverlap(oldRegions);
		}

		string oldDisplayName, newDisplayName;
		public string DisplayName
		{
			get => newDisplayName;
			set => newDisplayName = value;
		}

		string oldFileName, newFileName;
		public string FileName
		{
			get => newFileName;
			set => newFileName = value;
		}

		bool oldIsModified, newIsModified;
		public bool IsModified
		{
			get => newIsModified;
			set => newIsModified = value;
		}

		List<string> oldClipboard, newClipboard;
		public List<string> Clipboard
		{
			get => newClipboard;
			set => newClipboard = value;
		}

		bool oldAutoRefresh, newAutoRefresh;
		public bool AutoRefresh
		{
			get => newAutoRefresh;
			set => newAutoRefresh = value;
		}

		string oldDBName, newDBName;
		public string DBName
		{
			get => newDBName;
			set => newDBName = value;
		}

		ParserType oldContentType, newContentType;
		public ParserType ContentType
		{
			get => newContentType;
			set => newContentType = value;
		}

		Coder.CodePage oldCodePage, newCodePage;
		public Coder.CodePage CodePage
		{
			get => newCodePage;
			set => newCodePage = value;
		}

		string oldAESKey, newAESKey;
		public string AESKey
		{
			get => newAESKey;
			set => newAESKey = value;
		}

		bool oldCompressed, newCompressed;
		public bool Compressed
		{
			get => newCompressed;
			set => newCompressed = value;
		}

		string oldLineEnding, newLineEnding;
		public string LineEnding
		{
			get => newLineEnding;
			set => newLineEnding = value;
		}

		bool oldDiffIgnoreWhitespace, newDiffIgnoreWhitespace;
		public bool DiffIgnoreWhitespace
		{
			get => newDiffIgnoreWhitespace;
			set => newDiffIgnoreWhitespace = value;
		}

		bool oldDiffIgnoreCase, newDiffIgnoreCase;
		public bool DiffIgnoreCase
		{
			get => newDiffIgnoreCase;
			set => newDiffIgnoreCase = value;
		}

		bool oldDiffIgnoreNumbers, newDiffIgnoreNumbers;
		public bool DiffIgnoreNumbers
		{
			get => newDiffIgnoreNumbers;
			set => newDiffIgnoreNumbers = value;
		}

		bool oldDiffIgnoreLineEndings, newDiffIgnoreLineEndings;
		public bool DiffIgnoreLineEndings
		{
			get => newDiffIgnoreLineEndings;
			set => newDiffIgnoreLineEndings = value;
		}

		bool oldIsDiff, newIsDiff;
		public bool IsDiff
		{
			get => newIsDiff;
			set => newIsDiff = value;
		}

		bool oldDiffEncodingMismatch, newDiffEncodingMismatch;
		public bool DiffEncodingMismatch
		{
			get => newDiffEncodingMismatch;
			set => newDiffEncodingMismatch = value;
		}

		int oldTextEditorOrder, newTextEditorOrder;
		public int TextEditorOrder
		{
			get => newTextEditorOrder;
			set => newTextEditorOrder = value;
		}

		string oldTabLabel, newTabLabel;
		public string TabLabel
		{
			get => newTabLabel;
			set => newTabLabel = value;
		}

		bool oldKeepSelections, newKeepSelections;
		public bool KeepSelections
		{
			get => newKeepSelections;
			set => newKeepSelections = value;
		}

		bool oldHighlightSyntax, newHighlightSyntax;
		public bool HighlightSyntax
		{
			get => newHighlightSyntax;
			set => newHighlightSyntax = value;
		}

		bool oldStrictParsing, newStrictParsing;
		public bool StrictParsing
		{
			get => newStrictParsing;
			set => newStrictParsing = value;
		}

		JumpByType oldJumpBy, newJumpBy;
		public JumpByType JumpBy
		{
			get => newJumpBy;
			set => newJumpBy = value;
		}

		bool oldViewValues, newViewValues;
		public bool ViewValues
		{
			get => newViewValues;
			set => newViewValues = value;
		}

		IList<byte> oldViewValuesData, newViewValuesData;
		public IList<byte> ViewValuesData
		{
			get => newViewValuesData;
			set => newViewValuesData = value;
		}

		bool oldViewValuesHasSel, newViewValuesHasSel;
		public bool ViewValuesHasSel
		{
			get => newViewValuesHasSel;
			set => newViewValuesHasSel = value;
		}

		double oldXScrollValue, newXScrollValue;
		double XScrollValue
		{
			get => newXScrollValue;
			set => newXScrollValue = value;
		}

		double oldYScrollValue, newYScrollValue;
		double YScrollValue
		{
			get => newYScrollValue;
			set => newYScrollValue = value;
		}

		double oldXScrollViewport, newXScrollViewport;
		double XScrollViewport
		{
			get => newXScrollViewport;
			set => newXScrollViewport = value;
		}

		double oldYScrollViewport, newYScrollViewport;
		double YScrollViewport
		{
			get => newYScrollViewport;
			set => newYScrollViewport = value;
		}

		int XScrollViewportFloor => (int)Math.Floor(XScrollViewport);
		int XScrollViewportCeiling => (int)Math.Ceiling(XScrollViewport);
		int YScrollViewportFloor => (int)Math.Floor(YScrollViewport);
		int YScrollViewportCeiling => (int)Math.Ceiling(YScrollViewport);

		public void Commit()
		{
			oldText = newText;
			undoRedo = newUndoRedo;
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
			oldTabLabel = newTabLabel;
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
		}

		public void Rollback()
		{
			newText = oldText;
			newUndoRedo = undoRedo;
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
			newTabLabel = oldTabLabel;
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
		}
	}
}
