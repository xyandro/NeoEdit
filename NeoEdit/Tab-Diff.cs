using System;
using System.Data;
using System.IO;
using System.Linq;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Models;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tab
	{
		// TODO: Everything here
		//void DoRangesDiff(RangeList ranges)
		//{
		//	if (!ranges.Any())
		//		return;

		//	if (ranges.Count % 2 != 0)
		//		throw new Exception("Must have even number of items.");

		//	var codePage = CodePage; // Must save as other threads can't access DependencyProperties
		//	var tabsWindow = new TabsWindow();
		//	var batches = ranges.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str => Coder.StringToBytes(str, codePage)).Batch(2).ToList();
		//	foreach (var batch in batches)
		//		tabsWindow.AddDiff(bytes1: batch[0], codePage1: codePage, modified1: false, bytes2: batch[1], codePage2: codePage, modified2: false);
		//}

		//Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		//{
		//	if (next)
		//	{
		//		var endLine = TextView.GetPositionLine(range.End);

		//		while ((endLine < TextView.NumLines) && (Data.GetLineDiffMatches(endLine)))
		//			++endLine;
		//		while ((endLine < TextView.NumLines) && (!Data.GetLineDiffMatches(endLine)))
		//			++endLine;

		//		var startLine = endLine;
		//		while ((startLine > 0) && (!Data.GetLineDiffMatches(startLine - 1)))
		//			--startLine;

		//		return Tuple.Create(startLine, endLine);
		//	}
		//	else
		//	{
		//		var startLine = TextView.GetPositionLine(Math.Max(0, range.Start - 1));

		//		while ((startLine > 0) && (Data.GetLineDiffMatches(startLine)))
		//			--startLine;
		//		while ((startLine > 0) && (!Data.GetLineDiffMatches(startLine - 1)))
		//			--startLine;

		//		var endLine = startLine;
		//		while ((endLine < TextView.NumLines) && (!Data.GetLineDiffMatches(endLine)))
		//			++endLine;

		//		return Tuple.Create(startLine, endLine);
		//	}
		//}

		//void Command_Diff_Selections() => DoRangesDiff(Selections);

		//void Command_Diff_SelectedFiles()
		//{
		//	if (!Selections.Any())
		//		return;

		//	if (Selections.Count % 2 != 0)
		//		throw new Exception("Must have even number of selections.");

		//	var files = GetSelectionStrings();
		//	if (files.Any(file => !File.Exists(file)))
		//		throw new Exception("Selections must be files.");

		//	var tabsWindow = new TabsWindow();
		//	var batches = files.Batch(2).ToList();
		//	foreach (var batch in batches)
		//		tabsWindow.AddDiff(fileName1: batch[0], fileName2: batch[1]);
		//}

		//void Command_Diff_Diff_VCSNormalFiles()
		//{
		//	var files = GetSelectionStrings();
		//	if (files.Any(file => !File.Exists(file)))
		//		throw new Exception("Selections must be files.");

		//	var original = files.Select(file => Versioner.GetUnmodifiedFile(file)).ToList();
		//	var invalidIndexes = original.Indexes(file => file == null);
		//	if (invalidIndexes.Any())
		//		throw new Exception($"Unable to get unmodified files:\n{string.Join("\n", invalidIndexes.Select(index => files[index]))}");

		//	var tabsWindow = new TabsWindow();
		//	for (var ctr = 0; ctr < files.Count; ctr++)
		//		tabsWindow.AddDiff(displayName1: Path.GetFileName(files[ctr]), modified1: false, bytes1: original[ctr], fileName2: files[ctr]);
		//}

		//void Command_Diff_Regions_Region(int useRegion) => DoRangesDiff(Regions[useRegion]);

		//void Command_Diff_Break() => DiffTarget = null;

		//void Command_Diff_IgnoreWhitespace(bool? multiStatus)
		//{
		//	DiffIgnoreWhitespace = multiStatus != true;
		//	CalculateDiff();
		//}

		//void Command_Diff_IgnoreCase(bool? multiStatus)
		//{
		//	DiffIgnoreCase = multiStatus != true;
		//	CalculateDiff();
		//}

		//void Command_Diff_IgnoreNumbers(bool? multiStatus)
		//{
		//	DiffIgnoreNumbers = multiStatus != true;
		//	CalculateDiff();
		//}

		//void Command_Diff_IgnoreLineEndings(bool? multiStatus)
		//{
		//	DiffIgnoreLineEndings = multiStatus != true;
		//	CalculateDiff();
		//}

		//DiffIgnoreCharactersDialogResult Command_Diff_IgnoreCharacters_Dialog() => state.ParentWindow.RunDiffIgnoreCharactersDialog(TabsParent, DiffIgnoreCharacters);

		//void Command_Diff_IgnoreCharacters(DiffIgnoreCharactersDialogResult result)
		//{
		//	DiffIgnoreCharacters = result.IgnoreCharacters;
		//	CalculateDiff();
		//}

		//void Command_Diff_Reset()
		//{
		//	DiffIgnoreWhitespace = DiffIgnoreCase = DiffIgnoreNumbers = DiffIgnoreLineEndings = false;
		//	DiffIgnoreCharacters = null;
		//	CalculateDiff();
		//}

		//void Command_Diff_NextPrevious(bool next, bool shiftDown)
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	if ((TabsParent.GetTabIndex(this) < DiffTarget.TabsParent.GetTabIndex(DiffTarget)) && (DiffTarget.IsActive))
		//		return;

		//	var lines = Selections.AsParallel().AsOrdered().Select(range => GetDiffNextPrevious(range, next)).ToList();
		//	for (var pass = 0; pass < 2; ++pass)
		//	{
		//		var target = pass == 0 ? this : DiffTarget;
		//		var sels = lines.Select(tuple => new Range(target.TextView.GetPosition(tuple.Item2, 0, true), target.TextView.GetPosition(tuple.Item1, 0, true))).ToList();
		//		if (shiftDown)
		//			sels.AddRange(target.Selections);
		//		target.Selections = sels;
		//	}
		//}

		//void Command_Diff_CopyLeftRight(bool moveLeft)
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	var target = TabsParent.GetTabIndex(this) < DiffTarget.TabsParent.GetTabIndex(DiffTarget) ? this : DiffTarget;
		//	var source = target == this ? DiffTarget : this;
		//	if (!moveLeft)
		//		Helpers.Swap(ref target, ref source);

		//	// If both tabs are active only do this from the target tab
		//	var bothActive = TabsParent.TabIsActive(DiffTarget);
		//	if ((bothActive) && (target != this))
		//		return;

		//	var strs = source.GetSelectionStrings();
		//	target.ReplaceSelections(strs);
		//	// If both tabs are active queue an empty edit so undo will take both back to the same place
		//	if (bothActive)
		//		source.ReplaceSelections(strs);
		//}

		//DiffFixWhitespaceDialogResult Command_Diff_Fix_Whitespace_Dialog() => state.ParentWindow.RunDiffFixWhitespaceDialog(TabsParent);

		//void Command_Diff_Fix_Whitespace(DiffFixWhitespaceDialogResult result)
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	var fixes = TextData.GetDiffFixes(DiffTarget.Data, Data, result.LineStartTabStop, null, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
		//	Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
		//	ReplaceSelections(fixes.Item2);
		//}

		//void Command_Diff_Fix_Case()
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	var fixes = TextData.GetDiffFixes(DiffTarget.Data, Data, 0, DiffIgnoreWhitespace, null, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
		//	Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
		//	ReplaceSelections(fixes.Item2);
		//}

		//void Command_Diff_Fix_Numbers()
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	var fixes = TextData.GetDiffFixes(DiffTarget.Data, Data, 0, DiffIgnoreWhitespace, DiffIgnoreCase, null, DiffIgnoreLineEndings, DiffIgnoreCharacters);
		//	Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
		//	ReplaceSelections(fixes.Item2);
		//}

		//void Command_Diff_Fix_LineEndings()
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	var fixes = TextData.GetDiffFixes(DiffTarget.Data, Data, 0, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, null, DiffIgnoreCharacters);
		//	Selections = fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList();
		//	ReplaceSelections(fixes.Item2);
		//}

		//void Command_Diff_Fix_Encoding()
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	CodePage = DiffTarget.CodePage;
		//}

		//void Command_Diff_Select_MatchDiff(bool matching)
		//{
		//	if (DiffTarget == null)
		//		throw new Exception("Diff not in progress");

		//	Selections = Data.GetDiffMatches(matching).Select(tuple => new Range(tuple.Item2, tuple.Item1)).ToList();
		//}
	}
}
