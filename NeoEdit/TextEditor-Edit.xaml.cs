using NeoEdit;
using NeoEdit.Expressions;
using NeoEdit.Parsing;
using NeoEdit.Transform;
using NeoEdit.Controls;
using NeoEdit.Dialogs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace NeoEdit
{
	partial class TextEditor
	{
		JumpByType jumpBy;
		[DepProp]
		public JumpByType JumpBy { get { return UIHelper<TextEditor>.GetPropValue<JumpByType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); jumpBy = JumpBy; } }

		public enum SortScope { Selections, Lines, Regions }
		public enum SortType { Smart, String, Length, Integer, Float, Hex, DateTime, Keys, Clipboard, Reverse, Randomize, Frequency }
		public enum JumpByType { Words, Numbers, Paths }

		void FindNext(ITextEditor te, bool forward, bool selecting)
		{
			if (Searches.Count == 0)
			{
				te.SetSelections(new List<Range>());
				return;
			}

			var sels = new List<Range>();
			foreach (var selection in te.Selections)
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
			te.SetSelections(sels);
		}

		List<Range> GetEnclosingRegions(ITextEditor te, int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var useRegions = te.Regions[useRegion];
			var regions = new List<Range>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in te.Selections)
			{
				while ((currentRegion < useRegions.Count) && (useRegions[currentRegion].End <= selection.Start))
				{
					if ((useAllRegions) && (!used))
						throw new Exception("Extra regions found.");
					used = false;
					++currentRegion;
				}
				if ((currentRegion < useRegions.Count) && (selection.Start >= useRegions[currentRegion].Start) && (selection.End <= useRegions[currentRegion].End))
				{
					regions.Add(useRegions[currentRegion]);
					used = true;
				}
				else if (mustBeInRegion)
					throw new Exception("No region found.  All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((te.Selections.Any()) && (useAllRegions) && (currentRegion != useRegions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		Range GetNextPrevBookmark(ITextEditor te, Range range, bool next, bool selecting)
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
			return te.MoveCursor(range, Bookmarks[index].Start, selecting);
		}

		List<int> GetOrdering(ITextEditor te, SortType type, bool caseSensitive, bool ascending)
		{
			var entries = te.Selections.Select((range, index) => new { value = te.GetString(range), index = index }).ToList();

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
				case SortType.Keys: entries = OrderByAscDesc(entries, entry => entry.value, ascending, Comparer<string>.Create((value1, value2) => (keysHash.ContainsKey(value1) ? keysHash[value1] : int.MaxValue).CompareTo(keysHash.ContainsKey(value2) ? keysHash[value2] : int.MaxValue))).ToList(); break;
				case SortType.Clipboard:
					{
						var sort = Clipboard.Distinct().Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
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

			return entries.Select(entry => entry.index).ToList();
		}

		List<Range> GetSortLines(ITextEditor te) => te.Selections.Select(range => te.Data.GetOffsetLine(range.Start)).Select(line => Range.FromIndex(te.Data.GetOffset(line, 0), te.Data.GetLineLength(line))).ToList();

		List<Range> GetSortSource(ITextEditor te, SortScope scope, int useRegion)
		{
			List<Range> sortSource = null;
			switch (scope)
			{
				case SortScope.Selections: sortSource = te.Selections.ToList(); break;
				case SortScope.Lines: sortSource = GetSortLines(te); break;
				case SortScope.Regions: sortSource = GetEnclosingRegions(te, useRegion, true); break;
				default: throw new Exception("Invalid sort type");
			}

			if (te.Selections.Count != sortSource.Count)
				throw new Exception("Selections and regions counts must match");

			var orderedRegions = sortSource.OrderBy(range => range.Start).ToList();
			var pos = 0;
			foreach (var range in orderedRegions)
			{
				if (range.Start < pos)
					throw new Exception("Regions cannot overlap");
				pos = range.End;
			}

			for (var ctr = 0; ctr < te.Selections.Count; ++ctr)
			{
				if ((te.Selections[ctr].Start < sortSource[ctr].Start) || (te.Selections[ctr].End > sortSource[ctr].End))
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

		void Command_Edit_Undo(ITextEditor te)
		{
			var undo = undoRedo.GetUndo();
			if (undo == null)
				return;
			te.SetSelections(undo.ranges, false);
			te.ReplaceSelections(undo.text, replaceType: ReplaceType.Undo);
		}

		void Command_Edit_Redo(ITextEditor te)
		{
			var redo = undoRedo.GetRedo();
			if (redo == null)
				return;
			te.SetSelections(redo.ranges);
			te.ReplaceSelections(redo.text, replaceType: ReplaceType.Redo);
		}

		void Command_Edit_Copy_CutCopy(ITextEditor te, bool isCut)
		{
			var strs = te.GetSelectionStrings();

			if (!StringsAreFiles(strs))
				SetClipboardStrings(strs);
			else
				SetClipboardFiles(strs, isCut);
			if (isCut)
				te.ReplaceSelections("");
		}

		public void Command_Edit_Paste_AllFiles(ITextEditor te, string str, bool highlight) => te.ReplaceSelections(te.Selections.Select(value => str).ToList(), highlight);

		void Command_Edit_Paste_Paste(ITextEditor te, bool highlight, bool rotate)
		{
			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 0) && (te.Selections.Count == 0))
				return;

			if ((te.Selections.Count == 1) && (clipboardStrings.Count != 1))
			{
				ReplaceOneWithMany(clipboardStrings, null);
				return;
			}

			if (clipboardStrings.Count == 0)
				throw new Exception("Nothing on clipboard!");

			var repeat = te.Selections.Count / clipboardStrings.Count;
			if (repeat * clipboardStrings.Count != te.Selections.Count)
				throw new Exception("Number of selections must be a multiple of number of clipboards.");

			if (repeat != 1)
				if (rotate)
					clipboardStrings = Enumerable.Repeat(clipboardStrings, repeat).SelectMany(x => x).ToList();
				else
					clipboardStrings = clipboardStrings.SelectMany(str => Enumerable.Repeat(str, repeat)).ToList();

			te.ReplaceSelections(clipboardStrings, highlight);
		}

		EditFindFindDialog.Result Command_Edit_Find_Find_Dialog(ITextEditor te)
		{
			string text = null;
			var selectionOnly = te.Selections.AsParallel().Any(range => range.HasSelection);

			if (te.Selections.Count == 1)
			{
				var sel = te.Selections.Single();
				if ((selectionOnly) && (te.Data.GetOffsetLine(sel.Cursor) == te.Data.GetOffsetLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = te.GetString(sel);
				}
			}

			return EditFindFindDialog.Run(te.TabsParent, text, selectionOnly);
		}

		void Command_Edit_Find_Find(ITextEditor te, bool selecting, EditFindFindDialog.Result result)
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
				te.SetSelections(te.Selections.AsParallel().AsOrdered().Where(range => regex.IsMatch(te.GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var regions = result.SelectionOnly ? te.Selections.ToList() : new List<Range> { te.FullRange };
			var resultsByRegion = regions.AsParallel().AsOrdered().Select(region => te.Data.RegexMatches(regex, region.Start, region.Length, result.MultiLine, result.RegexGroups, false)).ToList();

			if (result.Type == EditFindFindDialog.ResultType.CopyCount)
			{
				SetClipboardStrings(resultsByRegion.Select(list => list.Count.ToString()));
				return;
			}

			var results = resultsByRegion.SelectMany().Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			if (result.AddMatches)
				results.AddRange(te.Selections);

			switch (result.Type)
			{
				case EditFindFindDialog.ResultType.FindFirst:
					SetSearches(results);
					FindNext(te, true, selecting);
					break;
				case EditFindFindDialog.ResultType.FindAll:
					te.SetSelections(results);
					break;
			}
		}

		void Command_Edit_Find_NextPrevious(ITextEditor te, bool next, bool selecting) => FindNext(te, next, selecting);

		void Command_Edit_Find_Selected(ITextEditor te, bool selecting)
		{
			if ((te.Selections.Count != 1) || (!te.Selections[0].HasSelection))
				throw new Exception("Must have one selection with selected text.");

			var text = Regex.Escape(te.GetString(te.Selections[0]));
			var regex = new Regex(text, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

			SetSearches(te.Data.RegexMatches(regex, BeginOffset, EndOffset, false, false, false).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList());
			FindNext(te, true, selecting);
		}

		EditFindMassFindDialog.Result Command_Edit_Find_MassFind_Dialog(ITextEditor te) => EditFindMassFindDialog.Run(te.TabsParent, te.Selections.Any(range => range.HasSelection), GetVariables());

		void Command_Edit_Find_MassFind(ITextEditor te, EditFindMassFindDialog.Result result)
		{
			var texts = GetVariableExpressionResults<string>(result.Text);

			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var set = new HashSet<string>(texts, result.MatchCase ? (IEqualityComparer<string>)EqualityComparer<string>.Default : StringComparer.OrdinalIgnoreCase);
				te.SetSelections(te.Selections.AsParallel().AsOrdered().Where(range => set.Contains(te.GetString(range)) == result.KeepMatching).ToList());
				return;
			}

			var searcher = new Searcher(texts, result.MatchCase);
			var selections = result.SelectionOnly ? te.Selections.ToList() : new List<Range> { te.FullRange };
			var ranges = selections.AsParallel().AsOrdered().SelectMany(selection => te.Data.StringMatches(searcher, selection.Start, selection.Length)).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			te.SetSelections(ranges);
		}

		EditFindReplaceDialog.Result Command_Edit_Find_Replace_Dialog(ITextEditor te)
		{
			string text = null;
			var selectionOnly = te.Selections.AsParallel().Any(range => range.HasSelection);

			if (te.Selections.Count == 1)
			{
				var sel = te.Selections.Single();
				if ((selectionOnly) && (te.Data.GetOffsetLine(sel.Cursor) == te.Data.GetOffsetLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = te.GetString(sel);
				}
			}

			return EditFindReplaceDialog.Run(te.TabsParent, text, selectionOnly);
		}

		void Command_Edit_Find_Replace(ITextEditor te, EditFindReplaceDialog.Result result)
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

			var regions = result.SelectionOnly ? te.Selections.ToList() : new List<Range> { te.FullRange };
			var sels = regions.AsParallel().AsOrdered().SelectMany(region => te.Data.RegexMatches(regex, region.Start, region.Length, result.MultiLine, false, false)).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)).ToList();
			te.SetSelections(sels);
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => regex.Replace(te.GetString(range), result.Replace)).ToList());
		}

		void Command_Edit_Find_ClearSearchResults() => SetSearches(new List<Range>());

		void Command_Edit_CopyDown(ITextEditor te)
		{
			var strs = te.GetSelectionStrings();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (string.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			te.ReplaceSelections(strs);
		}

		EditRotateDialog.Result Command_Edit_Rotate_Dialog(ITextEditor te) => EditRotateDialog.Run(te.TabsParent, GetVariables());

		void Command_Edit_Rotate(ITextEditor te, EditRotateDialog.Result result)
		{
			var count = new NEExpression(result.Count).Evaluate<int>(GetVariables());

			var strs = te.GetSelectionStrings();
			if (count < 0)
				count = -count % strs.Count;
			else
				count = strs.Count - count % strs.Count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			te.ReplaceSelections(strs);
		}

		EditRepeatDialog.Result Command_Edit_Repeat_Dialog(ITextEditor te) => EditRepeatDialog.Run(te.TabsParent, te.Selections.Count == 1, GetVariables());

		void Command_Edit_Repeat(ITextEditor te, EditRepeatDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			if (results.Any(repeatCount => repeatCount < 0))
				throw new Exception("Repeat count must be >= 0");
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(te.GetString(range), results[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < te.Selections.Count; ++ctr)
					if (results[ctr] != 0)
					{
						var selection = te.Selections[ctr];
						var len = selection.Length / results[ctr];
						for (var index = selection.Start; index < selection.End; index += len)
							sels.Add(new Range(index + len, index));
					}
				te.SetSelections(sels);
			}
		}

		void Command_Edit_Markup_Escape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlEncode(te.GetString(range))).ToList());

		void Command_Edit_Markup_Unescape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlDecode(te.GetString(range))).ToList());

		void Command_Edit_RegEx_Escape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(te.GetString(range))).ToList());

		void Command_Edit_RegEx_Unescape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(te.GetString(range))).ToList());

		void Command_Edit_URL_Escape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(te.GetString(range))).ToList());

		void Command_Edit_URL_Unescape(ITextEditor te) => te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(te.GetString(range))).ToList());

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Edit_URL_Absolute_Dialog(ITextEditor te) => FilesNamesMakeAbsoluteRelativeDialog.Run(te.TabsParent, GetVariables(), true, false);

		void Command_Edit_URL_Absolute(ITextEditor te, FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			te.ReplaceSelections(te.GetSelectionStrings().Select((str, index) =>
			{
				var uri = new Uri(new Uri(results[index]), str);
				return uri.AbsoluteUri;
			}).ToList());
		}

		EditDataHashDialog.Result Command_Edit_Data_Hash_Dialog(ITextEditor te) => EditDataHashDialog.Run(te.TabsParent, te.CodePage);

		void Command_Edit_Data_Hash(ITextEditor te, EditDataHashDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			te.ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Compress_Dialog(ITextEditor te) => EditDataCompressDialog.Run(te.TabsParent, te.CodePage, true);

		void Command_Edit_Data_Compress(ITextEditor te, EditDataCompressDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(compressed, result.OutputCodePage))
				return;
			te.ReplaceSelections(compressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataCompressDialog.Result Command_Edit_Data_Decompress_Dialog(ITextEditor te) => EditDataCompressDialog.Run(te.TabsParent, te.CodePage, false);

		void Command_Edit_Data_Decompress(ITextEditor te, EditDataCompressDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsParallel().AsOrdered().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(decompressed, result.OutputCodePage))
				return;
			te.ReplaceSelections(decompressed.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Encrypt_Dialog(ITextEditor te) => EditDataEncryptDialog.Run(te.TabsParent, te.CodePage, true);

		void Command_Edit_Data_Encrypt(ITextEditor te, EditDataEncryptDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(encrypted, result.OutputCodePage))
				return;
			te.ReplaceSelections(encrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataEncryptDialog.Result Command_Edit_Data_Decrypt_Dialog(ITextEditor te) => EditDataEncryptDialog.Run(te.TabsParent, te.CodePage, false);

		void Command_Edit_Data_Decrypt(ITextEditor te, EditDataEncryptDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsParallel().AsOrdered().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(decrypted, result.OutputCodePage))
				return;
			te.ReplaceSelections(decrypted.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		EditDataSignDialog.Result Command_Edit_Data_Sign_Dialog(ITextEditor te) => EditDataSignDialog.Run(te.TabsParent, te.CodePage);

		void Command_Edit_Data_Sign(ITextEditor te, EditDataSignDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			te.ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Cryptor.Sign(Coder.StringToBytes(str, result.CodePage), result.CryptorType, result.Key, result.Hash)).ToList());
		}

		EditSortDialog.Result Command_Edit_Sort_Dialog(ITextEditor te) => EditSortDialog.Run(te.TabsParent);

		void Command_Edit_Sort(ITextEditor te, EditSortDialog.Result result)
		{
			var regions = GetSortSource(te, result.SortScope, result.UseRegion);
			var ordering = GetOrdering(te, result.SortType, result.CaseSensitive, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

			var newSelections = te.Selections.ToList();
			var orderedRegions = ordering.Select(index => regions[index]).ToList();
			var orderedRegionText = orderedRegions.Select(range => te.GetString(range)).ToList();

			te.Replace(regions, orderedRegionText);

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

			te.SetSelections(newSelections);
			if (result.SortScope == SortScope.Regions)
				SetRegions(result.UseRegion, newRegions);
		}

		EditConvertDialog.Result Command_Edit_Convert_Dialog(ITextEditor te) => EditConvertDialog.Run(te.TabsParent);

		void Command_Edit_Convert(ITextEditor te, EditConvertDialog.Result result)
		{
			var strs = te.GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputType))
				return;
			var bytes = strs.AsParallel().AsOrdered().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanEncode(bytes, result.OutputType))
				return;
			te.ReplaceSelections(bytes.AsParallel().AsOrdered().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		void Command_Edit_Bookmarks_Toggle(ITextEditor te)
		{
			var linePairs = te.Selections.AsParallel().AsOrdered().Select(range => new { start = te.Data.GetOffsetLine(range.Start), end = te.Data.GetOffsetLine(range.End) }).ToList();
			if (linePairs.Any(pair => pair.start != pair.end))
				throw new Exception("Selections must be on a single line.");

			var bookmarks = Bookmarks.ToList();
			var lineRanges = linePairs.AsParallel().AsOrdered().Select(pair => new Range(te.Data.GetOffset(pair.start, 0))).ToList();
			var comparer = Comparer<Range>.Create((r1, r2) => r1.Start.CompareTo(r2.Start));
			var indexes = lineRanges.AsParallel().Select(range => new { range = range, index = bookmarks.BinarySearch(range, comparer) }).Reverse().ToList();

			if (indexes.Any(index => index.index < 0))
			{
				foreach (var pair in indexes)
					if (pair.index < 0)
						bookmarks.Insert(~pair.index, pair.range);
			}
			else
			{
				foreach (var pair in indexes)
					bookmarks.RemoveAt(pair.index);
			}
			SetBookmarks(bookmarks);
		}

		void Command_Edit_Bookmarks_NextPreviousBookmark(ITextEditor te, bool next, bool selecting)
		{
			if (!Bookmarks.Any())
				return;
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range => GetNextPrevBookmark(te, range, next, selecting)).ToList());
		}

		void Command_Edit_Bookmarks_Clear() => SetBookmarks(new List<Range>());

		void Command_Edit_Navigate_WordLeftRight(ITextEditor te, bool next, bool selecting)
		{
			if ((!selecting) && (te.Selections.Any(range => range.HasSelection)))
			{
				te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range => new Range(next ? range.End : range.Start)).ToList());
				return;
			}

			var func = next ? (Func<int, int>)GetNextWord : GetPrevWord;
			te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range => te.MoveCursor(range, func(range.Cursor), selecting)).ToList());
		}

		void Command_Edit_Navigate_AllLeft(ITextEditor te, bool selecting)
		{
			if (!te.Selections.Any())
				return;

			var offsets = te.Selections.Select(range => range.Cursor).ToList();
			var lines = offsets.AsParallel().AsOrdered().Select(offset => te.Data.GetOffsetLine(offset)).ToList();

			var indexes = offsets.Zip(lines, (offset, line) => te.Data.GetOffsetIndex(offset, line)).ToList();
			var index = Math.Min(indexes.Max() - 1, indexes.Min());

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index < 0)
				{
					offsets = lines.Select(line => te.Data.GetOffset(line, 0)).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(line, index) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					offsets = lines.Select(line => te.Data.GetOffset(line, index + 1)).ToList();
					break;
				}

				--index;
			}

			te.SetSelections(te.Selections.Zip(offsets, (range, offset) => te.MoveCursor(range, offset, selecting)).ToList());
		}

		void Command_Edit_Navigate_AllRight(ITextEditor te, bool selecting)
		{
			if (!te.Selections.Any())
				return;

			var offsets = te.Selections.Select(range => range.Cursor).ToList();
			var lines = offsets.AsParallel().AsOrdered().Select(offset => te.Data.GetOffsetLine(offset)).ToList();

			var index = offsets.Zip(lines, (offset, line) => te.Data.GetOffsetIndex(offset, line)).Min();
			var endIndex = lines.Select(line => te.Data.GetLineLength(line)).Min();

			var currentIsSpace = default(bool?);

			while (true)
			{
				if (index > endIndex)
				{
					offsets = lines.Select(line => te.Data.GetOffset(line, te.Data.GetLineLength(line))).ToList();
					break;
				}

				var isSpace = lines.All(line => GetWordSkipType(line, index) == WordSkipType.Space);

				if (!currentIsSpace.HasValue)
					currentIsSpace = isSpace;
				else if (isSpace != currentIsSpace)
				{
					offsets = lines.Select(line => te.Data.GetOffset(line, index)).ToList();
					break;
				}

				++index;
			}

			te.SetSelections(te.Selections.Zip(offsets, (range, offset) => te.MoveCursor(range, offset, selecting)).ToList());
		}

		void Command_Edit_Navigate_JumpBy(JumpByType jumpBy) => JumpBy = jumpBy;

		IOrderedEnumerable<TSource> OrderByAscDesc<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool ascending, IComparer<TKey> comparer = null)
		{
			var func = ascending ? (Func<IEnumerable<TSource>, Func<TSource, TKey>, IComparer<TKey>, IOrderedEnumerable<TSource>>)Enumerable.OrderBy : Enumerable.OrderByDescending;
			return func(source, keySelector, comparer ?? Comparer<TKey>.Default);
		}
	}
}
