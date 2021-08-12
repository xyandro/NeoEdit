using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEFile : INEFile
	{
		static ThreadSafeRandom random = new ThreadSafeRandom();
		static EditorExecuteState state => EditorExecuteState.CurrentState;
		static int nextDiffSerial;

		public readonly NEText Text = new NEText("");
		public int DiffSerial { get; } = Interlocked.Increment(ref nextDiffSerial);

		public DateTime LastActive { get; set; }
		public bool IsModified { get; private set; }
		public DateTime LastWriteTime { get; private set; }
		public DateTime LastExternalWriteTime { get; private set; }
		public DateTime LastActivatedTime { get; set; }
		public string DBName { get; private set; }
		int currentSelection;
		public int CurrentSelection { get => Math.Min(Math.Max(0, currentSelection), Selections.Count - 1); private set { currentSelection = value; } }

		public string DisplayName { get; private set; }
		public string FileName { get; private set; }
		public bool AutoRefresh { get; private set; }
		public ParserType ContentType { get; set; }
		Coder.CodePage codePage;
		public Coder.CodePage CodePage { get => codePage; private set { codePage = value; SetIsModified(); } }
		bool hasBOM;
		public bool HasBOM { get => hasBOM; private set { hasBOM = value; SetIsModified(); } }
		string aesKey;
		public string AESKey { get => aesKey; private set { aesKey = value; SetIsModified(); } }
		bool compressed;
		public bool Compressed { get => compressed; private set { compressed = value; SetIsModified(); } }
		bool diffIgnoreWhitespace;
		public bool DiffIgnoreWhitespace { get => diffIgnoreWhitespace; private set { diffIgnoreWhitespace = value; Text.ClearDiff(); } }
		bool diffIgnoreCase;
		public bool DiffIgnoreCase { get => diffIgnoreCase; private set { diffIgnoreCase = value; Text.ClearDiff(); } }
		bool diffIgnoreNumbers;
		public bool DiffIgnoreNumbers { get => diffIgnoreNumbers; private set { diffIgnoreNumbers = value; Text.ClearDiff(); } }
		bool diffIgnoreLineEndings;
		public bool DiffIgnoreLineEndings { get => diffIgnoreLineEndings; private set { diffIgnoreLineEndings = value; Text.ClearDiff(); } }
		HashSet<char> diffIgnoreCharacters = new HashSet<char>();
		public HashSet<char> DiffIgnoreCharacters { get => diffIgnoreCharacters; private set { diffIgnoreCharacters = value; Text.ClearDiff(); } }
		public bool KeepSelections { get; private set; }
		public bool HighlightSyntax { get; private set; }
		public bool StrictParsing { get; private set; }
		public JumpByType JumpBy { get; private set; }
		public bool ViewBinary { get; private set; }
		public HashSet<Coder.CodePage> ViewBinaryCodePages { get; private set; }
		public IReadOnlyList<HashSet<string>> ViewBinarySearches { get; private set; }

		int startRow, startColumn;
		public int StartRow
		{
			get => startRow;
			private set
			{
				startRow = value;
				if (DiffTarget != null)
					DiffTarget.startRow = value;
			}
		}

		public int StartColumn
		{
			get => startColumn;
			private set
			{
				startColumn = value;
				if (DiffTarget != null)
					DiffTarget.startColumn = value;
			}
		}

		NEFile diffTarget;
		public NEFile DiffTarget
		{
			get => diffTarget;
			set
			{
				if (DiffTarget != null)
				{
					DiffTarget.NEWindow?.SetNeedsRender();
					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.diffTarget = null;
					diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					diffTarget = value;
					value.diffTarget = this;
					DiffTarget.NEWindow?.SetNeedsRender();
				}
			}
		}

		string savedText;
		NETextPoint savedTextPoint;
		Coder.CodePage savedCodePage;
		bool savedHasBOM;
		string savedAESKey;
		bool savedCompressed;

		public NEFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, int? index = null)
		{
			Data = new NEFileData(this);

			Selections = new List<NERange>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<NERange>());
			ViewBinaryCodePages = new HashSet<Coder.CodePage>(Coder.DefaultCodePages);
			StrictParsing = true;

			if (fileName != null)
				fileName = Path.GetFullPath(fileName.Trim('"'));

			AutoRefresh = KeepSelections = HighlightSyntax = true;
			JumpBy = JumpByType.Words;

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column, index);
		}

		public static NEFile CreateSummaryFile(string displayName, List<(string str, int count)> summary)
		{
			var sb = new StringBuilder();
			var countRanges = new List<NERange>();
			var stringRanges = new List<NERange>();
			foreach (var tuple in summary)
			{
				var countStr = tuple.count.ToString();
				countRanges.Add(NERange.FromIndex(sb.Length, countStr.Length));
				sb.Append(countStr);

				sb.Append(" ");

				stringRanges.Add(NERange.FromIndex(sb.Length, tuple.str.Length));
				sb.Append(tuple.str);

				if ((!tuple.str.EndsWith("\r")) && (!tuple.str.EndsWith("\n")))
					sb.Append("\r\n");
			}

			return new NEFile(displayName: displayName, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false) { Selections = stringRanges };
		}

		public string NEFileLabel => $"{DisplayName ?? (string.IsNullOrEmpty(FileName) ? "[Untitled]" : Path.GetFileName(FileName))}{(IsModified ? "*" : "")}{(LastWriteTime != LastExternalWriteTime ? "+" : "")}{(DiffTarget != null ? $" (Diff{(CodePage != DiffTarget.CodePage ? " - Encoding mismatch" : "")})" : "")}";

		CacheValue previousData = new CacheValue();
		ParserType previousType;
		ParserNode previousRoot;

		void ReplaceOneWithMany(IReadOnlyList<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var ending = addNewLines ?? strs.Any(str => !str.EndsWith(Text.DefaultEnding)) ? Text.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var ranges = new List<NERange>();
			if (strs.Count > 0)
			{
				ranges.Add(Selections[0]);
				ranges.AddRange(Enumerable.Repeat(new NERange(Selections[0].End), strs.Count - 1));
			}
			var position = Selections.Single().Start;
			Replace(ranges, strs);

			var sels = new List<NERange>();
			foreach (var str in strs)
			{
				sels.Add(NERange.FromIndex(position, str.Length - ending.Length));
				position += str.Length;
			}
			Selections = sels;
		}

		void ReplaceSelections(string str, bool highlight = true) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight);

		void ReplaceSelections(IReadOnlyList<string> strs, bool highlight = true)
		{
			Replace(Selections, strs);

			if (highlight)
				Selections = Selections.AsTaskRunner().Select((range, index) => new NERange(range.End - (strs == null ? 0 : strs[index].Length), range.End)).ToList();
			else
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.End)).ToList();
		}

		void Replace(IReadOnlyList<NERange> ranges, IReadOnlyList<string> strs = null)
		{
			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			NETextPoint = Text.CreateTextPoint(ranges, strs);
			SetIsModified();

			var translateMap = GetTranslateMap(ranges, strs, new List<IReadOnlyList<NERange>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			Selections = Translate(Selections, translateMap);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		#region Translate
		static int[] GetTranslateNums(IReadOnlyList<IReadOnlyList<NERange>> rangeLists)
		{
			var nums = new int[rangeLists.Sum(rangeList => rangeList.Count * 2)];
			var numsStart = 0;
			foreach (var rangeList in rangeLists)
			{
				var size = Math.Max(65536, (rangeList.Count + 31) / 32);
				Helpers.PartitionedParallelForEach(rangeList.Count, size, (start, end) =>
				{
					var numPos = numsStart + start * 2;
					for (var r = start; r < end; ++r)
					{
						nums[numPos++] = rangeList[r].Start;
						nums[numPos++] = rangeList[r].End;
					}
				});
				numsStart += rangeList.Count * 2;
			}

			Array.Sort(nums);

			var outPos = -1;
			for (var inPos = 0; inPos < nums.Length; ++inPos)
			{
				if ((outPos != -1) && (nums[inPos] == nums[outPos]))
					continue;
				nums[++outPos] = nums[inPos];
			}

			Array.Resize(ref nums, outPos + 1);
			return nums;
		}

		static Tuple<int[], int[]> GetTranslateMap(IReadOnlyList<NERange> replaceRanges, IReadOnlyList<string> strs, IReadOnlyList<IReadOnlyList<NERange>> rangeLists)
		{
			var translateNums = GetTranslateNums(rangeLists);
			var translateResults = new int[translateNums.Length];
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < translateNums.Length)
			{
				int start = int.MaxValue, end = int.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (translateNums[current] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = translateNums[current];
				if ((value > start) && (value < end))
					value = start + length;

				translateResults[current] = value + offset;
				++current;
			}

			return Tuple.Create(translateNums, translateResults);
		}

		static IReadOnlyList<NERange> Translate(IReadOnlyList<NERange> ranges, Tuple<int[], int[]> translateMap)
		{
			var result = Helpers.PartitionedParallelForEach<NERange>(ranges.Count, Math.Max(65536, (ranges.Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].End, current);
					if (ranges[ctr].Cursor < ranges[ctr].Anchor)
						list.Add(new NERange(translateMap.Item2[current], translateMap.Item2[startPos]));
					else
						list.Add(new NERange(translateMap.Item2[startPos], translateMap.Item2[current]));
				}
			});
			return result;
		}
		#endregion

		IReadOnlyList<NERange> GetSortedSelections() => AllowOverlappingSelections ? Sort(Selections) : Selections;

		static bool NeedsSort(IReadOnlyList<NERange> items)
		{
			for (var ctr = 1; ctr < items.Count; ++ctr)
				if ((items[ctr].Start < items[ctr - 1].Start) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End < items[ctr - 1].End)))
					return true;

			return false;
		}

		static IReadOnlyList<NERange> Sort(IReadOnlyList<NERange> items)
		{
			if (!NeedsSort(items))
				return items;
			return items.OrderBy(range => range.Start).ThenByDescending(range => range.End).ToList();
		}

		#region DeOverlap
		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		static IReadOnlyList<NERange> DeOverlap(IReadOnlyList<NERange> items)
		{
			while (true)
			{
				switch (GetDeOverlapStep(items))
				{
					case DeOverlapStep.Sort: items = items.OrderBy(range => range.Start).ThenBy(range => range.End).ToList(); break;
					case DeOverlapStep.DeOverlap: return DoDeOverlap(items);
					case DeOverlapStep.Done: return items;
					default: throw new Exception("Invalid step");
				}
			}
		}

		static DeOverlapStep GetDeOverlapStep(IReadOnlyList<NERange> items)
		{
			var result = DeOverlapStep.Done;
			for (var ctr = 1; ctr < items.Count; ++ctr)
			{
				if ((items[ctr].Start < items[ctr - 1].Start) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End < items[ctr - 1].End)))
					return DeOverlapStep.Sort;

				if ((items[ctr].Start < items[ctr - 1].End) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End == items[ctr - 1].End)))
					result = DeOverlapStep.DeOverlap;
			}

			return result;
		}

		static IReadOnlyList<NERange> DoDeOverlap(IReadOnlyList<NERange> items)
		{
			var result = new List<NERange>();

			using (var enumerator = items.GetEnumerator())
			{
				var last = default(NERange);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Anchor)
							result.Add(new NERange(range.Start, last.Start));
						else
							result.Add(new NERange(last.Start, range.Start));
						last = null;
					}

					if (range == null)
						break;

					if ((last != null) && (range.End <= last.End))
						continue;

					last = range;
				}
			}

			return result;
		}
		#endregion

		#region Configure
		public static void Configure()
		{
			var handled = true;
			switch (state.Command)
			{
				case NECommand.Window_CustomGrid: Configure__Window_CustomGrid(); break;
				default: handled = false; break;
			}

			if ((handled) || (state.NEWindow.Focused == null))
				return;

			switch (state.Command)
			{
				case NECommand.Edit_Select_Lines: Configure__Edit_Select_Lines(); break;
				case NECommand.Edit_Select_ToggleAnchor: Configure__Edit_Select_ToggleAnchor(); break;
				case NECommand.Edit_Paste_Paste: Configure__Edit_Paste_Paste__Edit_Paste_RotatePaste(); break;
				case NECommand.Edit_Paste_RotatePaste: Configure__Edit_Paste_Paste__Edit_Paste_RotatePaste(); break;
				case NECommand.Edit_Repeat: Configure__Edit_Repeat(); break;
				case NECommand.Edit_Rotate: Configure__Edit_Rotate(); break;
				case NECommand.Edit_Expression_Expression: Configure__Edit_Expression_Expression(); break;
				case NECommand.Edit_ModifyRegions: Configure__Edit_ModifyRegions(); break;
				case NECommand.Edit_Advanced_Convert: Configure__Edit_Advanced_Convert(); break;
				case NECommand.Edit_Advanced_Hash: Configure__Edit_Advanced_Hash(); break;
				case NECommand.Edit_Advanced_Compress: Configure__Edit_Advanced_Compress(); break;
				case NECommand.Edit_Advanced_Decompress: Configure__Edit_Advanced_Decompress(); break;
				case NECommand.Edit_Advanced_Encrypt: Configure__Edit_Advanced_Encrypt(); break;
				case NECommand.Edit_Advanced_Decrypt: Configure__Edit_Advanced_Decrypt(); break;
				case NECommand.Edit_Advanced_Sign: Configure__Edit_Advanced_Sign(); break;
				case NECommand.Text_Select_WholeWord: Configure__Text_Select_WholeWord__Text_Select_BoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: Configure__Text_Select_WholeWord__Text_Select_BoundedWord(false); break;
				case NECommand.Text_Select_Trim: Configure__Text_Select_Trim(); break;
				case NECommand.Text_Select_Split: Configure__Text_Select_Split(); break;
				case NECommand.Text_Select_Repeats_ByCount_IgnoreCase: Configure__Text_Select_Repeats_ByCount_IgnoreCase__Text_Select_Repeats_ByCount_MatchCase(); break;
				case NECommand.Text_Select_Repeats_ByCount_MatchCase: Configure__Text_Select_Repeats_ByCount_IgnoreCase__Text_Select_Repeats_ByCount_MatchCase(); break;
				case NECommand.Text_Select_ByWidth: Configure__Text_Select_ByWidth(); break;
				case NECommand.Text_Find_Find: Configure__Text_Find_Find(); break;
				case NECommand.Text_Find_RegexReplace: Configure__Text_Find_RegexReplace(); break;
				case NECommand.Text_Trim: Configure__Text_Trim(); break;
				case NECommand.Text_Width: Configure__Text_Width(); break;
				case NECommand.Text_Sort: Configure__Text_Sort(); break;
				case NECommand.Text_Random: Configure__Text_Random(); break;
				case NECommand.Text_Advanced_Unicode: Configure__Text_Advanced_Unicode(); break;
				case NECommand.Text_Advanced_FirstDistinct: Configure__Text_Advanced_FirstDistinct(); break;
				case NECommand.Text_Advanced_ReverseRegex: Configure__Text_Advanced_ReverseRegex(); break;
				case NECommand.Numeric_Select_Limit: Configure__Numeric_Select_Limit(); break;
				case NECommand.Numeric_Round: Configure__Numeric_Round(); break;
				case NECommand.Numeric_Floor: Configure__Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: Configure__Numeric_Ceiling(); break;
				case NECommand.Numeric_Scale: Configure__Numeric_Scale(); break;
				case NECommand.Numeric_Cycle: Configure__Numeric_Cycle(); break;
				case NECommand.Numeric_Series_Linear: Configure__Numeric_Series_Linear__Numeric_Series_Geometric(true); break;
				case NECommand.Numeric_Series_Geometric: Configure__Numeric_Series_Linear__Numeric_Series_Geometric(false); break;
				case NECommand.Numeric_ConvertBase_ConvertBase: Configure__Numeric_ConvertBase_ConvertBase(); break;
				case NECommand.Numeric_RandomNumber: Configure__Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: Configure__Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: Configure__Numeric_MinMaxValues(); break;
				case NECommand.Files_Select_ByContent: Configure__Files_Select_ByContent(); break;
				case NECommand.Files_Select_BySourceControlStatus: Configure__Files_Select_BySourceControlStatus(); break;
				case NECommand.Files_Copy: Configure__Files_Copy__Files_Move(false); break;
				case NECommand.Files_Move: Configure__Files_Copy__Files_Move(true); break;
				case NECommand.Files_Name_MakeAbsolute: Configure__Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: Configure__Files_Name_MakeRelative(); break;
				case NECommand.Files_Get_Hash: Configure__Files_Get_Hash(); break;
				case NECommand.Files_Get_Content: Configure__Files_Get_Content(); break;
				case NECommand.Files_Set_Size: Configure__Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: Configure__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(); break;
				case NECommand.Files_Set_Time_Access: Configure__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(); break;
				case NECommand.Files_Set_Time_Create: Configure__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(); break;
				case NECommand.Files_Set_Time_All: Configure__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(); break;
				case NECommand.Files_Set_Attributes: Configure__Files_Set_Attributes(); break;
				case NECommand.Files_Set_Content: Configure__Files_Set_Content(); break;
				case NECommand.Files_Set_Encoding: Configure__Files_Set_Encoding(); break;
				case NECommand.Files_Compress: Configure__Files_Compress(); break;
				case NECommand.Files_Decompress: Configure__Files_Decompress(); break;
				case NECommand.Files_Encrypt: Configure__Files_Encrypt(); break;
				case NECommand.Files_Decrypt: Configure__Files_Decrypt(); break;
				case NECommand.Files_Sign: Configure__Files_Sign(); break;
				case NECommand.Files_Advanced_SplitFiles: Configure__Files_Advanced_SplitFiles(); break;
				case NECommand.Files_Advanced_CombineFiles: Configure__Files_Advanced_CombineFiles(); break;
				case NECommand.Content_Ancestor: Configure__Content_Ancestor(); break;
				case NECommand.Content_Attributes: Configure__Content_Attributes(); break;
				case NECommand.Content_WithAttribute: Configure__Content_WithAttribute(); break;
				case NECommand.Content_Children_WithAttribute: Configure__Content_Children_WithAttribute(); break;
				case NECommand.Content_Descendants_WithAttribute: Configure__Content_Descendants_WithAttribute(); break;
				case NECommand.DateTime_ToTimeZone: Configure__DateTime_ToTimeZone(); break;
				case NECommand.DateTime_Format: Configure__DateTime_Format(); break;
				case NECommand.Table_Select_RowsByExpression: Configure__Table_Select_RowsByExpression(); break;
				case NECommand.Table_New_FromSelection: Configure__Table_New_FromSelection(); break;
				case NECommand.Table_Edit: Configure__Table_Edit(); break;
				case NECommand.Table_Convert: Configure__Table_Convert(); break;
				case NECommand.Table_Join: Configure__Table_Join(); break;
				case NECommand.Table_Database_GenerateInserts: Configure__Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: Configure__Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: Configure__Table_Database_GenerateDeletes(); break;
				case NECommand.Image_Size: Configure__Image_Size(); break;
				case NECommand.Image_Crop: Configure__Image_Crop(); break;
				case NECommand.Image_GrabColor: Configure__Image_GrabColor(); break;
				case NECommand.Image_GrabImage: Configure__Image_GrabImage(); break;
				case NECommand.Image_AddColor: Configure__Image_AddColor__Image_OverlayColor(true); break;
				case NECommand.Image_AdjustColor: Configure__Image_AdjustColor(); break;
				case NECommand.Image_OverlayColor: Configure__Image_AddColor__Image_OverlayColor(false); break;
				case NECommand.Image_Rotate: Configure__Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: Configure__Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: Configure__Image_GIF_Split(); break;
				case NECommand.Image_SetTakenDate: Configure__Image_SetTakenDate(); break;
				case NECommand.Position_Goto_Lines: Configure__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Line); break;
				case NECommand.Position_Goto_Columns: Configure__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: Configure__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Index); break;
				case NECommand.Position_Goto_Positions: Configure__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Position); break;
				case NECommand.Diff_IgnoreCharacters: Configure__Diff_IgnoreCharacters(); break;
				case NECommand.Diff_Fix_Whitespace: Configure__Diff_Fix_Whitespace(); break;
				case NECommand.Network_AbsoluteURL: Configure__Network_AbsoluteURL(); break;
				case NECommand.Network_Fetch_File: Configure__Network_Fetch_File(); break;
				case NECommand.Network_Fetch_Stream: Configure__Network_Fetch_Stream(); break;
				case NECommand.Network_Fetch_Playlist: Configure__Network_Fetch_Playlist(); break;
				case NECommand.Network_Ping: Configure__Network_Ping(); break;
				case NECommand.Network_ScanPorts: Configure__Network_ScanPorts(); break;
				case NECommand.Network_WCF_GetConfig: Configure__Network_WCF_GetConfig(); break;
				case NECommand.Network_WCF_InterceptCalls: Configure__Network_WCF_InterceptCalls(); break;
				case NECommand.Database_Connect: Configure__Database_Connect(); break;
				case NECommand.Database_Examine: Configure__Database_Examine(); break;
				case NECommand.Window_BinaryCodePages: Configure__Window_BinaryCodePages(); break;
			}
		}
		#endregion

		#region PreExecute
		public static bool PreExecute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Scroll: PreExecute__Internal_Scroll(); break;
				case NECommand.File_Close_Active: PreExecute__File_Close_Active__File_Close_Inactive(true); break;
				case NECommand.File_Close_Inactive: PreExecute__File_Close_Active__File_Close_Inactive(false); break;
				case NECommand.Edit_Undo_BetweenFiles_Text: PreExecute__Edit_Undo_BetweenFiles_Text__Edit_Undo_BetweenFiles_Step(true); break;
				case NECommand.Edit_Undo_BetweenFiles_Step: PreExecute__Edit_Undo_BetweenFiles_Text__Edit_Undo_BetweenFiles_Step(false); break;
				case NECommand.Edit_Redo_BetweenFiles_Text: PreExecute__Edit_Redo_BetweenFiles_Text__Edit_Redo_BetweenFiles_Step(true); break;
				case NECommand.Edit_Redo_BetweenFiles_Step: PreExecute__Edit_Redo_BetweenFiles_Text__Edit_Redo_BetweenFiles_Step(false); break;
				case NECommand.Edit_Advanced_EscapeClearsSelections: PreExecute__Edit_Advanced_EscapeClearsSelections(); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase: PreExecute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(true); break;
				case NECommand.Files_Select_BySourceControlStatus: PreExecute__Files_Select_BySourceControlStatus__Files_Get_SourceControlStatus(); break;
				case NECommand.Files_Get_SourceControlStatus: PreExecute__Files_Select_BySourceControlStatus__Files_Get_SourceControlStatus(); break;
				case NECommand.Diff_Select_LeftFile: PreExecute__Diff_Select_LeftFile__Diff_Select_RightFile__Diff_Select_BothFiles(true); break;
				case NECommand.Diff_Select_RightFile: PreExecute__Diff_Select_LeftFile__Diff_Select_RightFile__Diff_Select_BothFiles(false); break;
				case NECommand.Diff_Select_BothFiles: PreExecute__Diff_Select_LeftFile__Diff_Select_RightFile__Diff_Select_BothFiles(null); break;
				case NECommand.Diff_Diff: PreExecute__Diff_Diff(); break;
				case NECommand.Window_New_New: PreExecute__Window_New_New(); break;
				case NECommand.Window_New_FromSelections_All: PreExecute__Window_New_FromSelections_All(); break;
				case NECommand.Window_New_FromSelections_Files: PreExecute__Window_New_FromSelections_Files(); break;
				case NECommand.Window_New_FromSelections_Selections: PreExecute__Window_New_FromSelections_Selections(); break;
				case NECommand.Window_New_SummarizeSelections_Files_IgnoreCase: PreExecute__Window_New_SummarizeSelections_Files_IgnoreCase__Window_New_SummarizeSelections_Files_MatchCase__Window_New_SummarizeSelections_Selections_IgnoreCase__Window_New_SummarizeSelections_Selections_MatchCase(false, true); break;
				case NECommand.Window_New_SummarizeSelections_Files_MatchCase: PreExecute__Window_New_SummarizeSelections_Files_IgnoreCase__Window_New_SummarizeSelections_Files_MatchCase__Window_New_SummarizeSelections_Selections_IgnoreCase__Window_New_SummarizeSelections_Selections_MatchCase(true, true); break;
				case NECommand.Window_New_SummarizeSelections_Selections_IgnoreCase: PreExecute__Window_New_SummarizeSelections_Files_IgnoreCase__Window_New_SummarizeSelections_Files_MatchCase__Window_New_SummarizeSelections_Selections_IgnoreCase__Window_New_SummarizeSelections_Selections_MatchCase(false, false); break;
				case NECommand.Window_New_SummarizeSelections_Selections_MatchCase: PreExecute__Window_New_SummarizeSelections_Files_IgnoreCase__Window_New_SummarizeSelections_Files_MatchCase__Window_New_SummarizeSelections_Selections_IgnoreCase__Window_New_SummarizeSelections_Selections_MatchCase(true, false); break;
				case NECommand.Window_New_FromClipboard_All: PreExecute__Window_New_FromClipboard_All(); break;
				case NECommand.Window_New_FromClipboard_Files: PreExecute__Window_New_FromClipboard_Files(); break;
				case NECommand.Window_New_FromClipboard_Selections: PreExecute__Window_New_FromClipboard_Selections(); break;
				case NECommand.Window_New_FromFiles_Active: PreExecute__Window_New_FromFiles_Active(); break;
				case NECommand.Window_New_FromFiles_CopiedCut: PreExecute__Window_New_FromFiles_CopiedCut(); break;
				case NECommand.Window_Full: PreExecute__Window_Full(); break;
				case NECommand.Window_Grid: PreExecute__Window_Grid(); break;
				case NECommand.Window_CustomGrid: PreExecute__Window_CustomGrid(); break;
				case NECommand.Window_WorkMode: PreExecute__Window_WorkMode(); break;
				case NECommand.Window_Font_Size: PreExecute__Window_Font_Size(); break;
				case NECommand.Window_Font_ShowSpecial: PreExecute__Window_Font_ShowSpecial(); break;
				case NECommand.Help_Tutorial: PreExecute__Help_Tutorial(); break;
				case NECommand.Help_Update: PreExecute__Help_Update(); break;
				case NECommand.Help_Advanced_Shell_Integrate: PreExecute__Help_Advanced_Shell_Integrate(); break;
				case NECommand.Help_Advanced_Shell_Unintegrate: PreExecute__Help_Advanced_Shell_Unintegrate(); break;
				case NECommand.Help_Advanced_CopyCommandLine: PreExecute__Help_Advanced_CopyCommandLine(); break;
				case NECommand.Help_Advanced_RunGC: PreExecute__Help_Advanced_RunGC(); break;
				case NECommand.Help_About: PreExecute__Help_About(); break;
			}

			return false;
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: Execute__Internal_Key(); break;
				case NECommand.Internal_Text: Execute__Internal_Text(); break;
				case NECommand.Internal_SetBinaryValue: Execute__Internal_SetBinaryValue(); break;
				case NECommand.File_Open_ReopenWithEncoding: Execute__File_Open_ReopenWithEncoding(); break;
				case NECommand.File_Refresh: Execute__File_Refresh(); break;
				case NECommand.File_AutoRefresh: Execute__File_AutoRefresh(); break;
				case NECommand.File_Revert: Execute__File_Revert(); break;
				case NECommand.File_Save_SaveModified: Execute__File_Save_SaveModified(); break;
				case NECommand.File_Save_SaveAll: Execute__File_Save_SaveAll(); break;
				case NECommand.File_Save_SaveAs: Execute__File_Save_SaveAs__File_Copy_Copy(); break;
				case NECommand.File_Save_SaveAsByExpression: Execute__File_Save_SaveAsByExpression__File_Copy_CopyByExpression(); break;
				case NECommand.File_Move_Move: Execute__File_Move_Move(); break;
				case NECommand.File_Move_MoveByExpression: Execute__File_Move_MoveByExpression(); break;
				case NECommand.File_Copy_Copy: Execute__File_Save_SaveAs__File_Copy_Copy(true); break;
				case NECommand.File_Copy_CopyByExpression: Execute__File_Save_SaveAsByExpression__File_Copy_CopyByExpression(true); break;
				case NECommand.File_Copy_Path: Execute__File_Copy_Path(); break;
				case NECommand.File_Copy_Name: Execute__File_Copy_Name(); break;
				case NECommand.File_Copy_DisplayName: Execute__File_Copy_DisplayName(); break;
				case NECommand.File_Delete: Execute__File_Delete(); break;
				case NECommand.File_Encoding: Execute__File_Encoding(); break;
				case NECommand.File_LineEndings: Execute__File_LineEndings(); break;
				case NECommand.File_FileIndex: Execute__File_FileIndex__File_ActiveFileIndex(false); break;
				case NECommand.File_ActiveFileIndex: Execute__File_FileIndex__File_ActiveFileIndex(true); break;
				case NECommand.File_Advanced_Compress: Execute__File_Advanced_Compress(); break;
				case NECommand.File_Advanced_Encrypt: Execute__File_Advanced_Encrypt(); break;
				case NECommand.File_Advanced_Explore: Execute__File_Advanced_Explore(); break;
				case NECommand.File_Advanced_CommandPrompt: Execute__File_Advanced_CommandPrompt(); break;
				case NECommand.File_Advanced_DragDrop: Execute__File_Advanced_DragDrop(); break;
				case NECommand.File_Advanced_SetDisplayName: Execute__File_Advanced_SetDisplayName(); break;
				case NECommand.File_Close_WithSelections: Execute__File_Close_WithSelections__File_Close_WithoutSelections(true); break;
				case NECommand.File_Close_WithoutSelections: Execute__File_Close_WithSelections__File_Close_WithoutSelections(false); break;
				case NECommand.File_Close_Modified: Execute__File_Close_Modified__File_Close_Unmodified(true); break;
				case NECommand.File_Close_Unmodified: Execute__File_Close_Modified__File_Close_Unmodified(false); break;
				case NECommand.File_Close_ExternalModified: Execute__File_Close_ExternalModified__File_Close_ExternalUnmodified(true); break;
				case NECommand.File_Close_ExternalUnmodified: Execute__File_Close_ExternalModified__File_Close_ExternalUnmodified(false); break;
				case NECommand.Edit_Select_All: Execute__Edit_Select_All(); break;
				case NECommand.Edit_Select_Nothing: Execute__Edit_Select_Nothing(); break;
				case NECommand.Edit_Select_Join: Execute__Edit_Select_Join(); break;
				case NECommand.Edit_Select_Invert: Execute__Edit_Select_Invert(); break;
				case NECommand.Edit_Select_Limit: Execute__Edit_Select_Limit(); break;
				case NECommand.Edit_Select_Lines: Execute__Edit_Select_Lines(); break;
				case NECommand.Edit_Select_WholeLines: Execute__Edit_Select_WholeLines(); break;
				case NECommand.Edit_Select_Empty: Execute__Edit_Select_Empty__Edit_Select_NonEmpty(true); break;
				case NECommand.Edit_Select_NonEmpty: Execute__Edit_Select_Empty__Edit_Select_NonEmpty(false); break;
				case NECommand.Edit_Select_AllowOverlappingSelections: Execute__Edit_Select_AllowOverlappingSelections(); break;
				case NECommand.Edit_Select_ToggleAnchor: Execute__Edit_Select_ToggleAnchor(); break;
				case NECommand.Edit_Select_Focused_First: Execute__Edit_Select_Focused_First(); break;
				case NECommand.Edit_Select_Focused_Next: Execute__Edit_Select_Focused_Next__Edit_Select_Focused_Previous(true); break;
				case NECommand.Edit_Select_Focused_Previous: Execute__Edit_Select_Focused_Next__Edit_Select_Focused_Previous(false); break;
				case NECommand.Edit_Select_Focused_Single: Execute__Edit_Select_Focused_Single(); break;
				case NECommand.Edit_Select_Focused_Remove: Execute__Edit_Select_Focused_Remove(); break;
				case NECommand.Edit_Select_Focused_RemoveBeforeCurrent: Execute__Edit_Select_Focused_RemoveBeforeCurrent(); break;
				case NECommand.Edit_Select_Focused_RemoveAfterCurrent: Execute__Edit_Select_Focused_RemoveAfterCurrent(); break;
				case NECommand.Edit_Select_Focused_CenterVertically: Execute__Edit_Select_Focused_CenterVertically(); break;
				case NECommand.Edit_Select_Focused_Center: Execute__Edit_Select_Focused_Center(); break;
				case NECommand.Edit_Copy: Execute__Edit_Copy__Edit_Cut(false); break;
				case NECommand.Edit_Cut: Execute__Edit_Copy__Edit_Cut(true); break;
				case NECommand.Edit_Paste_Paste: Execute__Edit_Paste_Paste__Edit_Paste_RotatePaste(state.ShiftDown, false); break;
				case NECommand.Edit_Paste_RotatePaste: Execute__Edit_Paste_Paste__Edit_Paste_RotatePaste(true, true); break;
				case NECommand.Edit_Undo_Text: Execute__Edit_Undo_Text__Edit_Undo_Step(true); break;
				case NECommand.Edit_Undo_Step: Execute__Edit_Undo_Text__Edit_Undo_Step(false); break;
				case NECommand.Edit_Redo_Text: Execute__Edit_Redo_Text__Edit_Redo_Step(true); break;
				case NECommand.Edit_Redo_Step: Execute__Edit_Redo_Text__Edit_Redo_Step(false); break;
				case NECommand.Edit_Repeat: Execute__Edit_Repeat(); break;
				case NECommand.Edit_Rotate: Execute__Edit_Rotate(); break;
				case NECommand.Edit_Expression_Expression: Execute__Edit_Expression_Expression(); break;
				case NECommand.Edit_Expression_EvaluateSelected: Execute__Edit_Expression_EvaluateSelected(); break;
				case NECommand.Edit_ModifyRegions: Execute__Edit_ModifyRegions(state.Configuration as Configuration_Edit_ModifyRegions); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Select, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Next, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region1: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region2: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region3: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region4: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region5: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region6: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region7: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region8: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region9: Execute__Edit_ModifyRegions(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 9); break;
				case NECommand.Edit_Navigate_WordLeft: Execute__Edit_Navigate_WordLeft__Edit_Navigate_WordRight(false); break;
				case NECommand.Edit_Navigate_WordRight: Execute__Edit_Navigate_WordLeft__Edit_Navigate_WordRight(true); break;
				case NECommand.Edit_Navigate_AllLeft: Execute__Edit_Navigate_AllLeft(); break;
				case NECommand.Edit_Navigate_AllRight: Execute__Edit_Navigate_AllRight(); break;
				case NECommand.Edit_Navigate_JumpBy_Words: Execute__Edit_Navigate_JumpBy_Words__Edit_Navigate_JumpBy_Numbers__Edit_Navigate_JumpBy_Paths(JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: Execute__Edit_Navigate_JumpBy_Words__Edit_Navigate_JumpBy_Numbers__Edit_Navigate_JumpBy_Paths(JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: Execute__Edit_Navigate_JumpBy_Words__Edit_Navigate_JumpBy_Numbers__Edit_Navigate_JumpBy_Paths(JumpByType.Paths); break;
				case NECommand.Edit_RepeatCount: Execute__Edit_RepeatCount(); break;
				case NECommand.Edit_RepeatIndex: Execute__Edit_RepeatIndex(); break;
				case NECommand.Edit_Advanced_Convert: Execute__Edit_Advanced_Convert(); break;
				case NECommand.Edit_Advanced_Hash: Execute__Edit_Advanced_Hash(); break;
				case NECommand.Edit_Advanced_Compress: Execute__Edit_Advanced_Compress(); break;
				case NECommand.Edit_Advanced_Decompress: Execute__Edit_Advanced_Decompress(); break;
				case NECommand.Edit_Advanced_Encrypt: Execute__Edit_Advanced_Encrypt(); break;
				case NECommand.Edit_Advanced_Decrypt: Execute__Edit_Advanced_Decrypt(); break;
				case NECommand.Edit_Advanced_Sign: Execute__Edit_Advanced_Sign(); break;
				case NECommand.Edit_Advanced_RunCommand_Parallel: Execute__Edit_Advanced_RunCommand_Parallel(); break;
				case NECommand.Edit_Advanced_RunCommand_Sequential: Execute__Edit_Advanced_RunCommand_Sequential(); break;
				case NECommand.Edit_Advanced_RunCommand_Shell: Execute__Edit_Advanced_RunCommand_Shell(); break;
				case NECommand.Text_Select_WholeWord: Execute__Text_Select_WholeWord__Text_Select_BoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: Execute__Text_Select_WholeWord__Text_Select_BoundedWord(false); break;
				case NECommand.Text_Select_Trim: Execute__Text_Select_Trim(); break;
				case NECommand.Text_Select_Split: Execute__Text_Select_Split(); break;
				case NECommand.Text_Select_Repeats_Unique_IgnoreCase: Execute__Text_Select_Repeats_Unique_IgnoreCase__Text_Select_Repeats_Unique_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_Unique_MatchCase: Execute__Text_Select_Repeats_Unique_IgnoreCase__Text_Select_Repeats_Unique_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_Duplicates_IgnoreCase: Execute__Text_Select_Repeats_Duplicates_IgnoreCase__Text_Select_Repeats_Duplicates_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_Duplicates_MatchCase: Execute__Text_Select_Repeats_Duplicates_IgnoreCase__Text_Select_Repeats_Duplicates_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_NonMatchPrevious_IgnoreCase: Execute__Text_Select_Repeats_NonMatchPrevious_IgnoreCase__Text_Select_Repeats_NonMatchPrevious_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_NonMatchPrevious_MatchCase: Execute__Text_Select_Repeats_NonMatchPrevious_IgnoreCase__Text_Select_Repeats_NonMatchPrevious_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_MatchPrevious_IgnoreCase: Execute__Text_Select_Repeats_MatchPrevious_IgnoreCase__Text_Select_Repeats_MatchPrevious_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_MatchPrevious_MatchCase: Execute__Text_Select_Repeats_MatchPrevious_IgnoreCase__Text_Select_Repeats_MatchPrevious_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_ByCount_IgnoreCase: Execute__Text_Select_Repeats_ByCount_IgnoreCase__Text_Select_Repeats_ByCount_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_ByCount_MatchCase: Execute__Text_Select_Repeats_ByCount_IgnoreCase__Text_Select_Repeats_ByCount_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Ordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Ordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase: Execute__Text_Select_Repeats_BetweenFiles_Matches_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Matches_Unordered_MatchCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_IgnoreCase__Text_Select_Repeats_BetweenFiles_Diffs_Unordered_MatchCase(false); break;
				case NECommand.Text_Select_ByWidth: Execute__Text_Select_ByWidth(); break;
				case NECommand.Text_Select_Min_Text: Execute__Text_Select_Min_Text__Text_Select_Max_Text(false); break;
				case NECommand.Text_Select_Min_Length: Execute__Text_Select_Min_Length__Text_Select_Max_Length(false); break;
				case NECommand.Text_Select_Max_Text: Execute__Text_Select_Min_Text__Text_Select_Max_Text(true); break;
				case NECommand.Text_Select_Max_Length: Execute__Text_Select_Min_Length__Text_Select_Max_Length(true); break;
				case NECommand.Text_Select_ToggleOpenClose: Execute__Text_Select_ToggleOpenClose(); break;
				case NECommand.Text_Find_Find: Execute__Text_Find_Find(); break;
				case NECommand.Text_Find_RegexReplace: Execute__Text_Find_RegexReplace(); break;
				case NECommand.Text_Trim: Execute__Text_Trim(); break;
				case NECommand.Text_Width: Execute__Text_Width(); break;
				case NECommand.Text_SingleLine: Execute__Text_SingleLine(); break;
				case NECommand.Text_Case_Upper: Execute__Text_Case_Upper(); break;
				case NECommand.Text_Case_Lower: Execute__Text_Case_Lower(); break;
				case NECommand.Text_Case_Proper: Execute__Text_Case_Proper(); break;
				case NECommand.Text_Case_Invert: Execute__Text_Case_Invert(); break;
				case NECommand.Text_Sort: Execute__Text_Sort(); break;
				case NECommand.Text_Escape_Markup: Execute__Text_Escape_Markup(); break;
				case NECommand.Text_Escape_Regex: Execute__Text_Escape_Regex(); break;
				case NECommand.Text_Escape_URL: Execute__Text_Escape_URL(); break;
				case NECommand.Text_Unescape_Markup: Execute__Text_Unescape_Markup(); break;
				case NECommand.Text_Unescape_Regex: Execute__Text_Unescape_Regex(); break;
				case NECommand.Text_Unescape_URL: Execute__Text_Unescape_URL(); break;
				case NECommand.Text_Random: Execute__Text_Random(); break;
				case NECommand.Text_Advanced_Unicode: Execute__Text_Advanced_Unicode(); break;
				case NECommand.Text_Advanced_FirstDistinct: Execute__Text_Advanced_FirstDistinct(); break;
				case NECommand.Text_Advanced_GUID: Execute__Text_Advanced_GUID(); break;
				case NECommand.Text_Advanced_ReverseRegex: Execute__Text_Advanced_ReverseRegex(); break;
				case NECommand.Numeric_Select_Min: Execute__Numeric_Select_Min__Numeric_Select_Max(false); break;
				case NECommand.Numeric_Select_Max: Execute__Numeric_Select_Min__Numeric_Select_Max(true); break;
				case NECommand.Numeric_Select_Limit: Execute__Numeric_Select_Limit(); break;
				case NECommand.Numeric_Round: Execute__Numeric_Round(); break;
				case NECommand.Numeric_Floor: Execute__Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: Execute__Numeric_Ceiling(); break;
				case NECommand.Numeric_Sum_Sum: Execute__Numeric_Sum_Sum(); break;
				case NECommand.Numeric_Sum_Increment: Execute__Numeric_Sum_Increment__Numeric_Sum_Decrement(true); break;
				case NECommand.Numeric_Sum_Decrement: Execute__Numeric_Sum_Increment__Numeric_Sum_Decrement(false); break;
				case NECommand.Numeric_Sum_AddClipboard: Execute__Numeric_Sum_AddClipboard__Numeric_Sum_SubtractClipboard(true); break;
				case NECommand.Numeric_Sum_SubtractClipboard: Execute__Numeric_Sum_AddClipboard__Numeric_Sum_SubtractClipboard(false); break;
				case NECommand.Numeric_Sum_ForwardSum: Execute__Numeric_Sum_ForwardSum__Numeric_Sum_UndoForwardSum__Numeric_Sum_ReverseSum__Numeric_Sum_UndoReverseSum(true, false); break;
				case NECommand.Numeric_Sum_UndoForwardSum: Execute__Numeric_Sum_ForwardSum__Numeric_Sum_UndoForwardSum__Numeric_Sum_ReverseSum__Numeric_Sum_UndoReverseSum(true, true); break;
				case NECommand.Numeric_Sum_ReverseSum: Execute__Numeric_Sum_ForwardSum__Numeric_Sum_UndoForwardSum__Numeric_Sum_ReverseSum__Numeric_Sum_UndoReverseSum(false, false); break;
				case NECommand.Numeric_Sum_UndoReverseSum: Execute__Numeric_Sum_ForwardSum__Numeric_Sum_UndoForwardSum__Numeric_Sum_ReverseSum__Numeric_Sum_UndoReverseSum(false, true); break;
				case NECommand.Numeric_AbsoluteValue: Execute__Numeric_AbsoluteValue(); break;
				case NECommand.Numeric_Scale: Execute__Numeric_Scale(); break;
				case NECommand.Numeric_Cycle: Execute__Numeric_Cycle(); break;
				case NECommand.Numeric_Trim: Execute__Numeric_Trim(); break;
				case NECommand.Numeric_Fraction: Execute__Numeric_Fraction(); break;
				case NECommand.Numeric_Factor: Execute__Numeric_Factor(); break;
				case NECommand.Numeric_Series_ZeroBased: Execute__Numeric_Series_ZeroBased(); break;
				case NECommand.Numeric_Series_OneBased: Execute__Numeric_Series_OneBased(); break;
				case NECommand.Numeric_Series_Linear: Execute__Numeric_Series_Linear__Numeric_Series_Geometric(true); break;
				case NECommand.Numeric_Series_Geometric: Execute__Numeric_Series_Linear__Numeric_Series_Geometric(false); break;
				case NECommand.Numeric_ConvertBase_ToHex: Execute__Numeric_ConvertBase_ToHex(); break;
				case NECommand.Numeric_ConvertBase_FromHex: Execute__Numeric_ConvertBase_FromHex(); break;
				case NECommand.Numeric_ConvertBase_ConvertBase: Execute__Numeric_ConvertBase_ConvertBase(); break;
				case NECommand.Numeric_RandomNumber: Execute__Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: Execute__Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: Execute__Numeric_MinMaxValues(); break;
				case NECommand.Files_Select_Files: Execute__Files_Select_Files(); break;
				case NECommand.Files_Select_Directories: Execute__Files_Select_Directories(); break;
				case NECommand.Files_Select_Existing: Execute__Files_Select_Existing__Files_Select_NonExisting(true); break;
				case NECommand.Files_Select_NonExisting: Execute__Files_Select_Existing__Files_Select_NonExisting(false); break;
				case NECommand.Files_Select_Name_Directory: Execute__Files_Select_Name_Directory__Files_Select_Name_Name__Files_Select_Name_NameWOExtension__Files_Select_Name_Extension(GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: Execute__Files_Select_Name_Directory__Files_Select_Name_Name__Files_Select_Name_NameWOExtension__Files_Select_Name_Extension(GetPathType.FileName); break;
				case NECommand.Files_Select_Name_NameWOExtension: Execute__Files_Select_Name_Directory__Files_Select_Name_Name__Files_Select_Name_NameWOExtension__Files_Select_Name_Extension(GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: Execute__Files_Select_Name_Directory__Files_Select_Name_Name__Files_Select_Name_NameWOExtension__Files_Select_Name_Extension(GetPathType.Extension); break;
				case NECommand.Files_Select_Name_Next: Execute__Files_Select_Name_Next(); break;
				case NECommand.Files_Select_Name_CommonAncestor: Execute__Files_Select_Name_CommonAncestor(); break;
				case NECommand.Files_Select_Name_MatchDepth: Execute__Files_Select_Name_MatchDepth(); break;
				case NECommand.Files_Select_Roots: Execute__Files_Select_Roots__Files_Select_NonRoots(true); break;
				case NECommand.Files_Select_NonRoots: Execute__Files_Select_Roots__Files_Select_NonRoots(false); break;
				case NECommand.Files_Select_ByContent: Execute__Files_Select_ByContent(); break;
				case NECommand.Files_Select_BySourceControlStatus: Execute__Files_Select_BySourceControlStatus(); break;
				case NECommand.Files_Copy: Execute__Files_Copy__Files_Move(false); break;
				case NECommand.Files_Move: Execute__Files_Copy__Files_Move(true); break;
				case NECommand.Files_Delete: Execute__Files_Delete(); break;
				case NECommand.Files_Name_MakeAbsolute: Execute__Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: Execute__Files_Name_MakeRelative(); break;
				case NECommand.Files_Name_Simplify: Execute__Files_Name_Simplify(); break;
				case NECommand.Files_Name_Sanitize: Execute__Files_Name_Sanitize(); break;
				case NECommand.Files_Get_Size: Execute__Files_Get_Size(); break;
				case NECommand.Files_Get_Time_Write: Execute__Files_Get_Time_Write__Files_Get_Time_Access__Files_Get_Time_Create(TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: Execute__Files_Get_Time_Write__Files_Get_Time_Access__Files_Get_Time_Create(TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: Execute__Files_Get_Time_Write__Files_Get_Time_Access__Files_Get_Time_Create(TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: Execute__Files_Get_Attributes(); break;
				case NECommand.Files_Get_Version_File: Execute__Files_Get_Version_File(); break;
				case NECommand.Files_Get_Version_Product: Execute__Files_Get_Version_Product(); break;
				case NECommand.Files_Get_Version_Assembly: Execute__Files_Get_Version_Assembly(); break;
				case NECommand.Files_Get_Hash: Execute__Files_Get_Hash(); break;
				case NECommand.Files_Get_SourceControlStatus: Execute__Files_Get_SourceControlStatus(); break;
				case NECommand.Files_Get_Children: Execute__Files_Get_Children__Files_Get_Descendants(false); break;
				case NECommand.Files_Get_Descendants: Execute__Files_Get_Children__Files_Get_Descendants(true); break;
				case NECommand.Files_Get_Content: Execute__Files_Get_Content(); break;
				case NECommand.Files_Set_Size: Execute__Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: Execute__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(TimestampType.Write); break;
				case NECommand.Files_Set_Time_Access: Execute__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(TimestampType.Access); break;
				case NECommand.Files_Set_Time_Create: Execute__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(TimestampType.Create); break;
				case NECommand.Files_Set_Time_All: Execute__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(TimestampType.All); break;
				case NECommand.Files_Set_Attributes: Execute__Files_Set_Attributes(); break;
				case NECommand.Files_Set_Content: Execute__Files_Set_Content(); break;
				case NECommand.Files_Set_Encoding: Execute__Files_Set_Encoding(); break;
				case NECommand.Files_Create_Files: Execute__Files_Create_Files(); break;
				case NECommand.Files_Create_Directories: Execute__Files_Create_Directories(); break;
				case NECommand.Files_Compress: Execute__Files_Compress(); break;
				case NECommand.Files_Decompress: Execute__Files_Decompress(); break;
				case NECommand.Files_Encrypt: Execute__Files_Encrypt(); break;
				case NECommand.Files_Decrypt: Execute__Files_Decrypt(); break;
				case NECommand.Files_Sign: Execute__Files_Sign(); break;
				case NECommand.Files_Advanced_Explore: Execute__Files_Advanced_Explore(); break;
				case NECommand.Files_Advanced_CommandPrompt: Execute__Files_Advanced_CommandPrompt(); break;
				case NECommand.Files_Advanced_DragDrop: Execute__Files_Advanced_DragDrop(); break;
				case NECommand.Files_Advanced_SplitFiles: Execute__Files_Advanced_SplitFiles(); break;
				case NECommand.Files_Advanced_CombineFiles: Execute__Files_Advanced_CombineFiles(); break;
				case NECommand.Content_Type_SetFromExtension: Execute__Content_Type_SetFromExtension(); break;
				case NECommand.Content_Type_None: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.None); break;
				case NECommand.Content_Type_Balanced: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.HTML); break;
				case NECommand.Content_Type_JSON: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.JSON); break;
				case NECommand.Content_Type_SQL: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.SQL); break;
				case NECommand.Content_Type_TSV: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.TSV); break;
				case NECommand.Content_Type_XML: Execute__Content_Type_None__Content_Type_Balanced__Content_Type_Columns__Content_Type_CPlusPlus__Content_Type_CSharp__Content_Type_CSV__Content_Type_ExactColumns__Content_Type_HTML__Content_Type_JSON__Content_Type_SQL__Content_Type_TSV__Content_Type_XML(ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: Execute__Content_HighlightSyntax(); break;
				case NECommand.Content_StrictParsing: Execute__Content_StrictParsing(); break;
				case NECommand.Content_Reformat: Execute__Content_Reformat(); break;
				case NECommand.Content_Comment: Execute__Content_Comment(); break;
				case NECommand.Content_Uncomment: Execute__Content_Uncomment(); break;
				case NECommand.Content_Copy: Execute__Content_Copy(); break;
				case NECommand.Content_TogglePosition: Execute__Content_TogglePosition(); break;
				case NECommand.Content_Current: Execute__Content_Current(); break;
				case NECommand.Content_Parent: Execute__Content_Parent(); break;
				case NECommand.Content_Ancestor: Execute__Content_Ancestor(); break;
				case NECommand.Content_Attributes: Execute__Content_Attributes(); break;
				case NECommand.Content_WithAttribute: Execute__Content_WithAttribute(); break;
				case NECommand.Content_Children_Children: Execute__Content_Children_Children(); break;
				case NECommand.Content_Children_SelfAndChildren: Execute__Content_Children_SelfAndChildren(); break;
				case NECommand.Content_Children_First: Execute__Content_Children_First(); break;
				case NECommand.Content_Children_WithAttribute: Execute__Content_Children_WithAttribute(); break;
				case NECommand.Content_Descendants_Descendants: Execute__Content_Descendants_Descendants(); break;
				case NECommand.Content_Descendants_SelfAndDescendants: Execute__Content_Descendants_SelfAndDescendants(); break;
				case NECommand.Content_Descendants_First: Execute__Content_Descendants_First(); break;
				case NECommand.Content_Descendants_WithAttribute: Execute__Content_Descendants_WithAttribute(); break;
				case NECommand.Content_Navigate_Up: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Up, state.ShiftDown); break;
				case NECommand.Content_Navigate_Down: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Down, state.ShiftDown); break;
				case NECommand.Content_Navigate_Left: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Left, state.ShiftDown); break;
				case NECommand.Content_Navigate_Right: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Right, state.ShiftDown); break;
				case NECommand.Content_Navigate_Home: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Home, state.ShiftDown); break;
				case NECommand.Content_Navigate_End: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.End, state.ShiftDown); break;
				case NECommand.Content_Navigate_Pgup: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.PgUp, state.ShiftDown); break;
				case NECommand.Content_Navigate_Pgdn: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.PgDn, state.ShiftDown); break;
				case NECommand.Content_Navigate_Row: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: Execute__Content_Navigate_Up__Content_Navigate_Down__Content_Navigate_Left__Content_Navigate_Right__Content_Navigate_Home__Content_Navigate_End__Content_Navigate_Pgup__Content_Navigate_Pgdn__Content_Navigate_Row__Content_Navigate_Column(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: Execute__Content_KeepSelections(); break;
				case NECommand.DateTime_Now: Execute__DateTime_Now(); break;
				case NECommand.DateTime_UTCNow: Execute__DateTime_UTCNow(); break;
				case NECommand.DateTime_ToUTC: Execute__DateTime_ToUTC(); break;
				case NECommand.DateTime_ToLocal: Execute__DateTime_ToLocal(); break;
				case NECommand.DateTime_ToTimeZone: Execute__DateTime_ToTimeZone(); break;
				case NECommand.DateTime_Format: Execute__DateTime_Format(); break;
				case NECommand.DateTime_AddClipboard: Execute__DateTime_AddClipboard(); break;
				case NECommand.DateTime_SubtractClipboard: Execute__DateTime_SubtractClipboard(); break;
				case NECommand.Table_Select_RowsByExpression: Execute__Table_Select_RowsByExpression(); break;
				case NECommand.Table_New_FromSelection: Execute__Table_New_FromSelection(); break;
				case NECommand.Table_New_FromLineSelections: Execute__Table_New_FromLineSelections(); break;
				case NECommand.Table_New_FromRegionSelections_Region1: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(1); break;
				case NECommand.Table_New_FromRegionSelections_Region2: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(2); break;
				case NECommand.Table_New_FromRegionSelections_Region3: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(3); break;
				case NECommand.Table_New_FromRegionSelections_Region4: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(4); break;
				case NECommand.Table_New_FromRegionSelections_Region5: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(5); break;
				case NECommand.Table_New_FromRegionSelections_Region6: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(6); break;
				case NECommand.Table_New_FromRegionSelections_Region7: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(7); break;
				case NECommand.Table_New_FromRegionSelections_Region8: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(8); break;
				case NECommand.Table_New_FromRegionSelections_Region9: Execute__Table_New_FromRegionSelections_Region1__Table_New_FromRegionSelections_Region2__Table_New_FromRegionSelections_Region3__Table_New_FromRegionSelections_Region4__Table_New_FromRegionSelections_Region5__Table_New_FromRegionSelections_Region6__Table_New_FromRegionSelections_Region7__Table_New_FromRegionSelections_Region8__Table_New_FromRegionSelections_Region9(9); break;
				case NECommand.Table_Edit: Execute__Table_Edit(); break;
				case NECommand.Table_DetectType: Execute__Table_DetectType(); break;
				case NECommand.Table_Convert: Execute__Table_Convert(); break;
				case NECommand.Table_SetJoinSource: Execute__Table_SetJoinSource(); break;
				case NECommand.Table_Join: Execute__Table_Join(); break;
				case NECommand.Table_Transpose: Execute__Table_Transpose(); break;
				case NECommand.Table_Database_GenerateInserts: Execute__Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: Execute__Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: Execute__Table_Database_GenerateDeletes(); break;
				case NECommand.Image_Size: Execute__Image_Size(); break;
				case NECommand.Image_Crop: Execute__Image_Crop(); break;
				case NECommand.Image_GrabColor: Execute__Image_GrabColor(); break;
				case NECommand.Image_GrabImage: Execute__Image_GrabImage(); break;
				case NECommand.Image_AddColor: Execute__Image_AddColor(); break;
				case NECommand.Image_AdjustColor: Execute__Image_AdjustColor(); break;
				case NECommand.Image_OverlayColor: Execute__Image_OverlayColor(); break;
				case NECommand.Image_FlipHorizontal: Execute__Image_FlipHorizontal(); break;
				case NECommand.Image_FlipVertical: Execute__Image_FlipVertical(); break;
				case NECommand.Image_Rotate: Execute__Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: Execute__Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: Execute__Image_GIF_Split(); break;
				case NECommand.Image_GetTakenDate: Execute__Image_GetTakenDate(); break;
				case NECommand.Image_SetTakenDate: Execute__Image_SetTakenDate(); break;
				case NECommand.Position_Goto_Lines: Execute__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Line, state.ShiftDown); break;
				case NECommand.Position_Goto_Columns: Execute__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Column, state.ShiftDown); break;
				case NECommand.Position_Goto_Indexes: Execute__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Index, state.ShiftDown); break;
				case NECommand.Position_Goto_Positions: Execute__Position_Goto_Lines__Position_Goto_Columns__Position_Goto_Indexes__Position_Goto_Positions(GotoType.Position, state.ShiftDown); break;
				case NECommand.Position_Copy_Lines: Execute__Position_Copy_Lines__Position_Copy_Columns__Position_Copy_Indexes__Position_Copy_Positions(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Execute__Position_Copy_Lines__Position_Copy_Columns__Position_Copy_Indexes__Position_Copy_Positions(GotoType.Column, !state.ShiftDown); break;
				case NECommand.Position_Copy_Indexes: Execute__Position_Copy_Lines__Position_Copy_Columns__Position_Copy_Indexes__Position_Copy_Positions(GotoType.Index, !state.ShiftDown); break;
				case NECommand.Position_Copy_Positions: Execute__Position_Copy_Lines__Position_Copy_Columns__Position_Copy_Indexes__Position_Copy_Positions(GotoType.Position, false); break;
				case NECommand.Diff_Select_Matches: Execute__Diff_Select_Matches__Diff_Select_Diffs(true); break;
				case NECommand.Diff_Select_Diffs: Execute__Diff_Select_Matches__Diff_Select_Diffs(false); break;
				case NECommand.Diff_Break: Execute__Diff_Break(); break;
				case NECommand.Diff_SourceControl: Execute__Diff_SourceControl(); break;
				case NECommand.Diff_IgnoreWhitespace: Execute__Diff_IgnoreWhitespace(state.MultiStatus); break;
				case NECommand.Diff_IgnoreCase: Execute__Diff_IgnoreCase(state.MultiStatus); break;
				case NECommand.Diff_IgnoreNumbers: Execute__Diff_IgnoreNumbers(state.MultiStatus); break;
				case NECommand.Diff_IgnoreLineEndings: Execute__Diff_IgnoreLineEndings(state.MultiStatus); break;
				case NECommand.Diff_IgnoreCharacters: Execute__Diff_IgnoreCharacters(); break;
				case NECommand.Diff_Reset: Execute__Diff_Reset(); break;
				case NECommand.Diff_Next: Execute__Diff_Next__Diff_Previous(true, state.ShiftDown); break;
				case NECommand.Diff_Previous: Execute__Diff_Next__Diff_Previous(false, state.ShiftDown); break;
				case NECommand.Diff_CopyLeft: Execute__Diff_CopyLeft__Diff_CopyRight(true); break;
				case NECommand.Diff_CopyRight: Execute__Diff_CopyLeft__Diff_CopyRight(false); break;
				case NECommand.Diff_Fix_Whitespace: Execute__Diff_Fix_Whitespace(); break;
				case NECommand.Diff_Fix_Case: Execute__Diff_Fix_Case(); break;
				case NECommand.Diff_Fix_Numbers: Execute__Diff_Fix_Numbers(); break;
				case NECommand.Diff_Fix_LineEndings: Execute__Diff_Fix_LineEndings(); break;
				case NECommand.Diff_Fix_Encoding: Execute__Diff_Fix_Encoding(); break;
				case NECommand.Network_AbsoluteURL: Execute__Network_AbsoluteURL(); break;
				case NECommand.Network_Fetch_Fetch: Execute__Network_Fetch_Fetch__Network_Fetch_Hex(); break;
				case NECommand.Network_Fetch_Hex: Execute__Network_Fetch_Fetch__Network_Fetch_Hex(Coder.CodePage.Hex); break;
				case NECommand.Network_Fetch_File: Execute__Network_Fetch_File(); break;
				case NECommand.Network_Fetch_Custom: Execute__Network_Fetch_Custom(); break;
				case NECommand.Network_Fetch_Stream: Execute__Network_Fetch_Stream(); break;
				case NECommand.Network_Fetch_Playlist: Execute__Network_Fetch_Playlist(); break;
				case NECommand.Network_Lookup_IP: Execute__Network_Lookup_IP(); break;
				case NECommand.Network_Lookup_Hostname: Execute__Network_Lookup_Hostname(); break;
				case NECommand.Network_AdaptersInfo: Execute__Network_AdaptersInfo(); break;
				case NECommand.Network_Ping: Execute__Network_Ping(); break;
				case NECommand.Network_ScanPorts: Execute__Network_ScanPorts(); break;
				case NECommand.Network_WCF_GetConfig: Execute__Network_WCF_GetConfig(); break;
				case NECommand.Network_WCF_Execute: Execute__Network_WCF_Execute(); break;
				case NECommand.Network_WCF_InterceptCalls: Execute__Network_WCF_InterceptCalls(); break;
				case NECommand.Network_WCF_ResetClients: Execute__Network_WCF_ResetClients(); break;
				case NECommand.Database_Connect: Execute__Database_Connect(); break;
				case NECommand.Database_ExecuteQuery: Execute__Database_ExecuteQuery(); break;
				case NECommand.Database_GetSproc: Execute__Database_GetSproc(); break;
				case NECommand.KeyValue_Set_Keys_IgnoreCase: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(0, false); break;
				case NECommand.KeyValue_Set_Keys_MatchCase: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(0, true); break;
				case NECommand.KeyValue_Set_Values1: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(1); break;
				case NECommand.KeyValue_Set_Values2: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(2); break;
				case NECommand.KeyValue_Set_Values3: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(3); break;
				case NECommand.KeyValue_Set_Values4: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(4); break;
				case NECommand.KeyValue_Set_Values5: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(5); break;
				case NECommand.KeyValue_Set_Values6: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(6); break;
				case NECommand.KeyValue_Set_Values7: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(7); break;
				case NECommand.KeyValue_Set_Values8: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(8); break;
				case NECommand.KeyValue_Set_Values9: Execute__KeyValue_Set_Keys_IgnoreCase__KeyValue_Set_Keys_MatchCase__KeyValue_Set_Values1__KeyValue_Set_Values2__KeyValue_Set_Values3__KeyValue_Set_Values4__KeyValue_Set_Values5__KeyValue_Set_Values6__KeyValue_Set_Values7__KeyValue_Set_Values8__KeyValue_Set_Values9(9); break;
				case NECommand.KeyValue_Add_Keys: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(0); break;
				case NECommand.KeyValue_Add_Values1: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(1); break;
				case NECommand.KeyValue_Add_Values2: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(2); break;
				case NECommand.KeyValue_Add_Values3: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(3); break;
				case NECommand.KeyValue_Add_Values4: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(4); break;
				case NECommand.KeyValue_Add_Values5: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(5); break;
				case NECommand.KeyValue_Add_Values6: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(6); break;
				case NECommand.KeyValue_Add_Values7: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(7); break;
				case NECommand.KeyValue_Add_Values8: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(8); break;
				case NECommand.KeyValue_Add_Values9: Execute__KeyValue_Add_Keys__KeyValue_Add_Values1__KeyValue_Add_Values2__KeyValue_Add_Values3__KeyValue_Add_Values4__KeyValue_Add_Values5__KeyValue_Add_Values6__KeyValue_Add_Values7__KeyValue_Add_Values8__KeyValue_Add_Values9(9); break;
				case NECommand.KeyValue_Remove_Keys: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(0); break;
				case NECommand.KeyValue_Remove_Values1: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(1); break;
				case NECommand.KeyValue_Remove_Values2: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(2); break;
				case NECommand.KeyValue_Remove_Values3: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(3); break;
				case NECommand.KeyValue_Remove_Values4: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(4); break;
				case NECommand.KeyValue_Remove_Values5: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(5); break;
				case NECommand.KeyValue_Remove_Values6: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(6); break;
				case NECommand.KeyValue_Remove_Values7: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(7); break;
				case NECommand.KeyValue_Remove_Values8: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(8); break;
				case NECommand.KeyValue_Remove_Values9: Execute__KeyValue_Remove_Keys__KeyValue_Remove_Values1__KeyValue_Remove_Values2__KeyValue_Remove_Values3__KeyValue_Remove_Values4__KeyValue_Remove_Values5__KeyValue_Remove_Values6__KeyValue_Remove_Values7__KeyValue_Remove_Values8__KeyValue_Remove_Values9(9); break;
				case NECommand.KeyValue_Replace_Values1: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(1); break;
				case NECommand.KeyValue_Replace_Values2: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(2); break;
				case NECommand.KeyValue_Replace_Values3: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(3); break;
				case NECommand.KeyValue_Replace_Values4: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(4); break;
				case NECommand.KeyValue_Replace_Values5: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(5); break;
				case NECommand.KeyValue_Replace_Values6: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(6); break;
				case NECommand.KeyValue_Replace_Values7: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(7); break;
				case NECommand.KeyValue_Replace_Values8: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(8); break;
				case NECommand.KeyValue_Replace_Values9: Execute__KeyValue_Replace_Values1__KeyValue_Replace_Values2__KeyValue_Replace_Values3__KeyValue_Replace_Values4__KeyValue_Replace_Values5__KeyValue_Replace_Values6__KeyValue_Replace_Values7__KeyValue_Replace_Values8__KeyValue_Replace_Values9(9); break;
				case NECommand.Window_ViewBinary: Execute__Window_ViewBinary(); break;
				case NECommand.Window_BinaryCodePages: Execute__Window_BinaryCodePages(); break;
			}
		}
		#endregion

		#region PostExecute
		public void PostExecute()
		{
		}
		#endregion

		#region Watcher
		FileSystemWatcher watcher = null;

		void SetupWatcher()
		{
			watcher?.Dispose();
			watcher = null;

			void SetLastExternalWriteTime() => LastExternalWriteTime = GetFileWriteTime(FileName);

			SetLastExternalWriteTime();
			if ((NEWindow == null) || (string.IsNullOrWhiteSpace(FileName)))
				return;

			void FileChanged()
			{
				SetLastExternalWriteTime();
				if (AutoRefresh)
					try { NEWindow?.neWindowUI?.SendActivateIfActive(); } catch { }
				else
					LastActivatedTime = LastExternalWriteTime;
			}

			watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(FileName),
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
				Filter = Path.GetFileName(FileName),
			};
			watcher.Changed += (s1, e1) => FileChanged();
			watcher.Deleted += (s1, e1) => FileChanged();
			watcher.Renamed += (s1, e1) => FileChanged();
			watcher.Created += (s1, e1) => FileChanged();
			watcher.EnableRaisingEvents = true;
		}
		#endregion

		void SetIsModified(bool? newValue = null)
		{
			if (newValue.HasValue)
			{
				if (newValue == false)
				{
					savedText = Text.GetString();
					// Don't save text if > 50 MB
					if ((savedText.Length >> 20) >= 50)
						savedText = null;
					savedTextPoint = NETextPoint;
					savedCodePage = CodePage;
					savedHasBOM = HasBOM;
					savedAESKey = AESKey;
					savedCompressed = Compressed;
				}
				else
				{
					// Nothing will match, file will be perpetually modified
					savedText = null;
					savedTextPoint = null;
					savedCodePage = Coder.CodePage.None;
					savedHasBOM = false;
					savedAESKey = null;
					savedCompressed = false;
				}
			}

			IsModified = (savedCodePage != CodePage) || (savedHasBOM != HasBOM) || (savedAESKey != AESKey) || (savedCompressed != Compressed) || ((savedTextPoint != NETextPoint) && (savedText != Text.GetString()));
		}

		public IReadOnlyList<string> GetSelectionStrings() => Selections.AsTaskRunner().Select(range => Text.GetString(range)).ToList();

		void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
				return;

			var range = Selections[CurrentSelection];
			var lineMin = Text.GetPositionLine(range.Start);
			var lineMax = Text.GetPositionLine(range.End);
			var indexMin = Text.GetPositionIndex(range.Start, lineMin);
			var indexMax = Text.GetPositionIndex(range.End, lineMax);

			if (centerVertically)
			{
				StartRow = (lineMin + lineMax - NEWindow.DisplayRows) / 2;
				if (centerHorizontally)
					StartColumn = (Text.GetColumnFromIndex(lineMin, indexMin) + Text.GetColumnFromIndex(lineMax, indexMax) - NEWindow.DisplayColumns) / 2;
				else
					StartColumn = 0;
			}

			var line = Text.GetPositionLine(range.Cursor);
			var index = Text.GetPositionIndex(range.Cursor, line);
			var x = Text.GetColumnFromIndex(line, index);
			StartRow = Math.Min(line, Math.Max(line - (NEWindow?.DisplayRows ?? 1) + 1, StartRow));
			StartColumn = Math.Min(x, Math.Max(x - (NEWindow?.DisplayColumns ?? 1) + 1, StartColumn));
		}

		public NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var results = new NEVariables();

			var strs = default(List<string>);
			var initializeStrs = new RunOnceAction(() => strs = Selections.Select(range => Text.GetString(range)).ToList());
			results.Add(NEVariable.List("x", "Selection", () => { initializeStrs.Invoke(); return strs; }));
			results.Add(NEVariable.Constant("xn", "Selection count", () => Selections.Count));
			results.Add(NEVariable.List("xl", "Selection length", () => Selections.Select(range => range.Length)));
			results.Add(NEVariable.Constant("xlmin", "Selection min length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xlmax", "Selection max length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xmin", "Selection numeric min", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xmax", "Selection numeric max", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xtmin", "Selection text min", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).First()));
			results.Add(NEVariable.Constant("xtmax", "Selection text max", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).Last()));

			for (var ctr = 1; ctr <= 9; ++ctr)
			{
				var region = ctr; // If we don't copy this variable it passes the most recent value (10 after it's done) to GetRegions
				var regions = default(List<string>);
				var initializeRegions = new RunOnceAction(() => regions = GetRegions(region).Select(range => Text.GetString(range)).ToList());
				results.Add(NEVariable.List($"r{region}", $"Region {region}", () => { initializeRegions.Invoke(); return regions; }));
				results.Add(NEVariable.Constant($"r{region}n", $"Region {region} count", () => GetRegions(region).Count));
				results.Add(NEVariable.List($"r{region}l", $"Region {region} length", () => GetRegions(region).Select(range => range.Length)));
				results.Add(NEVariable.Constant($"r{region}lmin", $"Region {region} min length", () => GetRegions(region).Select(range => range.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"r{region}lmax", $"Region {region} max length", () => GetRegions(region).Select(range => range.Length).DefaultIfEmpty(0).Max()));
			}

			results.Add(NEVariable.Series("y", "One-based index", index => index + 1));
			results.Add(NEVariable.Series("z", "Zero-based index", index => index));

			if (Clipboard.Count == 1)
			{
				results.Add(NEVariable.Constant("c", "Clipboard", () => Clipboard[0]));
				results.Add(NEVariable.Constant("cl", "Clipboard length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard[0].Length));
			}
			else
			{
				results.Add(NEVariable.List("c", "Clipboard", () => Clipboard));
				results.Add(NEVariable.List("cl", "Clipboard length", () => Clipboard.Select(str => str.Length)));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}
			results.Add(NEVariable.Constant("cn", "Clipboard count", () => Clipboard.Count));

			results.Add(NEVariable.Constant("f", "Filename", () => FileName));
			results.Add(NEVariable.Constant("d", "Display name", () => DisplayName));

			var lineStarts = default(IReadOnlyList<int>);
			var initializeLineStarts = new RunOnceAction(() => lineStarts = Selections.AsTaskRunner().Select(range => Text.GetPositionLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => { initializeLineStarts.Invoke(); return lineStarts; }));
			var lineEnds = default(IReadOnlyList<int>);
			var initializeLineEnds = new RunOnceAction(() => lineEnds = Selections.AsTaskRunner().Select(range => Text.GetPositionLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => { initializeLineEnds.Invoke(); return lineEnds; }));

			var colStarts = default(IReadOnlyList<int>);
			var initializeColStarts = new RunOnceAction(() => { initializeLineStarts.Invoke(); colStarts = Selections.AsTaskRunner().Select((range, index) => Text.GetPositionIndex(range.Start, lineStarts[index] - 1) + 1).ToList(); });
			results.Add(NEVariable.List("col", "Selection column start", () => { initializeColStarts.Invoke(); return colStarts; }));
			var colEnds = default(IReadOnlyList<int>);
			var initializeColEnds = new RunOnceAction(() => { initializeLineEnds.Invoke(); colEnds = Selections.AsTaskRunner().Select((range, index) => Text.GetPositionIndex(range.End, lineEnds[index] - 1) + 1).ToList(); });
			results.Add(NEVariable.List("colend", "Selection column end", () => { initializeColEnds.Invoke(); return colEnds; }));

			var posStarts = default(IReadOnlyList<int>);
			var initializePosStarts = new RunOnceAction(() => posStarts = Selections.Select(range => range.Start).ToList());
			results.Add(NEVariable.List("pos", "Selection position start", () => { initializePosStarts.Invoke(); return posStarts; }));
			var posEnds = default(IReadOnlyList<int>);
			var initializePosEnds = new RunOnceAction(() => posEnds = Selections.Select(range => range.End).ToList());
			results.Add(NEVariable.List("posend", "Selection position end", () => { initializePosEnds.Invoke(); return posEnds; }));

			for (var ctr = 0; ctr < 10; ++ctr)
			{
				var name = ctr == 0 ? "k" : $"v{ctr}";
				var desc = ctr == 0 ? "Keys" : $"Values {ctr}";
				var values = GetKeysAndValues(ctr).Values;
				if (values == null)
					continue;
				results.Add(NEVariable.List(name, desc, () => values));
				results.Add(NEVariable.Constant($"{name}n", $"{desc} count", () => values.Count));
				results.Add(NEVariable.List($"{name}l", $"{desc} length", () => values.Select(str => str.Length)));
				results.Add(NEVariable.Constant($"{name}lmin", $"{desc} min length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"{name}lmax", $"{desc} max length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}

			if (Coder.IsImage(CodePage))
			{
				results.Add(NEVariable.Constant("width", "Image width", () => GetBitmap().Width));
				results.Add(NEVariable.Constant("height", "Image height", () => GetBitmap().Height));
			}

			var nonNulls = default(IReadOnlyList<Tuple<double, int>>);
			double lineStart = 0, lineIncrement = 0, geoStart = 0, geoIncrement = 0;
			var initializeNonNulls = new RunOnceAction(() => nonNulls = Selections.AsTaskRunner().Select((range, index) => new { str = Text.GetString(range), index }).NonNullOrWhiteSpace(obj => obj.str).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList());
			var initializeLineSeries = new RunOnceAction(() =>
			{
				initializeNonNulls.Invoke();
				if (nonNulls.Count == 0)
					lineStart = lineIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					lineStart = nonNulls[0].Item1;
					lineIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					lineIncrement = (last.Item1 - first.Item1) / (last.Item2 - first.Item2);
					lineStart = first.Item1 - lineIncrement * first.Item2;
				}
			});
			var initializeGeoSeries = new RunOnceAction(() =>
			{
				initializeNonNulls.Invoke();
				if (nonNulls.Count == 0)
					geoStart = geoIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					geoStart = nonNulls[0].Item1;
					geoIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					geoIncrement = Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
					geoStart = first.Item1 / Math.Pow(geoIncrement, first.Item2);
				}
			});
			results.Add(NEVariable.Constant("linestart", "Linear series start", () => { initializeLineSeries.Invoke(); return lineStart; }));
			results.Add(NEVariable.Constant("lineincrement", "Linear series increment", () => { initializeLineSeries.Invoke(); return lineIncrement; }));
			results.Add(NEVariable.Constant("geostart", "Geometric series start", () => { initializeGeoSeries.Invoke(); return geoStart; }));
			results.Add(NEVariable.Constant("geoincrement", "Geometric series increment", () => { initializeGeoSeries.Invoke(); return geoIncrement; }));

			return results;
		}

		List<T> GetExpressionResults<T>(string expression, int? count = null) => state.GetExpression(expression).Evaluate<T>(GetVariables(), count);

		List<NERange> GetEnclosingRegions(int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var useRegions = GetRegions(useRegion);
			var regions = new List<NERange>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in Selections)
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
					throw new Exception("No region found. All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((Selections.Any()) && (useAllRegions) && (currentRegion != useRegions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		bool StringsAreFiles(IReadOnlyList<string> strs)
		{
			if ((strs.Count == 0) || (strs.Count > 500))
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => (!str.StartsWith("\\\\")) && (!drives.Any(drive => str.StartsWith(drive, StringComparison.OrdinalIgnoreCase)))))
				return false;
			if (strs.Any(str => !Helpers.FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsTaskRunner().All(data => Coder.CanEncode(data, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsTaskRunner().All(str => Coder.CanEncode(str, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool ConfirmContinueWhenCannotEncode()
		{
			return QueryUser(nameof(ConfirmContinueWhenCannotEncode), "The specified encoding cannot fully represent the data. Continue anyway?", MessageOptions.Yes);
		}

		DbConnection dbConnection { get; set; }

		void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			AddNewNEFile(new NEFile(displayName: name, bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: contentType, modified: false));
		}

		IReadOnlyList<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsTaskRunner().Select(range => fileName.RelativeChild(Text.GetString(range))).ToList();
		}

		bool VerifyCanEncode()
		{
			if (Text.CanEncode(CodePage))
				return true;

			if (QueryUser(nameof(VerifyCanEncode), "The current encoding cannot fully represent this data. Switch to UTF-8?", MessageOptions.Yes))
			{
				CodePage = Coder.CodePage.UTF8;
				HasBOM = true;
			}
			return true;
		}

		DateTime GetFileWriteTime(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return DateTime.MinValue;
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				return DateTime.MinValue;
			return fileInfo.LastWriteTimeUtc;
		}

		void Save(string fileName, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Text.Length >> 20) < 50) && (!VerifyCanEncode()))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = false;
					File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(Text.GetBytes(CodePage, HasBOM), Compressed), AESKey));
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = true;
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (!QueryUser(nameof(Save), "Save failed. Remove read-only flag?", MessageOptions.Yes))
						throw;
					new FileInfo(fileName).IsReadOnly = false;
					triedReadOnly = true;
				}
			}

			if (!copyOnly)
			{
				SetFileName(fileName);
				LastWriteTime = LastExternalWriteTime = LastActivatedTime = GetFileWriteTime(FileName);
				SetIsModified(false);
			}
		}

		void SetFileName(string fileName)
		{
			if (FileName == fileName)
				return;

			FileName = fileName;
			ContentType = ParserExtensions.GetParserType(FileName);
			DisplayName = null;

			SetupWatcher();
		}

		public void VerifyCanClose()
		{
			string prompt = null;

			if (IsModified)
			{
				if (LastWriteTime != LastExternalWriteTime)
					prompt = "You have modified this file, and it has been changed externally. Do you want to save your copy?";
				else
					prompt = "You have modified this file. Do you want to save your copy?";
			}
			else if (LastWriteTime != LastExternalWriteTime)
				prompt = "This file has been changed externally. Do you want to save your copy?";

			if (prompt != null)
				if (QueryUser($"{nameof(VerifyCanClose)}-{IsModified}-{LastWriteTime != LastExternalWriteTime}", prompt, MessageOptions.None))
					Execute__File_Save_SaveAll();
		}

		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null)
		{
			SetFileName(fileName);
			if (ContentType == ParserType.None)
				ContentType = contentType;
			DisplayName = displayName;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}

			FileSaver.HandleDecrypt(ref bytes, out var aesKey);
			AESKey = aesKey;

			bytes = FileSaver.Decompress(bytes, out var compressed);
			Compressed = compressed;

			CodePage = Coder.ResolveCodePage(codePage, bytes);
			HasBOM = Coder.HasBOM(bytes, CodePage);

			var data = Coder.BytesToString(bytes, CodePage, true);
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { data });

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanExactlyEncode(bytes, CodePage);

			SetIsModified(isModified);
			LastWriteTime = LastExternalWriteTime = LastActivatedTime = GetFileWriteTime(FileName);
		}

		public void Goto(int? line, int? column, int? index)
		{
			var pos = 0;
			if (line.HasValue)
			{
				var useLine = Math.Max(0, Math.Min(line.Value, Text.NumLines) - 1);
				int useIndex;
				if (column.HasValue)
					useIndex = Text.GetIndexFromColumn(useLine, Math.Max(0, column.Value - 1), true);
				else if (index.HasValue)
					useIndex = Math.Max(0, Math.Min(index.Value - 1, Text.GetLineLength(useLine)));
				else
					useIndex = 0;

				pos = Text.GetPosition(useLine, useIndex);
			}
			Selections = new List<NERange> { new NERange(pos) };
		}

		string savedBitmapText;
		System.Drawing.Bitmap savedBitmap;
		readonly List<ShutdownData> shutdownDatas = new List<ShutdownData>();

		System.Drawing.Bitmap GetBitmap()
		{
			if (!Coder.IsImage(CodePage))
			{
				savedBitmapText = null;
				savedBitmap = null;
			}
			else if (Text.GetString() != savedBitmapText)
			{
				savedBitmapText = Text.GetString();
				savedBitmap = Coder.StringToBitmap(Text.GetString());
			}
			return savedBitmap;
		}

		public bool Empty() => (FileName == null) && (!IsModified) && (Text.Length == 0);

		public void AddShutdownData(ShutdownData shutdownData)
		{
			if (shutdownData != null)
				shutdownDatas.Add(shutdownData);
		}

		public void Close()
		{
			DiffTarget = null;
			shutdownDatas.ForEach(shutdownData => shutdownData.OnShutdown());
			shutdownDatas.Clear();
			ClearNEFiles();
		}

		public override string ToString() => DisplayName ?? FileName;

		public void ViewGetBinaryData(out byte[] data, out bool hasSel)
		{
			if (Selections.Any())
			{
				var range = Selections[CurrentSelection];
				data = Coder.StringToBytes(Text.GetString(range.Start, Math.Min(range.HasSelection ? range.Length : 100, Text.Length - range.Start)), CodePage);
				hasSel = range.HasSelection;
			}
			else
			{
				data = null;
				hasSel = false;
			}
		}

		public void ViewSetDisplaySize(int columns, int rows) => NEWindow?.SetDisplaySize(columns, rows);

		public List<string> ViewGetStatusBar()
		{
			var status = new List<string>();

			if (!Selections.Any())
			{
				status.Add("Selection 0/0");
				status.Add("Col");
				status.Add("In");
				status.Add("Pos");
			}
			else
			{
				var range = Selections[CurrentSelection];
				var startLine = Text.GetPositionLine(range.Start);
				var endLine = Text.GetPositionLine(range.End);
				var indexMin = Text.GetPositionIndex(range.Start, startLine);
				var indexMax = Text.GetPositionIndex(range.End, endLine);
				var lineMin = Text.GetDiffLine(startLine);
				var lineMax = Text.GetDiffLine(endLine);
				var columnMin = Text.GetColumnFromIndex(startLine, indexMin);
				var columnMax = Text.GetColumnFromIndex(endLine, indexMax);
				var posMin = range.Start;
				var posMax = range.End;

				status.Add($"Selection {CurrentSelection + 1:n0}/{Selections.Count:n0}");
				status.Add($"Col {lineMin + 1:n0}:{columnMin + 1:n0}{((lineMin == lineMax) && (columnMin == columnMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{columnMax + 1:n0}")}");
				status.Add($"In {lineMin + 1:n0}:{indexMin + 1:n0}{((lineMin == lineMax) && (indexMin == indexMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{indexMax + 1:n0}")}");
				status.Add($"Pos {posMin:n0}{(posMin == posMax ? "" : $"-{posMax:n0} ({posMax - posMin:n0})")}");
			}

			status.Add($"Regions {string.Join(" / ", Enumerable.Range(1, 9).Select(region => $"{GetRegions(region).Count:n0}"))}");
			status.Add($"Database {DBName}");

			return status;
		}

		public bool QueryUser(string name, string text, MessageOptions defaultAccept, MessageOptions options = MessageOptions.YesNoAllCancel)
		{
			lock (state)
			{
				if ((!state.SavedAnswers[name].HasFlag(MessageOptions.All)) && (!state.SavedAnswers[name].HasFlag(MessageOptions.Cancel)))
					NEWindow.ShowFile(this, () => state.SavedAnswers[name] = NEWindow.neWindowUI.RunDialog_ShowMessage("Confirm", text, options, defaultAccept, MessageOptions.Cancel));
				if (state.SavedAnswers[name] == MessageOptions.Cancel)
					throw new OperationCanceledException();
				return state.SavedAnswers[name].HasFlag(MessageOptions.Yes);
			}
		}

		public int ViewMaxColumn => Text.MaxColumn;

		public int ViewNumLines => Text.NumLines;

		public int ViewGetPosition(int line, int index, bool allowJustPastEnd = false) => Text.GetPosition(line, index, allowJustPastEnd);

		public int ViewGetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false) => Text.GetIndexFromColumn(line, findColumn, returnMaxOnFail);

		public int ViewGetLineLength(int line) => Text.GetLineLength(line);

		public int ViewGetPositionLine(int position) => Text.GetPositionLine(position);

		public int ViewGetPositionIndex(int position, int line) => Text.GetPositionIndex(position, line);

		public int ViewGetColumnFromIndex(int line, int findIndex) => Text.GetColumnFromIndex(line, findIndex);

		public int ViewGetLineColumnsLength(int line) => Text.GetLineColumnsLength(line);

		public string ViewGetLineColumns(int line, int startColumn, int endColumn) => Text.GetLineColumns(line, startColumn, endColumn);

		public DiffType ViewGetLineDiffType(int line) => Text.GetLineDiffType(line);

		public List<int> ViewGetLineColumnMap(int line, bool includeEnding = false) => Text.GetLineColumnMap(line, includeEnding);

		public List<Tuple<int, int>> ViewGetLineColumnDiffs(int line) => Text.GetLineColumnDiffs(line);

		public List<Tuple<double, double>> ViewGetDiffRanges() => Text.GetDiffRanges();

		public bool CheckModified(MessageOptions defaultAccept)
		{
			if (!IsModified)
				return true;
			return QueryUser(nameof(Execute__File_Open_ReopenWithEncoding), "You have unsaved changes. Are you sure you want to reload?", defaultAccept);
		}
	}
}
