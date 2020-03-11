using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditorData
	{
		const int tabStop = 4;

		public TextEditorData()
		{
			Text = new NEText(File.ReadAllText(@"..\..\a.txt"));
			Selections = new List<Range>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<Range>());
			undoRedo = newUndoRedo = new UndoRedo2();
			Commit();
		}

		UndoRedo2 undoRedo, newUndoRedo;

		NEText text, newText;
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

		public void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight, replaceType, tryJoinUndo);

		public void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections.ToList(), strs, replaceType, tryJoinUndo);

			if (highlight)
				SetSelections(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				SetSelections(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		public void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = Range.FromIndex(ranges[ctr].Start + change, strs[ctr].Length);
				undoRanges.Add(undoRange);
				undoText.Add(Text.GetString(ranges[ctr]));
				change = undoRange.End - ranges[ctr].End;
			}

			var textCanvasUndoRedo = new UndoRedo2.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: UndoRedo2.AddUndone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Redo: UndoRedo2.AddRedone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Normal: UndoRedo2.AddUndo(ref newUndoRedo, textCanvasUndoRedo, IsModified); break;
			}

			Text = Text.Replace(ranges, strs);

			var translateMap = GetTranslateMap(ranges, strs, new List<List<Range>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			SetSelections(Translate(Selections, translateMap));
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		NETextView textView, newTextView;
		public NETextView TextView
		{
			get => newTextView;
			set => newTextView = value;
		}

		public int NumLines => TextView.NumLines;

		int maxColumn, newMaxColumn;
		public int MaxColumn
		{
			get => newMaxColumn;
			set => newMaxColumn = value;
		}

		int currentSelection, newCurrentSelection;
		public int CurrentSelection
		{
			get => newCurrentSelection;
			set => newCurrentSelection = Math.Max(0, Math.Min(value, Selections.Count - 1));
		}

		List<Range> selections, newSelections;
		public List<Range> Selections
		{
			get => newSelections;
			set
			{
				newSelections = value;
				CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			}
		}

		public void SetSelections(List<Range> selections) => Selections = DeOverlap(selections);

		readonly List<Range>[] regions = new List<Range>[9];
		readonly List<Range>[] newRegions = new List<Range>[9];
		public List<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return newRegions[region - 1];
		}

		public void SetRegions(int region, List<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			newRegions[region - 1] = DeOverlap(regions);
		}

		string displayName, newDisplayName;
		public string DisplayName
		{
			get => newDisplayName;
			set => newDisplayName = value;
		}

		string fileName, newFileName;
		public string FileName
		{
			get => newFileName;
			set => newFileName = value;
		}

		bool isModified, newIsModified;
		public bool IsModified
		{
			get => newIsModified;
			set => newIsModified = value;
		}

		bool autoRefresh, newAutoRefresh;
		public bool AutoRefresh
		{
			get => newAutoRefresh;
			set => newAutoRefresh = value;
		}

		ParserType contentType, newContentType;
		public ParserType ContentType
		{
			get => newContentType;
			set => newContentType = value;
		}

		Coder.CodePage codePage, newCodePage;
		public Coder.CodePage CodePage
		{
			get => newCodePage;
			set => newCodePage = value;
		}

		string aesKey, newAESKey;
		public string AESKey
		{
			get => newAESKey;
			set => newAESKey = value;
		}

		bool compressed, newCompressed;
		public bool Compressed
		{
			get => newCompressed;
			set => newCompressed = value;
		}

		string lineEnding, newLineEnding;
		public string LineEnding
		{
			get => newLineEnding;
			set => newLineEnding = value;
		}

		bool diffIgnoreWhitespace, newDiffIgnoreWhitespace;
		public bool DiffIgnoreWhitespace
		{
			get => newDiffIgnoreWhitespace;
			set => newDiffIgnoreWhitespace = value;
		}

		bool diffIgnoreCase, newDiffIgnoreCase;
		public bool DiffIgnoreCase
		{
			get => newDiffIgnoreCase;
			set => newDiffIgnoreCase = value;
		}

		bool diffIgnoreNumbers, newDiffIgnoreNumbers;
		public bool DiffIgnoreNumbers
		{
			get => newDiffIgnoreNumbers;
			set => newDiffIgnoreNumbers = value;
		}

		bool diffIgnoreLineEndings, newDiffIgnoreLineEndings;
		public bool DiffIgnoreLineEndings
		{
			get => newDiffIgnoreLineEndings;
			set => newDiffIgnoreLineEndings = value;
		}

		bool isDiff, newIsDiff;
		public bool IsDiff
		{
			get => newIsDiff;
			set => newIsDiff = value;
		}

		bool diffEncodingMismatch, newDiffEncodingMismatch;
		public bool DiffEncodingMismatch
		{
			get => newDiffEncodingMismatch;
			set => newDiffEncodingMismatch = value;
		}

		int textEditorOrder, newTextEditorOrder;
		public int TextEditorOrder
		{
			get => newTextEditorOrder;
			set => newTextEditorOrder = value;
		}

		string tabLabel, newTabLabel;
		public string TabLabel
		{
			get => newTabLabel;
			set => newTabLabel = value;
		}

		bool keepSelections, newKeepSelections;
		public bool KeepSelections
		{
			get => newKeepSelections;
			set => newKeepSelections = value;
		}

		bool highlightSyntax, newHighlightSyntax;
		public bool HighlightSyntax
		{
			get => newHighlightSyntax;
			set => newHighlightSyntax = value;
		}

		bool strictParsing, newStrictParsing;
		public bool StrictParsing
		{
			get => newStrictParsing;
			set => newStrictParsing = value;
		}

		JumpByType jumpBy, newJumpBy;
		public JumpByType JumpBy
		{
			get => newJumpBy;
			set => newJumpBy = value;
		}

		bool viewValues, newViewValues;
		public bool ViewValues
		{
			get => newViewValues;
			set => newViewValues = value;
		}

		IList<byte> viewValuesData, newViewValuesData;
		public IList<byte> ViewValuesData
		{
			get => newViewValuesData;
			set => newViewValuesData = value;
		}

		bool viewValuesHasSel, newViewValuesHasSel;
		public bool ViewValuesHasSel
		{
			get => newViewValuesHasSel;
			set => newViewValuesHasSel = value;
		}


		public void Commit()
		{
			text = newText;
			undoRedo = newUndoRedo;
			textView = newTextView;
			maxColumn = newMaxColumn;
			currentSelection = newCurrentSelection;
			selections = newSelections;
			for (var ctr = 0; ctr < regions.Length; ++ctr)
				regions[ctr] = newRegions[ctr];
			displayName = newDisplayName;
			fileName = newFileName;
			isModified = newIsModified;
			autoRefresh = newAutoRefresh;
			contentType = newContentType;
			codePage = newCodePage;
			aesKey = newAESKey;
			compressed = newCompressed;
			lineEnding = newLineEnding;
			diffIgnoreWhitespace = newDiffIgnoreWhitespace;
			diffIgnoreCase = newDiffIgnoreCase;
			diffIgnoreNumbers = newDiffIgnoreNumbers;
			diffIgnoreLineEndings = newDiffIgnoreLineEndings;
			isDiff = newIsDiff;
			diffEncodingMismatch = newDiffEncodingMismatch;
			textEditorOrder = newTextEditorOrder;
			tabLabel = newTabLabel;
			keepSelections = newKeepSelections;
			highlightSyntax = newHighlightSyntax;
			strictParsing = newStrictParsing;
			jumpBy = newJumpBy;
			viewValues = newViewValues;
			viewValuesData = newViewValuesData;
			viewValuesHasSel = newViewValuesHasSel;
		}

		public void Rollback()
		{
			newText = text;
			newUndoRedo = undoRedo;
			newTextView = textView;
			newMaxColumn = maxColumn;
			newCurrentSelection = currentSelection;
			newSelections = selections;
			for (var ctr = 0; ctr < regions.Length; ++ctr)
				newRegions[ctr] = regions[ctr];
			newDisplayName = displayName;
			newFileName = fileName;
			newIsModified = isModified;
			newAutoRefresh = autoRefresh;
			newContentType = contentType;
			newCodePage = codePage;
			newAESKey = aesKey;
			newCompressed = compressed;
			newLineEnding = lineEnding;
			newDiffIgnoreWhitespace = diffIgnoreWhitespace;
			newDiffIgnoreCase = diffIgnoreCase;
			newDiffIgnoreNumbers = diffIgnoreNumbers;
			newDiffIgnoreLineEndings = diffIgnoreLineEndings;
			newIsDiff = isDiff;
			newDiffEncodingMismatch = diffEncodingMismatch;
			newTextEditorOrder = textEditorOrder;
			newTabLabel = tabLabel;
			newKeepSelections = keepSelections;
			newHighlightSyntax = highlightSyntax;
			newStrictParsing = strictParsing;
			newJumpBy = jumpBy;
			newViewValues = viewValues;
			newViewValuesData = viewValuesData;
			newViewValuesHasSel = viewValuesHasSel;
		}

		#region Translate
		static int[] GetTranslateNums(List<List<Range>> rangeLists)
		{
			var nums = new int[rangeLists.Sum(rangeList => rangeList.Count * 2)];
			var numsStart = 0;
			foreach (var rangeList in rangeLists)
			{
				var size = Math.Max(65536, (rangeList.Count + 31) / 32);
				Helpers.PartitionedParallelForEach(rangeList.Count, size, (start, end) =>
				{
					var numPos = numsStart + start * 2;
					for (var r = start; r < end; ++r)
					{
						nums[numPos++] = rangeList[r].Start;
						nums[numPos++] = rangeList[r].End;
					}
				});
				numsStart += rangeList.Count * 2;
			}

			Array.Sort(nums);

			var outPos = -1;
			for (var inPos = 0; inPos < nums.Length; ++inPos)
			{
				if ((outPos != -1) && (nums[inPos] == nums[outPos]))
					continue;
				nums[++outPos] = nums[inPos];
			}

			Array.Resize(ref nums, outPos + 1);
			return nums;
		}

		static Tuple<int[], int[]> GetTranslateMap(List<Range> replaceRanges, List<string> strs, List<List<Range>> rangeLists)
		{
			var translateNums = GetTranslateNums(rangeLists);
			var translateResults = new int[translateNums.Length];
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < translateNums.Length)
			{
				int start = int.MaxValue, end = int.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (translateNums[current] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = translateNums[current];
				if ((value > start) && (value < end))
					value = start + length;

				translateResults[current] = value + offset;
				++current;
			}

			return Tuple.Create(translateNums, translateResults);
		}

		static List<Range> Translate(List<Range> ranges, Tuple<int[], int[]> translateMap)
		{
			var result = Helpers.PartitionedParallelForEach<Range>(ranges.Count, Math.Max(65536, (ranges.Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].End, current);
					if (ranges[ctr].Cursor < ranges[ctr].Anchor)
						list.Add(new Range(translateMap.Item2[startPos], translateMap.Item2[current]));
					else
						list.Add(new Range(translateMap.Item2[current], translateMap.Item2[startPos]));
				}
			});
			return result;
		}
		#endregion

		#region DeOverlap
		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		static List<Range> DeOverlap(List<Range> items)
		{
			while (true)
			{
				switch (GetDeOverlapStep(items))
				{
					case DeOverlapStep.Sort: items = items.OrderBy(range => range.Start).ThenBy(range => range.End).ToList(); break;
					case DeOverlapStep.DeOverlap: return DoDeOverlap(items);
					case DeOverlapStep.Done: return items;
					default: throw new Exception("Invalid step");
				}
			}
		}

		static DeOverlapStep GetDeOverlapStep(List<Range> items)
		{
			var result = DeOverlapStep.Done;
			for (var ctr = 1; ctr < items.Count; ++ctr)
			{
				if ((items[ctr].Start < items[ctr - 1].Start) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End < items[ctr - 1].End)))
					return DeOverlapStep.Sort;

				if ((items[ctr].Start < items[ctr - 1].End) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End == items[ctr - 1].End)))
					result = DeOverlapStep.DeOverlap;
			}

			return result;
		}

		static List<Range> DoDeOverlap(List<Range> items)
		{
			var result = new List<Range>();

			using (var enumerator = items.GetEnumerator())
			{
				var last = default(Range);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Anchor)
							result.Add(new Range(last.Start, range.Start));
						else
							result.Add(new Range(range.Start, last.Start));
						last = null;
					}

					if (range == null)
						break;

					if ((last != null) && (range.End <= last.End))
						continue;

					last = range;
				}
			}

			return result;
		}
		#endregion

		#region Columns
		public int GetPositionLine(int position) => TextView.GetPositionLine(position);
		public int GetPositionIndex(int position, int line) => TextView.GetPositionIndex(position, line);
		public int GetLineLength(int line) => TextView.GetLineLength(line);
		public int GetPosition(int line, int index, bool allowJustPastEnd = false) => TextView.GetPosition(line, index, allowJustPastEnd);

		public int GetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var position = TextView.GetPosition(line, 0);
			var end = position + GetLineLength(line);
			while (column < findColumn)
			{
				var find = Text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findColumn - column;
				else
					find = Math.Min(find - position, findColumn - column);

				column += find;
				position += find;
			}
			if (position > end + 1)
			{
				if (returnMaxOnFail)
					return GetLineLength(line) + 1;
				throw new IndexOutOfRangeException();
			}
			return position - TextView.GetPosition(line, 0);
		}

		public int GetColumnFromIndex(int line, int findIndex)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();

			var column = 0;
			var position = TextView.GetPosition(line, 0);
			var findPosition = findIndex + position;
			var end = position + GetLineLength(line);
			while (position < findPosition)
			{
				var find = Text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findPosition - position;
				else
					find = Math.Min(find, findPosition) - position;

				column += find;
				position += find;
			}
			return column;
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = TextView.GetPosition(line, 0);
			var len = TextView.GetLineLength(line);
			var columns = 0;
			while (len > 0)
			{
				var find = Text.IndexOf('\t', index, len);
				if (find == index)
				{
					columns = (columns / tabStop + 1) * tabStop;
					++index;
					--len;
					continue;
				}

				if (find == -1)
					find = len;
				else
					find -= index;
				columns += find;
				index += find;
				len -= find;
			}

			return columns;
		}

		public string GetLineColumns(int line, int startColumn, int endColumn)
		{
			if ((line < 0) || (line >= TextView.NumLines))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = TextView.GetPosition(line, 0);
			var endIndex = index + TextView.GetLineLength(line);
			var sb = new StringBuilder();
			while ((column < endColumn) && (index < endIndex))
			{
				var skipColumns = Math.Max(0, startColumn - column);
				var takeColumns = endColumn - column;

				var tabIndex = Text.IndexOf('\t', index, Math.Min(endIndex - index, takeColumns));
				if (tabIndex == index)
				{
					var repeatCount = (column / tabStop + 1) * tabStop - column;
					var useColumns = Math.Min(Math.Max(0, repeatCount - skipColumns), takeColumns);
					sb.Append(' ', useColumns);
					column += repeatCount;
					++index;
					continue;
				}

				if (tabIndex == -1)
					tabIndex = endIndex;

				if (skipColumns > 0)
				{
					var useColumns = Math.Min(skipColumns, tabIndex - index);
					index += useColumns;
					column += useColumns;
				}

				{
					var useColumns = Math.Min(tabIndex - index, takeColumns);
					sb.Append(Text.GetString(index, useColumns));
					column += useColumns;
					index += useColumns;
				}
			}

			return sb.ToString();
		}
		#endregion

		public void ExecuteCommand(CommandState state)
		{
			switch (state.Command)
			{
				case NECommand.Text: Command_Text(state.Parameters as string); break;
				case NECommand.Edit_Undo: Command_Edit_Undo(); break;
				case NECommand.Edit_Redo: Command_Edit_Redo(); break;
				case NECommand.Select_All: Command_Select_All(); break;
				case NECommand.Select_Lines: Command_Select_Lines(); break;
				case NECommand.Select_WholeLines: Command_Select_WholeLines(); break;
			}
		}

		void Command_Text(string text) => ReplaceSelections(text, false, tryJoinUndo: true);
	}
}
