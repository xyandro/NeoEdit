﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFileHandler
	{
		IReadOnlyList<int> GetOrdering(SortType type, bool caseSensitive, bool ascending)
		{
			var entries = Selections.AsTaskRunner().Select((range, index) => Tuple.Create(Text.GetString(range), index));

			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

			switch (type)
			{
				case SortType.Smart: entries = OrderByAscDesc(entries, entry => entry.Item1, ascending, Helpers.SmartComparer(caseSensitive)); break;
				case SortType.String: entries = OrderByAscDesc(entries, entry => entry.Item1, ascending, stringComparer); break;
				case SortType.Length: entries = OrderByAscDesc(entries, entry => entry.Item1.Length, ascending); break;
				case SortType.Integer: entries = OrderByAscDesc(entries, entry => BigInteger.Parse(entry.Item1), ascending); break;
				case SortType.Float: entries = OrderByAscDesc(entries, entry => double.Parse(entry.Item1), ascending); break;
				case SortType.Hex: entries = OrderByAscDesc(entries, entry => BigInteger.Parse("0" + entry.Item1, NumberStyles.HexNumber), ascending); break;
				case SortType.DateTime: entries = OrderByAscDesc(entries, entry => DateTime.Parse(entry.Item1), ascending); break;
				case SortType.Keys:
					{
						var keysHash = EditorExecuteState.CurrentState.GetKeysAndValues(0, this).Lookup;
						entries = OrderByAscDesc(entries, entry => entry.Item1, ascending, Comparer<string>.Create((value1, value2) => (keysHash.ContainsKey(value1) ? keysHash[value1] : int.MaxValue).CompareTo(keysHash.ContainsKey(value2) ? keysHash[value2] : int.MaxValue)));
					}
					break;
				case SortType.Clipboard:
					{
						var sort = Clipboard.Distinct().Select((key, index) => new { key, index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = OrderByAscDesc(entries, entry => entry.Item1, ascending, Comparer<string>.Create((value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue)));
					}
					break;
				case SortType.Reverse: entries = entries.Reverse(); break;
				case SortType.Randomize: entries = entries.OrderBy(entry => random.Next()); break;
				case SortType.Frequency:
					{
						entries = entries.ToList().AsTaskRunner();
						var frequency = entries.GroupBy(a => a.Item1, stringComparer).ToDictionary(a => a.Key, a => a.Count(), stringComparer);
						entries = OrderByAscDesc(entries, entry => frequency[entry.Item1], ascending);
					}
					break;
			}

			return entries.Select(entry => entry.Item2).ToList();
		}

		List<Range> GetSortLines() => Selections.Select(range => Text.GetPositionLine(range.Start)).Select(line => Range.FromIndex(Text.GetPosition(line, 0), Text.GetLineLength(line))).ToList();

		List<Range> GetSortSource(SortScope scope, int useRegion)
		{
			List<Range> sortSource = null;
			switch (scope)
			{
				case SortScope.Selections: sortSource = Selections.ToList(); break;
				case SortScope.Lines: sortSource = GetSortLines(); break;
				case SortScope.Regions: sortSource = GetEnclosingRegions(useRegion, true); break;
				default: throw new Exception("Invalid sort type");
			}

			if (Selections.Count != sortSource.Count)
				throw new Exception("Selections and regions counts must match");

			var orderedRegions = sortSource.OrderBy(range => range.Start).ToList();
			var pos = 0;
			foreach (var range in orderedRegions)
			{
				if (range.Start < pos)
					throw new Exception("Regions cannot overlap");
				pos = range.End;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				if ((Selections[ctr].Start < sortSource[ctr].Start) || (Selections[ctr].End > sortSource[ctr].End))
					throw new Exception("All selections must be a region");
			}

			return sortSource;
		}

		static FluentTaskRunner<Tuple<string, int>> OrderByAscDesc<TKey>(FluentTaskRunner<Tuple<string, int>> source, Func<Tuple<string, int>, TKey> keySelector, bool ascending, IComparer<TKey> comparer = null)
		{
			source = source.OrderBy(x => x.Item2);
			var func = (Func<Func<Tuple<string, int>, TKey>, IComparer<TKey>, Func<Tuple<string, int>, long>, FluentTaskRunner<Tuple<string, int>>>)source.OrderBy;
			if (ascending)
				source = source.OrderBy(keySelector, comparer);
			else
				source = source.OrderByDescending(keySelector, comparer);
			return source;
		}

		static string RepeatString(string input, int count)
		{
			var builder = new StringBuilder(input.Length * count);
			for (int ctr = 0; ctr < count; ++ctr)
				builder.Append(input);
			return builder.ToString();
		}

		void Execute_Edit_Select_All() => Selections = new List<Range> { Range.FromIndex(0, Text.Length) };

		void Execute_Edit_Select_Nothing() => Selections = new List<Range>();

		void Execute_Edit_Select_Join()
		{
			var sels = new List<Range>();
			var start = 0;
			while (start < Selections.Count)
			{
				var end = start;
				while ((end + 1 < Selections.Count) && (Selections[end].End == Selections[end + 1].Start))
					++end;
				sels.Add(new Range(Selections[end].End, Selections[start].Start));
				start = end + 1;
			}
			Selections = sels;
		}

		void Execute_Edit_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Text.Length });
			Selections = Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Text.Length))).ToList();
		}

		static void Configure_Edit_Select_Limit() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Select_Limit(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Edit_Select_Limit()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Select_Limit;
			var variables = GetVariables();
			var firstSelection = EditorExecuteState.CurrentState.GetExpression(result.FirstSelection).Evaluate<int>(variables);
			var everyNth = EditorExecuteState.CurrentState.GetExpression(result.EveryNth).Evaluate<int>(variables);
			var takeCount = EditorExecuteState.CurrentState.GetExpression(result.TakeCount).Evaluate<int>(variables);
			var numSels = EditorExecuteState.CurrentState.GetExpression(result.NumSelections).Evaluate<int>(variables);

			var sels = Selections.Skip(firstSelection - 1);
			if (result.JoinSelections)
				sels = sels.Batch(everyNth).Select(batch => batch.Take(takeCount)).Select(batch => new Range(batch.Last().End, batch.First().Start));
			else
				sels = sels.EveryNth(everyNth, takeCount);
			sels = sels.Take(numSels);

			Selections = sels.ToList();
		}

		void Execute_Edit_Select_Lines()
		{
			var lineSets = Selections.AsTaskRunner().Select(range => new { start = Text.GetPositionLine(range.Start), end = Text.GetPositionLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[Text.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if ((hasLine[line]) && (!Text.IsDiffGapLine(line)))
					lines.Add(line);

			Selections = lines.AsTaskRunner().Select(line => Range.FromIndex(Text.GetPosition(line, 0), Text.GetLineLength(line))).ToList();
		}

		void Execute_Edit_Select_WholeLines()
		{
			var sels = Selections.AsTaskRunner().Select(range =>
			{
				var startLine = Text.GetPositionLine(range.Start);
				var startPosition = Text.GetPosition(startLine, 0);
				var endLine = Text.GetPositionLine(Math.Max(range.Start, range.End - 1));
				var endPosition = Text.GetPosition(endLine, 0) + Text.GetLineLength(endLine) + Text.GetEndingLength(endLine);
				return new Range(endPosition, startPosition);
			}).ToList();

			Selections = sels;
		}

		void Execute_Edit_Select_EmptyNonEmpty(bool include) => Selections = Selections.Where(range => range.HasSelection != include).ToList();

		static void Configure_Edit_Select_ToggleAnchor() => EditorExecuteState.CurrentState.Configuration = new Configuration_Edit_Select_ToggleAnchor { AnchorStart = EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.Anchor > range.Cursor)) };

		void Execute_Edit_Select_ToggleAnchor()
		{
			var anchorStart = (EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Select_ToggleAnchor).AnchorStart;
			Selections = Selections.Select(range => new Range(anchorStart ? range.End : range.Start, anchorStart ? range.Start : range.End)).ToList();
		}

		void Execute_Edit_Select_Focused_First()
		{
			CurrentSelection = 0;
			EnsureVisible();
		}

		void Execute_Edit_Select_Focused_NextPrevious(bool next)
		{
			var newSelection = CurrentSelection + (next ? 1 : -1);
			if (newSelection < 0)
				newSelection = Selections.Count - 1;
			if (newSelection >= Selections.Count)
				newSelection = 0;
			CurrentSelection = newSelection;
			EnsureVisible();
		}

		void Execute_Edit_Select_Focused_Single()
		{
			if (!Selections.Any())
				return;
			Selections = new List<Range> { Selections[CurrentSelection] };
			CurrentSelection = 0;
		}

		void Execute_Edit_Select_Focused_Remove()
		{
			Selections = Selections.Where((sel, index) => index != CurrentSelection).ToList();
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
		}

		void Execute_Edit_Select_Focused_RemoveBeforeCurrent()
		{
			Selections = Selections.Where((sel, index) => index >= CurrentSelection).ToList();
			CurrentSelection = 0;
		}

		void Execute_Edit_Select_Focused_RemoveAfterCurrent()
		{
			Selections = Selections.Where((sel, index) => index <= CurrentSelection).ToList();
		}

		void Execute_Edit_Select_Focused_CenterVertically() => EnsureVisible(true);

		void Execute_Edit_Select_Focused_Center() => EnsureVisible(true, true);

		void Execute_Edit_CopyCut(bool isCut)
		{
			var strs = GetSelectionStrings();

			if (!StringsAreFiles(strs))
				Clipboard = strs;
			else if (isCut)
				ClipboardCut = strs;
			else
				ClipboardCopy = strs;
			if (isCut)
				ReplaceSelections("");
		}

		static void Configure_Edit_Paste_PasteRotatePaste()
		{
			var configuration = new Configuration_Edit_Paste_PasteRotatePaste();
			configuration.ReplaceOneWithMany = (EditorExecuteState.CurrentState.NEFiles.ActiveFiles.All(neFile => neFile.Selections.Count == 1)) && (EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Any(neFile => neFile.Clipboard.Count != 1));
			EditorExecuteState.CurrentState.Configuration = configuration;
		}

		void Execute_Edit_Paste_PasteRotatePaste(bool highlight, bool rotate)
		{
			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 0) && (Selections.Count == 0))
				return;

			if ((EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Paste_PasteRotatePaste).ReplaceOneWithMany)
			{
				ReplaceOneWithMany(clipboardStrings, null);
				return;
			}

			if (clipboardStrings.Count == 0)
				throw new Exception("Nothing on clipboard!");

			var repeat = Selections.Count / clipboardStrings.Count;
			if (repeat * clipboardStrings.Count != Selections.Count)
				throw new Exception("Number of selections must be a multiple of number of clipboards.");

			if (repeat != 1)
				if (rotate)
					clipboardStrings = Enumerable.Repeat(clipboardStrings, repeat).SelectMany(x => x).ToList();
				else
					clipboardStrings = clipboardStrings.SelectMany(str => Enumerable.Repeat(str, repeat)).ToList();

			ReplaceSelections(clipboardStrings, highlight);
		}

		void Execute_Edit_Undo()
		{
			UndoRedo.UndoRedoStep step;
			(UndoRedo, step) = UndoRedo.GetUndo();
			if (step == null)
				return;

			Replace(step.ranges, step.text, ReplaceType.Undo);

			var offset = 0;
			var sels = new List<Range>();
			for (var ctr = 0; ctr < step.ranges.Count; ++ctr)
			{
				sels.Add(Range.FromIndex(step.ranges[ctr].Start + offset, step.text[ctr].Length));
				offset += step.text[ctr].Length - step.ranges[ctr].Length;
			}

			Selections = sels;
		}

		void Execute_Edit_Redo()
		{
			UndoRedo.UndoRedoStep step;
			(UndoRedo, step) = UndoRedo.GetRedo();
			if (step == null)
				return;

			Replace(step.ranges, step.text, ReplaceType.Redo);

			var offset = 0;
			var sels = new List<Range>();
			for (var ctr = 0; ctr < step.ranges.Count; ++ctr)
			{
				sels.Add(Range.FromIndex(step.ranges[ctr].Start + offset, step.text[ctr].Length));
				offset += step.text[ctr].Length - step.ranges[ctr].Length;
			}

			Selections = sels;
		}

		static void Configure_Edit_Repeat() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Repeat(EditorExecuteState.CurrentState.NEFiles.Focused.Selections.Count == 1, EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Edit_Repeat()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Repeat;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			if (results.Any(repeatCount => repeatCount < 0))
				throw new Exception("Repeat count must be >= 0");
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => RepeatString(Text.GetString(range), results[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
					if (results[ctr] != 0)
					{
						var selection = Selections[ctr];
						var len = selection.Length / results[ctr];
						for (var index = selection.Start; index < selection.End; index += len)
							sels.Add(Range.FromIndex(index, len));
					}
				Selections = sels;
			}
		}

		static void Configure_Edit_Rotate() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Rotate(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Edit_Rotate()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Rotate;
			var count = EditorExecuteState.CurrentState.GetExpression(result.Count).Evaluate<int>(GetVariables());

			var strs = GetSelectionStrings().ToList();
			if (count < 0)
				count = -count % strs.Count;
			else
				count = strs.Count - count % strs.Count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			ReplaceSelections(strs);
		}

		static void Configure_Edit_Expression_Expression() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Expression_Expression(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables());

		void Execute_Edit_Expression_Expression()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Expression_Expression;
			switch (result.Action)
			{
				case Configuration_Edit_Expression_Expression.Actions.Evaluate: ReplaceSelections(GetExpressionResults<string>(result.Expression, Selections.Count())); break;
				case Configuration_Edit_Expression_Expression.Actions.Copy: Clipboard = GetExpressionResults<string>(result.Expression); break;
			}
		}

		void Execute_Edit_Expression_EvaluateSelected() => ReplaceSelections(GetExpressionResults<string>("Eval(x)", Selections.Count()));

		void Execute_Edit_Navigate_WordLeftRight(bool next)
		{
			if ((!EditorExecuteState.CurrentState.ShiftDown) && (Selections.Any(range => range.HasSelection)))
			{
				Selections = Selections.AsTaskRunner().Select(range => new Range(next ? range.End : range.Start)).ToList();
				return;
			}

			var func = next ? (Func<int, int>)GetNextWord : GetPrevWord;
			Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, func(range.Cursor), EditorExecuteState.CurrentState.ShiftDown)).ToList();
		}

		void Execute_Edit_Navigate_AllLeft()
		{
			if (!Selections.Any())
				return;

			var positions = Selections.Select(range => range.Cursor).ToList();
			var lines = positions.AsTaskRunner().Select(position => Text.GetPositionLine(position)).ToList();

			var indexes = positions.Zip(lines, (position, line) => Text.GetPositionIndex(position, line)).ToList();
			var index = Math.Min(indexes.Max() - 1, indexes.Min());

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index < 0)
				{
					positions = lines.Select(line => Text.GetPosition(line, 0)).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(Text.GetPosition(line, index)) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					positions = lines.Select(line => Text.GetPosition(line, index + 1)).ToList();
					break;
				}

				--index;
			}

			Selections = Selections.Zip(positions, (range, position) => MoveCursor(range, position, EditorExecuteState.CurrentState.ShiftDown)).ToList();
		}

		void Execute_Edit_Navigate_AllRight()
		{
			if (!Selections.Any())
				return;

			var positions = Selections.Select(range => range.Cursor).ToList();
			var lines = positions.AsTaskRunner().Select(position => Text.GetPositionLine(position)).ToList();

			var index = positions.Zip(lines, (position, line) => Text.GetPositionIndex(position, line)).Min();
			var endIndex = lines.Select(line => Text.GetLineLength(line)).Min();

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index > endIndex)
				{
					positions = lines.Select(line => Text.GetPosition(line, Text.GetLineLength(line))).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(Text.GetPosition(line, index)) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					positions = lines.Select(line => Text.GetPosition(line, index)).ToList();
					break;
				}

				++index;
			}

			Selections = Selections.Zip(positions, (range, position) => MoveCursor(range, position, EditorExecuteState.CurrentState.ShiftDown)).ToList();
		}

		void Execute_Edit_Navigate_JumpBy_Various(JumpByType jumpBy) => JumpBy = jumpBy;

		void Execute_Edit_RepeatCount()
		{
			var strs = GetSelectionStrings();
			var counts = strs.GroupBy(str => str).ToDictionary(group => group.Key, group => group.Count());
			ReplaceSelections(strs.Select(str => counts[str].ToString()).ToList());
		}

		void Execute_Edit_RepeatIndex()
		{
			var counts = new Dictionary<string, int>();
			var strs = GetSelectionStrings();
			var newStrs = new List<string>();
			foreach (var str in strs)
			{
				if (!counts.ContainsKey(str))
					counts[str] = 0;
				++counts[str];
				newStrs.Add(counts[str].ToString());
			}
			ReplaceSelections(newStrs);
		}

		static void Configure_Edit_Advanced_Convert() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_Convert();

		void Execute_Edit_Advanced_Convert()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_Convert;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputType))
				return;
			var bytes = strs.AsTaskRunner().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanEncode(bytes, result.OutputType))
				return;
			ReplaceSelections(bytes.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		static void Configure_Edit_Advanced_Hash() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_Hash(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage);

		void Execute_Edit_Advanced_Hash()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_Hash;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsTaskRunner().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		static void Configure_Edit_Advanced_Compress() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_CompressDecompress(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage, true);

		void Execute_Edit_Advanced_Compress()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_CompressDecompress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsTaskRunner().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(compressed, result.OutputCodePage))
				return;
			ReplaceSelections(compressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Decompress() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_CompressDecompress(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage, false);

		void Execute_Edit_Advanced_Decompress()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_CompressDecompress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsTaskRunner().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(decompressed, result.OutputCodePage))
				return;
			ReplaceSelections(decompressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Encrypt() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_EncryptDecrypt(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage, true);

		void Execute_Edit_Advanced_Encrypt()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_EncryptDecrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsTaskRunner().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(encrypted, result.OutputCodePage))
				return;
			ReplaceSelections(encrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Decrypt() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_EncryptDecrypt(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage, false);

		void Execute_Edit_Advanced_Decrypt()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_EncryptDecrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsTaskRunner().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(decrypted, result.OutputCodePage))
				return;
			ReplaceSelections(decrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Sign() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_Edit_Advanced_Sign(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage);

		void Execute_Edit_Advanced_Sign()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_Edit_Advanced_Sign;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsTaskRunner().Select(str => Cryptor.Sign(Coder.StringToBytes(str, result.CodePage), result.CryptorType, result.Key, result.Hash)).ToList());
		}

		void Execute_Edit_Advanced_RunCommand_Parallel()
		{
			var workingDirectory = Path.GetDirectoryName(FileName ?? "");
			ReplaceSelections(Selections.AsTaskRunner().Select(range => RunCommand(Text.GetString(range), workingDirectory)).ToList());
		}

		void Execute_Edit_Advanced_RunCommand_Sequential() => ReplaceSelections(GetSelectionStrings().Select(str => RunCommand(str, Path.GetDirectoryName(FileName ?? ""))).ToList());

		void Execute_Edit_Advanced_RunCommand_Shell() => GetSelectionStrings().ForEach(str => Process.Start(str));

		static bool PreExecute_Edit_Advanced_EscapeClearsSelections()
		{
			Settings.EscapeClearsSelections = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}
	}
}