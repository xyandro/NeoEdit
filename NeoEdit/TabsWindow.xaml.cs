using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
		public int WindowIndex { get { return UIHelper<TabsWindow>.GetPropValue<int>(this); } private set { UIHelper<TabsWindow>.SetPropValue(this, value); } }

		static int curWindowIndex = 0;

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

		public TabsWindow(bool addEmpty = false)
		{
			oldTabs = newTabs = oldActiveTabs = newActiveTabs = new List<TextEditor>();
			oldRows = newRows = oldColumns = newColumns = 1;

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			WindowIndex = ++curWindowIndex;

			activateTabsTimer = new RunOnceTimer(() => ActivateTabs());
			NEClipboard.ClipboardChanged += () => SetStatusBarText();

			//scrollBar.ValueChanged += (s, e) => QueueUpdateLayout(false);
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			if (addEmpty)
				HandleCommand(new ExecuteState(NECommand.File_New_New));
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			DrawAll();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			try { SetPosition(Settings.WindowPosition); } catch { }
		}

		public IReadOnlyDictionary<TextEditor, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var clipboardDataMap = Tabs.ToDictionary(x => x, x => empty);

			if (NEClipboard.Current.Count == ActiveTabs.Count)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[ActiveTabs[index]] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ChildCount == ActiveTabs.Count)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[ActiveTabs[index]] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == ActiveTabs.Sum(tab => tab.Selections.Count)))
				NEClipboard.Current.Strings.Take(ActiveTabs.Select(tab => tab.Selections.Count)).ForEach((obj, index) => clipboardDataMap[ActiveTabs[index]] = new Tuple<IReadOnlyList<string>, bool?>(obj.ToList(), NEClipboard.Current.IsCut));
			else
			{
				var strs = NEClipboard.Current.Strings;
				ActiveTabs.ForEach(tab => clipboardDataMap[tab] = new Tuple<IReadOnlyList<string>, bool?>(strs, NEClipboard.Current.IsCut));
			}

			return clipboardDataMap;
		}

		IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), 10).ToArray();
		public Dictionary<TextEditor, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var keysAndValuesMap = Tabs.ToDictionary(x => x, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				Tabs.ForEach(tab => keysAndValuesMap[tab] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == ActiveTabs.Count)
				ActiveTabs.ForEach((tab, index) => keysAndValuesMap[tab] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => AddTextEditor(new TextEditor(file)));
			e.Handled = true;
		}

		public Macro RecordingMacro { get; set; }
		public Macro MacroPlaying { get; set; }

		public void HandleCommand(ExecuteState state, bool fromMacro = false)
		{
			try
			{
				BeginTransaction();
				Tabs.ForEach(tab => tab.BeginTransaction(state));

				state.ClipboardDataMapFunc = GetClipboardDataMap;
				state.KeysAndValuesFunc = GetKeysAndValuesMap;

				if (!fromMacro)
				{
					if (MacroPlaying != null)
						return;

					state.ActiveTabs = ActiveTabs;
					state.ShiftDown = shiftDown;
					state.ControlDown = controlDown;
					state.AltDown = altDown;

					state.Configuration = Configure(state);

					if (state.Configuration == null)
						throw new OperationCanceledException();

					if (state.Configuration == ExecuteState.ConfigureUnnecessary)
						state.Configuration = null;
				}

				Execute(state);

				Commit();
				Tabs.ForEach(tab => tab.Commit());

				PostExecute();

				RecordingMacro?.AddAction(state);

				DrawAll();
			}
			catch
			{
				Rollback();
				Tabs.ForEach(tab => tab.Rollback());
				throw;
			}
		}

		object Configure(ExecuteState state)
		{
			if (state.Command == NECommand.Internal_Key)
				state.Handled = false;

			switch (state.Command)
			{
				case NECommand.File_Open_Open: return Configure_File_Open_Open();
				case NECommand.Macro_Open_Open: return Configure_File_Open_Open(Macro.MacroDirectory);
				case NECommand.Window_CustomGrid: return Configure_Window_CustomGrid();
			}

			if (Focused == null)
				return ExecuteState.ConfigureUnnecessary;

			return Focused.Configure();
		}


		void Execute(ExecuteState state)
		{
			switch (state.Command)
			{
				case NECommand.Internal_Activate: Execute_Internal_Activate(); break;
				case NECommand.Internal_AddTextEditor: Execute_Internal_AddTextEditor(state.Configuration as TextEditor); break;
				case NECommand.Internal_Key: Execute_Internal_Key(state); break;
				case NECommand.File_New_New: Execute_File_New_New(shiftDown); break;
				case NECommand.File_New_FromClipboards: Execute_File_New_FromClipboards(); break;
				case NECommand.File_New_FromClipboardSelections: Execute_File_New_FromClipboardSelections(); break;
				case NECommand.File_Open_Open: Execute_File_Open_Open(state.Configuration as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: Execute_File_Open_CopiedCut(); break;
				case NECommand.File_MoveToNewWindow: Execute_File_MoveToNewWindow(); break;
				case NECommand.File_Shell_Integrate: Execute_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: Execute_File_Shell_Unintegrate(); break;
				case NECommand.File_Exit: Execute_File_Exit(); break;
				case NECommand.Diff_Diff: Execute_Diff_Diff(shiftDown); break;
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
				case NECommand.Window_NewWindow: Execute_Window_NewWindow(); break;
				case NECommand.Window_Full: Execute_Window_Full(); break;
				case NECommand.Window_Grid: Execute_Window_Grid(); break;
				case NECommand.Window_CustomGrid: Execute_Window_CustomGrid(state.Configuration as WindowCustomGridDialog.Result); break;
				case NECommand.Window_ActiveTabs: Execute_Window_ActiveTabs(); break;
				case NECommand.Window_Font_Size: Execute_Window_Font_Size(); break;
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

			ActiveTabs.ForEach(tab => tab.Execute());
		}

		void PostExecute()
		{
			var clipboardDatas = Tabs.Select(tab => tab.ChangedClipboardData).NonNull().ToList();
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
				var newKeysAndValues = Tabs.Select(tab => tab.GetChangedKeysAndValues(kvIndex)).NonNull().ToList();
				if (newKeysAndValues.Any())
					keysAndValues[kvIndex] = newKeysAndValues;
			}

			var dragFiles = Tabs.SelectMany(tab => tab.ChangedDragFiles.NonNullOrWhiteSpace()).ToList();
			var nonExisting = dragFiles.Where(x => !File.Exists(x)).ToList();
			if (nonExisting.Any())
				throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
			// TODO: Make these files actually do something
			//Focused.DragFiles = fileNames;
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

			var state = new ExecuteState(NECommand.Internal_Key) { Key = key };
			HandleCommand(state);
			e.Handled = state.Handled;
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

			HandleCommand(new ExecuteState(NECommand.Internal_Text) { Text = e.Text });
			e.Handled = true;
		}

		void ShowFocused()
		{
			//TODO
			//if (Focused == null)
			//	return;
			//Focused.Focus();
		}

		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null)
		{
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
		}

		public void AddTextEditor(TextEditor textEditor, int? index = null, bool canReplace = true)
		{
			var tabs = Tabs.ToList();
			var replace = (canReplace) && (!index.HasValue) && (!textEditor.Empty()) && (Focused != null) && (Focused.Empty()) ? Focused : default(TextEditor);
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

		public void RemoveTextEditor(TextEditor textEditor, bool close = true)
		{
			Tabs = Tabs.Except(textEditor).ToList();
			if (close)
				textEditor.Closed();
			UpdateFocused(true);
		}

		public void AddDiff(TextEditor textEdit1, TextEditor textEdit2)
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
			var te1 = new TextEditor(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, index1, shutdownData1);
			var te2 = new TextEditor(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, index2, shutdownData2);
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

		public bool TabIsActive(TextEditor textEditor) => ActiveTabs.Contains(textEditor);

		public int GetTabIndex(TextEditor textEditor, bool activeOnly = false)
		{
			var index = (activeOnly ? ActiveTabs : Tabs).Indexes(x => x == textEditor).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		void SetFocused(TextEditor textEditorData, bool deselectOthers = false)
		{
			Focused = textEditorData;
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

		bool HandleClick(TextEditor textEditor)
		{
			if (!shiftDown)
				ActiveTabs = new List<TextEditor> { textEditor };
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

		void OnDrop(DragEventArgs e, TextEditor toTextEditor)
		{
			//TODO
			//var fromTabs = e.Data.GetData(typeof(List<TextEditor>)) as List<TextEditor>;
			//if (fromTabs == null)
			//	return;

			//var toIndex = GetTabIndex(toTextEditor);
			//fromTabs.ForEach(fromTextEditor => fromTextEditor.TabsParent.RemoveTextEditor(fromTextEditor));

			//if (toIndex == -1)
			//	toIndex = Tabs.Count;
			//else
			//	toIndex = Math.Min(toIndex, Tabs.Count);

			//foreach (var fromTab in fromTabs)
			//{
			//	AddTextEditor(fromTab, toIndex);
			//	++toIndex;
			//	e.Handled = true;
			//}
		}

		UIElement GetTabLabel(TextEditor textEditor)
		{
			var border = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(2), BorderThickness = new Thickness(2), Tag = textEditor };
			border.MouseLeftButtonDown += (s, e) => HandleClick(textEditor);
			border.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = ActiveTabs.ToList();
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<TextEditor>), active), DragDropEffects.Move);
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
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextEditor.TabLabel)) { Source = textEditor });
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
			statusBar.Items.Add($"Active: {plural(ActiveTabs.Count(), "file")}, {plural(ActiveTabs.Sum(textEditor => textEditor.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Inactive: {plural(Tabs.Except(ActiveTabs).Count(), "file")}, {plural(Tabs.Except(ActiveTabs).Sum(textEditor => textEditor.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Total: {plural(Tabs.Count, "file")}, {plural(Tabs.Sum(textEditor => textEditor.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Clipboard: {plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Keys/Values: {string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");

		}

		void SetMenuCheckboxes()
		{
			bool? GetMultiStatus(Func<TextEditor, bool> func)
			{
				var results = ActiveTabs.Select(func).Distinct().ToList();
				if (results.Count != 1)
					return default;
				return results[0];
			}

			menu.file_AutoRefresh.MultiStatus = GetMultiStatus(x => x.AutoRefresh);
			menu.file_Encrypt.MultiStatus = GetMultiStatus(x => !string.IsNullOrWhiteSpace(x.AESKey));
			menu.file_Compress.MultiStatus = GetMultiStatus(x => x.Compressed);
			menu.edit_Navigate_JumpBy_Words.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Words);
			menu.edit_Navigate_JumpBy_Numbers.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Numbers);
			menu.edit_Navigate_JumpBy_Paths.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Paths);
			menu.diff_IgnoreWhitespace.MultiStatus = GetMultiStatus(x => x.DiffIgnoreWhitespace);
			menu.diff_IgnoreCase.MultiStatus = GetMultiStatus(x => x.DiffIgnoreCase);
			menu.diff_IgnoreNumbers.MultiStatus = GetMultiStatus(x => x.DiffIgnoreNumbers);
			menu.diff_IgnoreLineEndings.MultiStatus = GetMultiStatus(x => x.DiffIgnoreLineEndings);
			menu.content_Type_None.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.None);
			menu.content_Type_Balanced.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.Balanced);
			menu.content_Type_Columns.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.Columns);
			menu.content_Type_CPlusPlus.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CPlusPlus);
			menu.content_Type_CSharp.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CSharp);
			menu.content_Type_CSV.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CSV);
			menu.content_Type_ExactColumns.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.ExactColumns);
			menu.content_Type_HTML.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.HTML);
			menu.content_Type_JSON.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.JSON);
			menu.content_Type_SQL.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.SQL);
			menu.content_Type_TSV.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.TSV);
			menu.content_Type_XML.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.XML);
			menu.content_HighlightSyntax.MultiStatus = GetMultiStatus(x => x.HighlightSyntax);
			menu.content_StrictParsing.MultiStatus = GetMultiStatus(x => x.StrictParsing);
			menu.content_KeepSelections.MultiStatus = GetMultiStatus(x => x.KeepSelections);
			menu.window_ViewValues.MultiStatus = GetMultiStatus(x => x.ViewValues);
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
				var tabLabel = GetTabLabel(tab);
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as TextEditor);
				stackPanel.Children.Add(tabLabel);
			}

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
				var content = new TextEditorWindow(Focused);
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

				var textEditor = new TextEditorWindow(Tabs[ctr]);
				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, Tabs[ctr]);
				var tabLabel = GetTabLabel(Tabs[ctr]);
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
		}

		public void Move(TextEditor tab, int newIndex)
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
			//tab.Execute_File_Refresh();
			//tab.Goto(line, column, index);
			return true;
		}
	}
}
