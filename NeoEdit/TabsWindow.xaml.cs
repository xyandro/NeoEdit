﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Misc;
using NeoEdit.Program.NEClipboards;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		[DepProp]
		public int ActiveUpdateCount { get { return UIHelper<TabsWindow>.GetPropValue<int>(this); } private set { UIHelper<TabsWindow>.SetPropValue(this, value); } }
		[DepProp]
		public int WindowIndex { get { return UIHelper<TabsWindow>.GetPropValue<int>(this); } private set { UIHelper<TabsWindow>.SetPropValue(this, value); } }
		public DateTime LastActivated { get; private set; }

		static int curWindowIndex = 0;

		Action<TextEditorData> ShowTextEditor;
		int addedCounter = 0, lastAddedCounter = -1, textEditorOrder = 0;

		static bool showIndex;
		static public bool ShowIndex { get => showIndex; set { showIndex = value; ShowIndexChanged?.Invoke(null, EventArgs.Empty); } }
		public static event EventHandler ShowIndexChanged;

		static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
		static readonly Brush FocusedWindowBorderBrush = new SolidColorBrush(Color.FromRgb(31, 113, 216));
		static readonly Brush ActiveWindowBorderBrush = new SolidColorBrush(Color.FromRgb(28, 101, 193));
		static readonly Brush InactiveWindowBorderBrush = Brushes.Transparent;
		static readonly Brush FocusedWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(23, 81, 156));
		static readonly Brush ActiveWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(14, 50, 96));
		static readonly Brush InactiveWindowBackgroundBrush = Brushes.Transparent;

		static TabsWindow()
		{
			UIHelper<TabsWindow>.Register();
			OutlineBrush.Freeze();
			BackgroundBrush.Freeze();
			FocusedWindowBorderBrush.Freeze();
			ActiveWindowBorderBrush.Freeze();
			InactiveWindowBorderBrush.Freeze();
			FocusedWindowBackgroundBrush.Freeze();
			ActiveWindowBackgroundBrush.Freeze();
			InactiveWindowBackgroundBrush.Freeze();
		}

		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

		readonly RunOnceTimer doActivatedTimer;
		public TabsWindow(bool addEmpty = false)
		{
			oldTabs = newTabs = oldActiveTabs = newActiveTabs = new List<TextEditorData>();
			oldRows = newRows = oldColumns = newColumns = 1;

			Focusable = true;
			FocusVisualStyle = null;
			VerticalAlignment = VerticalAlignment.Stretch;

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new CommandState(command)));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			WindowIndex = ++curWindowIndex;

			AllowDrop = true;
			Drop += OnDrop;
			doActivatedTimer = new RunOnceTimer(() => DoActivated());
			NEClipboard.ClipboardChanged += () => SetStatusBarText();
			Activated += OnActivated;

			//SizeChanged += (s, e) => QueueUpdateLayout();
			//scrollBar.ValueChanged += (s, e) => QueueUpdateLayout(false);
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			if (addEmpty)
				AddTextEditor(new TextEditorData());

			DrawAll();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			try { SetPosition(Settings.WindowPosition); } catch { }
		}

		Dictionary<TextEditorData, List<string>> clipboard;
		public List<string> GetClipboard(TextEditorData textEditor)
		{
			if (clipboard == null)
			{
				var empty = new List<string>();
				clipboard = Tabs.ToDictionary(x => x, x => empty);

				var activeTabs = ActiveTabs.ToList();

				if (NEClipboard.Current.Count == activeTabs.Count)
					NEClipboard.Current.ForEach((cb, index) => clipboard[activeTabs[index]] = cb.Strings);
				else if (NEClipboard.Current.ChildCount == activeTabs.Count)
					NEClipboard.Current.Strings.ForEach((str, index) => clipboard[activeTabs[index]] = new List<string> { str });
				else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == activeTabs.Sum(tab => tab.NumSelections)))
					NEClipboard.Current.Strings.Take(activeTabs.Select(tab => tab.NumSelections)).ForEach((obj, index) => clipboard[activeTabs[index]] = obj.ToList());
				else
				{
					var strs = NEClipboard.Current.Strings;
					activeTabs.ForEach(tab => clipboard[tab] = strs);
				}
			}

			return clipboard[textEditor];
		}

		static List<List<List<string>>> keysAndValues = Enumerable.Range(0, 10).Select(x => new List<List<string>>()).ToList();
		static List<Dictionary<string, int>> keysHash = new List<Dictionary<string, int>>();
		List<Dictionary<TextEditorData, List<string>>> keysAndValuesLookup = keysAndValues.Select(x => default(Dictionary<TextEditorData, List<string>>)).ToList();
		Dictionary<TextEditorData, Dictionary<string, int>> keysHashLookup;
		List<List<List<string>>> newKeysAndValues;
		bool newKeysCaseSensitive;

		void SetupNewKeys()
		{
			if (newKeysAndValues == null)
				return;

			for (var kvIndex = 0; kvIndex < keysAndValues.Count; ++kvIndex)
				if (newKeysAndValues[kvIndex] != null)
				{
					keysAndValues[kvIndex] = newKeysAndValues[kvIndex];
					if (kvIndex == 0)
					{
						keysHash.Clear();
						for (var list = 0; list < keysAndValues[0].Count; ++list)
						{
							var hash = new Dictionary<string, int>(newKeysCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
							for (var index = 0; index < keysAndValues[0][list].Count; ++index)
								hash[keysAndValues[0][list][index]] = index;
							keysHash.Add(hash);
						}
					}
				}
		}

		public void SetKeysAndValues(int kvIndex, List<string> values, bool? caseSensitive = null)
		{
			if (kvIndex == 0)
			{
				if (caseSensitive.HasValue)
					newKeysCaseSensitive = caseSensitive.Value;
				if (values.Distinct(str => newKeysCaseSensitive ? str : str.ToLowerInvariant()).Count() != values.Count)
					throw new ArgumentException("Cannot have duplicate keys");
			}

			newKeysAndValues = newKeysAndValues ?? keysAndValues.Select(x => default(List<List<string>>)).ToList();
			newKeysAndValues[kvIndex] = newKeysAndValues[kvIndex] ?? new List<List<string>>();
			newKeysAndValues[kvIndex].Add(values);
		}

		public bool SetupKeysAndValuesLookup(int kvIndex, bool throwOnException = true)
		{
			if (keysAndValuesLookup[kvIndex] == null)
			{
				if (keysAndValues[kvIndex].Count == 1)
				{
					keysAndValuesLookup[kvIndex] = Tabs.ToDictionary(textEditor => textEditor, textEditor => keysAndValues[kvIndex][0]);
					if (kvIndex == 0)
						keysHashLookup = Tabs.ToDictionary(textEditor => textEditor, textEditor => keysHash[0]);
				}
				else
				{
					var activeTabs = ActiveTabs.ToList();
					if (keysAndValues[kvIndex].Count != activeTabs.Count)
					{
						if (throwOnException)
							throw new Exception("Tab count doesn't match keys count");
						return false;
					}
					keysAndValuesLookup[kvIndex] = activeTabs.Select((textEditor, index) => new { textEditor, index }).ToDictionary(obj => obj.textEditor, obj => keysAndValues[kvIndex][obj.index]);
					if (kvIndex == 0)
						keysHashLookup = activeTabs.Select((textEditor, index) => new { textEditor, index }).ToDictionary(obj => obj.textEditor, obj => keysHash[obj.index]);
				}
			}

			return true;
		}

		public List<string> GetKeysAndValues(TextEditorData textEditor, int index, bool throwOnException = true)
		{
			if (!SetupKeysAndValuesLookup(index, throwOnException))
				return null;
			if (!keysAndValuesLookup[index].ContainsKey(textEditor))
			{
				if (!throwOnException)
					return null;
				throw new Exception("Keys/values not available");
			}
			return keysAndValuesLookup[index][textEditor];
		}

		public Dictionary<string, int> GetKeysHash(TextEditorData textEditor)
		{
			SetupKeysAndValuesLookup(0);
			if (!keysHashLookup.ContainsKey(textEditor))
				throw new Exception("Keys hash not available");
			return keysHashLookup[textEditor];
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => AddTextEditor(new TextEditorData(file)));
			e.Handled = true;
		}

		public Macro RecordingMacro { get; set; }
		public Macro MacroPlaying { get; set; }

		//internal void RunCommand(NECommand command, bool? multiStatus)
		//{
		//	if (MacroPlaying != null)
		//		return;

		//	switch (command)
		//	{
		//		case NECommand.Macro_Record_Quick_1: Command_Macro_Record_Quick(1); return;
		//		case NECommand.Macro_Record_Quick_2: Command_Macro_Record_Quick(2); return;
		//		case NECommand.Macro_Record_Quick_3: Command_Macro_Record_Quick(3); return;
		//		case NECommand.Macro_Record_Quick_4: Command_Macro_Record_Quick(4); return;
		//		case NECommand.Macro_Record_Quick_5: Command_Macro_Record_Quick(5); return;
		//		case NECommand.Macro_Record_Quick_6: Command_Macro_Record_Quick(6); return;
		//		case NECommand.Macro_Record_Quick_7: Command_Macro_Record_Quick(7); return;
		//		case NECommand.Macro_Record_Quick_8: Command_Macro_Record_Quick(8); return;
		//		case NECommand.Macro_Record_Quick_9: Command_Macro_Record_Quick(9); return;
		//		case NECommand.Macro_Record_Quick_10: Command_Macro_Record_Quick(10); return;
		//		case NECommand.Macro_Record_Quick_11: Command_Macro_Record_Quick(11); return;
		//		case NECommand.Macro_Record_Quick_12: Command_Macro_Record_Quick(12); return;
		//		case NECommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
		//		case NECommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
		//		case NECommand.Macro_Append_Quick_1: Command_Macro_Append_Quick(1); return;
		//		case NECommand.Macro_Append_Quick_2: Command_Macro_Append_Quick(2); return;
		//		case NECommand.Macro_Append_Quick_3: Command_Macro_Append_Quick(3); return;
		//		case NECommand.Macro_Append_Quick_4: Command_Macro_Append_Quick(4); return;
		//		case NECommand.Macro_Append_Quick_5: Command_Macro_Append_Quick(5); return;
		//		case NECommand.Macro_Append_Quick_6: Command_Macro_Append_Quick(6); return;
		//		case NECommand.Macro_Append_Quick_7: Command_Macro_Append_Quick(7); return;
		//		case NECommand.Macro_Append_Quick_8: Command_Macro_Append_Quick(8); return;
		//		case NECommand.Macro_Append_Quick_9: Command_Macro_Append_Quick(9); return;
		//		case NECommand.Macro_Append_Quick_10: Command_Macro_Append_Quick(10); return;
		//		case NECommand.Macro_Append_Quick_11: Command_Macro_Append_Quick(11); return;
		//		case NECommand.Macro_Append_Quick_12: Command_Macro_Append_Quick(12); return;
		//		case NECommand.Macro_Append_Append: Command_Macro_Append_Append(); return;
		//		case NECommand.Macro_Play_Quick_1: Command_Macro_Play_Quick(1); return;
		//		case NECommand.Macro_Play_Quick_2: Command_Macro_Play_Quick(2); return;
		//		case NECommand.Macro_Play_Quick_3: Command_Macro_Play_Quick(3); return;
		//		case NECommand.Macro_Play_Quick_4: Command_Macro_Play_Quick(4); return;
		//		case NECommand.Macro_Play_Quick_5: Command_Macro_Play_Quick(5); return;
		//		case NECommand.Macro_Play_Quick_6: Command_Macro_Play_Quick(6); return;
		//		case NECommand.Macro_Play_Quick_7: Command_Macro_Play_Quick(7); return;
		//		case NECommand.Macro_Play_Quick_8: Command_Macro_Play_Quick(8); return;
		//		case NECommand.Macro_Play_Quick_9: Command_Macro_Play_Quick(9); return;
		//		case NECommand.Macro_Play_Quick_10: Command_Macro_Play_Quick(10); return;
		//		case NECommand.Macro_Play_Quick_11: Command_Macro_Play_Quick(11); return;
		//		case NECommand.Macro_Play_Quick_12: Command_Macro_Play_Quick(12); return;
		//		case NECommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
		//		case NECommand.Macro_Play_Repeat: Command_Macro_Play_Repeat(); return;
		//		case NECommand.Macro_Play_PlayOnCopiedFiles: Command_Macro_Play_PlayOnCopiedFiles(); return;
		//	}

		//	var shiftDown = this.shiftDown;

		//	object dialogResult;
		//	if (!GetDialogResult(command, out dialogResult, multiStatus))
		//		return;

		//	if (RecordingMacro != null)
		//		RecordingMacro.AddCommand(command, shiftDown, dialogResult, multiStatus);

		//	HandleCommand(command, shiftDown, dialogResult, multiStatus);
		//}

		NEClipboard newClipboard;
		public void AddClipboardStrings(IEnumerable<string> strings, bool? isCut = null)
		{
			newClipboard = newClipboard ?? new NEClipboard();
			newClipboard.Add(NEClipboardList.Create(strings));
			newClipboard.IsCut = isCut;
		}

		public void HandleCommand(CommandState state)
		{
			try
			{
				PreExecuteCommand(state);

				GetCommandParameters(state);

				ExecuteCommand(state);

				Commit();
				Tabs.ForEach(tab => tab.Commit());

				DrawAll();
			}
			catch
			{
				Tabs.ForEach(tab => tab.Rollback());
				Rollback();
				throw;
			}
		}

		void PreExecuteCommand(CommandState state) => ActiveTabs.ForEach(tab => tab.PreHandleCommand(state));

		void GetCommandParameters(CommandState state)
		{
			var found = true;
			switch (state.Command)
			{
				case NECommand.File_Open_Open: state.Parameters = Command_File_Open_Open_Dialog(); break;
				case NECommand.Macro_Open_Open: state.Parameters = Command_File_Open_Open_Dialog(Macro.MacroDirectory); break;
				case NECommand.Window_CustomGrid: state.Parameters = Command_Window_CustomGrid_Dialog(); break;
				default: found = false; break;
			}
			if (found)
				return;

			if (Focused == null)
				return;

			Focused.GetCommandParameters(state, this);
		}

		void ExecuteCommand(CommandState state)
		{
			var done = true;
			switch (state.Command)
			{
				case NECommand.File_New_New: Command_File_New_New(shiftDown); break;
				case NECommand.File_New_FromClipboards: Command_File_New_FromClipboards(); break;
				case NECommand.File_New_FromClipboardSelections: Command_File_New_FromClipboardSelections(); break;
				case NECommand.File_Open_Open: Command_File_Open_Open(state.Parameters as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case NECommand.File_Operations_DragDrop: Command_File_Operations_DragDrop(); break;
				case NECommand.File_MoveToNewWindow: Command_File_MoveToNewWindow(); break;
				case NECommand.File_Shell_Integrate: Command_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: Command_File_Shell_Unintegrate(); break;
				case NECommand.File_Exit: Command_File_Exit(); break;
				case NECommand.Diff_Diff: Command_Diff_Diff(shiftDown); break;
				case NECommand.Diff_Select_LeftTab: Command_Diff_Select_LeftRightBothTabs(true); break;
				case NECommand.Diff_Select_RightTab: Command_Diff_Select_LeftRightBothTabs(false); break;
				case NECommand.Diff_Select_BothTabs: Command_Diff_Select_LeftRightBothTabs(null); break;
				case NECommand.Macro_Open_Quick_1: Command_Macro_Open_Quick(1); break;
				case NECommand.Macro_Open_Quick_2: Command_Macro_Open_Quick(2); break;
				case NECommand.Macro_Open_Quick_3: Command_Macro_Open_Quick(3); break;
				case NECommand.Macro_Open_Quick_4: Command_Macro_Open_Quick(4); break;
				case NECommand.Macro_Open_Quick_5: Command_Macro_Open_Quick(5); break;
				case NECommand.Macro_Open_Quick_6: Command_Macro_Open_Quick(6); break;
				case NECommand.Macro_Open_Quick_7: Command_Macro_Open_Quick(7); break;
				case NECommand.Macro_Open_Quick_8: Command_Macro_Open_Quick(8); break;
				case NECommand.Macro_Open_Quick_9: Command_Macro_Open_Quick(9); break;
				case NECommand.Macro_Open_Quick_10: Command_Macro_Open_Quick(10); break;
				case NECommand.Macro_Open_Quick_11: Command_Macro_Open_Quick(11); break;
				case NECommand.Macro_Open_Quick_12: Command_Macro_Open_Quick(12); break;
				case NECommand.Macro_Open_Open: Command_File_Open_Open(state.Parameters as OpenFileDialogResult); break;
				case NECommand.Window_NewWindow: Command_Window_NewWindow(); break;
				case NECommand.Window_Full: Command_Window_Full(); break;
				case NECommand.Window_Grid: Command_Window_Grid(); break;
				case NECommand.Window_CustomGrid: Command_Window_CustomGrid(state.Parameters as WindowCustomGridDialog.Result); break;
				case NECommand.Window_ActiveTabs: Command_Window_ActiveTabs(); break;
				case NECommand.Window_Font_Size: Command_Window_Font_Size(); break;
				case NECommand.Window_Select_AllTabs: Command_Window_Select_AllTabs(); break;
				case NECommand.Window_Select_NoTabs: Command_Window_Select_NoTabs(); break;
				case NECommand.Window_Select_TabsWithSelections: Command_Window_Select_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Select_TabsWithoutSelections: Command_Window_Select_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Select_ModifiedTabs: Command_Window_Select_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Select_UnmodifiedTabs: Command_Window_Select_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Select_InactiveTabs: Command_Window_Select_InactiveTabs(); break;
				case NECommand.Window_Close_TabsWithSelections: Command_Window_Close_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Close_TabsWithoutSelections: Command_Window_Close_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Close_ModifiedTabs: Command_Window_Close_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Close_UnmodifiedTabs: Command_Window_Close_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Close_ActiveTabs: Command_Window_Close_ActiveInactiveTabs(true); break;
				case NECommand.Window_Close_InactiveTabs: Command_Window_Close_ActiveInactiveTabs(false); break;
				case NECommand.Window_WordList: Command_Window_WordList(); break;
				case NECommand.Help_About: Command_Help_About(); break;
				case NECommand.Help_Tutorial: Command_Help_Tutorial(); break;
				case NECommand.Help_Update: Command_Help_Update(); break;
				case NECommand.Help_Extract: Command_Help_Extract(); break;
				case NECommand.Help_RunGC: Command_Help_RunGC(); break;
				default: done = false; break;
			}

			if (!done)
				ActiveTabs.ForEach(tab => tab.ExecuteCommand(state));
		}

		//public bool HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		//{
		//	Tabs.ForEach(textEditor => textEditor.DragFiles = null);

		//	switch (command)
		//	{
		//	}

		//	try
		//	{
		//		var preResult = default(object);
		//		foreach (var textEditor in ActiveTabs.ToList())
		//			textEditor.PreHandleCommand(command, ref preResult);

		//		foreach (var textEditor in ActiveTabs.ToList())
		//			textEditor.HandleCommand(command, shiftDown, dialogResult, multiStatus, preResult);
		//		if (newClipboard != null)
		//			NEClipboard.Current = newClipboard;
		//		SetupNewKeys();
		//	}
		//	catch (OperationCanceledException) { }
		//	finally
		//	{
		//		clipboard = null;
		//		newClipboard = null;

		//		for (var ctr = 0; ctr < keysAndValues.Count; ++ctr)
		//			keysAndValuesLookup[ctr] = null;
		//		keysHashLookup = null;
		//		newKeysAndValues = null;
		//		savedAnswers.Clear();
		//	}

		//	return true;
		//}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			LastActivated = DateTime.Now;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (MacroPlaying != null)
			{
				if (e.Key == Key.Escape)
					MacroPlaying.Stop();
				return;
			}

			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;

			var state = new CommandState(NECommand.Internal_Key) { Parameters = key, ShiftDown = shiftDown, ControlDown = controlDown, AltDown = altDown };
			HandleCommand(state);
			e.Handled = state.Result;
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if (MacroPlaying != null)
				return;

			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			HandleCommand(new CommandState(NECommand.Internal_Text) { Parameters = e.Text });
			e.Handled = true;
		}

		public void QueueDoActivated() => doActivatedTimer.Start();

		void OnActivated(object sender, EventArgs e) => QueueDoActivated();

		void DoActivated()
		{
			//TODO
			//if (!IsActive)
			//	return;

			//Activated -= OnActivated;
			//try
			//{
			//	foreach (var textEditor in Tabs)
			//		textEditor.Activated();
			//}
			//finally { Activated += OnActivated; }
		}

		void ShowFocused()
		{
			//TODO
			//if (Focused == null)
			//	return;
			//ShowTextEditor?.Invoke(Focused);
			//Focused.Focus();
		}

		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null)
		{
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
		}

		public void AddTextEditor(TextEditorData textEditor, int? index = null, bool canReplace = true)
		{
			textEditor.TabsParent = this;

			var tabs = Tabs.ToList();
			var replace = (canReplace) && (!index.HasValue) && (!textEditor.Empty()) && (Focused != null) && (Focused.Empty()) ? Focused : default(TextEditorData);
			if (replace != null)
				tabs[tabs.IndexOf(replace)] = textEditor;
			else
				tabs.Insert(index ?? tabs.Count, textEditor);

			Tabs = tabs;

			var activeTabs = ActiveTabs.ToList();
			if (oldActiveTabs == newActiveTabs)
				activeTabs.Clear();

			activeTabs.Add(textEditor);
			ActiveTabs = activeTabs;
		}

		public void RemoveTextEditor(TextEditorData textEditor, bool close = true)
		{
			//TODO
			//activeTabs.Remove(textEditor);
			//tabs.Remove(textEditor);
			//if (close)
			//	textEditor.Closed();
			//UpdateFocused(true);
			//statusBarTimer.Start();
			//QueueUpdateLayout();
		}

		public void AddDiff(TextEditorData textEdit1, TextEditorData textEdit2)
		{
			//TODO
			//if (textEdit1.ContentType == ParserType.None)
			//	textEdit1.ContentType = textEdit2.ContentType;
			//if (textEdit2.ContentType == ParserType.None)
			//	textEdit2.ContentType = textEdit1.ContentType;
			//AddTextEditor(textEdit1);
			//AddTextEditor(textEdit2);
			//textEdit1.DiffTarget = textEdit2;
			//SetLayout(maxColumns: 2);
		}

		public void AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, int? index1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, int? index2 = null, ShutdownData shutdownData2 = null)
		{
			var te1 = new TextEditorData(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, index1, shutdownData1);
			var te2 = new TextEditorData(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, index2, shutdownData2);
			AddDiff(te1, te2);
		}

		public void UpdateFocused(bool focusInactive = false)
		{
			var focused = Focused;
			if (!ActiveTabs.Contains(focused))
				focused = null;
			if (focused == null)
				focused = ActiveTabs.OrderByDescending(tab => tab.TextEditorOrder).FirstOrDefault();
			if ((focused == null) && (focusInactive))
				focused = Tabs.OrderByDescending(tab => tab.TextEditorOrder).FirstOrDefault();
			SetFocused(focused);
		}

		public bool TabIsActive(TextEditorData textEditor) => ActiveTabs.Contains(textEditor);

		public int GetTabIndex(TextEditorData textEditor, bool activeOnly = false)
		{
			var index = (activeOnly ? ActiveTabs : Tabs).Indexes(x => x == textEditor).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			if ((controlDown) && (!altDown))
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.PageUp: MovePrev(); break;
					case Key.PageDown: MoveNext(); break;
					case Key.Tab: MoveTabOrder(); break;
					default: e.Handled = false; break;
				}
			}
		}

		protected override void OnPreviewKeyUp(KeyEventArgs e)
		{
			base.OnPreviewKeyUp(e);
			if ((e.Key == Key.LeftCtrl) || (e.Key == Key.RightCtrl))
				if (Focused != null)
					Focused.TextEditorOrder = ++textEditorOrder;
		}

		void MovePrev()
		{
			if (Focused == null)
				return;

			var index = GetTabIndex(Focused) - 1;
			if (index < 0)
				index = Tabs.Count - 1;
			if (index >= 0)
				SetFocused(Tabs[index], !shiftDown);
		}

		void SetFocused(TextEditorData textEditorData, bool v = false)
		{
			Focused = textEditorData;
		}

		void MoveNext()
		{
			if (Focused == null)
				return;

			var index = GetTabIndex(Focused) + 1;
			if (index >= Tabs.Count)
				index = 0;
			if (index < Tabs.Count)
				SetFocused(Tabs[index], !shiftDown);
		}

		void MoveTabOrder()
		{
			if (Focused == null)
				return;
			var ordering = Tabs.OrderBy(textEditor => textEditor.TextEditorOrder).ToList();
			var current = ordering.IndexOf(Focused) - 1;
			if (current == -2) // Not found
				return;
			if (current == -1)
				current = ordering.Count - 1;
			SetFocused(ordering[current], !shiftDown);
		}

		bool HandleClick(TextEditorData textEditor)
		{
			if (!shiftDown)
				ActiveTabs = new List<TextEditorData> { textEditor };
			else if (Focused != textEditor)
			{
				SetFocused(textEditor);
				return true;
			}
			return false;
		}

		//TODO
		//protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		//{
		//	for (var source = e.OriginalSource as FrameworkElement; source != null; source = source.Parent as FrameworkElement)
		//		if (source is TextEditorData textEditor)
		//		{
		//			if (HandleClick(textEditor))
		//			{
		//				e.Handled = true;
		//				return;
		//			}
		//			break;
		//		}
		//	base.OnPreviewMouseLeftButtonDown(e);
		//}

		void OnDrop(DragEventArgs e, TextEditorData toTextEditor)
		{
			var fromTabs = e.Data.GetData(typeof(List<TextEditorData>)) as List<TextEditorData>;
			if (fromTabs == null)
				return;

			var toIndex = GetTabIndex(toTextEditor);
			fromTabs.ForEach(fromTextEditor => fromTextEditor.TabsParent.RemoveTextEditor(fromTextEditor));

			if (toIndex == -1)
				toIndex = Tabs.Count;
			else
				toIndex = Math.Min(toIndex, Tabs.Count);

			foreach (var fromTab in fromTabs)
			{
				AddTextEditor(fromTab, toIndex);
				++toIndex;
				e.Handled = true;
			}
		}

		UIElement GetTabLabel(bool tiles, TextEditorData textEditor)
		{
			var border = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(2), BorderThickness = new Thickness(2), Tag = textEditor };
			border.MouseLeftButtonDown += (s, e) => HandleClick(textEditor);
			border.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = textEditor.TabsParent.ActiveTabs.ToList();
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<TextEditorData>), active), DragDropEffects.Move);
				}
			};

			if (Focused == textEditor)
			{
				border.BorderBrush = FocusedWindowBorderBrush;
				border.Background = FocusedWindowBackgroundBrush;
			}
			else if (ActiveTabs.Contains(textEditor))
			{
				border.BorderBrush = ActiveWindowBorderBrush;
				border.Background = ActiveWindowBackgroundBrush;
			}
			else
			{
				border.BorderBrush = InactiveWindowBorderBrush;
				border.Background = InactiveWindowBackgroundBrush;
			}

			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, Margin = new Thickness(4, -4, 0, 0) };
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextEditorData.TabLabel)) { Source = textEditor });
			Grid.SetRow(text, 0);
			Grid.SetColumn(text, 0);
			grid.Children.Add(text);

			var closeButton = new Button
			{
				Content = "🗙",
				BorderThickness = new Thickness(0),
				Style = FindResource(ToolBar.ButtonStyleKey) as Style,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2, -6, 0, 0),
				Foreground = Brushes.Red,
				Focusable = false,
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			closeButton.Click += (s, e) =>
			{
				if (textEditor.CanClose())
					RemoveTextEditor(textEditor);
			};
			Grid.SetRow(closeButton, 0);
			Grid.SetColumn(closeButton, 1);
			grid.Children.Add(closeButton);

			border.Child = grid;
			return border;
		}

		void DrawAll()
		{
			canvas.Children.Clear();
			if ((Columns == 1) && (Rows == 1))
				DoFullLayout();
			else
				DoGridLayout();

			Title = $"{(Focused == null ? "" : $"{Focused.FileName} - ")}NeoEdit Text Editor{(Helpers.IsAdministrator() ? " (Administrator)" : "")}{(ShowIndex ? $" - {WindowIndex}" : "")}";
			SetStatusBarText();
			SetMenuCheckboxes();
		}

		void SetStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";
			statusBar.Items.Clear();
			statusBar.Items.Add($"Active: {plural(ActiveTabs.Count(), "file")}, {plural(ActiveTabs.Sum(textEditor => textEditor.NumSelections), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Inactive: {plural(Tabs.Except(ActiveTabs).Count(), "file")}, {plural(Tabs.Except(ActiveTabs).Sum(textEditor => textEditor.NumSelections), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Total: {plural(Tabs.Count, "file")}, {plural(Tabs.Sum(textEditor => textEditor.NumSelections), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Clipboard: {plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Keys/Values: {string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Count):n0}"))}");

		}

		void SetMenuCheckboxes()
		{
			Image GetIcon(Func<TextEditorData, bool> func)
			{
				var results = ActiveTabs.Select(func).Distinct().ToList();
				switch (results.Count == 1 ? results.First() : default(bool?))
				{
					case true: return new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Checked.png")) };
					case false: return new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Unchecked.png")) };
					case null: return new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Indeterminate.png")) };
					default: throw new Exception("Invalid");
				}
			}

			menu.file_AutoRefresh.Icon = GetIcon(x => x.AutoRefresh);
			menu.file_Encrypt.Icon = GetIcon(x => !string.IsNullOrWhiteSpace(x.AESKey));
			menu.file_Compress.Icon = GetIcon(x => x.Compressed);
			menu.edit_Navigate_JumpBy_Words.Icon = GetIcon(x => x.JumpBy == JumpByType.Words);
			menu.edit_Navigate_JumpBy_Numbers.Icon = GetIcon(x => x.JumpBy == JumpByType.Numbers);
			menu.edit_Navigate_JumpBy_Paths.Icon = GetIcon(x => x.JumpBy == JumpByType.Paths);
			menu.diff_IgnoreWhitespace.Icon = GetIcon(x => x.DiffIgnoreWhitespace);
			menu.diff_IgnoreCase.Icon = GetIcon(x => x.DiffIgnoreCase);
			menu.diff_IgnoreNumbers.Icon = GetIcon(x => x.DiffIgnoreNumbers);
			menu.diff_IgnoreLineEndings.Icon = GetIcon(x => x.DiffIgnoreLineEndings);
			menu.content_Type_None.Icon = GetIcon(x => x.ContentType == ParserType.None);
			menu.content_Type_Balanced.Icon = GetIcon(x => x.ContentType == ParserType.Balanced);
			menu.content_Type_Columns.Icon = GetIcon(x => x.ContentType == ParserType.Columns);
			menu.content_Type_CPlusPlus.Icon = GetIcon(x => x.ContentType == ParserType.CPlusPlus);
			menu.content_Type_CSharp.Icon = GetIcon(x => x.ContentType == ParserType.CSharp);
			menu.content_Type_CSV.Icon = GetIcon(x => x.ContentType == ParserType.CSV);
			menu.content_Type_ExactColumns.Icon = GetIcon(x => x.ContentType == ParserType.ExactColumns);
			menu.content_Type_HTML.Icon = GetIcon(x => x.ContentType == ParserType.HTML);
			menu.content_Type_JSON.Icon = GetIcon(x => x.ContentType == ParserType.JSON);
			menu.content_Type_SQL.Icon = GetIcon(x => x.ContentType == ParserType.SQL);
			menu.content_Type_TSV.Icon = GetIcon(x => x.ContentType == ParserType.TSV);
			menu.content_Type_XML.Icon = GetIcon(x => x.ContentType == ParserType.XML);
			menu.content_HighlightSyntax.Icon = GetIcon(x => x.HighlightSyntax);
			menu.content_StrictParsing.Icon = GetIcon(x => x.StrictParsing);
			menu.content_KeepSelections.Icon = GetIcon(x => x.KeepSelections);
			menu.window_ViewValues.Icon = GetIcon(x => x.ViewValues);
		}

		void DoFullLayout()
		{
			if (scrollBar.Visibility != Visibility.Collapsed)
			{
				scrollBar.Visibility = Visibility.Collapsed;
				UpdateLayout();
			}

			var outerBorder = new Border
			{
				Width = canvas.ActualWidth,
				Height = canvas.ActualHeight,
				BorderBrush = OutlineBrush,
				Background = BackgroundBrush,
				BorderThickness = new Thickness(2),
				CornerRadius = new CornerRadius(8),
			};

			var grid = new Grid { AllowDrop = true };
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var tabLabels = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };

			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var tab in Tabs)
			{
				var tabLabel = GetTabLabel(false, tab);
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as TextEditorData);
				stackPanel.Children.Add(tabLabel);
			}

			//ShowTextEditor = textEditor =>
			//{
			//	var show = stackPanel.Children.OfType<FrameworkElement>().Where(x => x.Tag == textEditor).FirstOrDefault();
			//	if (show == null)
			//		return;
			//	tabLabels.UpdateLayout();
			//	var left = show.TranslatePoint(new Point(0, 0), tabLabels).X + tabLabels.HorizontalOffset;
			//	tabLabels.ScrollToHorizontalOffset(Math.Min(left, Math.Max(tabLabels.HorizontalOffset, left + show.ActualWidth - tabLabels.ViewportWidth)));
			//};

			tabLabels.Content = stackPanel;
			Grid.SetRow(tabLabels, 0);
			Grid.SetColumn(tabLabels, 1);
			grid.Children.Add(tabLabels);

			var moveLeft = new RepeatButton { Width = 20, Height = 20, Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveLeft.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset - 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Width = 20, Height = 20, Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveRight.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset + 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveRight, 0);
			Grid.SetColumn(moveRight, 2);
			grid.Children.Add(moveRight);

			if (Focused != null)
			{
				var content = new TextEditor(Focused);
				Grid.SetRow(content, 1);
				Grid.SetColumn(content, 0);
				Grid.SetColumnSpan(content, 3);
				grid.Children.Add(content);
				content.DrawAll();
			}

			outerBorder.Child = grid;
			canvas.Children.Add(outerBorder);
		}

		void DoGridLayout()
		{
			int? columns = null, rows = null;
			if (Columns.HasValue)
				columns = Math.Max(1, Columns.Value);
			if (Rows.HasValue)
				rows = Math.Max(1, Rows.Value);
			if ((!columns.HasValue) && (!rows.HasValue))
				columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Tabs.Count)), MaxColumns ?? int.MaxValue));
			if (!rows.HasValue)
				rows = Math.Max(1, Math.Min((Tabs.Count + columns.Value - 1) / columns.Value, MaxRows ?? int.MaxValue));
			if (!columns.HasValue)
				columns = Math.Max(1, Math.Min((Tabs.Count + rows.Value - 1) / rows.Value, MaxColumns ?? int.MaxValue));

			var totalRows = (Tabs.Count + columns.Value - 1) / columns.Value;

			scrollBar.Visibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			UpdateLayout();

			var width = canvas.ActualWidth / columns.Value;
			var height = canvas.ActualHeight / rows.Value;

			scrollBar.ViewportSize = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - canvas.ActualHeight;

			for (var ctr = 0; ctr < Tabs.Count; ++ctr)
			{
				var top = ctr / columns.Value * height - scrollBar.Value;
				if ((top + height < 0) || (top > canvas.ActualHeight))
					continue;

				var border = new Border
				{
					BorderBrush = OutlineBrush,
					Background = BackgroundBrush,
					BorderThickness = new Thickness(2),
					CornerRadius = new CornerRadius(8)
				};
				Canvas.SetLeft(border, ctr % columns.Value * width);
				Canvas.SetTop(border, top);

				var textEditor = new TextEditor(Tabs[ctr]);
				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, Tabs[ctr]);
				var tabLabel = GetTabLabel(true, Tabs[ctr]);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					textEditor.SetValue(DockPanel.DockProperty, Dock.Bottom);
					textEditor.FocusVisualStyle = null;
					dockPanel.Children.Add(textEditor);
				}
				textEditor.DrawAll();

				border.Child = dockPanel;

				border.Width = width;
				border.Height = height;
				canvas.Children.Add(border);
			}

			ShowTextEditor = textEditor =>
			{
				var index = GetTabIndex(textEditor);
				if (index == -1)
					return;
				var top = index / columns.Value * height;
				scrollBar.Value = Math.Min(top, Math.Max(scrollBar.Value, top + height - scrollBar.ViewportSize));
			};
		}

		public void Move(TextEditorData tab, int newIndex)
		{
			var oldIndex = GetTabIndex(tab);
			if (oldIndex == -1)
				return;

			var tabs = Tabs.ToList();
			tabs.RemoveAt(oldIndex);
			tabs.Insert(newIndex, tab);
			Tabs = tabs;
			//QueueUpdateLayout();
		}

		//TODO
		//protected override void OnClosing(CancelEventArgs e)
		//{
		//	var saveActive = ActiveTabs.ToList();
		//	var saveFocused = Focused;
		//	foreach (var tab in Tabs)
		//	{
		//		SetFocused(tab, true);
		//		if (!tab.CanClose())
		//		{
		//			e.Cancel = true;
		//			SetActive(saveActive);
		//			SetFocused(saveFocused);
		//			return;
		//		}
		//	}
		//	Tabs.ToList().ForEach(textEditor => textEditor.Closed());
		//	base.OnClosing(e);

		//	try { Settings.WindowPosition = GetPosition(); } catch { }
		//}

		public bool GotoTab(string fileName, int? line, int? column, int? index)
		{
			var tab = Tabs.FirstOrDefault(x => x.FileName == fileName);
			if (tab == null)
				return false;
			Activate();
			SetFocused(tab, true);
			//TODO
			//tab.Command_File_Refresh();
			//tab.Goto(line, column, index);
			return true;
		}
	}
}
