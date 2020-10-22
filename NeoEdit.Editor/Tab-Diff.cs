using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.Transactional;
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

		Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		{
			if (next)
			{
				var endLine = Text.GetPositionLine(range.End);

				while ((endLine < Text.NumLines) && (Text.GetLineDiffMatches(endLine)))
					++endLine;
				while ((endLine < Text.NumLines) && (!Text.GetLineDiffMatches(endLine)))
					++endLine;

				var startLine = endLine;
				while ((startLine > 0) && (!Text.GetLineDiffMatches(startLine - 1)))
					--startLine;

				return Tuple.Create(startLine, endLine);
			}
			else
			{
				var startLine = Text.GetPositionLine(Math.Max(0, range.Start - 1));

				while ((startLine > 0) && (Text.GetLineDiffMatches(startLine)))
					--startLine;
				while ((startLine > 0) && (!Text.GetLineDiffMatches(startLine - 1)))
					--startLine;

				var endLine = startLine;
				while ((endLine < Text.NumLines) && (!Text.GetLineDiffMatches(endLine)))
					++endLine;

				return Tuple.Create(startLine, endLine);
			}
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
				var sels = lines.Select(tuple => new Range(target.Text.GetPosition(tuple.Item2, 0, true), target.Text.GetPosition(tuple.Item1, 0, true))).ToList();
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
			var fixes = NEText.GetDiffFixes(DiffTarget.Text, Text, result.LineStartTabStop, null, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_Case()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = NEText.GetDiffFixes(DiffTarget.Text, Text, 0, DiffIgnoreWhitespace, null, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_Numbers()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = NEText.GetDiffFixes(DiffTarget.Text, Text, 0, DiffIgnoreWhitespace, DiffIgnoreCase, null, DiffIgnoreLineEndings, DiffIgnoreCharacters);
			Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
			ReplaceSelections(fixes.Item2);
		}

		void Execute_Diff_Fix_LineEndings()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = NEText.GetDiffFixes(DiffTarget.Text, Text, 0, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, null, DiffIgnoreCharacters);
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

			Selections = Text.GetDiffMatches(matching).Select(tuple => new Range(tuple.Item2, tuple.Item1)).ToList();
		}
	}
}
