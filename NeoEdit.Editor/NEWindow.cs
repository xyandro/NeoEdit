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

		public bool WorkMode { get; set; }

		public int DisplayColumns { get; private set; }

		public int DisplayRows { get; private set; }

		public DateTime LastActive { get; set; }

		public NEWindow()
		{
			Data = new NEWindowData(this);
			state.NEGlobal.AddNewNEWindow(this);
		}

		public IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var clipboardDataMap = ActiveFiles.ToDictionary(x => x as INEFile, x => empty);

			if (NEClipboard.Current.FileCount == ActiveFiles.Count)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[ActiveFiles.GetIndex(index)] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ItemCount == ActiveFiles.Count)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[ActiveFiles.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.FileCount == 1) || (NEClipboard.Current.FileCount == NEClipboard.Current.ItemCount)) && (NEClipboard.Current.ItemCount == ActiveFiles.Sum(neFile => neFile.Selections.Count)))
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

			var renderParameters = new RenderParameters
			{
				NEFiles = NEFiles,
				FileCount = NEFiles.Count,
				ActiveFiles = ActiveFiles,
				FocusedFile = Focused,
				WindowLayout = WindowLayout,
				WorkMode = WorkMode,
				StatusBar = GetStatusBar(),
				MenuStatus = GetMenuStatus(),
			};

			if (renderParameters.WorkMode)
			{
				var workFiles = ActiveFiles.Where(neFile => neFile.Selections.Any()).ToList();
				if (workFiles.Any())
				{
					renderParameters.NEFiles = workFiles.Concat(renderParameters.NEFiles.Except(workFiles)).ToList();
					renderParameters.FileCount = workFiles.Count;
				}
			}

			neWindowUI?.Render(renderParameters);
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
				[nameof(NECommand.Edit_Select_AllowOverlappingSelections)] = GetMultiStatus(neFile => neFile.AllowOverlappingSelections),
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
				[nameof(NECommand.Window_WorkMode)] = WorkMode,
				[nameof(NECommand.Window_Font_ShowSpecial)] = Settings.ShowSpecialChars,
				[nameof(NECommand.Window_ViewBinary)] = GetMultiStatus(neFile => neFile.ViewBinary),
			};
		}

		public void Configure()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: Configure__Internal_Key(); break;
				case NECommand.File_Select_SelectByExpression: Configure__File_Select_SelectByExpression__File_Save_SaveAsByExpression__File_Copy_CopyByExpression__File_Advanced_SetDisplayName(); break;
				case NECommand.File_Open_Open: Configure__File_Open_Open__Macro_Open_Open(); break;
				case NECommand.File_Open_ReopenWithEncoding: Configure__File_Open_ReopenWithEncoding(); break;
				case NECommand.File_Save_SaveAsByExpression: Configure__File_Select_SelectByExpression__File_Save_SaveAsByExpression__File_Copy_CopyByExpression__File_Advanced_SetDisplayName(); break;
				case NECommand.File_Move_MoveByExpression: Configure__File_Move_MoveByExpression(); break;
				case NECommand.File_Copy_CopyByExpression: Configure__File_Select_SelectByExpression__File_Save_SaveAsByExpression__File_Copy_CopyByExpression__File_Advanced_SetDisplayName(); break;
				case NECommand.File_Encoding: Configure__File_Encoding(); break;
				case NECommand.File_LineEndings: Configure__File_LineEndings(); break;
				case NECommand.File_Advanced_Encrypt: Configure__File_Advanced_Encrypt(); break;
				case NECommand.File_Advanced_SetDisplayName: Configure__File_Select_SelectByExpression__File_Save_SaveAsByExpression__File_Copy_CopyByExpression__File_Advanced_SetDisplayName(); break;
				case NECommand.File_Exit: Configure__File_Exit(); break;
				case NECommand.Edit_Select_Limit: Configure__Edit_Select_Limit(); break;
				case NECommand.Macro_Open_Open: Configure__File_Open_Open__Macro_Open_Open(Macro.MacroDirectory); break;
				default: NEFile.Configure(); break;
			}
		}

		#region PreExecute
		public void PreExecute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: PreExecute__Internal_Key(); break;
				default: NEFile.PreExecute(); break;
			}
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Activate: Execute__Internal_Activate(); break;
				case NECommand.Internal_CloseFile: Execute__Internal_CloseFile(); break;
				case NECommand.Internal_Mouse: Execute__Internal_Mouse(); break;
				case NECommand.Internal_Redraw: Execute__Internal_Redraw(); break;
				case NECommand.File_Select_All: Execute__File_Select_All(); break;
				case NECommand.File_Select_None: Execute__File_Select_None(); break;
				case NECommand.File_Select_WithSelections: Execute__File_Select_WithSelections__File_Select_WithoutSelections(true); break;
				case NECommand.File_Select_WithoutSelections: Execute__File_Select_WithSelections__File_Select_WithoutSelections(false); break;
				case NECommand.File_Select_Modified: Execute__File_Select_Modified__File_Select_Unmodified(true); break;
				case NECommand.File_Select_Unmodified: Execute__File_Select_Modified__File_Select_Unmodified(false); break;
				case NECommand.File_Select_ExternalModified: Execute__File_Select_ExternalModified__File_Select_ExternalUnmodified(true); break;
				case NECommand.File_Select_ExternalUnmodified: Execute__File_Select_ExternalModified__File_Select_ExternalUnmodified(false); break;
				case NECommand.File_Select_Inactive: Execute__File_Select_Inactive(); break;
				case NECommand.File_Select_SelectByExpression: Execute__File_Select_SelectByExpression(); break;
				case NECommand.File_Select_Choose: Execute__File_Select_Choose(); break;
				case NECommand.File_New_New: Execute__File_New_New(); break;
				case NECommand.File_New_FromSelections_All: Execute__File_New_FromSelections_All__File_New_FromSelections_Files__File_New_FromSelections_Selections(); break;
				case NECommand.File_New_FromSelections_Files: Execute__File_New_FromSelections_All__File_New_FromSelections_Files__File_New_FromSelections_Selections(); break;
				case NECommand.File_New_FromSelections_Selections: Execute__File_New_FromSelections_All__File_New_FromSelections_Files__File_New_FromSelections_Selections(); break;
				case NECommand.File_New_FromClipboard_All: Execute__File_New_FromClipboard_All(); break;
				case NECommand.File_New_FromClipboard_Files: Execute__File_New_FromClipboard_Files(); break;
				case NECommand.File_New_FromClipboard_Selections: Execute__File_New_FromClipboard_Selections(); break;
				case NECommand.File_New_WordList: Execute__File_New_WordList(); break;
				case NECommand.File_Open_Open: Execute__File_Open_Open__Macro_Open_Open(); break;
				case NECommand.File_Open_CopiedCut: Execute__File_Open_CopiedCut(); break;
				case NECommand.File_Exit: Execute__File_Exit(); break;
				case NECommand.Macro_Open_Open: Execute__File_Open_Open__Macro_Open_Open(); break;
				default: ActiveFiles.AsTaskRunner().ForAll(neFile => neFile.Execute()); break;
			}
		}
		#endregion

		#region PostExecute
		public void PostExecute()
		{
			switch (state.Command)
			{
				case NECommand.File_New_FromSelections_All: PostExecute__File_New_FromSelections_All(); break;
				case NECommand.File_New_FromSelections_Files: PostExecute__File_New_FromSelections_Files(); break;
				case NECommand.File_New_FromSelections_Selections: PostExecute__File_New_FromSelections_Selections(); break;
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
			status.Add($"Clipboard: {plural(NEClipboard.Current?.FileCount ?? 0, "file")}, {plural(NEClipboard.Current?.ItemCount ?? 0, "selection")}");
			status.Add($"Keys/Values: {string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");
			return status;
		}

		public void SetForeground() => neWindowUI.SetForeground();

		public void CalculateDiffs()
		{
			var diffFiles = NEFiles.Where(neFile => (neFile.DiffTarget != null) && (!neFile.Text.HasDiff)).ToList();
			diffFiles = diffFiles.Select(neFile => neFile.DiffSerial < neFile.DiffTarget.DiffSerial ? neFile : neFile.DiffTarget).Distinct().ToList();
			diffFiles.AsTaskRunner().ForAll(neFile => NEText.CalculateDiff(neFile.Text, neFile.DiffTarget.Text, neFile.DiffIgnoreWhitespace, neFile.DiffIgnoreCase, neFile.DiffIgnoreNumbers, neFile.DiffIgnoreLineEndings, neFile.DiffIgnoreCharacters));
		}

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
