using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		void DoRangesDiff(IReadOnlyList<Range> ranges)
		{
			if (!ranges.Any())
				return;

			if (ranges.Count % 2 != 0)
				throw new Exception("Must have even number of items.");

			var codePage = CodePage; // Must save as other threads can't access DependencyProperties
			var tabs = new Tabs();
			var batches = ranges.AsTaskRunner().Select(range => Text.GetString(range)).Select(str => Coder.StringToBytes(str, codePage)).Batch(2).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(new Tab(bytes: batch[0], codePage: codePage, modified: false), new Tab(bytes: batch[1], codePage: codePage, modified: false));
		}

		bool GetLineDiffMatches(int line) => diffData == null ? true : diffData.LineCompare[line] == DiffType.Match;

		Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		{
			if (next)
			{
				var endLine = DiffView.GetPositionLine(range.End);

				while ((endLine < DiffView.NumLines) && (GetLineDiffMatches(endLine)))
					++endLine;
				while ((endLine < DiffView.NumLines) && (!GetLineDiffMatches(endLine)))
					++endLine;

				var startLine = endLine;
				while ((startLine > 0) && (!GetLineDiffMatches(startLine - 1)))
					--startLine;

				return Tuple.Create(startLine, endLine);
			}
			else
			{
				var startLine = DiffView.GetPositionLine(Math.Max(0, range.Start - 1));

				while ((startLine > 0) && (GetLineDiffMatches(startLine)))
					--startLine;
				while ((startLine > 0) && (!GetLineDiffMatches(startLine - 1)))
					--startLine;

				var endLine = startLine;
				while ((endLine < DiffView.NumLines) && (!GetLineDiffMatches(endLine)))
					++endLine;

				return Tuple.Create(startLine, endLine);
			}
		}

		static public Tuple<List<Tuple<int, int>>, List<string>> GetDiffFixes(Tab src, Tab dest, int lineStartTabStop, bool? ignoreWhitespace, bool? ignoreCase, bool? ignoreNumbers, bool? ignoreLineEndings, string ignoreCharacters)
		{
			var tab = new Tab[] { src, dest };
			var lineMap = new Dictionary<int, int>[2];
			var lines = new List<string>[2];
			var textLines = new List<string>[2];
			var diffParams = new DiffParams(ignoreWhitespace ?? true, ignoreCase ?? true, ignoreNumbers ?? true, ignoreLineEndings ?? true, ignoreCharacters, lineStartTabStop);
			for (var pass = 0; pass < 2; ++pass)
			{
				lineMap[pass] = Enumerable.Range(0, tab[pass].DiffView.NumLines).Indexes(line => tab[pass].diffData?.LineCompare[line] != DiffType.GapMismatch).Select((index1, index2) => new { index1, index2 }).ToDictionary(obj => obj.index2, obj => obj.index1);
				lines[pass] = lineMap[pass].Values.Select(line => tab[pass].Text.GetString(tab[pass].DiffView.GetLine(line, true))).ToList();
				textLines[pass] = lines[pass].Select(line => diffParams.FormatLine(line).Item1).ToList();
			}

			var linesLCS = LCS.GetLCS(textLines[0], textLines[1], (str1, str2) => string.IsNullOrWhiteSpace(str1) == string.IsNullOrWhiteSpace(str2));

			var ranges = new List<Tuple<int, int>>();
			var strs = new List<string>();
			var curLine = new int[] { -1, -1 };
			diffParams = new DiffParams(ignoreWhitespace ?? false, ignoreCase ?? false, ignoreNumbers ?? false, ignoreLineEndings ?? false, ignoreCharacters);
			for (var line = 0; line < linesLCS.Count; ++line)
			{
				var mappedCurLine = new int[2];
				for (var pass = 0; pass < 2; ++pass)
					if (linesLCS[line][pass] != LCS.MatchType.Gap)
					{
						++curLine[pass];
						mappedCurLine[pass] = lineMap[pass][curLine[pass]];
					}

				if (linesLCS[line].IsMatch)
				{
					var colLines = new string[2];
					var map = new List<int>[2];
					for (var pass = 0; pass < 2; ++pass)
					{
						var formatDiffLine = diffParams.FormatLine(lines[pass][curLine[pass]]);
						colLines[pass] = formatDiffLine.Item1;
						map[pass] = formatDiffLine.Item2;
					}

					if (colLines[0] != colLines[1])
					{
						var colsLCS = LCS.GetLCS(colLines[0], colLines[1]);
						for (var pass = 0; pass < 2; ++pass)
						{
							var start = default(int?);
							var pos = -1;
							for (var ctr = 0; ctr <= colsLCS.Count; ++ctr)
							{
								if ((ctr == colsLCS.Count) || (colsLCS[ctr][pass] != LCS.MatchType.Gap))
									++pos;

								if ((ctr == colsLCS.Count) || (colsLCS[ctr].IsMatch))
								{
									if (start.HasValue)
									{
										var linePosition = tab[pass].DiffView.GetPosition(mappedCurLine[pass], 0);
										var begin = linePosition + map[pass][start.Value];
										var end = linePosition + map[pass][pos];
										if (pass == 0)
											strs.Add(tab[pass].Text.GetString(begin, end - begin));
										else
											ranges.Add(Tuple.Create(begin, end));
										start = null;
									}
									continue;
								}

								start = start ?? pos + (colsLCS[ctr][pass] == LCS.MatchType.Gap ? 1 : 0);
							}
						}
					}
				}

				//if ((ignoreLineEndings == null) && (src.OnlyEnding != null) && (linesLCS[line][1] != LCS.MatchType.Gap))
				//{
				//	var endingStart = dest.endingPosition[mappedCurLine[1]];
				//	var endingEnd = dest.linePosition[mappedCurLine[1] + 1];
				//	if (endingStart == endingEnd)
				//		continue;

				//	if (dest.Data.Substring(endingStart, endingEnd - endingStart) != src.OnlyEnding)
				//	{
				//		ranges.Add(Tuple.Create(endingStart, endingEnd));
				//		strs.Add(src.OnlyEnding);
				//	}
				//}
			}

			return Tuple.Create(ranges, strs);
		}

		List<Range> GetDiffMatches(bool match)
		{
			if (diffData == null)
				throw new ArgumentException("No diff in progress");

			var result = new List<Range>();
			var matchTuple = default(Range);
			var line = 0;
			while (true)
			{
				var end = line >= DiffView.NumLines;

				if ((!end) && (GetLineDiffMatches(line) == match))
				{
					var lineRange = DiffView.GetLine(line, true);
					if (matchTuple == default)
						matchTuple = lineRange;
					else
						matchTuple = new Range(lineRange.End, matchTuple.Start);
				}
				else if (matchTuple != null)
				{
					result.Add(matchTuple);
					matchTuple = null;
				}

				if (end)
					break;

				++line;
			}
			return result;
		}

		void Execute_Diff_Selections() => DoRangesDiff(Selections);

		void Execute_Diff_SelectedFiles()
		{
			if (!Selections.Any())
				return;

			if (Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var files = GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var tabs = new Tabs();
			var batches = files.Batch(2).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(new Tab(fileName: batch[0]), new Tab(fileName: batch[1]));
		}

		void Execute_Diff_Diff_VCSNormalFiles()
		{
			var files = GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var original = files.Select(file => Versioner.GetUnmodifiedFile(file)).ToList();
			var invalidIndexes = original.Indexes(file => file == null);
			if (invalidIndexes.Any())
				throw new Exception($"Unable to get unmodified files:\n{string.Join("\n", invalidIndexes.Select(index => files[index]))}");

			var tabs = new Tabs();
			for (var ctr = 0; ctr < files.Count; ctr++)
				tabs.AddDiff(new Tab(displayName: Path.GetFileName(files[ctr]), modified: false, bytes: original[ctr]), new Tab(fileName: files[ctr]));
		}

		void Execute_Diff_Regions_Region(int useRegion) => DoRangesDiff(GetRegions(useRegion));

		void Execute_Diff_Break() => DiffTarget = null;

		void Execute_Diff_IgnoreWhitespace(bool? multiStatus)
		{
			DiffIgnoreWhitespace = multiStatus != true;
			CalculateDiff();
		}

		void Execute_Diff_IgnoreCase(bool? multiStatus)
		{
			DiffIgnoreCase = multiStatus != true;
			CalculateDiff();
		}

		void Execute_Diff_IgnoreNumbers(bool? multiStatus)
		{
			DiffIgnoreNumbers = multiStatus != true;
			CalculateDiff();
		}

		void Execute_Diff_IgnoreLineEndings(bool? multiStatus)
		{
			DiffIgnoreLineEndings = multiStatus != true;
			CalculateDiff();
		}

		Configuration_Diff_IgnoreCharacters Configure_Diff_IgnoreCharacters() => Tabs.TabsWindow.Configure_Diff_IgnoreCharacters(DiffIgnoreCharacters);

		void Execute_Diff_IgnoreCharacters()
		{
			var result = state.Configuration as Configuration_Diff_IgnoreCharacters;
			DiffIgnoreCharacters = result.IgnoreCharacters;
			CalculateDiff();
		}

		void Execute_Diff_Reset()
		{
			DiffIgnoreWhitespace = DiffIgnoreCase = DiffIgnoreNumbers = DiffIgnoreLineEndings = false;
			DiffIgnoreCharacters = null;
			CalculateDiff();
		}

		void Execute_Diff_NextPrevious(bool next, bool shiftDown)
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			if ((Tabs.GetTabIndex(this) < DiffTarget.Tabs.GetTabIndex(DiffTarget)) && (Tabs.ActiveTabs.Contains(DiffTarget)))
				return;

			Tabs.AddToTransaction(DiffTarget);
			var lines = Selections.AsTaskRunner().Select(range => GetDiffNextPrevious(range, next)).ToList();
			for (var pass = 0; pass < 2; ++pass)
			{
				var target = pass == 0 ? this : DiffTarget;
				var sels = lines.Select(tuple => new Range(target.DiffView.GetPosition(tuple.Item2, 0, true), target.DiffView.GetPosition(tuple.Item1, 0, true))).ToList();
				if (shiftDown)
					sels.AddRange(target.Selections);
				target.Selections = sels;
			}
		}

		void Execute_Diff_CopyLeftRight(bool moveLeft)
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var target = Tabs.GetTabIndex(this) < DiffTarget.Tabs.GetTabIndex(DiffTarget) ? this : DiffTarget;
			var source = target == this ? DiffTarget : this;
			if (!moveLeft)
				Helpers.Swap(ref target, ref source);

			// If both tabs are active only do this from the target tab
			var bothActive = Tabs.ActiveTabs.Contains(DiffTarget);
			if ((bothActive) && (target != this))
				return;

			Tabs.AddToTransaction(DiffTarget);
			var strs = source.GetSelectionStrings();
			target.ReplaceSelections(strs);
			// If both tabs are active queue an empty edit so undo will take both back to the same place
			if (bothActive)
				source.ReplaceSelections(strs);
		}

		Configuration_Diff_Fix_Whitespace_Dialog Configure_Diff_Fix_Whitespace_Dialog() => Tabs.TabsWindow.Configure_Diff_Fix_Whitespace_Dialog();

		void Execute_Diff_Fix_Whitespace()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var result = state.Configuration as Configuration_Diff_Fix_Whitespace_Dialog;
			var fixes = GetDiffFixes(DiffTarget, this, result.LineStartTabStop, null, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_Case()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = GetDiffFixes(DiffTarget, this, 0, DiffIgnoreWhitespace, null, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_Numbers()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = GetDiffFixes(DiffTarget, this, 0, DiffIgnoreWhitespace, DiffIgnoreCase, null, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_LineEndings()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = GetDiffFixes(DiffTarget, this, 0, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, null, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_Encoding()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			CodePage = DiffTarget.CodePage;
		}

		void Execute_Diff_Select_MatchDiff(bool matching)
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			Selections = GetDiffMatches(matching);
		}
	}
}
