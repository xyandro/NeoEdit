using System;
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
	partial class NEFile
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
						var keysHash = state.GetKeysAndValues(0, this).Lookup;
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

		List<NERange> GetSortLines() => Selections.Select(range => Text.GetPositionLine(range.Start)).Select(line => NERange.FromIndex(Text.GetPosition(line, 0), Text.GetLineLength(line))).ToList();

		List<NERange> GetSortSource(SortScope scope, int useRegion)
		{
			List<NERange> sortSource = null;
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

		void Execute_Edit_Select_All() => Selections = new List<NERange> { NERange.FromIndex(0, Text.Length) };

		void Execute_Edit_Select_Nothing() => Selections = new List<NERange>();

		void Execute_Edit_Select_Join()
		{
			var sels = new List<NERange>();
			var start = 0;
			while (start < Selections.Count)
			{
				var end = start;
				while ((end + 1 < Selections.Count) && (Selections[end].End == Selections[end + 1].Start))
					++end;
				sels.Add(new NERange(Selections[start].Start, Selections[end].End));
				start = end + 1;
			}
			Selections = sels;
		}

		void Execute_Edit_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Text.Length });
			Selections = Enumerable.Zip(start, end, (startPos, endPos) => new NERange(startPos, endPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Text.Length))).ToList();
		}

		static void Configure_Edit_Select_Limit() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Select_Limit(state.NEWindow.Focused.GetVariables());

		void Execute_Edit_Select_Limit()
		{
			var result = state.Configuration as Configuration_Edit_Select_Limit;
			var variables = GetVariables();
			var firstSelection = state.GetExpression(result.FirstSelection).Evaluate<int>(variables);
			var everyNth = state.GetExpression(result.EveryNth).Evaluate<int>(variables);
			var takeCount = state.GetExpression(result.TakeCount).Evaluate<int>(variables);
			var numSels = state.GetExpression(result.NumSelections).Evaluate<int>(variables);

			var sels = Selections.Skip(firstSelection - 1);
			if (result.JoinSelections)
				sels = sels.Batch(everyNth).Select(batch => batch.Take(takeCount)).Select(batch => new NERange(batch.First().Start, batch.Last().End));
			else
				sels = sels.EveryNth(everyNth, takeCount);
			sels = sels.Take(numSels);

			Selections = sels.ToList();
		}

		static void Configure_Edit_Select_Lines() => state.Configuration = new Configuration_Edit_Select_Lines { HasSelections = state.NEWindow.ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.HasSelection)) };

		void Execute_Edit_Select_Lines()
		{
			IReadOnlyList<NERange> lineSets;
			if ((state.Configuration as Configuration_Edit_Select_Lines).HasSelections)
				lineSets = Selections.AsTaskRunner().Where(range => range.HasSelection).Select(range => new NERange(Text.GetPositionLine(range.Start), Text.GetPositionLine(range.End - 1))).ToList();
			else
				lineSets = Selections.AsTaskRunner().Select(range => new NERange(Text.GetPositionLine(range.Start))).ToList();

			var hasLine = new bool[Text.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.Start; ctr <= set.End; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if ((hasLine[line]) && (!Text.IsDiffGapLine(line)))
					lines.Add(line);

			Selections = lines.AsTaskRunner().Select(line => NERange.FromIndex(Text.GetPosition(line, 0), Text.GetLineLength(line))).ToList();
		}

		void Execute_Edit_Select_WholeLines()
		{
			var sels = Selections.AsTaskRunner().Select(range =>
			{
				var startLine = Text.GetPositionLine(range.Start);
				var startPosition = Text.GetPosition(startLine, 0);
				var endLine = Text.GetPositionLine(Math.Max(range.Start, range.End - 1));
				var endPosition = Text.GetPosition(endLine, 0) + Text.GetLineLength(endLine) + Text.GetEndingLength(endLine);
				return new NERange(startPosition, endPosition);
			}).ToList();

			Selections = sels;
		}

		void Execute_Edit_Select_EmptyNonEmpty(bool include) => Selections = Selections.Where(range => range.HasSelection != include).ToList();

		void Execute_Edit_Select_AllowOverlappingSelections()
		{
			AllowOverlappingSelections = state.MultiStatus != true;
			if (!AllowOverlappingSelections)
			{
				var sels = DeOverlap(Selections);
				if (!sels.Matches(Selections))
					Selections = sels;
			}
		}

		static void Configure_Edit_Select_ToggleAnchor() => state.Configuration = new Configuration_Edit_Select_ToggleAnchor { AnchorStart = state.NEWindow.ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.Anchor > range.Cursor)) };

		void Execute_Edit_Select_ToggleAnchor()
		{
			var anchorStart = (state.Configuration as Configuration_Edit_Select_ToggleAnchor).AnchorStart;
			Selections = Selections.Select(range => new NERange(anchorStart ? range.Start : range.End, anchorStart ? range.End : range.Start)).ToList();
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
			Selections = new List<NERange> { Selections[CurrentSelection] };
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
			configuration.ReplaceOneWithMany = (state.NEWindow.ActiveFiles.All(neFile => neFile.Selections.Count == 1)) && (state.NEWindow.ActiveFiles.Any(neFile => neFile.Clipboard.Count != 1));
			state.Configuration = configuration;
		}

		void Execute_Edit_Paste_PasteRotatePaste(bool highlight, bool rotate)
		{
			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 0) && (Selections.Count == 0))
				return;

			if ((state.Configuration as Configuration_Edit_Paste_PasteRotatePaste).ReplaceOneWithMany)
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

		void Execute_Edit_Undo_Text()
		{
			var neFileData = Data;
			while ((neFileData.Undo != null) && (neFileData.NETextPoint == NETextPoint))
				neFileData = neFileData.Undo;
			SetData(neFileData);
			NEWindow.CreateResult();
		}

		void Execute_Edit_Undo_Step()
		{
			if (Data.Undo != null)
			{
				SetData(Data.Undo);
				NEWindow.CreateResult();
			}
		}

		static void PreExecute_Edit_Undo_BetweenFiles_Text()
		{
			var target = int.MinValue;
			foreach (var neFile in state.NEWindow.ActiveFiles)
			{
				var neFileData = neFile.Data;
				while ((neFileData.Undo != null) && (neFileData.NETextPoint == neFile.NETextPoint))
					neFileData = neFileData.Undo;
				target = Math.Max(target, neFileData.NESerial);
			}
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		static void PreExecute_Edit_Undo_BetweenFiles_Step()
		{
			var target = state.NEWindow.ActiveFiles.Select(x => x.Data.Undo).NonNull().Select(x => x.NESerial).DefaultIfEmpty(int.MinValue).Max();
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		static void PreExecute_Edit_Undo_BetweenFiles_Sync()
		{
			var target = state.NEWindow.ActiveFiles.Select(x => x.Data.Redo).NonNull().Select(x => x.NESerial - 1).DefaultIfEmpty(int.MinValue).Max();
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		void Execute_Edit_Redo_Text()
		{
			var neFileData = Data;
			while ((neFileData.Redo != null) && (neFileData.NETextPoint == NETextPoint))
				neFileData = neFileData.Redo;
			SetData(neFileData);
			NEWindow.CreateResult();
		}

		void Execute_Edit_Redo_Step()
		{
			if (Data.Redo != null)
			{
				SetData(Data.Redo);
				NEWindow.CreateResult();
			}
		}

		static void PreExecute_Edit_Redo_BetweenFiles_Text()
		{
			var target = int.MaxValue;
			foreach (var neFile in state.NEWindow.ActiveFiles)
			{
				var neFileData = neFile.Data;
				while ((neFileData != null) && (neFileData.NETextPoint == neFile.NETextPoint))
					neFileData = neFileData.Redo;
				target = Math.Min(target, neFileData?.NESerial ?? int.MaxValue);
			}
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		static void PreExecute_Edit_Redo_BetweenFiles_Step()
		{
			var target = state.NEWindow.ActiveFiles.Select(x => x.Data.Redo).NonNull().Select(x => x.NESerial).DefaultIfEmpty(int.MaxValue).Min();
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		static void PreExecute_Edit_Redo_BetweenFiles_Sync()
		{
			var target = state.NEWindow.ActiveFiles.Select(x => x.Data.NESerial).DefaultIfEmpty(int.MaxValue).Min();
			state.NEWindow.ActiveFiles.ForEach(neFile => neFile.SetData(target));
			state.NEWindow.CreateResult();
		}

		static void Configure_Edit_Repeat() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Repeat(state.NEWindow.Focused.GetVariables());

		void Execute_Edit_Repeat()
		{
			var result = state.Configuration as Configuration_Edit_Repeat;
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			if (results.Any(repeatCount => repeatCount < 0))
				throw new Exception("Repeat count must be >= 0");
			ReplaceSelections(Selections.AsTaskRunner().Select((range, index) => RepeatString(Text.GetString(range), results[index])).ToList());

			var sels = new List<NERange>();
			for (var ctr = 0; ctr < Selections.Count; ++ctr)
				if (results[ctr] != 0)
				{
					var selection = Selections[ctr];
					var len = selection.Length / results[ctr];
					for (var index = selection.Start; index < selection.End; index += len)
						sels.Add(NERange.FromIndex(index, len));
				}
			Selections = sels;
		}

		static void Configure_Edit_Rotate() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Rotate(state.NEWindow.Focused.GetVariables());

		void Execute_Edit_Rotate()
		{
			var result = state.Configuration as Configuration_Edit_Rotate;
			var count = state.GetExpression(result.Count).Evaluate<int>(GetVariables());

			var strs = GetSelectionStrings().ToList();
			if (count < 0)
				count = -count % strs.Count;
			else
				count = strs.Count - count % strs.Count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			ReplaceSelections(strs);
		}

		static void Configure_Edit_Expression_Expression() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Expression_Expression(state.NEWindow.Focused.GetVariables());

		void Execute_Edit_Expression_Expression()
		{
			var result = state.Configuration as Configuration_Edit_Expression_Expression;
			switch (result.Action)
			{
				case Configuration_Edit_Expression_Expression.Actions.Evaluate: ReplaceSelections(GetExpressionResults<string>(result.Expression, Selections.Count())); break;
				case Configuration_Edit_Expression_Expression.Actions.Copy: Clipboard = GetExpressionResults<string>(result.Expression); break;
			}
		}

		void Execute_Edit_Expression_EvaluateSelected() => ReplaceSelections(GetExpressionResults<string>("Eval(x)", Selections.Count()));

		void Execute_Edit_Navigate_WordLeftRight(bool next)
		{
			if ((!state.ShiftDown) && (Selections.Any(range => range.HasSelection)))
			{
				Selections = Selections.AsTaskRunner().Select(range => new NERange(next ? range.End : range.Start)).ToList();
				return;
			}

			var func = next ? (Func<int, int>)GetNextWord : GetPrevWord;
			Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, func(range.Cursor), state.ShiftDown)).ToList();
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

			Selections = Selections.Zip(positions, (range, position) => MoveCursor(range, position, state.ShiftDown)).ToList();
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

			Selections = Selections.Zip(positions, (range, position) => MoveCursor(range, position, state.ShiftDown)).ToList();
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

		static void Configure_Edit_Advanced_Convert() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_Convert();

		void Execute_Edit_Advanced_Convert()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_Convert;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputType))
				return;
			var bytes = strs.AsTaskRunner().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanEncode(bytes, result.OutputType))
				return;
			ReplaceSelections(bytes.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		static void Configure_Edit_Advanced_Hash() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_Hash(state.NEWindow.Focused.CodePage);

		void Execute_Edit_Advanced_Hash()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_Hash;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsTaskRunner().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType)).ToList());
		}

		static void Configure_Edit_Advanced_Compress() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_CompressDecompress(state.NEWindow.Focused.CodePage, true);

		void Execute_Edit_Advanced_Compress()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_CompressDecompress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsTaskRunner().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(compressed, result.OutputCodePage))
				return;
			ReplaceSelections(compressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Decompress() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_CompressDecompress(state.NEWindow.Focused.CodePage, false);

		void Execute_Edit_Advanced_Decompress()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_CompressDecompress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsTaskRunner().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(decompressed, result.OutputCodePage))
				return;
			ReplaceSelections(decompressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Encrypt() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_EncryptDecrypt(state.NEWindow.Focused.CodePage, true);

		void Execute_Edit_Advanced_Encrypt()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_EncryptDecrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsTaskRunner().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(encrypted, result.OutputCodePage))
				return;
			ReplaceSelections(encrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Decrypt() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_EncryptDecrypt(state.NEWindow.Focused.CodePage, false);

		void Execute_Edit_Advanced_Decrypt()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_EncryptDecrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsTaskRunner().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(decrypted, result.OutputCodePage))
				return;
			ReplaceSelections(decrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		static void Configure_Edit_Advanced_Sign() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Edit_Advanced_Sign(state.NEWindow.Focused.CodePage);

		void Execute_Edit_Advanced_Sign()
		{
			var result = state.Configuration as Configuration_Edit_Advanced_Sign;
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

		static void PreExecute_Edit_Advanced_EscapeClearsSelections() => Settings.EscapeClearsSelections = state.MultiStatus != true;
	}
}
