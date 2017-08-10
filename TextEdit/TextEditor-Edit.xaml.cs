﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		public enum SortScope { Selections, Lines, Regions }
		public enum SortType { Smart, String, Length, Integer, Float, Hex, DateTime, Keys, Clipboard, Reverse, Randomize, Frequency }

		void FindNext(bool forward, bool selecting)
		{
			if (Searches.Count == 0)
			{
				Selections.Clear();
				return;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(range => range.Start >= Selections[ctr].End);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(range => range.Start < Selections[ctr].Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				if (!selecting)
					Selections[ctr] = new Range(Searches[index].End, Searches[index].Start);
				else if (forward)
					Selections[ctr] = new Range(Searches[index].End, Selections[ctr].Start);
				else
					Selections[ctr] = new Range(Searches[index].Start, Selections[ctr].End);
			}
		}

		List<Range> GetEnclosingRegions(bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var regions = new List<Range>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in Selections)
			{
				while ((currentRegion < Regions.Count) && (Regions[currentRegion].End <= selection.Start))
				{
					if ((useAllRegions) && (!used))
						throw new Exception("Extra regions found.");
					used = false;
					++currentRegion;
				}
				if ((currentRegion < Regions.Count) && (selection.Start >= Regions[currentRegion].Start) && (selection.End <= Regions[currentRegion].End))
				{
					regions.Add(Regions[currentRegion]);
					used = true;
				}
				else if (mustBeInRegion)
					throw new Exception("No region found.  All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((Selections.Any()) && (useAllRegions) && (currentRegion != Regions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		Range GetNextPrevBookmark(Range range, bool next, bool selecting)
		{
			int index;
			if (next)
			{
				index = Bookmarks.BinaryFindFirst(r => r.Start > range.Cursor);
				if (index == -1)
					index = 0;
			}
			else
			{
				index = Bookmarks.BinaryFindLast(r => r.Start < range.Cursor);
				if (index == -1)
					index = Bookmarks.Count - 1;
			}
			return MoveCursor(range, Bookmarks[index].Start, selecting);
		}

		List<int> GetOrdering(bool withinRegions, SortType type, bool caseSensitive, bool ascending)
		{
			var entries = Selections.Select((range, index) => new { value = GetString(range), index = index }).ToList();

			var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

			switch (type)
			{
				case SortType.Smart: entries = OrderByAscDesc(entries, entry => entry.value, ascending, Helpers.SmartComparer(caseSensitive)).ToList(); break;
				case SortType.String: entries = OrderByAscDesc(entries, entry => entry.value, ascending, stringComparer).ToList(); break;
				case SortType.Length: entries = OrderByAscDesc(entries, entry => entry.value.Length, ascending).ToList(); break;
				case SortType.Integer: entries = OrderByAscDesc(entries, entry => BigInteger.Parse(entry.value), ascending).ToList(); break;
				case SortType.Float: entries = OrderByAscDesc(entries, entry => double.Parse(entry.value), ascending).ToList(); break;
				case SortType.Hex: entries = OrderByAscDesc(entries, entry => BigInteger.Parse("0" + entry.value, NumberStyles.HexNumber), ascending).ToList(); break;
				case SortType.DateTime: entries = OrderByAscDesc(entries, entry => DateTime.Parse(entry.value), ascending).ToList(); break;
				case SortType.Keys:
					{
						var sort = KeysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = OrderByAscDesc(entries, entry => entry.value, ascending, Comparer<string>.Create((value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue))).ToList();
					}
					break;
				case SortType.Clipboard:
					{
						var sort = clipboard.Strings.Distinct().Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
						entries = OrderByAscDesc(entries, entry => entry.value, ascending, Comparer<string>.Create((value1, value2) => (sort.ContainsKey(value1) ? sort[value1] : int.MaxValue).CompareTo(sort.ContainsKey(value2) ? sort[value2] : int.MaxValue))).ToList();
					}
					break;
				case SortType.Reverse: entries.Reverse(); break;
				case SortType.Randomize: entries = entries.OrderBy(entry => random.Next()).ToList(); break;
				case SortType.Frequency:
					{
						var frequency = entries.GroupBy(a => a.value, stringComparer).ToDictionary(a => a.Key, a => a.Count(), stringComparer);
						entries = OrderByAscDesc(entries, entry => frequency[entry.value], ascending).ToList();
					}
					break;
			}

			if (withinRegions)
			{
				var regions = GetEnclosingRegions();
				var regionIndexes = Regions.Select((region, index) => new { region = region, index = index }).ToDictionary(obj => obj.region, obj => obj.index);
				entries = entries.OrderBy(entry => regionIndexes[regions[entry.index]]).ToList();
			}

			return entries.Select(entry => entry.index).ToList();
		}

		List<Range> GetSortLines() => Selections.Select(range => Data.GetOffsetLine(range.Start)).Select(line => Range.FromIndex(Data.GetOffset(line, 0), Data.GetLineLength(line))).ToList();

		List<Range> GetSortSource(SortScope scope)
		{
			List<Range> sortSource = null;
			switch (scope)
			{
				case SortScope.Selections: sortSource = Selections.ToList(); break;
				case SortScope.Lines: sortSource = GetSortLines(); break;
				case SortScope.Regions: sortSource = GetEnclosingRegions(true); break;
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

		public string RepeatString(string input, int count)
		{
			var builder = new StringBuilder(input.Length * count);
			for (int ctr = 0; ctr < count; ++ctr)
				builder.Append(input);
			return builder.ToString();
		}

		void Command_Edit_Undo()
		{
			var undo = undoRedo.GetUndo();
			if (undo == null)
				return;
			Selections.Replace(undo.ranges);
			ReplaceSelections(undo.text, replaceType: ReplaceType.Undo);
		}

		void Command_Edit_Redo()
		{
			var redo = undoRedo.GetRedo();
			if (redo == null)
				return;
			Selections.Replace(redo.ranges);
			ReplaceSelections(redo.text, replaceType: ReplaceType.Redo);
		}

		void Command_Edit_Copy_CutCopy(bool isCut)
		{
			var strs = GetSelectionStrings();

			if (!StringsAreFiles(strs))
				SetClipboardStrings(strs);
			else
				SetClipboardFiles(strs, isCut);
			if (isCut)
				ReplaceSelections("");
		}

		public void Command_Edit_Paste_AllFiles(string str, bool highlight) => ReplaceSelections(Selections.Select(value => str).ToList(), highlight);

		void Command_Edit_Paste_Paste(bool highlight, bool rotate)
		{
			var clipboardStrings = clipboard.Strings;
			if ((clipboardStrings.Count == 0) && (Selections.Count == 0))
				return;

			if (clipboardStrings.Count == 0)
				throw new Exception("Nothing on clipboard!");

			if ((Selections.Count == 1) && (clipboardStrings.Count > 1))
			{
				ReplaceOneWithMany(clipboardStrings, null);
				return;
			}

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

		EditFindFindDialog.Result Command_Edit_Find_Find_Dialog()
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return EditFindFindDialog.Run(WindowParent, text, selectionOnly);
		}

		void Command_Edit_Find_Find(bool selecting, EditFindFindDialog.Result result)
		{
			var text = result.Text;
			if (!result.IsRegex)
				text = Regex.Escape(text);
			text = $"(?:{text})";
			if (result.WholeWords)
				text = $"\\b{text}\\b";
			if (result.EntireSelection)
				text = $"\\A{text}\\Z";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!result.MatchCase)
				options |= RegexOptions.IgnoreCase;
			var regex = new Regex(text, options);

			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => regex.IsMatch(GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var resultsByRegion = regions.AsParallel().AsOrdered().Select(region => Data.RegexMatches(regex, region.Start, region.Length, result.MultiLine, result.RegexGroups, false)).ToList();

			if (result.Type == EditFindFindDialog.ResultType.CopyCount)
			{
				SetClipboardStrings(resultsByRegion.Select(list => list.Count.ToString()));
				return;
			}

			var results = resultsByRegion.SelectMany().Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			if (result.AddMatches)
				results.AddRange(Selections);

			switch (result.Type)
			{
				case EditFindFindDialog.ResultType.FindFirst:
					Searches.Replace(results);
					FindNext(true, selecting);
					break;
				case EditFindFindDialog.ResultType.FindAll:
					Selections.Replace(results);
					break;
			}
		}

		void Command_Edit_Find_NextPrevious(bool next, bool selecting) => FindNext(next, selecting);

		void Command_Edit_Find_Selected(bool selecting)
		{
			if ((Selections.Count != 1) || (!Selections[0].HasSelection))
				throw new Exception("Must have one selection with selected text.");

			var text = Regex.Escape(GetString(Selections[0]));
			var regex = new Regex(text, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

			Searches.Replace(Data.RegexMatches(regex, BeginOffset, EndOffset, false, false, false).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));
			FindNext(true, selecting);
		}

		EditFindMassFindDialog.Result Command_Edit_Find_MassFind_Dialog()
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return EditFindMassFindDialog.Run(WindowParent, text, selectionOnly, GetVariables());
		}

		void Command_Edit_Find_MassFind(EditFindMassFindDialog.Result result)
		{
			var texts = GetVariableExpressionResults<string>(result.Text);

			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var set = new HashSet<string>(texts, result.MatchCase ? (IEqualityComparer<string>)EqualityComparer<string>.Default : StringComparer.OrdinalIgnoreCase);
				Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => set.Contains(GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var searcher = new Searcher(texts, result.MatchCase);
			var selections = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var ranges = selections.AsParallel().AsOrdered().SelectMany(selection => Data.StringMatches(searcher, selection.Start, selection.Length)).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			Selections.Replace(ranges);
		}

		EditFindReplaceDialog.Result Command_Edit_Find_Replace_Dialog()
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return EditFindReplaceDialog.Run(WindowParent, text, selectionOnly);
		}

		void Command_Edit_Find_Replace(EditFindReplaceDialog.Result result)
		{
			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var sels = regions.AsParallel().AsOrdered().SelectMany(region => Data.RegexMatches(result.Regex, region.Start, region.Length, result.MultiLine, false, false)).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			Selections.Replace(sels);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => result.Regex.Replace(GetString(range), result.Replace)).ToList());
		}

		void Command_Edit_Find_ClearSearchResults() => Searches.Clear();

		void Command_Edit_CopyDown()
		{
			var strs = GetSelectionStrings();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (string.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			ReplaceSelections(strs);
		}

		EditRotateDialog.Result Command_Edit_Rotate_Dialog() => EditRotateDialog.Run(WindowParent, GetVariables());

		void Command_Edit_Rotate(EditRotateDialog.Result result)
		{
			var count = new NEExpression(result.Count).EvaluateRow<int>(GetVariables());
			if (count == 0)
				return;

			var strs = GetSelectionStrings();
			if (count < 0)
				count = -count;
			else
				count = strs.Count - count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			ReplaceSelections(strs);
		}

		EditRepeatDialog.Result Command_Edit_Repeat_Dialog() => EditRepeatDialog.Run(WindowParent, Selections.Count == 1, GetVariables());

		void Command_Edit_Repeat(EditRepeatDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(GetString(range), results[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
				{
					var selection = Selections[ctr];
					var repeatCount = results[ctr];
					var len = selection.Length / repeatCount;
					for (var index = selection.Start; index < selection.End; index += len)
						sels.Add(new Range(index + len, index));
				}
				Selections.Replace(sels);
			}
		}

		void Command_Edit_Markup_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlEncode(GetString(range))).ToList());

		void Command_Edit_Markup_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlDecode(GetString(range))).ToList());

		void Command_Edit_RegEx_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(GetString(range))).ToList());

		void Command_Edit_RegEx_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(GetString(range))).ToList());

		void Command_Edit_URL_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(GetString(range))).ToList());

		void Command_Edit_URL_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(GetString(range))).ToList());

		FilesNamesMakeAbsoluteDialog.Result Command_Edit_URL_Absolute_Dialog() => FilesNamesMakeAbsoluteDialog.Run(WindowParent, GetVariables(), false);

		void Command_Edit_URL_Absolute(FilesNamesMakeAbsoluteDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) =>
			{
				var uri = new Uri(new Uri(results[index]), str);
				return uri.AbsoluteUri;
			}).ToList());
		}

		EditColorDialog.Result Command_Edit_Color_Dialog() => EditColorDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault());

		void Command_Edit_Color(EditColorDialog.Result result) => ReplaceSelections(result.Color);

		EditDataHashDialog.Result Command_Edit_Data_Hash_Dialog() => EditDataHashDialog.Run(WindowParent, CodePage);

		void Command_Edit_Data_Hash(EditDataHashDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Compress_Dialog() => EditDataCompressDialog.Run(WindowParent, CodePage, true);

		void Command_Edit_Data_Compress(EditDataCompressDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanFullyEncode(compressed, result.OutputCodePage))
				return;
			ReplaceSelections(compressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Decompress_Dialog() => EditDataCompressDialog.Run(WindowParent, CodePage, false);

		void Command_Edit_Data_Decompress(EditDataCompressDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanFullyEncode(decompressed, result.OutputCodePage))
				return;
			ReplaceSelections(decompressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Encrypt_Dialog() => EditDataEncryptDialog.Run(WindowParent, CodePage, true);

		void Command_Edit_Data_Encrypt(EditDataEncryptDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanFullyEncode(encrypted, result.OutputCodePage))
				return;
			ReplaceSelections(encrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Decrypt_Dialog() => EditDataEncryptDialog.Run(WindowParent, CodePage, false);

		void Command_Edit_Data_Decrypt(EditDataEncryptDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanFullyEncode(decrypted, result.OutputCodePage))
				return;
			ReplaceSelections(decrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataSignDialog.Result Command_Edit_Data_Sign_Dialog() => EditDataSignDialog.Run(WindowParent, CodePage);

		void Command_Edit_Data_Sign(EditDataSignDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Cryptor.Sign(Coder.StringToBytes(str, result.CodePage), result.CryptorType, result.Key, result.Hash)).ToList());
		}

		EditSortDialog.Result Command_Edit_Sort_Dialog() => EditSortDialog.Run(WindowParent);

		void Command_Edit_Sort(EditSortDialog.Result result)
		{
			var regions = GetSortSource(result.SortScope);
			var ordering = GetOrdering(result.WithinRegions, result.SortType, result.CaseSensitive, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

			var newSelections = Selections.ToList();
			var orderedRegions = ordering.Select(index => regions[index]).ToList();
			var orderedRegionText = orderedRegions.Select(range => GetString(range)).ToList();

			Replace(regions, orderedRegionText);

			var newRegions = regions.ToList();
			var add = 0;
			for (var ctr = 0; ctr < newSelections.Count; ++ctr)
			{
				var orderCtr = ordering[ctr];
				newSelections[orderCtr] = new Range(newSelections[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add, newSelections[orderCtr].Anchor - regions[orderCtr].Start + regions[ctr].Start + add);
				newRegions[orderCtr] = new Range(newRegions[orderCtr].Cursor - regions[orderCtr].Start + regions[ctr].Start + add, newRegions[orderCtr].Anchor - regions[orderCtr].Start + regions[ctr].Start + add);
				add += orderedRegionText[ctr].Length - regions[ctr].Length;
			}
			newSelections = ordering.Select(num => newSelections[num]).ToList();

			Selections.Replace(newSelections);
			if (result.SortScope == SortScope.Regions)
				Regions.Replace(newRegions);
		}

		EditConvertDialog.Result Command_Edit_Convert_Dialog() => EditConvertDialog.Run(WindowParent);

		void Command_Edit_Convert(EditConvertDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanFullyEncode(strs, result.InputType))
				return;
			var bytes = strs.AsParallel().AsOrdered().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanFullyEncode(bytes, result.OutputType))
				return;
			ReplaceSelections(bytes.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		void Command_Edit_Bookmarks_Toggle()
		{
			var linePairs = Selections.AsParallel().AsOrdered().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End) }).ToList();
			if (linePairs.Any(pair => pair.start != pair.end))
				throw new Exception("Selections must be on a single line.");

			var lineRanges = linePairs.AsParallel().AsOrdered().Select(pair => new Range(Data.GetOffset(pair.start, 0))).ToList();
			var comparer = Comparer<Range>.Create((r1, r2) => r1.Start.CompareTo(r2.Start));
			var indexes = lineRanges.AsParallel().Select(range => new { range = range, index = Bookmarks.BinarySearch(range, comparer) }).Reverse().ToList();

			if (indexes.Any(index => index.index < 0))
			{
				foreach (var pair in indexes)
					if (pair.index < 0)
						Bookmarks.Insert(~pair.index, pair.range);
			}
			else
			{
				foreach (var pair in indexes)
					Bookmarks.RemoveAt(pair.index);
			}
		}

		void Command_Edit_Bookmarks_NextPreviousBookmark(bool next, bool selecting)
		{
			if (!Bookmarks.Any())
				return;
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetNextPrevBookmark(range, next, selecting)).ToList());
		}

		void Command_Edit_Bookmarks_Clear() => Bookmarks.Clear();

		void Command_Edit_Navigate_WordLeft(bool selecting) => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, GetPrevWord(range.Cursor), selecting)).ToList());

		void Command_Edit_Navigate_WordRight(bool selecting) => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, GetNextWord(range.Cursor), selecting)).ToList());

		IOrderedEnumerable<TSource> OrderByAscDesc<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending, IComparer<TKey> comparer = null)
		{
			var func = ascending ? (Func<IEnumerable<TSource>, Func<TSource, TKey>, IComparer<TKey>, IOrderedEnumerable<TSource>>)Enumerable.OrderBy : Enumerable.OrderByDescending;
			return func(source, keySelector, comparer ?? Comparer<TKey>.Default);
		}
	}
}
