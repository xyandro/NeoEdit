using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Editor.CommandLine;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEWindow : INEWindow
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public INEWindowUI neWindowUI { get; private set; }

		int displayColumns;
		public int DisplayColumns
		{
			get => displayColumns;
			private set
			{
				displayColumns = value;
				//AllFiles.ForEach(neFile => neFile.ResetView());
			}
		}

		public int DisplayRows { get; private set; }

		public DateTime LastActivated { get; set; }

		public NEWindow(bool addEmpty = false)
		{
			data = new NEWindowData(this);
			NEFileDatas = new OrderedHashSet<NEFileData>();
			ActiveFiles = new OrderedHashSet<NEFile>();
			WindowLayout = new WindowLayout(1, 1);
			state.NEGlobal.AddNEWindow(this);

			if (addEmpty)
				AddNewNEFile(new NEFile());
		}

		bool attached = false;
		public void Attach()
		{
			if (attached)
				throw new Exception("Window already attached");
			attached = true;
			neWindowUI = INEWindowUIStatic.CreateNEWindowUI(this);
		}

		public void Detach()
		{
			if (!attached)
				throw new Exception("Window not attached");
			attached = false;
			neWindowUI.CloseWindow();
			neWindowUI = null;
		}

		IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
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
		Dictionary<INEFile, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
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
			var renderParameters = new RenderParameters
			{
				AllFiles = ActiveOnly ? ActiveFiles : NEFiles,
				ActiveFiles = ActiveFiles,
				FocusedFile = Focused,
				WindowLayout = WindowLayout,
				StatusBar = GetStatusBar(),
				MenuStatus = GetMenuStatus(),
			};
			state.NEWindow.neWindowUI?.Render(renderParameters);
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
				[nameof(NECommand.Macro_Visualize)] = MacroVisualize,
				[nameof(NECommand.Window_ActiveOnly)] = ActiveOnly,
				[nameof(NECommand.Window_Font_ShowSpecial)] = Font.ShowSpecialChars,
				[nameof(NECommand.Window_Binary)] = GetMultiStatus(neFile => neFile.ViewBinary),
			};
		}

		public long RunCommand()
		{
			state.ClipboardDataMapFunc = GetClipboardDataMap;
			state.KeysAndValuesFunc = GetKeysAndValuesMap;

			if (state.Configuration == null)
				NEFile.Configure();

			var sw = Stopwatch.StartNew();

			state.NEWindow.neWindowUI.SetTaskRunnerProgress(0);
			try
			{
				if (!NEFile.PreExecute())
					TaskRunner.Run(Execute, percent => state.NEWindow.neWindowUI.SetTaskRunnerProgress(percent));
			}
			finally { state.NEWindow.neWindowUI.SetTaskRunnerProgress(null); }

			var elapsed = sw.ElapsedMilliseconds;

			if ((recordingMacro != null) && (state.MacroInclude))
				recordingMacro.AddAction(new ExecuteState(state));

			return elapsed;
		}

		public void Execute() => ActiveFiles.AsTaskRunner().ForAll(neFile => neFile.Execute());

		public void SetLayout(WindowLayout windowLayout) => WindowLayout = windowLayout;

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
				var saveActiveFiles = ActiveFiles;
				var saveFocused = Focused;
				var saveWindowLayout = WindowLayout;

				SetActiveFile(neFile);
				WindowLayout = new WindowLayout(1, 1);

				RenderNEWindowUI();

				var result = action();

				ActiveFiles = saveActiveFiles;
				Focused = saveFocused;
				WindowLayout = saveWindowLayout;

				return result;
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

		public static CommandLineParams ParseCommandLine(string commandLine) => CommandLineVisitor.GetCommandLineParams(commandLine);

		public void SetupDiff()
		{
			for (var ctr = 0; ctr + 1 < NEFiles.Count; ctr += 2)
			{
				NEFiles[ctr].DiffTarget = NEFiles[ctr + 1];
				if (NEFiles[ctr].ContentType == ParserType.None)
					NEFiles[ctr].ContentType = NEFiles[ctr + 1].ContentType;
				if (NEFiles[ctr + 1].ContentType == ParserType.None)
					NEFiles[ctr + 1].ContentType = NEFiles[ctr].ContentType;
			}
			SetLayout(new WindowLayout(maxColumns: 2));
		}

		public NEFile GetFile(string fileName) => ActiveFiles.FirstOrDefault(neFile => neFile.FileName == fileName);
	}
}
