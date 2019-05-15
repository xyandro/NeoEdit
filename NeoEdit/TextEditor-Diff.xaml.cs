﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using NeoEdit;
using NeoEdit.Dialogs;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class TextEditor
	{
		static void DoRangesDiff(ITextEditor te, RangeList ranges)
		{
			if (!ranges.Any())
				return;

			if (ranges.Count % 2 != 0)
				throw new Exception("Must have even number of items.");

			var codePage = te.CodePage; // Must save as other threads can't access DependencyProperties
			var tabs = new Tabs();
			var batches = ranges.AsParallel().AsOrdered().Select(range => te.GetString(range)).Select(str => Coder.StringToBytes(str, codePage)).Batch(2).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(new TextEditor(bytes: batch[0], codePage: codePage, modified: false), new TextEditor(bytes: batch[1], codePage: codePage, modified: false));
		}

		static Tuple<int, int> GetDiffNextPrevious(ITextEditor te, Range range, bool next)
		{
			if (next)
			{
				var endLine = te.Data.GetOffsetLine(range.End);

				while ((endLine < te.Data.NumLines) && (te.Data.GetLineDiffMatches(endLine)))
					++endLine;
				while ((endLine < te.Data.NumLines) && (!te.Data.GetLineDiffMatches(endLine)))
					++endLine;

				var startLine = endLine;
				while ((startLine > 0) && (!te.Data.GetLineDiffMatches(startLine - 1)))
					--startLine;

				return Tuple.Create(startLine, endLine);
			}
			else
			{
				var startLine = te.Data.GetOffsetLine(Math.Max(0, range.Start - 1));

				while ((startLine > 0) && (te.Data.GetLineDiffMatches(startLine)))
					--startLine;
				while ((startLine > 0) && (!te.Data.GetLineDiffMatches(startLine - 1)))
					--startLine;

				var endLine = startLine;
				while ((endLine < te.Data.NumLines) && (!te.Data.GetLineDiffMatches(endLine)))
					++endLine;

				return Tuple.Create(startLine, endLine);
			}
		}

		static void Command_Diff_Selections(ITextEditor te) => DoRangesDiff(te, te.Selections);

		static void Command_Diff_SelectedFiles(ITextEditor te)
		{
			if (!te.Selections.Any())
				return;

			if (te.Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var files = te.GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var tabs = new Tabs();
			var batches = files.Batch(2).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(new TextEditor(batch[0]), new TextEditor(batch[1]));
		}

		static void Command_Diff_Diff_VCSNormalFiles(ITextEditor te)
		{
			var files = te.GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var original = files.Select(file => Versioner.GetUnmodifiedFile(file)).ToList();
			var invalidIndexes = original.Indexes(file => file == null);
			if (invalidIndexes.Any())
				throw new Exception($"Unable to get unmodified files:\n{string.Join("\n", invalidIndexes.Select(index => files[index]))}");

			var tabs = new Tabs();
			for (var ctr = 0; ctr < files.Count; ctr++)
				tabs.AddDiff(new TextEditor(displayName: Path.GetFileName(files[ctr]), modified: false, bytes: original[ctr]), new TextEditor(fileName: files[ctr]));
		}

		static void Command_Diff_Regions_Region(ITextEditor te, int useRegion) => DoRangesDiff(te, te.Regions[useRegion]);

		static void Command_Diff_Break(ITextEditor te) => te.DiffTarget = null;

		static void Command_Diff_IgnoreWhitespace(ITextEditor te, bool? multiStatus)
		{
			te.DiffIgnoreWhitespace = multiStatus != true;
			te.CalculateDiff();
		}

		static void Command_Diff_IgnoreCase(ITextEditor te, bool? multiStatus)
		{
			te.DiffIgnoreCase = multiStatus != true;
			te.CalculateDiff();
		}

		static void Command_Diff_IgnoreNumbers(ITextEditor te, bool? multiStatus)
		{
			te.DiffIgnoreNumbers = multiStatus != true;
			te.CalculateDiff();
		}

		static void Command_Diff_IgnoreLineEndings(ITextEditor te, bool? multiStatus)
		{
			te.DiffIgnoreLineEndings = multiStatus != true;
			te.CalculateDiff();
		}

		static DiffIgnoreCharactersDialog.Result Command_Diff_IgnoreCharacters_Dialog(ITextEditor te) => DiffIgnoreCharactersDialog.Run(te.TabsParent, te.DiffIgnoreCharacters);

		static void Command_Diff_IgnoreCharacters(ITextEditor te, DiffIgnoreCharactersDialog.Result result)
		{
			te.DiffIgnoreCharacters = result.IgnoreCharacters;
			te.CalculateDiff();
		}

		static void Command_Diff_Reset(ITextEditor te)
		{
			te.DiffIgnoreWhitespace = te.DiffIgnoreCase = te.DiffIgnoreNumbers = te.DiffIgnoreLineEndings = false;
			te.DiffIgnoreCharacters = null;
			te.CalculateDiff();
		}

		static void Command_Diff_NextPrevious(ITextEditor te, bool next, bool shiftDown)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			if ((te.TabsParent.GetIndex(te as TextEditor) < te.DiffTarget.TabsParent.GetIndex(te.DiffTarget)) && (te.DiffTarget.Active))
				return;

			var lines = te.Selections.AsParallel().AsOrdered().Select(range => GetDiffNextPrevious(te, range, next)).ToList();
			for (var pass = 0; pass < 2; ++pass)
			{
				var target = pass == 0 ? te as TextEditor : te.DiffTarget;
				var sels = lines.Select(tuple => new Range(target.Data.GetOffset(tuple.Item2, 0, true), target.Data.GetOffset(tuple.Item1, 0, true))).ToList();
				if (shiftDown)
					sels.AddRange(target.Selections);
				target.SetSelections(sels);
			}
		}

		static void Command_Diff_CopyLeftRight(ITextEditor te, bool moveLeft)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			var target = te.TabsParent.GetIndex(te as TextEditor) < te.DiffTarget.TabsParent.GetIndex(te.DiffTarget) ? te as TextEditor : te.DiffTarget;
			var source = target == te ? te.DiffTarget : te as TextEditor;
			if (!moveLeft)
				Helpers.Swap(ref target, ref source);

			// If both tabs are active only do this from the target tab
			var bothActive = te.TabsParent.TabIsActive(te.DiffTarget);
			if ((bothActive) && (target != te))
				return;

			var strs = source.GetSelectionStrings();
			target.ReplaceSelections(strs);
			// If both tabs are active queue an empty edit so undo will take both back to the same place
			if (bothActive)
				source.ReplaceSelections(strs);
		}

		static DiffFixWhitespaceDialog.Result Command_Diff_Fix_Whitespace_Dialog(ITextEditor te) => DiffFixWhitespaceDialog.Run(te.TabsParent);

		static void Command_Diff_Fix_Whitespace(ITextEditor te, DiffFixWhitespaceDialog.Result result)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = TextData.GetDiffFixes(te.DiffTarget.Data, te.Data, result.LineStartTabStop, null, te.DiffIgnoreCase, te.DiffIgnoreNumbers, te.DiffIgnoreLineEndings, te.DiffIgnoreCharacters);
			te.SetSelections(fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList());
			te.ReplaceSelections(fixes.Item2);
		}

		static void Command_Diff_Fix_Case(ITextEditor te)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = TextData.GetDiffFixes(te.DiffTarget.Data, te.Data, 0, te.DiffIgnoreWhitespace, null, te.DiffIgnoreNumbers, te.DiffIgnoreLineEndings, te.DiffIgnoreCharacters);
			te.SetSelections(fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList());
			te.ReplaceSelections(fixes.Item2);
		}

		static void Command_Diff_Fix_Numbers(ITextEditor te)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = TextData.GetDiffFixes(te.DiffTarget.Data, te.Data, 0, te.DiffIgnoreWhitespace, te.DiffIgnoreCase, null, te.DiffIgnoreLineEndings, te.DiffIgnoreCharacters);
			te.SetSelections(fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList());
			te.ReplaceSelections(fixes.Item2);
		}

		static void Command_Diff_Fix_LineEndings(ITextEditor te)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			var fixes = TextData.GetDiffFixes(te.DiffTarget.Data, te.Data, 0, te.DiffIgnoreWhitespace, te.DiffIgnoreCase, te.DiffIgnoreNumbers, null, te.DiffIgnoreCharacters);
			te.SetSelections(fixes.Item1.Select(tuple => new Range(tuple.Item1, tuple.Item2)).ToList());
			te.ReplaceSelections(fixes.Item2);
		}

		static void Command_Diff_Fix_Encoding(ITextEditor te)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			te.CodePage = te.DiffTarget.CodePage;
		}

		static void Command_Diff_Select_MatchDiff(ITextEditor te, bool matching)
		{
			if (te.DiffTarget == null)
				throw new Exception("Diff not in progress");

			te.SetSelections(te.Data.GetDiffMatches(matching).Select(tuple => new Range(tuple.Item2, tuple.Item1)).ToList());
		}
	}
}
