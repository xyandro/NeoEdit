using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.Editor.Transactional;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
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

		static PreExecutionStop PreExecute_Diff_Diff(EditorExecuteState state)
		{
			var diffTargets = state.NEFiles.AllFiles.Count == 2 ? state.NEFiles.AllFiles.ToList() : state.NEFiles.ActiveFiles.ToList();
			diffTargets.ForEach(diffTarget => state.NEFiles.AddToTransaction(diffTarget));

			var inDiff = false;
			for (var ctr = 0; ctr < diffTargets.Count; ++ctr)
				if (diffTargets[ctr].DiffTarget != null)
				{
					inDiff = true;
					diffTargets[ctr].DiffTarget = null;
				}
			if (inDiff)
				return PreExecutionStop.Stop;

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (state.ShiftDown)
			{
				if (!state.NEFiles.AllFiles.Except(diffTargets).Any())
					state.NEFiles.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
				else
				{
					diffTargets.ForEach(diffTarget => state.NEFiles.RemoveFile(diffTarget));

					var neFiles = new NEFiles();
					neFiles.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
					diffTargets.ForEach(diffTarget => neFiles.AddFile(diffTarget));
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);

			return PreExecutionStop.Stop;
		}

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

		static Configuration_Diff_IgnoreCharacters Configure_Diff_IgnoreCharacters(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Diff_IgnoreCharacters(state.NEFiles.Focused.DiffIgnoreCharacters);

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

			if ((NEFiles.GetFileIndex(this) < DiffTarget.NEFiles.GetFileIndex(DiffTarget)) && (NEFiles.ActiveFiles.Contains(DiffTarget)))
				return;

			NEFiles.AddToTransaction(DiffTarget);
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

			var target = NEFiles.GetFileIndex(this) < DiffTarget.NEFiles.GetFileIndex(DiffTarget) ? this : DiffTarget;
			var source = target == this ? DiffTarget : this;
			if (!moveLeft)
				Helpers.Swap(ref target, ref source);

			// If both files are active only do this from the target file
			var bothActive = NEFiles.ActiveFiles.Contains(DiffTarget);
			if ((bothActive) && (target != this))
				return;

			NEFiles.AddToTransaction(DiffTarget);
			var strs = source.GetSelectionStrings();
			target.ReplaceSelections(strs);
			// If both files are active queue an empty edit so undo will take both back to the same place
			if (bothActive)
				source.ReplaceSelections(strs);
		}

		static Configuration_Diff_Fix_Whitespace Configure_Diff_Fix_Whitespace(EditorExecuteState state) => state.NEFiles.FilesWindow.Configure_Diff_Fix_Whitespace();

		void Execute_Diff_Fix_Whitespace()
		{
			if (DiffTarget == null)
				throw new Exception("Diff not in progress");

			var result = state.Configuration as Configuration_Diff_Fix_Whitespace;
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

		static PreExecutionStop PreExecute_Diff_Select_LeftRightBothFiles(EditorExecuteState state, bool? left)
		{
			var active = new HashSet<NEFile>(state.NEFiles.ActiveFiles.NonNull(item => item.DiffTarget).SelectMany(item => new List<NEFile> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((state.NEFiles.GetFileIndex(item) < state.NEFiles.GetFileIndex(item.DiffTarget)) == left)));
			state.NEFiles.AllFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, active.Contains(neFile)));

			return PreExecutionStop.Stop;
		}
	}
}
