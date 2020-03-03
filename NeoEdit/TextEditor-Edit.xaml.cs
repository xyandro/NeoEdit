﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void FindNext(bool forward, bool selecting)
		{
			if (Searches.Count == 0)
			{
				SetSelections(new List<Range>());
				return;
			}

			var sels = new List<Range>();
			foreach (var selection in Selections)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(range => range.Start >= selection.End);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(range => range.Start < selection.Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				if (!selecting)
					sels.Add(new Range(Searches[index].End, Searches[index].Start));
				else if (forward)
					sels.Add(new Range(Searches[index].End, selection.Start));
				else
					sels.Add(new Range(Searches[index].Start, selection.End));
			}
			SetSelections(sels);
		}

		List<int> GetOrdering(SortType type, bool caseSensitive, bool ascending)
		{
			var entries = Selections.AsParallel().Select((range, index) => Tuple.Create(GetString(range), index));

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
						var keysHash = TabsParent.GetKeysHash(this);
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
						entries = entries.ToList().AsParallel();
						var frequency = entries.GroupBy(a => a.Item1, stringComparer).ToDictionary(a => a.Key, a => a.Count(), stringComparer);
						entries = OrderByAscDesc(entries, entry => frequency[entry.Item1], ascending);
					}
					break;
			}

			return entries.Select(entry => entry.Item2).ToList();
		}

		List<Range> GetSortLines() => Selections.Select(range => Data.GetPositionLine(range.Start)).Select(line => Range.FromIndex(Data.GetPosition(line, 0), Data.GetLineLength(line))).ToList();

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

		static ParallelQuery<Tuple<string, int>> OrderByAscDesc<TKey>(ParallelQuery<Tuple<string, int>> source, Func<Tuple<string, int>, TKey> keySelector, bool ascending, IComparer<TKey> comparer = null)
		{
			var func = ascending ? (Func<ParallelQuery<Tuple<string, int>>, Func<Tuple<string, int>, TKey>, IComparer<TKey>, OrderedParallelQuery<Tuple<string, int>>>)ParallelEnumerable.OrderBy : ParallelEnumerable.OrderByDescending;
			return func(source, keySelector, comparer ?? Comparer<TKey>.Default).ThenBy(x => x.Item2);
		}

		static public string RepeatString(string input, int count)
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
			SetSelections(undo.ranges, false);
			ReplaceSelections(undo.text, replaceType: ReplaceType.Undo);
		}

		void Command_Edit_Redo()
		{
			var redo = undoRedo.GetRedo();
			if (redo == null)
				return;
			SetSelections(redo.ranges);
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

		void Command_Pre_Edit_Paste_Paste(ref object preResult)
		{
			// We want to paste OneWithMany if all Selections.Count == 1 (Item1) and any Clipboard.Count != 1 (Item2)

			if (preResult == null)
				preResult = Tuple.Create(true, false);
			var doOneWithMany = (Tuple<bool, bool>)preResult;
			if (doOneWithMany.Item1)
			{
				if (Selections.Count != 1)
					doOneWithMany = Tuple.Create(false, false);
				else if ((!doOneWithMany.Item2) && (Clipboard.Count != 1))
					doOneWithMany = Tuple.Create(true, true);
			}
			preResult = doOneWithMany;
		}

		void Command_Edit_Paste_Paste(bool highlight, bool rotate, object preResult)
		{
			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 0) && (Selections.Count == 0))
				return;

			var doOneWithMany = (Tuple<bool, bool>)preResult;
			if ((doOneWithMany.Item1) && (doOneWithMany.Item2))
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

		EditFindFindDialog.Result Command_Edit_Find_Find_Dialog()
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetPositionLine(sel.Cursor) == Data.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return EditFindFindDialog.Run(TabsParent, text, selectionOnly);
		}

		void Command_Edit_Find_Find(EditFindFindDialog.Result result)
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
				SetSelections(Selections.AsParallel().AsOrdered().Where(range => regex.IsMatch(GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var resultsByRegion = regions.AsParallel().AsOrdered().Select(region => RegexMatches(regex, region.Start, region.Length, result.RegexGroups, false)).ToList();

			if (result.Type == EditFindFindDialog.ResultType.CopyCount)
			{
				SetClipboardStrings(resultsByRegion.Select(list => list.Count.ToString()));
				return;
			}

			var results = resultsByRegion.SelectMany().Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			switch (result.Type)
			{
				case EditFindFindDialog.ResultType.FindNext:
					SetSearches(results);
					FindNext(true, true);
					break;
				case EditFindFindDialog.ResultType.FindAll:
					SetSelections(results);
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

			SetSearches(RegexMatches(regex, 0, Data.MaxPosition, false, false).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList());
			FindNext(true, selecting);
		}

		EditFindMassFindDialog.Result Command_Edit_Find_MassFind_Dialog() => EditFindMassFindDialog.Run(TabsParent, Selections.Any(range => range.HasSelection), GetVariables());

		void Command_Edit_Find_MassFind(EditFindMassFindDialog.Result result)
		{
			var texts = GetExpressionResults<string>(result.Text);

			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var set = new HashSet<string>(texts, result.MatchCase ? (IEqualityComparer<string>)EqualityComparer<string>.Default : StringComparer.OrdinalIgnoreCase);
				SetSelections(Selections.AsParallel().AsOrdered().Where(range => set.Contains(GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var searcher = new StringsSearcher(texts, result.MatchCase);
			var selections = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var ranges = selections.AsParallel().AsOrdered().SelectMany(selection => searcher.Find(Data.GetString(selection.Start, selection.Length)).Select(range => Range.FromIndex(range.Start + selection.Start, range.Length))).ToList();
			SetSelections(ranges);
		}

		EditFindBinaryDialog.Result Command_Edit_Find_Binary_Dialog()
		{
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);
			return EditFindBinaryDialog.Run(TabsParent, selectionOnly);
		}

		void Command_Edit_Find_Binary(EditFindBinaryDialog.Result result)
		{
			var findStrs = result.CodePages
				.Select(codePage => (Coder.TryStringToBytes(result.Text, codePage), (!Coder.IsStr(codePage)) || (result.MatchCase) || (Coder.AlwaysCaseSensitive(codePage))))
				.NonNull(tuple => tuple.Item1)
				.Select(tuple => (Coder.TryBytesToString(tuple.Item1, CodePage), tuple.Item2))
				.NonNullOrEmpty(tuple => tuple.Item1)
				.GroupBy(tuple => $"{tuple.Item1}-{tuple.Item2}")
				.Select(group => group.First())
				.ToList();
			var searcher = new StringsSearcher(findStrs);
			var selections = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var ranges = selections.AsParallel().AsOrdered().SelectMany(selection => searcher.Find(Data.GetString(selection.Start, selection.Length)).Select(range => Range.FromIndex(range.Start + selection.Start, range.Length))).ToList();
			ViewValuesFindValue = result.Text;
			SetSelections(ranges);
		}

		EditFindReplaceDialog.Result Command_Edit_Find_Replace_Dialog()
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetPositionLine(sel.Cursor) == Data.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return EditFindReplaceDialog.Run(TabsParent, text, selectionOnly);
		}

		void Command_Edit_Find_Replace(EditFindReplaceDialog.Result result)
		{
			var text = result.Text;
			var replace = result.Replace;
			if (!result.IsRegex)
			{
				text = Regex.Escape(text);
				replace = replace.Replace("$", "$$");
			}
			if (result.WholeWords)
				text = $"\\b{text}\\b";
			if (result.EntireSelection)
				text = $"\\A{text}\\Z";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!result.MatchCase)
				options |= RegexOptions.IgnoreCase;
			var regex = new Regex(text, options);

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			var sels = regions.AsParallel().AsOrdered().SelectMany(region => RegexMatches(regex, region.Start, region.Length, false, false)).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			SetSelections(sels);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => regex.Replace(GetString(range), result.Replace)).ToList());
		}

		void Command_Edit_Find_ClearSearchResults() => SetSearches(new List<Range>());

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

		EditExpressionExpressionDialog.Result Command_Edit_Expression_Expression_Dialog() => EditExpressionExpressionDialog.Run(TabsParent, GetVariables());

		void Command_Edit_Expression_Expression(EditExpressionExpressionDialog.Result result)
		{
			switch (result.Action)
			{
				case EditExpressionExpressionDialog.Action.Evaluate: ReplaceSelections(GetExpressionResults<string>(result.Expression, Selections.Count())); break;
				case EditExpressionExpressionDialog.Action.Copy: SetClipboardStrings(GetExpressionResults<string>(result.Expression)); break;
				case EditExpressionExpressionDialog.Action.Select:
					var results = GetExpressionResults<bool>(result.Expression, Selections.Count());
					SetSelections(Selections.Where((str, num) => results[num]).ToList());
					break;
			}
		}

		void Command_Edit_Expression_EvaluateSelected() => ReplaceSelections(GetExpressionResults<string>("Eval(x)", Selections.Count()));

		EditRotateDialog.Result Command_Edit_Rotate_Dialog() => EditRotateDialog.Run(TabsParent, GetVariables());

		void Command_Edit_Rotate(EditRotateDialog.Result result)
		{
			var count = new NEExpression(result.Count).Evaluate<int>(GetVariables());

			var strs = GetSelectionStrings();
			if (count < 0)
				count = -count % strs.Count;
			else
				count = strs.Count - count % strs.Count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			ReplaceSelections(strs);
		}

		EditRepeatDialog.Result Command_Edit_Repeat_Dialog() => EditRepeatDialog.Run(TabsParent, Selections.Count == 1, GetVariables());

		void Command_Edit_Repeat(EditRepeatDialog.Result result)
		{
			var results = GetExpressionResults<int>(result.Expression, Selections.Count());
			if (results.Any(repeatCount => repeatCount < 0))
				throw new Exception("Repeat count must be >= 0");
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(GetString(range), results[index])).ToList());
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
				SetSelections(sels);
			}
		}

		void Command_Edit_Escape_Markup() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlEncode(GetString(range))).ToList());

		void Command_Edit_Escape_RegEx() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(GetString(range))).ToList());

		void Command_Edit_Escape_URL() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(GetString(range))).ToList());

		void Command_Edit_Unescape_Markup() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlDecode(GetString(range))).ToList());

		void Command_Edit_Unescape_RegEx() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(GetString(range))).ToList());

		void Command_Edit_Unescape_URL() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(GetString(range))).ToList());

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Edit_AbsoluteURL_Dialog() => FilesNamesMakeAbsoluteRelativeDialog.Run(TabsParent, GetVariables(), true, false);

		void Command_Edit_AbsoluteURL(FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			ReplaceSelections(GetSelectionStrings().Select((str, index) =>
			{
				var uri = new Uri(new Uri(results[index]), str);
				return uri.AbsoluteUri;
			}).ToList());
		}

		EditDataHashDialog.Result Command_Edit_Data_Hash_Dialog() => EditDataHashDialog.Run(TabsParent, CodePage);

		void Command_Edit_Data_Hash(EditDataHashDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Compress_Dialog() => EditDataCompressDialog.Run(TabsParent, CodePage, true);

		void Command_Edit_Data_Compress(EditDataCompressDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(compressed, result.OutputCodePage))
				return;
			ReplaceSelections(compressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Decompress_Dialog() => EditDataCompressDialog.Run(TabsParent, CodePage, false);

		void Command_Edit_Data_Decompress(EditDataCompressDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(decompressed, result.OutputCodePage))
				return;
			ReplaceSelections(decompressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Encrypt_Dialog() => EditDataEncryptDialog.Run(TabsParent, CodePage, true);

		void Command_Edit_Data_Encrypt(EditDataEncryptDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(encrypted, result.OutputCodePage))
				return;
			ReplaceSelections(encrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Decrypt_Dialog() => EditDataEncryptDialog.Run(TabsParent, CodePage, false);

		void Command_Edit_Data_Decrypt(EditDataEncryptDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(decrypted, result.OutputCodePage))
				return;
			ReplaceSelections(decrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataSignDialog.Result Command_Edit_Data_Sign_Dialog() => EditDataSignDialog.Run(TabsParent, CodePage);

		void Command_Edit_Data_Sign(EditDataSignDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Cryptor.Sign(Coder.StringToBytes(str, result.CodePage), result.CryptorType, result.Key, result.Hash)).ToList());
		}

		EditSortDialog.Result Command_Edit_Sort_Dialog() => EditSortDialog.Run(TabsParent);

		void Command_Edit_Sort(EditSortDialog.Result result)
		{
			var regions = GetSortSource(result.SortScope, result.UseRegion);
			var ordering = GetOrdering(result.SortType, result.CaseSensitive, result.Ascending);
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

			SetSelections(newSelections);
			if (result.SortScope == SortScope.Regions)
				SetRegions(result.UseRegion, newRegions);
		}

		EditConvertDialog.Result Command_Edit_Convert_Dialog() => EditConvertDialog.Run(TabsParent);

		void Command_Edit_Convert(EditConvertDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputType))
				return;
			var bytes = strs.AsParallel().AsOrdered().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanEncode(bytes, result.OutputType))
				return;
			ReplaceSelections(bytes.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		void Command_Edit_Navigate_WordLeftRight(bool next, bool selecting)
		{
			if ((!selecting) && (Selections.Any(range => range.HasSelection)))
			{
				SetSelections(Selections.AsParallel().AsOrdered().Select(range => new Range(next ? range.End : range.Start)).ToList());
				return;
			}

			var func = next ? (Func<int, int>)GetNextWord : GetPrevWord;
			SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, func(range.Cursor), selecting)).ToList());
		}

		void Command_Edit_Navigate_AllLeft(bool selecting)
		{
			if (!Selections.Any())
				return;

			var positions = Selections.Select(range => range.Cursor).ToList();
			var lines = positions.AsParallel().AsOrdered().Select(position => Data.GetPositionLine(position)).ToList();

			var indexes = positions.Zip(lines, (position, line) => Data.GetPositionIndex(position, line)).ToList();
			var index = Math.Min(indexes.Max() - 1, indexes.Min());

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index < 0)
				{
					positions = lines.Select(line => Data.GetPosition(line, 0)).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(Data.GetPosition(line, index)) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					positions = lines.Select(line => Data.GetPosition(line, index + 1)).ToList();
					break;
				}

				--index;
			}

			SetSelections(Selections.Zip(positions, (range, position) => MoveCursor(range, position, selecting)).ToList());
		}

		void Command_Edit_Navigate_AllRight(bool selecting)
		{
			if (!Selections.Any())
				return;

			var positions = Selections.Select(range => range.Cursor).ToList();
			var lines = positions.AsParallel().AsOrdered().Select(position => Data.GetPositionLine(position)).ToList();

			var index = positions.Zip(lines, (position, line) => Data.GetPositionIndex(position, line)).Min();
			var endIndex = lines.Select(line => Data.GetLineLength(line)).Min();

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index > endIndex)
				{
					positions = lines.Select(line => Data.GetPosition(line, Data.GetLineLength(line))).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(Data.GetPosition(line, index)) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					positions = lines.Select(line => Data.GetPosition(line, index)).ToList();
					break;
				}

				++index;
			}

			SetSelections(Selections.Zip(positions, (range, position) => MoveCursor(range, position, selecting)).ToList());
		}

		void Command_Edit_Navigate_JumpBy(JumpByType jumpBy) => JumpBy = jumpBy;
	}
}
