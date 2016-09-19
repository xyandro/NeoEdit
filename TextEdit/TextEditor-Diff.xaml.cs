﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		{
			var offset = next ? range.End : Math.Max(0, range.Start - 1);
			var line = Data.GetOffsetLine(offset);
			var delta = next ? 1 : -1;
			int? start = null;
			while (true)
			{
				line += delta;
				if ((line < 0) || (line >= Data.NumLines) || ((start.HasValue) && (Data.GetLineDiffStatus(line) == LCS.MatchType.Match)))
				{
					line = Math.Max(-1, Math.Min(line, Data.NumLines - 1));
					if (!start.HasValue)
						start = line;
					if (next)
						return Tuple.Create(start.Value, line);
					return Tuple.Create(line + 1, start.Value + 1);
				}
				if ((!start.HasValue) && (Data.GetLineDiffStatus(line) != LCS.MatchType.Match))
					start = line;
			}
		}

		void Command_Diff_Selections()
		{
			if (!Selections.Any())
				return;

			if (Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var codePage = CodePage; // Must save as other threads can't access DependencyProperties
			var tabs = TextEditTabs.CreateDiff();
			var batches = Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str => Coder.StringToBytes(str, codePage)).Batch(2).Select(batch => batch.ToList()).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(bytes1: batch[0], bytes2: batch[1], codePage1: codePage, codePage2: codePage, modified1: false, modified2: false);
		}

		void Command_Diff_SelectedFiles()
		{
			if (!Selections.Any())
				return;

			if (Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var files = GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var tabs = TextEditTabs.CreateDiff();
			var batches = files.Batch(2).Select(batch => batch.ToList()).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(fileName1: batch[0], fileName2: batch[1]);
		}

		void Command_Diff_Break() => DiffTarget = null;

		void Command_Diff_IgnoreWhitespace(bool? multiStatus)
		{
			DiffIgnoreWhitespace = multiStatus != true;
			CalculateDiff();
		}

		void Command_Diff_IgnoreCase(bool? multiStatus)
		{
			DiffIgnoreCase = multiStatus != true;
			CalculateDiff();
		}

		void Command_Diff_IgnoreNumbers(bool? multiStatus)
		{
			DiffIgnoreNumbers = multiStatus != true;
			CalculateDiff();
		}

		void Command_Diff_IgnoreLineEndings(bool? multiStatus)
		{
			DiffIgnoreLineEndings = multiStatus != true;
			CalculateDiff();
		}

		void Command_Diff_Reset()
		{
			DiffIgnoreWhitespace = DiffIgnoreCase = DiffIgnoreNumbers = DiffIgnoreLineEndings = false;
			CalculateDiff();
		}

		void Command_Diff_NextPrevious(bool next)
		{
			if (DiffTarget == null)
				return;

			if ((TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget)) && (DiffTarget.Active))
				return;

			var lines = Selections.AsParallel().AsOrdered().Select(range => GetDiffNextPrevious(range, next)).ToList();
			for (var pass = 0; pass < 2; ++pass)
			{
				var target = pass == 0 ? this : DiffTarget;
				target.Selections.Replace(lines.Select(tuple => new Range(target.Data.GetOffset(tuple.Item1, 0), target.Data.GetOffset(tuple.Item2, 0))).ToList());
			}
		}

		void Command_Diff_CopyLeftRight(bool moveLeft)
		{
			if (DiffTarget == null)
				return;

			TextEditor left, right;
			if (TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget))
			{
				left = this;
				right = DiffTarget;
			}
			else
			{
				left = DiffTarget;
				right = this;
			}

			if (moveLeft)
				left.ReplaceSelections(right.GetSelectionStrings());
			else
				right.ReplaceSelections(left.GetSelectionStrings());
		}

		void Command_Diff_Select_MatchDiff(bool matching)
		{
			if (DiffTarget == null)
				return;

			Selections.Replace(Data.GetDiffMatches(matching).Select(tuple => new Range(tuple.Item2, tuple.Item1)));
		}
	}
}