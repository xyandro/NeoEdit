using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public partial class Tabs : ITabs
	{
		public ITabsWindow TabsWindow { get; }

		public static List<Tabs> Instances { get; } = new List<Tabs>();

		int tabColumns;
		public int TabColumns
		{
			get => tabColumns;
			private set
			{
				tabColumns = value;
				AllTabs.ForEach(tab => tab.ResetView());
			}
		}

		public int TabRows { get; private set; }

		ExecuteState state;

		bool timeNextAction;
		MacroAction lastAction;

		public Tabs(bool addEmpty = false)
		{
			Instances.Add(this);

			oldTabsList = newTabsList = new TabsList(this);
			oldWindowLayout = newWindowLayout = new WindowLayout(1, 1);

			BeginTransaction(new ExecuteState(NECommand.None));
			TabsWindow = ITabsWindowStatic.CreateITabsWindow(this);
			if (addEmpty)
				Execute_File_New_New(false);
			Commit();
		}

		IReadOnlyDictionary<ITab, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var clipboardDataMap = AllTabs.ToDictionary(x => x as ITab, x => empty);

			if (NEClipboard.Current.Count == ActiveTabs.Count)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[ActiveTabs.GetIndex(index)] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ChildCount == ActiveTabs.Count)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[ActiveTabs.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == ActiveTabs.Sum(tab => tab.Selections.Count)))
				NEClipboard.Current.Strings.Take(ActiveTabs.Select(tab => tab.Selections.Count)).ForEach((obj, index) => clipboardDataMap[ActiveTabs.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(obj.ToList(), NEClipboard.Current.IsCut));
			else
			{
				var strs = NEClipboard.Current.Strings;
				ActiveTabs.ForEach(tab => clipboardDataMap[tab] = new Tuple<IReadOnlyList<string>, bool?>(strs, NEClipboard.Current.IsCut));
			}

			return clipboardDataMap;
		}

		static IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), 10).ToArray();
		Dictionary<ITab, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var keysAndValuesMap = AllTabs.ToDictionary(x => x as ITab, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				AllTabs.ForEach(tab => keysAndValuesMap[tab] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == ActiveTabs.Count)
				ActiveTabs.ForEach((tab, index) => keysAndValuesMap[tab] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		void RenderTabsWindow()
		{
			var renderParameters = new RenderParameters
			{
				AllTabs = WindowLayout.ActiveOnly ? ActiveTabs : WindowLayout.ActiveFirst ? ActiveFirstTabs : AllTabs,
				ActiveTabs = ActiveTabs,
				FocusedTab = Focused,
				WindowLayout = WindowLayout,
				StatusBar = GetStatusBar(),
				MenuStatus = GetMenuStatus(),
			};
			TabsWindow.Render(renderParameters);
		}

		Dictionary<string, bool?> GetMenuStatus()
		{
			bool? GetMultiStatus(Func<Tab, bool> func)
			{
				var results = ActiveTabs.Select(func).Distinct().Take(2).ToList();
				if (results.Count != 1)
					return default;
				return results[0];
			}

			return new Dictionary<string, bool?>
			{
				[nameof(NECommand.File_AutoRefresh)] = GetMultiStatus(tab => tab.AutoRefresh),
				[nameof(NECommand.File_Encrypt)] = GetMultiStatus(tab => !string.IsNullOrWhiteSpace(tab.AESKey)),
				[nameof(NECommand.File_Compress)] = GetMultiStatus(tab => tab.Compressed),
				[nameof(NECommand.File_DontExitOnClose)] = Settings.DontExitOnClose,
				[nameof(NECommand.Edit_Navigate_JumpBy_Words)] = GetMultiStatus(tab => tab.JumpBy == JumpByType.Words),
				[nameof(NECommand.Edit_Navigate_JumpBy_Numbers)] = GetMultiStatus(tab => tab.JumpBy == JumpByType.Numbers),
				[nameof(NECommand.Edit_Navigate_JumpBy_Paths)] = GetMultiStatus(tab => tab.JumpBy == JumpByType.Paths),
				[nameof(NECommand.Edit_EscapeClearsSelections)] = Settings.EscapeClearsSelections,
				[nameof(NECommand.Diff_IgnoreWhitespace)] = GetMultiStatus(tab => tab.DiffIgnoreWhitespace),
				[nameof(NECommand.Diff_IgnoreCase)] = GetMultiStatus(tab => tab.DiffIgnoreCase),
				[nameof(NECommand.Diff_IgnoreNumbers)] = GetMultiStatus(tab => tab.DiffIgnoreNumbers),
				[nameof(NECommand.Diff_IgnoreLineEndings)] = GetMultiStatus(tab => tab.DiffIgnoreLineEndings),
				[nameof(NECommand.Content_Type_None)] = GetMultiStatus(tab => tab.ContentType == ParserType.None),
				[nameof(NECommand.Content_Type_Balanced)] = GetMultiStatus(tab => tab.ContentType == ParserType.Balanced),
				[nameof(NECommand.Content_Type_Columns)] = GetMultiStatus(tab => tab.ContentType == ParserType.Columns),
				[nameof(NECommand.Content_Type_CPlusPlus)] = GetMultiStatus(tab => tab.ContentType == ParserType.CPlusPlus),
				[nameof(NECommand.Content_Type_CSharp)] = GetMultiStatus(tab => tab.ContentType == ParserType.CSharp),
				[nameof(NECommand.Content_Type_CSV)] = GetMultiStatus(tab => tab.ContentType == ParserType.CSV),
				[nameof(NECommand.Content_Type_ExactColumns)] = GetMultiStatus(tab => tab.ContentType == ParserType.ExactColumns),
				[nameof(NECommand.Content_Type_HTML)] = GetMultiStatus(tab => tab.ContentType == ParserType.HTML),
				[nameof(NECommand.Content_Type_JSON)] = GetMultiStatus(tab => tab.ContentType == ParserType.JSON),
				[nameof(NECommand.Content_Type_SQL)] = GetMultiStatus(tab => tab.ContentType == ParserType.SQL),
				[nameof(NECommand.Content_Type_TSV)] = GetMultiStatus(tab => tab.ContentType == ParserType.TSV),
				[nameof(NECommand.Content_Type_XML)] = GetMultiStatus(tab => tab.ContentType == ParserType.XML),
				[nameof(NECommand.Content_HighlightSyntax)] = GetMultiStatus(tab => tab.HighlightSyntax),
				[nameof(NECommand.Content_StrictParsing)] = GetMultiStatus(tab => tab.StrictParsing),
				[nameof(NECommand.Content_KeepSelections)] = GetMultiStatus(tab => tab.KeepSelections),
				[nameof(NECommand.Macro_Visualize)] = MacroVisualize,
				[nameof(NECommand.Window_Font_ShowSpecial)] = Font.ShowSpecialChars,
				[nameof(NECommand.Window_ViewBinary)] = GetMultiStatus(tab => tab.ViewBinary),
			};
		}

		public bool CancelActive()
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

		void PlayMacro()
		{
			if (playingMacro == null)
				return;

			TabsWindow.SetMacroProgress(0);
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
					TabsWindow.SetMacroProgress((double)stepIndex / macro.Actions.Count);
				}

				if (!RunCommand(macro.Actions[stepIndex++].GetExecuteState(), true))
				{
					playingMacro = null;
					playingMacroNextAction = null;
					break;
				}
				if (MacroVisualize)
					RenderTabsWindow();
			}
			TabsWindow.SetMacroProgress(null);
		}

		public void HandleCommand(ExecuteState state, Func<bool> skipDraw = null)
		{
			RunCommand(state);
			PlayMacro();
			if ((skipDraw == null) || (!skipDraw()))
				RenderTabsWindow();
		}

		bool RunCommand(ExecuteState state, bool inMacro = false)
		{
			try
			{
				if (state.Command == NECommand.Macro_RepeatLastAction)
				{
					if (lastAction == null)
						throw new Exception("No last action available");
					state = lastAction.GetExecuteState();
					inMacro = true;
				}

				BeginTransaction(state);

				state.ClipboardDataMapFunc = GetClipboardDataMap;
				state.KeysAndValuesFunc = GetKeysAndValuesMap;

				if ((!inMacro) && (state.Configuration == null))
					state.Configuration = Configure();

				Stopwatch sw = null;
				if (timeNextAction)
					sw = Stopwatch.StartNew();

				TaskRunner.Add(Execute);
				TaskRunner.WaitForFinish(TabsWindow);
				PostExecute();

				if (sw != null)
				{
					timeNextAction = false;
					TabsWindow.RunMessageDialog("Timer", $"Elapsed time: {sw.ElapsedMilliseconds:n} ms", MessageOptions.Ok, MessageOptions.None, MessageOptions.None);
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
			catch (Exception ex) { TabsWindow.ShowExceptionMessage(ex); }

			Rollback();
			return false;
		}

		object Configure()
		{
			switch (state.Command)
			{
				case NECommand.File_Open_Open: return Configure_File_Open_Open();
				case NECommand.Macro_Open_Open: return Configure_File_Open_Open(Macro.MacroDirectory);
				case NECommand.Window_CustomGrid: return Configure_Window_CustomGrid();
			}

			if (Focused == null)
				return null;

			return Focused.Configure();
		}

		void Execute(TaskProgress progress)
		{
			progress.Name = "Tabs";
			switch (state.Command)
			{
				case NECommand.Internal_Activate: Execute_Internal_Activate(); break;
				case NECommand.Internal_AddTab: Execute_Internal_AddTab(state.Configuration as Tab); break;
				case NECommand.Internal_MouseActivate: Execute_Internal_MouseActivate(state.Configuration as Tab); break;
				case NECommand.Internal_CloseTab: Execute_Internal_CloseTab(state.Configuration as Tab); break;
				case NECommand.Internal_Key: Execute_Internal_Key(); break;
				case NECommand.Internal_Scroll: Execute_Internal_Scroll(); break;
				case NECommand.Internal_Mouse: Execute_Internal_Mouse(); break;
				case NECommand.File_New_New: Execute_File_New_New(state.ShiftDown); break;
				case NECommand.File_New_FromClipboards: Execute_File_New_FromClipboards(); break;
				case NECommand.File_New_FromClipboardSelections: Execute_File_New_FromClipboardSelections(); break;
				case NECommand.File_Open_Open: Execute_File_Open_Open(state.Configuration as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: Execute_File_Open_CopiedCut(); break;
				case NECommand.File_MoveToNewWindow: Execute_File_MoveToNewWindow(); break;
				case NECommand.File_Shell_Integrate: Execute_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: Execute_File_Shell_Unintegrate(); break;
				case NECommand.File_DontExitOnClose: Execute_File_DontExitOnClose(state.MultiStatus); break;
				case NECommand.File_Exit: Execute_File_Exit(); break;
				case NECommand.Edit_EscapeClearsSelections: Execute_Edit_EscapeClearsSelections(state.MultiStatus); break;
				case NECommand.Diff_Diff: Execute_Diff_Diff(state.ShiftDown); break;
				case NECommand.Diff_Select_LeftTab: Execute_Diff_Select_LeftRightBothTabs(true); break;
				case NECommand.Diff_Select_RightTab: Execute_Diff_Select_LeftRightBothTabs(false); break;
				case NECommand.Diff_Select_BothTabs: Execute_Diff_Select_LeftRightBothTabs(null); break;
				case NECommand.Macro_Record_Quick_1: Execute_Macro_Record_Quick(1); break;
				case NECommand.Macro_Record_Quick_2: Execute_Macro_Record_Quick(2); break;
				case NECommand.Macro_Record_Quick_3: Execute_Macro_Record_Quick(3); break;
				case NECommand.Macro_Record_Quick_4: Execute_Macro_Record_Quick(4); break;
				case NECommand.Macro_Record_Quick_5: Execute_Macro_Record_Quick(5); break;
				case NECommand.Macro_Record_Quick_6: Execute_Macro_Record_Quick(6); break;
				case NECommand.Macro_Record_Quick_7: Execute_Macro_Record_Quick(7); break;
				case NECommand.Macro_Record_Quick_8: Execute_Macro_Record_Quick(8); break;
				case NECommand.Macro_Record_Quick_9: Execute_Macro_Record_Quick(9); break;
				case NECommand.Macro_Record_Quick_10: Execute_Macro_Record_Quick(10); break;
				case NECommand.Macro_Record_Quick_11: Execute_Macro_Record_Quick(11); break;
				case NECommand.Macro_Record_Quick_12: Execute_Macro_Record_Quick(12); break;
				case NECommand.Macro_Record_Record: Execute_Macro_Record_Record(); break;
				case NECommand.Macro_Record_StopRecording: Execute_Macro_Record_StopRecording(); break;
				case NECommand.Macro_Append_Quick_1: Execute_Macro_Append_Quick(1); break;
				case NECommand.Macro_Append_Quick_2: Execute_Macro_Append_Quick(2); break;
				case NECommand.Macro_Append_Quick_3: Execute_Macro_Append_Quick(3); break;
				case NECommand.Macro_Append_Quick_4: Execute_Macro_Append_Quick(4); break;
				case NECommand.Macro_Append_Quick_5: Execute_Macro_Append_Quick(5); break;
				case NECommand.Macro_Append_Quick_6: Execute_Macro_Append_Quick(6); break;
				case NECommand.Macro_Append_Quick_7: Execute_Macro_Append_Quick(7); break;
				case NECommand.Macro_Append_Quick_8: Execute_Macro_Append_Quick(8); break;
				case NECommand.Macro_Append_Quick_9: Execute_Macro_Append_Quick(9); break;
				case NECommand.Macro_Append_Quick_10: Execute_Macro_Append_Quick(10); break;
				case NECommand.Macro_Append_Quick_11: Execute_Macro_Append_Quick(11); break;
				case NECommand.Macro_Append_Quick_12: Execute_Macro_Append_Quick(12); break;
				case NECommand.Macro_Append_Append: Execute_Macro_Append_Append(); break;
				case NECommand.Macro_Play_Quick_1: Execute_Macro_Play_Quick(1); break;
				case NECommand.Macro_Play_Quick_2: Execute_Macro_Play_Quick(2); break;
				case NECommand.Macro_Play_Quick_3: Execute_Macro_Play_Quick(3); break;
				case NECommand.Macro_Play_Quick_4: Execute_Macro_Play_Quick(4); break;
				case NECommand.Macro_Play_Quick_5: Execute_Macro_Play_Quick(5); break;
				case NECommand.Macro_Play_Quick_6: Execute_Macro_Play_Quick(6); break;
				case NECommand.Macro_Play_Quick_7: Execute_Macro_Play_Quick(7); break;
				case NECommand.Macro_Play_Quick_8: Execute_Macro_Play_Quick(8); break;
				case NECommand.Macro_Play_Quick_9: Execute_Macro_Play_Quick(9); break;
				case NECommand.Macro_Play_Quick_10: Execute_Macro_Play_Quick(10); break;
				case NECommand.Macro_Play_Quick_11: Execute_Macro_Play_Quick(11); break;
				case NECommand.Macro_Play_Quick_12: Execute_Macro_Play_Quick(12); break;
				case NECommand.Macro_Play_Play: Execute_Macro_Play_Play(); break;
				case NECommand.Macro_Play_Repeat: Execute_Macro_Play_Repeat(); break;
				case NECommand.Macro_Play_PlayOnCopiedFiles: Execute_Macro_Play_PlayOnCopiedFiles(); break;
				case NECommand.Macro_Open_Quick_1: Execute_Macro_Open_Quick(1); break;
				case NECommand.Macro_Open_Quick_2: Execute_Macro_Open_Quick(2); break;
				case NECommand.Macro_Open_Quick_3: Execute_Macro_Open_Quick(3); break;
				case NECommand.Macro_Open_Quick_4: Execute_Macro_Open_Quick(4); break;
				case NECommand.Macro_Open_Quick_5: Execute_Macro_Open_Quick(5); break;
				case NECommand.Macro_Open_Quick_6: Execute_Macro_Open_Quick(6); break;
				case NECommand.Macro_Open_Quick_7: Execute_Macro_Open_Quick(7); break;
				case NECommand.Macro_Open_Quick_8: Execute_Macro_Open_Quick(8); break;
				case NECommand.Macro_Open_Quick_9: Execute_Macro_Open_Quick(9); break;
				case NECommand.Macro_Open_Quick_10: Execute_Macro_Open_Quick(10); break;
				case NECommand.Macro_Open_Quick_11: Execute_Macro_Open_Quick(11); break;
				case NECommand.Macro_Open_Quick_12: Execute_Macro_Open_Quick(12); break;
				case NECommand.Macro_Open_Open: Execute_File_Open_Open(state.Configuration as OpenFileDialogResult); break;
				case NECommand.Macro_TimeNextAction: Execute_Macro_TimeNextAction(); break;
				case NECommand.Macro_Visualize: Execute_Macro_Visualize(state.MultiStatus); break;
				case NECommand.Window_NewWindow: Execute_Window_NewWindow(); break;
				case NECommand.Window_Full: Execute_Window_Full(); break;
				case NECommand.Window_Grid: Execute_Window_Grid(); break;
				case NECommand.Window_CustomGrid: Execute_Window_CustomGrid(state.Configuration as WindowLayout); break;
				case NECommand.Window_ActiveTabs: Execute_Window_ActiveTabs(); break;
				case NECommand.Window_Font_Size: Execute_Window_Font_Size(); break;
				case NECommand.Window_Font_ShowSpecial: Execute_Window_Font_ShowSpecial(state.MultiStatus); break;
				case NECommand.Window_Select_AllTabs: Execute_Window_Select_AllTabs(); break;
				case NECommand.Window_Select_NoTabs: Execute_Window_Select_NoTabs(); break;
				case NECommand.Window_Select_TabsWithSelections: Execute_Window_Select_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Select_TabsWithoutSelections: Execute_Window_Select_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Select_ModifiedTabs: Execute_Window_Select_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Select_UnmodifiedTabs: Execute_Window_Select_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Select_InactiveTabs: Execute_Window_Select_InactiveTabs(); break;
				case NECommand.Window_Close_TabsWithSelections: Execute_Window_Close_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Close_TabsWithoutSelections: Execute_Window_Close_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Close_ModifiedTabs: Execute_Window_Close_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Close_UnmodifiedTabs: Execute_Window_Close_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Close_ActiveTabs: Execute_Window_Close_ActiveInactiveTabs(true); break;
				case NECommand.Window_Close_InactiveTabs: Execute_Window_Close_ActiveInactiveTabs(false); break;
				case NECommand.Window_WordList: Execute_Window_WordList(); break;
				case NECommand.Help_About: Execute_Help_About(); break;
				case NECommand.Help_Tutorial: Execute_Help_Tutorial(); break;
				case NECommand.Help_Update: Execute_Help_Update(); break;
				case NECommand.Help_Extract: Execute_Help_Extract(); break;
				case NECommand.Help_RunGC: Execute_Help_RunGC(); break;
				default: ExecuteActiveTabs(); break;
			}
		}

		void ExecuteActiveTabs() => TaskRunner.Add(ActiveTabs.Select(tab => (Action<TaskProgress>)tab.Execute));

		void PostExecute()
		{
			ActiveTabs.Select(tab => tab.TabsToAdd).NonNull().SelectMany().ForEach(tab => AddTab(tab));

			var clipboardDatas = ActiveTabs.Select(tab => tab.ChangedClipboardData).NonNull().ToList();
			if (clipboardDatas.Any())
			{
				var newClipboard = new NEClipboard();
				foreach (var clipboardData in clipboardDatas)
				{
					newClipboard.Add(clipboardData.Item1);
					newClipboard.IsCut = clipboardData.Item2;
				}
				NEClipboard.Current = newClipboard;
			}

			for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
			{
				var newKeysAndValues = ActiveTabs.Select(tab => tab.GetChangedKeysAndValues(kvIndex)).NonNull().ToList();
				if (newKeysAndValues.Any())
					keysAndValues[kvIndex] = newKeysAndValues;
			}

			var dragFiles = ActiveTabs.Select(tab => tab.ChangedDragFiles).NonNull().SelectMany().Distinct().ToList();
			if (dragFiles.Any())
			{
				var nonExisting = dragFiles.Where(x => !File.Exists(x)).ToList();
				if (nonExisting.Any())
					throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
				// TODO: Make these files actually do something
				//Focused.DragFiles = fileNames;
			}
		}

		void SetLayout(WindowLayout windowLayout) => WindowLayout = windowLayout;

		public void AddTab(Tab tab, int? index = null, bool canReplace = true)
		{
			if ((canReplace) && (!index.HasValue) && (Focused != null) && (Focused.Empty()) && (oldTabsList.Contains(Focused)))
			{
				index = AllTabs.FindIndex(Focused);
				RemoveTab(Focused);
			}

			InsertTab(tab, index);
		}

		void AddDiff(Tab tab1, Tab tab2)
		{
			//TODO
			//if (tab1.ContentType == ParserType.None)
			//	tab1.ContentType = tab2.ContentType;
			//if (tab2.ContentType == ParserType.None)
			//	tab2.ContentType = tab1.ContentType;
			//AddTab(tab1);
			//AddTab(tab2);
			//tab1.DiffTarget = tab2;
			//SetLayout(maxColumns: 2);
		}

		void AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, int? index1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, int? index2 = null, ShutdownData shutdownData2 = null)
		{
			var te1 = new Tab(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, index1, shutdownData1);
			var te2 = new Tab(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, index2, shutdownData2);
			AddDiff(te1, te2);
		}

		bool TabIsActive(Tab tab) => ActiveTabs.Contains(tab);

		public int GetTabIndex(Tab tab, bool activeOnly = false)
		{
			var index = (activeOnly ? ActiveTabs : AllTabs).FindIndex(tab);
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		void MovePrevNext(int offset, bool shiftDown, bool orderByActive = false)
		{
			if (AllTabs.Count() <= 1)
				return;

			Tab tab;
			if (Focused == null)
				tab = AllTabs.GetIndex(0);
			else
			{
				var tabs = orderByActive ? AllTabs.OrderByDescending(x => x.LastActive).ToList() as IList<Tab> : AllTabs as IList<Tab>;
				var index = tabs.FindIndex(Focused) + offset;
				if (index < 0)
					index += AllTabs.Count();
				if (index >= AllTabs.Count())
					index -= AllTabs.Count();
				tab = tabs[index];
			}
			if (!shiftDown)
				ClearAllActive();
			SetActive(tab);
			Focused = tab;
		}

		void Move(Tab tab, int newIndex)
		{
			RemoveTab(tab);
			InsertTab(tab, newIndex);
		}

		public bool GotoTab(string fileName, int? line, int? column, int? index)
		{
			var tab = AllTabs.FirstOrDefault(x => x.FileName == fileName);
			if (tab == null)
				return false;
			//Activate();
			ClearAllActive();
			SetActive(tab);
			Focused = tab;
			//TODO
			//tab.Execute_File_Refresh();
			//tab.Goto(line, column, index);
			return true;
		}

		public void QueueActivateTabs() => TabsWindow.QueueActivateTabs();

		public T ShowTab<T>(Tab tab, Func<T> action)
		{
			lock (this)
			{
				var oldTabsData = newTabsList;
				newTabsList = new TabsList(newTabsList);

				ClearAllActive();
				SetActive(tab);
				RenderTabsWindow();

				var result = action();

				newTabsList = oldTabsData;

				return result;
			}
		}

		public void SetTabSize(int columns, int rows)
		{
			TabColumns = columns;
			TabRows = rows;
		}

		void SetForeground() => TabsWindow.SetForeground();

		List<string> GetStatusBar()
		{
			string plural(int count, string item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			var status = new List<string>();
			status.Add($"Active: {plural(ActiveTabs.Count, "file")}, {plural(ActiveTabs.Sum(tab => tab.Selections.Count), "selection")}, {ActiveTabs.Select(tab => tab.Selections.Count).DefaultIfEmpty(0).Min():n0} min / {ActiveTabs.Select(tab => tab.Selections.Count).DefaultIfEmpty(0).Max():n0} max");
			status.Add($"Inactive: {plural(AllTabs.Except(ActiveTabs).Count(), "file")}, {plural(AllTabs.Except(ActiveTabs).Sum(tab => tab.Selections.Count), "selection")}");
			status.Add($"Total: {plural(AllTabs.Count(), "file")}, {plural(AllTabs.Sum(tab => tab.Selections.Count), "selection")}");
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
	}
}
