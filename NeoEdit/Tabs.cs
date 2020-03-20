using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Misc;
using NeoEdit.Program.NEClipboards;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class Tabs
	{
		public TabsWindow TabsWindow { get; }

		public int TabColumns { get; private set; }
		public int TabRows { get; private set; }

		ExecuteState state;

		bool timeNextAction;
		MacroAction lastAction;

		public Tabs(TabsWindow tabsWindow)
		{
			TabsWindow = tabsWindow;
			oldAllTabs = newAllTabs = new OrderedHashSet<Tab>();
			oldActiveTabs = newActiveTabs = new OrderedHashSet<Tab>();
		}

		public IReadOnlyDictionary<Tab, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var clipboardDataMap = AllTabs.ToDictionary(x => x, x => empty);

			var activeTabs = SortedActiveTabs;
			if (NEClipboard.Current.Count == UnsortedActiveTabsCount)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[activeTabs.GetIndex(index)] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ChildCount == UnsortedActiveTabsCount)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[activeTabs.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == activeTabs.Sum(tab => tab.Selections.Count)))
				NEClipboard.Current.Strings.Take(activeTabs.Select(tab => tab.Selections.Count)).ForEach((obj, index) => clipboardDataMap[activeTabs.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(obj.ToList(), NEClipboard.Current.IsCut));
			else
			{
				var strs = NEClipboard.Current.Strings;
				activeTabs.ForEach(tab => clipboardDataMap[tab] = new Tuple<IReadOnlyList<string>, bool?>(strs, NEClipboard.Current.IsCut));
			}

			return clipboardDataMap;
		}

		public IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), 10).ToArray();
		public Dictionary<Tab, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var keysAndValuesMap = AllTabs.ToDictionary(x => x, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				AllTabs.ForEach(tab => keysAndValuesMap[tab] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == UnsortedActiveTabsCount)
				SortedActiveTabs.ForEach((tab, index) => keysAndValuesMap[tab] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		public Macro RecordingMacro { get; set; }
		public Macro MacroPlaying { get; set; }

		public bool HandleCommand(ExecuteState state, bool configure = true)
		{
			if ((MacroPlaying != null) && (configure))
			{
				// User is trying to do something in the middle of a macro
				if ((state.Command == NECommand.Internal_Key) && (state.Key == Key.Escape))
					MacroPlaying.Stop();
				return true;
			}

			bool commit = false;
			try
			{
				if (state.Command == NECommand.Macro_RepeatLastAction)
				{
					if (lastAction == null)
						throw new Exception("No last action available");
					state = lastAction.GetExecuteState();
					configure = false;
				}

				BeginTransaction(state);

				state.ClipboardDataMapFunc = GetClipboardDataMap;
				state.KeysAndValuesFunc = GetKeysAndValuesMap;

				if (configure)
				{
					if (MacroPlaying != null)
						return false;

					state.ActiveTabs = UnsortedActiveTabs;

					if (state.Configuration == null)
						state.Configuration = Configure();

					state.ActiveTabs = null;
				}

				Stopwatch sw = null;
				if (timeNextAction)
					sw = Stopwatch.StartNew();

				Execute();

				if (sw != null)
				{
					timeNextAction = false;
					new Message(state.Window)
					{
						Title = "Timer",
						Text = $"Elapsed time: {sw.ElapsedMilliseconds:n} ms",
						Options = MessageOptions.Ok,
					}.Show();
				}

				var action = MacroAction.GetMacroAction(state);
				if (action != null)
				{
					lastAction = action;
					if (RecordingMacro != null)
						RecordingMacro?.AddAction(action);
				}

				commit = true;
			}
			catch (OperationCanceledException) { }
			finally
			{
				if (commit)
				{
					Commit();
					PostExecute();
				}
				else
					Rollback();

				TabsWindow.QueueDraw();
			}

			return commit;
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


		void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Activate: Execute_Internal_Activate(); break;
				case NECommand.Internal_AddTab: Execute_Internal_AddTab(state.Configuration as Tab); break;
				case NECommand.Internal_MouseActivate: Execute_Internal_MouseActivate(state.Configuration as Tab); break;
				case NECommand.Internal_CloseTab: Execute_Internal_CloseTab(state.Configuration as Tab); break;
				case NECommand.Internal_Key: if (Execute_Internal_Key()) return; break;
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
				case NECommand.Window_CustomGrid: Execute_Window_CustomGrid(state.Configuration as WindowCustomGridDialog.Result); break;
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
			}

			SortedActiveTabs.ForEach(tab => tab.Execute());
		}

		void PostExecute()
		{
			var clipboardDatas = SortedActiveTabs.Select(tab => tab.ChangedClipboardData).NonNull().ToList();
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
				var newKeysAndValues = SortedActiveTabs.Select(tab => tab.GetChangedKeysAndValues(kvIndex)).NonNull().ToList();
				if (newKeysAndValues.Any())
					keysAndValues[kvIndex] = newKeysAndValues;
			}

			var dragFiles = SortedActiveTabs.Select(tab => tab.ChangedDragFiles).NonNull().SelectMany().Distinct().ToList();
			if (dragFiles.Any())
			{
				var nonExisting = dragFiles.Where(x => !File.Exists(x)).ToList();
				if (nonExisting.Any())
					throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
				// TODO: Make these files actually do something
				//Focused.DragFiles = fileNames;
			}
		}

		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null)
		{
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
		}

		public void AddTab(Tab tab, int? index = null, bool canReplace = true)
		{
			if ((canReplace) && (!index.HasValue) && (Focused != null) && (Focused.Empty()) && (oldAllTabs.Contains(Focused)))
			{
				index = AllTabs.FindIndex(Focused);
				RemoveTab(Focused);
			}

			InsertTab(tab, index);
		}

		public void AddDiff(Tab tab1, Tab tab2)
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

		public void AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, int? index1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, int? index2 = null, ShutdownData shutdownData2 = null)
		{
			var te1 = new Tab(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, index1, shutdownData1);
			var te2 = new Tab(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, index2, shutdownData2);
			AddDiff(te1, te2);
		}

		public bool TabIsActive(Tab tab) => UnsortedActiveTabs.Contains(tab);

		public int GetTabIndex(Tab tab, bool activeOnly = false)
		{
			var index = (activeOnly ? SortedActiveTabs : AllTabs).FindIndex(tab);
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
				var tabs = (orderByActive ? AllTabs.OrderByDescending(x => x.LastActive).ToList() : AllTabs) as IList<Tab>;
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

		public void Move(Tab tab, int newIndex)
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

		public void ShowTab(Tab tab, Action action) => ShowTab(tab, () => { action(); return 0; });

		public T ShowTab<T>(Tab tab, Func<T> action)
		{
			var activeTabs = UnsortedActiveTabs.ToList();
			var focusedTab = Focused;
			ClearAllActive();
			SetActive(tab);
			Focused = tab;
			TabsWindow.QueueDraw();

			var result = action();

			ClearAllActive();
			foreach (var activeTab in activeTabs)
				SetActive(activeTab);
			Focused = focusedTab;

			return result;
		}

		public void SetTabSize(int columns, int rows)
		{
			TabColumns = columns;
			TabRows = rows;
		}
	}
}
