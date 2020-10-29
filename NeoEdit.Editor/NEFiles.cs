using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.CommandLine;
using NeoEdit.Editor.PreExecution;
using NeoEdit.Editor.Transactional;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEFiles : INEFiles
	{
		public const int KeysAndValuesCount = 10;

		public INEFilesWindow FilesWindow { get; }

		public static List<NEFiles> Instances { get; } = new List<NEFiles>();

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

		EditorExecuteState state;

		public DateTime LastActivated { get; set; }

		public bool timeNextAction;
		MacroAction lastAction;

		public NEFiles(bool addEmpty = false)
		{
			Instances.Add(this);

			oldFilesList = newFilesList = new FilesList(this);
			oldWindowLayout = newWindowLayout = new WindowLayout(1, 1);

			BeginTransaction(new EditorExecuteState());
			FilesWindow = INEFilesWindowStatic.CreateINEFilesWindow(this);
			if (addEmpty)
				AddFile(new NEFile());
			Commit();
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

		static IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), KeysAndValuesCount).ToArray();
		Dictionary<INEFile, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var keysAndValuesMap = AllFiles.ToDictionary(x => x as INEFile, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				AllFiles.ForEach(neFile => keysAndValuesMap[neFile] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == ActiveFiles.Count)
				ActiveFiles.ForEach((neFile, index) => keysAndValuesMap[neFile] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		public void RenderFilesWindow()
		{
			var renderParameters = new RenderParameters
			{
				AllFiles = ActiveOnly ? ActiveFiles : AllFiles,
				ActiveFiles = ActiveFiles,
				FocusedFile = Focused,
				WindowLayout = WindowLayout,
				StatusBar = GetStatusBar(),
				MenuStatus = GetMenuStatus(),
			};
			FilesWindow.Render(renderParameters);
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

		public bool StopTasks()
		{
			var result = false;
			if (playingMacro != null)
			{
				playingMacro = null;
				result = true;
			}
			if (TaskRunner.Cancel())
				result = true;
			return result;
		}

		public bool KillTasks()
		{
			playingMacro = null;
			TaskRunner.ForceCancel();
			return true;
		}

		public void PlayMacro()
		{
			if (playingMacro == null)
				return;

			FilesWindow.SetMacroProgress(0);
			var stepIndex = 0;
			DateTime lastTime = DateTime.MinValue;
			while (true)
			{
				var macro = playingMacro;
				if (macro == null)
					break;

				if (stepIndex == macro.Actions.Count)
				{
					var action = playingMacroNextAction;
					playingMacro = null;
					playingMacroNextAction = null;
					stepIndex = 0;
					// The action may queue up another macro
					action?.Invoke();
					continue;
				}

				var now = DateTime.Now;
				if ((now - lastTime).TotalMilliseconds >= 100)
				{
					lastTime = now;
					FilesWindow.SetMacroProgress((double)stepIndex / macro.Actions.Count);
				}

				if (!RunCommand(macro.Actions[stepIndex++].GetExecuteState(this), true))
				{
					playingMacro = null;
					playingMacroNextAction = null;
					break;
				}
				if (MacroVisualize)
					RenderFilesWindow();
			}
			FilesWindow.SetMacroProgress(null);
		}

		public void HandleCommand(ExecuteState state, Func<bool> skipDraw = null)
		{
			var editorState = new EditorExecuteState(this, state);
			RunCommand(editorState);
			PlayMacro();
			if (skipDraw?.Invoke() != true)
				RenderFilesWindow();
		}

		bool RunCommand(EditorExecuteState state, bool inMacro = false)
		{
			try
			{
				if (state.Command == NECommand.Macro_RepeatLastAction)
				{
					if (lastAction == null)
						throw new Exception("No last action available");
					state = lastAction.GetExecuteState(this);
					inMacro = true;
				}

				BeginTransaction(state);

				state.ClipboardDataMapFunc = GetClipboardDataMap;
				state.KeysAndValuesFunc = GetKeysAndValuesMap;

				if ((!inMacro) && (state.Configuration == null))
					state.Configuration = NEFile.Configure(state);

				Stopwatch sw = null;
				if (timeNextAction)
					sw = Stopwatch.StartNew();

				FilesWindow.SetTaskRunnerProgress(0);
				state.PreExecution = NEFile.PreExecute(state);
				if (state.PreExecution != PreExecutionStop.Stop)
					TaskRunner.Run(Execute, percent => FilesWindow.SetTaskRunnerProgress(percent));
				FilesWindow.SetTaskRunnerProgress(null);
				PostExecute();

				if (sw != null)
				{
					timeNextAction = false;
					FilesWindow.RunDialog_ShowMessage("Timer", $"Elapsed time: {sw.ElapsedMilliseconds:n} ms", MessageOptions.Ok, MessageOptions.None, MessageOptions.None);
				}

				var action = MacroAction.GetMacroAction(state);
				if (action != null)
				{
					lastAction = action;
					recordingMacro?.AddAction(action);
				}

				Commit();
				return true;
			}
			catch (OperationCanceledException) { }
			catch (Exception ex) { FilesWindow.ShowExceptionMessage(ex); }

			FilesWindow.SetTaskRunnerProgress(null);
			Rollback();
			return false;
		}

		public void Execute() => ActiveFiles.AsTaskRunner().ForAll(neFile => neFile.Execute());

		void PostExecute()
		{
			var filesAdded = 0;
			NEClipboard setClipboard = null;
			List<KeysAndValues>[] setKeysAndValues = null;
			var dragFiles = new List<string>();
			foreach (var neFile in ActiveFiles)
			{
				foreach (var newFileTuple in neFile.FilesToAdd)
				{
					AddFile(newFileTuple.neFile, newFileTuple.index + filesAdded);
					++filesAdded;
				}

				if (neFile.ClipboardDataSet)
				{
					if (setClipboard == null)
						setClipboard = new NEClipboard();
					setClipboard.Add(neFile.ClipboardData.Item1);
					setClipboard.IsCut = neFile.ClipboardData.Item2;
				}

				for (var kvIndex = 0; kvIndex < KeysAndValuesCount; ++kvIndex)
					if (neFile.KeysAndValuesSet(kvIndex))
					{
						if (setKeysAndValues == null)
							setKeysAndValues = new List<KeysAndValues>[KeysAndValuesCount];
						if (setKeysAndValues[kvIndex] == null)
							setKeysAndValues[kvIndex] = new List<KeysAndValues>();
						setKeysAndValues[kvIndex].Add(neFile.GetKeysAndValues(kvIndex));
					}

				dragFiles.AddRange(neFile.DragFiles);
			}

			if (setClipboard != null)
				NEClipboard.Current = setClipboard;

			if (setKeysAndValues != null)
				for (var kvIndex = 0; kvIndex < KeysAndValuesCount; ++kvIndex)
					if (setKeysAndValues[kvIndex] != null)
						keysAndValues[kvIndex] = setKeysAndValues[kvIndex];

			if (dragFiles.Any())
			{
				var nonExisting = dragFiles.Where(x => !File.Exists(x)).ToList();
				if (nonExisting.Any())
					throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
				// TODO: Make these files actually do something
				//Focused.DragFiles = fileNames;
			}
		}

		public void SetLayout(WindowLayout windowLayout) => WindowLayout = windowLayout;

		public void AddFile(NEFile neFile, int? index = null, bool canReplace = true)
		{
			if ((canReplace) && (!index.HasValue) && (Focused != null) && (Focused.Empty()) && (oldFilesList.Contains(Focused)))
			{
				index = AllFiles.FindIndex(Focused);
				RemoveFile(Focused);
			}

			InsertFile(neFile, index);
		}

		public void AddDiff(NEFile neFile1, NEFile neFile2)
		{
			if (neFile1.ContentType == ParserType.None)
				neFile1.ContentType = neFile2.ContentType;
			if (neFile2.ContentType == ParserType.None)
				neFile2.ContentType = neFile1.ContentType;
			AddFile(neFile1);
			AddFile(neFile2);
			neFile1.DiffTarget = neFile2;
			SetLayout(new WindowLayout(maxColumns: 2));
		}

		void AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, int? index1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, int? index2 = null, ShutdownData shutdownData2 = null)
		{
			var te1 = new NEFile(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, index1, shutdownData1);
			var te2 = new NEFile(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, index2, shutdownData2);
			AddDiff(te1, te2);
		}

		bool FileIsActive(NEFile neFile) => ActiveFiles.Contains(neFile);

		public int GetFileIndex(NEFile neFile, bool activeOnly = false)
		{
			var index = (activeOnly ? ActiveFiles : AllFiles).FindIndex(neFile);
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void MovePrevNext(int offset, bool shiftDown, bool orderByActive = false)
		{
			if (AllFiles.Count() <= 1)
				return;

			NEFile neFile;
			if (Focused == null)
				neFile = AllFiles.GetIndex(0);
			else
			{
				var neFiles = orderByActive ? AllFiles.OrderByDescending(x => x.LastActive).ToList() : AllFiles as IList<NEFile>;
				var index = neFiles.FindIndex(Focused) + offset;
				if (index < 0)
					index += AllFiles.Count();
				if (index >= AllFiles.Count())
					index -= AllFiles.Count();
				neFile = neFiles[index];
			}
			if (!shiftDown)
				ClearAllActive();
			SetActive(neFile);
			Focused = neFile;
		}

		void Move(NEFile neFile, int newIndex)
		{
			RemoveFile(neFile);
			InsertFile(neFile, newIndex);
		}

		public bool GotoFile(string fileName, int? line, int? column, int? index)
		{
			var neFile = AllFiles.FirstOrDefault(x => x.FileName == fileName);
			if (neFile == null)
				return false;
			//Activate();
			ClearAllActive();
			SetActive(neFile);
			Focused = neFile;
			//TODO
			//neFile.Execute_File_Refresh();
			//neFile.Goto(line, column, index);
			return true;
		}

		public T ShowFile<T>(NEFile neFile, Func<T> action)
		{
			lock (this)
			{
				var saveFilesList = newFilesList;
				newFilesList = new FilesList(newFilesList);
				ClearAllActive();
				SetActive(neFile);

				var saveWindowLayout = newWindowLayout;
				WindowLayout = new WindowLayout(1, 1);

				RenderFilesWindow();

				var result = action();

				newFilesList = saveFilesList;
				newWindowLayout = saveWindowLayout;

				return result;
			}
		}

		public void SetDisplaySize(int columns, int rows)
		{
			DisplayColumns = columns;
			DisplayRows = rows;
		}

		void SetForeground() => FilesWindow.SetForeground();

		List<string> GetStatusBar()
		{
			string plural(int count, string item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			var status = new List<string>();
			status.Add($"Active: {plural(ActiveFiles.Count, "file")}, {plural(ActiveFiles.Sum(neFile => neFile.Selections.Count), "selection")}, {ActiveFiles.Select(neFile => neFile.Selections.Count).DefaultIfEmpty(0).Min():n0} min / {ActiveFiles.Select(neFile => neFile.Selections.Count).DefaultIfEmpty(0).Max():n0} max");
			status.Add($"Inactive: {plural(AllFiles.Except(ActiveFiles).Count(), "file")}, {plural(AllFiles.Except(ActiveFiles).Sum(neFile => neFile.Selections.Count), "selection")}");
			status.Add($"Total: {plural(AllFiles.Count(), "file")}, {plural(AllFiles.Sum(neFile => neFile.Selections.Count), "selection")}");
			status.Add($"Clipboard: {plural(NEClipboard.Current?.Count ?? 0, "file")}, {plural(NEClipboard.Current?.ChildCount ?? 0, "selection")}");
			status.Add($"Keys/Values: {string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");
			return status;
		}

		public static bool HandlesKey(ModifierKeys modifiers, Key key)
		{
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Escape:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
				case Key.Home:
				case Key.End:
				case Key.PageUp:
				case Key.PageDown:
				case Key.Tab:
				case Key.Enter:
					return true;
			}

			return false;
		}

		public static CommandLineParams ParseCommandLine(string commandLine) => CommandLineVisitor.GetCommandLineParams(commandLine);

		public static void CreateFiles(CommandLineParams commandLineParams)
		{
			NEFiles neFiles = null;
			try
			{
				if (commandLineParams.Background)
					return;

				if (!commandLineParams.Files.Any())
				{
					new NEFiles(true);
					return;
				}

				var shutdownData = string.IsNullOrWhiteSpace(commandLineParams.Wait) ? null : new ShutdownData(commandLineParams.Wait, commandLineParams.Files.Count);
				if (!commandLineParams.Diff)
					neFiles = Instances.OrderByDescending(x => x.LastActivated).FirstOrDefault();
				if (neFiles == null)
					neFiles = new NEFiles();
				foreach (var file in commandLineParams.Files)
				{
					if (commandLineParams.Existing)
					{
						var neFile = Instances.OrderByDescending(x => x.LastActivated).Select(x => x.GetFile(file.FileName)).NonNull().FirstOrDefault();
						if (neFile != null)
						{
							neFiles.HandleCommand(new ExecuteState(NECommand.Internal_GotoFile) { Configuration = new Configuration_Internal_GotoFile { NEFile = neFile, Line = file.Line, Column = file.Column, Index = file.Index } });
							continue;
						}
					}

					neFiles.HandleCommand(new ExecuteState(NECommand.Internal_AddFile) { Configuration = new Configuration_Internal_AddFile { NEFile = new NEFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData) } });
				}

				if (commandLineParams.Diff)
					neFiles.HandleCommand(new ExecuteState(NECommand.Internal_SetupDiff));

				neFiles.FilesWindow.SetForeground();
			}
			catch (Exception ex)
			{
				if (neFiles != null)
					neFiles.FilesWindow.ShowExceptionMessage(ex);
				else
					INEFilesWindowStatic.ShowExceptionMessage(ex);
			}
		}

		NEFile GetFile(string fileName) => ActiveFiles.FirstOrDefault(neFile => neFile.FileName == fileName);

		public static void AddFilesFromClipboards(NEFiles neFiles)
		{
			var index = 0;
			foreach (var strs in NEClipboard.Current)
			{
				++index;
				var ending = strs.Any(str => (!str.EndsWith("\r")) && (!str.EndsWith("\n"))) ? "\r\n" : "";
				var sb = new StringBuilder(strs.Sum(str => str.Length + ending.Length));
				var sels = new List<Range>();
				foreach (var str in strs)
				{
					var start = sb.Length;
					sb.Append(str);
					sels.Add(new Range(sb.Length, start));
					sb.Append(ending);
				}
				var te = new NEFile(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				neFiles.AddFile(te, canReplace: index == 1);
				te.Selections = sels;
			}
		}

		public static void AddFilesFromClipboardSelections(NEFiles neFiles) => NEClipboard.Current.Strings.ForEach((str, index) => neFiles.AddFile(new NEFile(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false)));
	}
}
