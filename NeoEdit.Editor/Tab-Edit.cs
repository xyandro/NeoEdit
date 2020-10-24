using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.Searchers;
using NeoEdit.Editor.Transactional;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
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

		void Execute_Edit_Undo()
		{
			var step = UndoRedo.GetUndo(ref tabState.undoRedo);
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
			var step = UndoRedo.GetRedo(ref tabState.undoRedo);
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

		void Execute_Edit_Copy_CutCopy(bool isCut)
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

		Configuration_Edit_Paste_Paste Configure_Edit_Paste_Paste()
		{
			var configuration = new Configuration_Edit_Paste_Paste();
			configuration.ReplaceOneWithMany = (Tabs.ActiveTabs.All(tab => tab.Selections.Count == 1)) && (Tabs.ActiveTabs.Any(tab => tab.Clipboard.Count != 1));
			return configuration;
		}

		void Execute_Edit_Paste_Paste(bool highlight, bool rotate)
		{
			var clipboardStrings = Clipboard;
			if ((clipboardStrings.Count == 0) && (Selections.Count == 0))
				return;

			if ((state.Configuration as Configuration_Edit_Paste_Paste).ReplaceOneWithMany)
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

		Configuration_Edit_Find_Find Configure_Edit_Find_Find()
		{
			string text = null;
			var selectionOnly = Selections.Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Text.GetPositionLine(sel.Cursor) == Text.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = Text.GetString(sel);
				}
			}

			return Tabs.TabsWindow.Configure_Edit_Find_Find(text, selectionOnly, ViewBinaryCodePages, GetVariables());
		}

		void Execute_Edit_Find_Find()
		{
			var result = state.Configuration as Configuration_Edit_Find_Find;
			// Determine selections to search
			List<Range> selections;
			var firstMatchOnly = (result.KeepMatching) || (result.RemoveMatching);
			if (result.Type == Configuration_Edit_Find_Find.ResultType.FindNext)
			{
				firstMatchOnly = true;
				selections = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
					selections.Add(new Range(Selections[ctr].End, ctr + 1 == Selections.Count ? Text.Length : Selections[ctr + 1].Start));
			}
			else if (result.SelectionOnly)
				selections = Selections.ToList();
			else
				selections = new List<Range> { Range.FromIndex(0, Text.Length) };

			if (!selections.Any())
				return;

			// For each selection, determine strings to find. The boolean is for MatchCase, since even if MatchCase is false some should be true (INT16LE 30000 = 0u, NOT 0U)
			List<List<(string, bool)>> stringsToFind;

			if (result.IsExpression)
			{
				var expressionResults = GetExpressionResults<string>(result.Text, result.AlignSelections ? selections.Count : default(int?));
				if (result.AlignSelections)
				{
					if (result.IsBoolean) // Either KeepMatching or RemoveMatching will also be true
					{
						Selections = Selections.Where((range, index) => (expressionResults[index] == "True") == result.KeepMatching).ToList();
						return;
					}

					stringsToFind = selections.Select((x, index) => new List<(string, bool)> { (expressionResults[index], result.MatchCase) }).ToList();
				}
				else
					stringsToFind = Enumerable.Repeat(expressionResults.Select(x => (x, result.MatchCase)).ToList(), selections.Count).ToList();
			}
			else
				stringsToFind = Enumerable.Repeat(new List<(string, bool)> { (result.Text, result.MatchCase) }, selections.Count).ToList();

			ViewBinarySearches = null;
			// If the strings are binary convert them to all codepages
			if (result.IsBinary)
			{
				ViewBinaryCodePages = result.CodePages;
				ViewBinarySearches = stringsToFind.SelectMany().Distinct().GroupBy(x => x.Item2).Select(g => new HashSet<string>(g.Select(x => x.Item1), g.Key ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)).ToList();
				var mapping = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => list.SelectMany(
							item => result.CodePages
								.Select(codePage => (Coder.TryStringToBytes(item.Item1, codePage), (item.Item2) || (Coder.AlwaysCaseSensitive(codePage))))
								.NonNull(tuple => tuple.Item1)
								.Select(tuple => (Coder.TryBytesToString(tuple.Item1, CodePage), tuple.Item2))
								.NonNullOrEmpty(tuple => tuple.Item1)
							).Distinct().ToList());

				stringsToFind = stringsToFind.Select(list => mapping[list]).ToList();
			}

			// Create searchers
			Dictionary<List<(string, bool)>, ISearcher> searchers;
			if (result.IsRegex)
			{
				searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => new RegexesSearcher(list.Select(x => x.Item1).ToList(), result.WholeWords, result.MatchCase, result.EntireSelection, firstMatchOnly, result.RegexGroups) as ISearcher);
			}
			else
			{
				searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list =>
						{
							if (list.Count == 1)
								return new StringSearcher(list[0].Item1, result.WholeWords, list[0].Item2, result.SkipSpace, result.EntireSelection, firstMatchOnly) as ISearcher;
							return new StringsSearcher(list, result.WholeWords, result.EntireSelection, firstMatchOnly) as ISearcher;
						});
			}

			// Perform search
			var results = selections.AsTaskRunner().Select((range, index) => searchers[stringsToFind[index]].Find(Text.GetString(range), range.Start)).ToList();

			switch (result.Type)
			{
				case Configuration_Edit_Find_Find.ResultType.CopyCount:
					Clipboard = results.Select(list => list.Count.ToString()).ToList();
					break;
				case Configuration_Edit_Find_Find.ResultType.FindNext:
					var newSels = new List<Range>();
					for (var ctr = 0; ctr < Selections.Count; ++ctr)
					{
						int endPos;
						if (results[ctr].Count >= 1)
							endPos = results[ctr][0].End;
						else if (ctr + 1 < Selections.Count)
							endPos = Selections[ctr + 1].Start;
						else
							endPos = Text.Length;
						newSels.Add(new Range(endPos, Selections[ctr].Start));
					}
					Selections = newSels;
					break;
				case Configuration_Edit_Find_Find.ResultType.FindAll:
					if ((result.KeepMatching) || (result.RemoveMatching))
						Selections = selections.Where((range, index) => results[index].Any() == result.KeepMatching).ToList();
					else
						Selections = results.SelectMany().ToList();
					break;
			}
		}

		Configuration_Edit_Find_RegexReplace Configure_Edit_Find_RegexReplace()
		{
			string text = null;
			var selectionOnly = Selections.Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Text.GetPositionLine(sel.Cursor) == Text.GetPositionLine(sel.Anchor)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = Text.GetString(sel);
				}
			}

			return Tabs.TabsWindow.Configure_Edit_Find_RegexReplace(text, selectionOnly);
		}

		void Execute_Edit_Find_RegexReplace()
		{
			var result = state.Configuration as Configuration_Edit_Find_RegexReplace;
			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { Range.FromIndex(0, Text.Length) };
			var searcher = new RegexesSearcher(new List<string> { result.Text }, result.WholeWords, result.MatchCase, result.EntireSelection);
			var sels = regions.AsTaskRunner().SelectMany(region => searcher.Find(Text.GetString(region), region.Start)).ToList();
			Selections = sels;
			ReplaceSelections(Selections.AsTaskRunner().Select(range => searcher.regexes[0].Replace(Text.GetString(range), result.Replace)).ToList());
		}

		void Execute_Edit_CopyDown()
		{
			var strs = GetSelectionStrings().ToList();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (string.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			ReplaceSelections(strs);
		}

		Configuration_Edit_Expression_Expression Configure_Edit_Expression_Expression() => Tabs.TabsWindow.Configure_Edit_Expression_Expression(GetVariables());

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

		Configuration_Edit_Rotate Configure_Edit_Rotate() => Tabs.TabsWindow.Configure_Edit_Rotate(GetVariables());

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

		Configuration_Edit_Repeat Configure_Edit_Repeat() => Tabs.TabsWindow.Configure_Edit_Repeat(Selections.Count == 1, GetVariables());

		void Execute_Edit_Repeat()
		{
			var result = state.Configuration as Configuration_Edit_Repeat;
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

		void Execute_Edit_Escape_Markup() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.HtmlEncode(Text.GetString(range))).ToList());

		void Execute_Edit_Escape_RegEx() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Regex.Escape(Text.GetString(range))).ToList());

		void Execute_Edit_Escape_URL() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.UrlEncode(Text.GetString(range))).ToList());

		void Execute_Edit_Unescape_Markup() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.HtmlDecode(Text.GetString(range))).ToList());

		void Execute_Edit_Unescape_RegEx() => ReplaceSelections(Selections.AsTaskRunner().Select(range => Regex.Unescape(Text.GetString(range))).ToList());

		void Execute_Edit_Unescape_URL() => ReplaceSelections(Selections.AsTaskRunner().Select(range => HttpUtility.UrlDecode(Text.GetString(range))).ToList());

		Configuration_Edit_Data_Hash Configure_Edit_Data_Hash() => Tabs.TabsWindow.Configure_Edit_Data_Hash(CodePage);

		void Execute_Edit_Data_Hash()
		{
			var result = state.Configuration as Configuration_Edit_Data_Hash;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsTaskRunner().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		Configuration_Edit_Data_Compress Configure_Edit_Data_Compress() => Tabs.TabsWindow.Configure_Edit_Data_Compress(CodePage, true);

		void Execute_Edit_Data_Compress()
		{
			var result = state.Configuration as Configuration_Edit_Data_Compress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var compressed = strs.AsTaskRunner().Select(str => Compressor.Compress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(compressed, result.OutputCodePage))
				return;
			ReplaceSelections(compressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		Configuration_Edit_Data_Compress Configure_Edit_Data_Decompress() => Tabs.TabsWindow.Configure_Edit_Data_Compress(CodePage, false);

		void Execute_Edit_Data_Decompress()
		{
			var result = state.Configuration as Configuration_Edit_Data_Compress;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decompressed = strs.AsTaskRunner().Select(str => Compressor.Decompress(Coder.StringToBytes(str, result.InputCodePage), result.CompressorType)).ToList();
			if (!CheckCanEncode(decompressed, result.OutputCodePage))
				return;
			ReplaceSelections(decompressed.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		Configuration_Edit_Data_Encrypt Configure_Edit_Data_Encrypt() => Tabs.TabsWindow.Configure_Edit_Data_Encrypt(CodePage, true);

		void Execute_Edit_Data_Encrypt()
		{
			var result = state.Configuration as Configuration_Edit_Data_Encrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var encrypted = strs.AsTaskRunner().Select(str => Cryptor.Encrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(encrypted, result.OutputCodePage))
				return;
			ReplaceSelections(encrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		Configuration_Edit_Data_Encrypt Configure_Edit_Data_Decrypt() => Tabs.TabsWindow.Configure_Edit_Data_Encrypt(CodePage, false);

		void Execute_Edit_Data_Decrypt()
		{
			var result = state.Configuration as Configuration_Edit_Data_Encrypt;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputCodePage))
				return;
			var decrypted = strs.AsTaskRunner().Select(str => Cryptor.Decrypt(Coder.StringToBytes(str, result.InputCodePage), result.CryptorType, result.Key)).ToList();
			if (!CheckCanEncode(decrypted, result.OutputCodePage))
				return;
			ReplaceSelections(decrypted.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputCodePage)).ToList());
		}

		Configuration_Edit_Data_Sign Configure_Edit_Data_Sign() => Tabs.TabsWindow.Configure_Edit_Data_Sign(CodePage);

		void Execute_Edit_Data_Sign()
		{
			var result = state.Configuration as Configuration_Edit_Data_Sign;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.CodePage))
				return;
			ReplaceSelections(strs.AsTaskRunner().Select(str => Cryptor.Sign(Coder.StringToBytes(str, result.CodePage), result.CryptorType, result.Key, result.Hash)).ToList());
		}

		Configuration_Edit_Sort Configure_Edit_Sort() => Tabs.TabsWindow.Configure_Edit_Sort();

		void Execute_Edit_Sort()
		{
			var result = state.Configuration as Configuration_Edit_Sort;
			var regions = GetSortSource(result.SortScope, result.UseRegion);
			var ordering = GetOrdering(result.SortType, result.CaseSensitive, result.Ascending);
			if (regions.Count != ordering.Count)
				throw new Exception("Ordering misaligned");

			var newSelections = Selections.ToList();
			var orderedRegions = ordering.Select(index => regions[index]).ToList();
			var orderedRegionText = orderedRegions.Select(range => Text.GetString(range)).ToList();

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

			Selections = newSelections;
			if (result.SortScope == SortScope.Regions)
				SetRegions(result.UseRegion, newRegions);
		}

		Configuration_Edit_Convert Configure_Edit_Convert() => Tabs.TabsWindow.Configure_Edit_Convert();

		void Execute_Edit_Convert()
		{
			var result = state.Configuration as Configuration_Edit_Convert;
			var strs = GetSelectionStrings();
			if (!CheckCanEncode(strs, result.InputType))
				return;
			var bytes = strs.AsTaskRunner().Select(str => Coder.StringToBytes(str, result.InputType, result.InputBOM)).ToList();
			if (!CheckCanEncode(bytes, result.OutputType))
				return;
			ReplaceSelections(bytes.AsTaskRunner().Select(data => Coder.BytesToString(data, result.OutputType, result.OutputBOM)).ToList());
		}

		void Execute_Edit_Navigate_WordLeftRight(bool next)
		{
			if ((!state.ShiftDown) && (Selections.Any(range => range.HasSelection)))
			{
				Selections = Selections.AsTaskRunner().Select(range => new Range(next ? range.End : range.Start)).ToList();
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

		void Execute_Edit_Navigate_JumpBy(JumpByType jumpBy) => JumpBy = jumpBy;
	}
}
