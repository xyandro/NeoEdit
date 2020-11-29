using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEWindow : INEWindow
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public INEWindowUI neWindowUI { get; private set; }

		bool NeedsRender { get; set; }
		public void SetNeedsRender() => NeedsRender = true;
		private WindowLayout windowLayout = new WindowLayout(1, 1);
		public WindowLayout WindowLayout
		{
			get => windowLayout; set
			{
				windowLayout = value;
				NeedsRender = true;
			}
		}

		bool activeFirst;
		public bool ActiveFirst
		{
			get => activeFirst;
			set
			{
				activeFirst = value;
				SetNeedsRender();
			}
		}

		public int DisplayColumns { get; private set; }

		public int DisplayRows { get; private set; }

		public DateTime LastActive { get; set; }

		IReadOnlyOrderedHashSet<NEFile> orderedNEFiles;
		bool orderedNEFiles_ActiveFirst;
		IReadOnlyOrderedHashSet<NEFile> orderedNEFiles_NEFiles;
		IReadOnlyOrderedHashSet<NEFile> orderedNEFiles_ActiveFiles;
		public IReadOnlyOrderedHashSet<NEFile> OrderedNEFiles
		{
			get
			{
				if ((ActiveFirst != orderedNEFiles_ActiveFirst) || (NEFiles != orderedNEFiles_NEFiles) || (ActiveFiles != orderedNEFiles_ActiveFiles))
					lock (this)
						if ((ActiveFirst != orderedNEFiles_ActiveFirst) || (NEFiles != orderedNEFiles_NEFiles) || (ActiveFiles != orderedNEFiles_ActiveFiles))
						{
							if (ActiveFirst)
							{
								var activeFiles = new OrderedHashSet<NEFile>();
								var inactiveFiles = new OrderedHashSet<NEFile>();
								foreach (var neFile in NEFiles)
									(ActiveFiles.Contains(neFile) ? activeFiles : inactiveFiles).Add(neFile);
								inactiveFiles.ForEach(activeFiles.Add);
								orderedNEFiles = activeFiles;
							}
							else
								orderedNEFiles = NEFiles;

							orderedNEFiles_ActiveFirst = ActiveFirst;
							orderedNEFiles_NEFiles = NEFiles;
							orderedNEFiles_ActiveFiles = ActiveFiles;
						}
				return orderedNEFiles;
			}
		}

		public NEWindow()
		{
			Data = new NEWindowData(this);
			state.NEGlobal.AddNewNEWindow(this);
		}

		public IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var clipboardDataMap = ActiveFiles.ToDictionary(x => x as INEFile, x => empty);

			if (NEClipboard.Current.Count == ActiveFiles.Count)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[ActiveFiles.GetIndex(index)] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ChildCount == ActiveFiles.Count)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[ActiveFiles.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == ActiveFiles.Sum(neFile => neFile.Selections.Count)))
				NEClipboard.Current.Strings.Take(ActiveFiles.Select(neFile => neFile.Selections.Count)).ForEach((obj, index) => clipboardDataMap[ActiveFiles.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(obj.ToList(), NEClipboard.Current.IsCut));
			else
			{
				var strs = NEClipboard.Current.Strings;
				ActiveFiles.ForEach(neFile => clipboardDataMap[neFile] = new Tuple<IReadOnlyList<string>, bool?>(strs, NEClipboard.Current.IsCut));
			}

			return clipboardDataMap;
		}

		public static IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), 10).ToArray();
		public Dictionary<INEFile, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var keysAndValuesMap = NEFiles.ToDictionary(x => x as INEFile, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				NEFiles.ForEach(neFile => keysAndValuesMap[neFile] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == ActiveFiles.Count)
				ActiveFiles.ForEach((neFile, index) => keysAndValuesMap[neFile] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		public void RenderNEWindowUI()
		{
			if (!NeedsRender)
				return;

			NeedsRender = false;
			neWindowUI?.Render(new RenderParameters
			{
				NEFiles = OrderedNEFiles,
				FileCount = (ActiveFirst) && (ActiveFiles.Count > 0) ? ActiveFiles.Count : NEFiles.Count,
				ActiveFiles = ActiveFiles,
				FocusedFile = Focused,
				WindowLayout = WindowLayout,
				StatusBar = GetStatusBar(),
				MenuStatus = GetMenuStatus(),
			});
		}

		Dictionary<string, bool?> GetMenuStatus()
		{
			bool? GetMultiStatus(Func<NEFile, bool> func)
			{
				var results = ActiveFiles.Select(func).Distinct().Take(2).ToList();
				if (results.Count != 1)
					return default;
				return results[0];
			}

			return new Dictionary<string, bool?>
			{
				[nameof(NECommand.File_AutoRefresh)] = GetMultiStatus(neFile => neFile.AutoRefresh),
				[nameof(NECommand.File_Advanced_Compress)] = GetMultiStatus(neFile => neFile.Compressed),
				[nameof(NECommand.File_Advanced_Encrypt)] = GetMultiStatus(neFile => !string.IsNullOrWhiteSpace(neFile.AESKey)),
				[nameof(NECommand.File_Advanced_DontExitOnClose)] = Settings.DontExitOnClose,
				[nameof(NECommand.Edit_Select_Overlap_AllowOverlappingSelections)] = GetMultiStatus(neFile => neFile.AllowOverlappingSelections),
				[nameof(NECommand.Edit_Navigate_JumpBy_Words)] = GetMultiStatus(neFile => neFile.JumpBy == JumpByType.Words),
				[nameof(NECommand.Edit_Navigate_JumpBy_Numbers)] = GetMultiStatus(neFile => neFile.JumpBy == JumpByType.Numbers),
				[nameof(NECommand.Edit_Navigate_JumpBy_Paths)] = GetMultiStatus(neFile => neFile.JumpBy == JumpByType.Paths),
				[nameof(NECommand.Edit_Advanced_EscapeClearsSelections)] = Settings.EscapeClearsSelections,
				[nameof(NECommand.Content_Type_None)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.None),
				[nameof(NECommand.Content_Type_Balanced)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.Balanced),
				[nameof(NECommand.Content_Type_Columns)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.Columns),
				[nameof(NECommand.Content_Type_CPlusPlus)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.CPlusPlus),
				[nameof(NECommand.Content_Type_CSharp)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.CSharp),
				[nameof(NECommand.Content_Type_CSV)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.CSV),
				[nameof(NECommand.Content_Type_ExactColumns)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.ExactColumns),
				[nameof(NECommand.Content_Type_HTML)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.HTML),
				[nameof(NECommand.Content_Type_JSON)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.JSON),
				[nameof(NECommand.Content_Type_SQL)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.SQL),
				[nameof(NECommand.Content_Type_TSV)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.TSV),
				[nameof(NECommand.Content_Type_XML)] = GetMultiStatus(neFile => neFile.ContentType == ParserType.XML),
				[nameof(NECommand.Content_HighlightSyntax)] = GetMultiStatus(neFile => neFile.HighlightSyntax),
				[nameof(NECommand.Content_StrictParsing)] = GetMultiStatus(neFile => neFile.StrictParsing),
				[nameof(NECommand.Content_KeepSelections)] = GetMultiStatus(neFile => neFile.KeepSelections),
				[nameof(NECommand.Diff_IgnoreWhitespace)] = GetMultiStatus(neFile => neFile.DiffIgnoreWhitespace),
				[nameof(NECommand.Diff_IgnoreCase)] = GetMultiStatus(neFile => neFile.DiffIgnoreCase),
				[nameof(NECommand.Diff_IgnoreNumbers)] = GetMultiStatus(neFile => neFile.DiffIgnoreNumbers),
				[nameof(NECommand.Diff_IgnoreLineEndings)] = GetMultiStatus(neFile => neFile.DiffIgnoreLineEndings),
				[nameof(NECommand.Macro_Visualize)] = NEGlobal.MacroVisualize,
				[nameof(NECommand.Window_ActiveFirst)] = ActiveFirst,
				[nameof(NECommand.Window_Font_ShowSpecial)] = Font.ShowSpecialChars,
				[nameof(NECommand.Window_ViewBinary)] = GetMultiStatus(neFile => neFile.ViewBinary),
			};
		}

		public void Configure()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: Configure_Internal_Key(); break;
				case NECommand.File_Open_Open: Configure_FileMacro_Open_Open(); break;
				case NECommand.Macro_Open_Open: Configure_FileMacro_Open_Open(Macro.MacroDirectory); break;
				default: NEFile.Configure(); break;
			}
		}

		#region PreExecute
		public void PreExecute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: PreExecute_Internal_Key(); break;
				case NECommand.File_New_FromSelections_All: PreExecute_File_New_FromSelections_AllFilesSelections(); break;
				case NECommand.File_New_FromSelections_Files: PreExecute_File_New_FromSelections_AllFilesSelections(); break;
				case NECommand.File_New_FromSelections_Selections: PreExecute_File_New_FromSelections_AllFilesSelections(); break;
				default: NEFile.PreExecute(); break;
			}
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Activate: Execute_Internal_Activate(); break;
				case NECommand.Internal_CloseFile: Execute_Internal_CloseFile(); break;
				case NECommand.Internal_Mouse: Execute_Internal_Mouse(); break;
				case NECommand.Internal_Redraw: Execute_Internal_Redraw(); break;
				case NECommand.File_Select_All: Execute_File_Select_All(); break;
				case NECommand.File_Select_None: Execute_File_Select_None(); break;
				case NECommand.File_Select_WithSelections: Execute_File_Select_WithWithoutSelections(true); break;
				case NECommand.File_Select_WithoutSelections: Execute_File_Select_WithWithoutSelections(false); break;
				case NECommand.File_Select_Modified: Execute_File_Select_ModifiedUnmodified(true); break;
				case NECommand.File_Select_Unmodified: Execute_File_Select_ModifiedUnmodified(false); break;
				case NECommand.File_Select_Inactive: Execute_File_Select_Inactive(); break;
				case NECommand.File_Select_Choose: Execute_File_Select_Choose(); break;
				case NECommand.File_New_New: Execute_File_New_New(); break;
				case NECommand.File_New_FromClipboard_All: Execute_File_New_FromClipboard_All(); break;
				case NECommand.File_New_FromClipboard_Files: Execute_File_New_FromClipboard_Files(); break;
				case NECommand.File_New_FromClipboard_Selections: Execute_File_New_FromClipboard_Selections(); break;
				case NECommand.File_New_WordList: Execute_File_New_WordList(); break;
				case NECommand.File_Open_Open: Execute_FileMacro_Open_Open(); break;
				case NECommand.File_Open_CopiedCut: Execute_File_Open_CopiedCut(); break;
				case NECommand.Macro_Open_Open: Execute_FileMacro_Open_Open(); break;
				default: ActiveFiles.AsTaskRunner().ForAll(neFile => neFile.Execute()); break;
			}
		}
		#endregion

		#region PostExecute
		public void PostExecute()
		{
			switch (state.Command)
			{
				case NECommand.File_New_FromSelections_All: PostExecute_File_New_FromSelections_All(); break;
				case NECommand.File_New_FromSelections_Files: PostExecute_File_New_FromSelections_Files(); break;
				case NECommand.File_New_FromSelections_Selections: PostExecute_File_New_FromSelections_Selections(); break;
			}
		}
		#endregion

		public int GetFileIndex(NEFile neFile, bool activeOnly = false)
		{
			var index = (activeOnly ? ActiveFiles : NEFiles).FindIndex(neFile);
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void MovePrevNext(int offset, bool shiftDown, bool orderByActive = false)
		{
			if (NEFiles.Count() <= 1)
				return;

			NEFile neFile;
			if (Focused == null)
				neFile = NEFiles.GetIndex(0);
			else
			{
				var neWindow = orderByActive ? NEFiles.OrderByDescending(x => x.LastActive).ToList() : NEFiles as IList<NEFile>;
				var index = neWindow.FindIndex(Focused) + offset;
				if (index < 0)
					index += NEFiles.Count();
				if (index >= NEFiles.Count())
					index -= NEFiles.Count();
				neFile = neWindow[index];
			}

			SetActiveFiles(NEFiles.Where(file => (file == neFile) || ((shiftDown) && (ActiveFiles.Contains(file)))));
			Focused = neFile;
		}

		public bool GotoFile(string fileName, int? line, int? column, int? index)
		{
			var neFile = NEFiles.FirstOrDefault(x => x.FileName == fileName);
			if (neFile == null)
				return false;
			//Activate();
			SetActiveFile(neFile);
			//TODO
			//neFile.Execute_File_Refresh();
			//neFile.Goto(line, column, index);
			return true;
		}

		public T ShowFile<T>(NEFile neFile, Func<T> action)
		{
			lock (this)
			{
				if (TaskRunner.Canceled)
					throw new OperationCanceledException();

				var saveActiveFiles = ActiveFiles;
				var saveFocused = Focused;
				var saveWindowLayout = WindowLayout;

				try
				{
					SetActiveFile(neFile);
					WindowLayout = new WindowLayout(1, 1);

					RenderNEWindowUI();

					return action();
				}
				finally
				{
					ActiveFiles = saveActiveFiles;
					Focused = saveFocused;
					WindowLayout = saveWindowLayout;
				}
			}
		}

		public void SetDisplaySize(int columns, int rows)
		{
			DisplayColumns = columns;
			DisplayRows = rows;
		}

		List<string> GetStatusBar()
		{
			string plural(int count, string item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			var status = new List<string>();
			status.Add($"Active: {plural(ActiveFiles.Count, "file")}, {plural(ActiveFiles.Sum(neFile => neFile.Selections.Count), "selection")}, {ActiveFiles.Select(neFile => neFile.Selections.Count).DefaultIfEmpty(0).Min():n0} min / {ActiveFiles.Select(neFile => neFile.Selections.Count).DefaultIfEmpty(0).Max():n0} max");
			status.Add($"Inactive: {plural(NEFiles.Except(ActiveFiles).Count(), "file")}, {plural(NEFiles.Except(ActiveFiles).Sum(neFile => neFile.Selections.Count), "selection")}");
			status.Add($"Total: {plural(NEFiles.Count(), "file")}, {plural(NEFiles.Sum(neFile => neFile.Selections.Count), "selection")}");
			status.Add($"Clipboard: {plural(NEClipboard.Current?.Count ?? 0, "file")}, {plural(NEClipboard.Current?.ChildCount ?? 0, "selection")}");
			status.Add($"Keys/Values: {string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");
			return status;
		}

		public void SetForeground() => neWindowUI.SetForeground();

		public void SetupDiff(IReadOnlyList<NEFile> neFiles)
		{
			var now = DateTime.Now;
			for (var ctr = 0; ctr + 1 < neFiles.Count; ctr += 2)
			{
				neFiles[ctr].DiffTarget = neFiles[ctr + 1];
				if (neFiles[ctr].ContentType == ParserType.None)
					neFiles[ctr].ContentType = neFiles[ctr + 1].ContentType;
				if (neFiles[ctr + 1].ContentType == ParserType.None)
					neFiles[ctr + 1].ContentType = neFiles[ctr].ContentType;
				neFiles[ctr + 1].LastActive = now; // Only set ctr + 1 active
			}
			WindowLayout = new WindowLayout(maxColumns: 2);
		}
	}
}
