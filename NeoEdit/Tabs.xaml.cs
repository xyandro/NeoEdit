using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Converters;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.MenuContent;
using NeoEdit.MenuDatabase;
using NeoEdit.MenuDateTime;
using NeoEdit.MenuDiff;
using NeoEdit.MenuEdit;
using NeoEdit.MenuExpression;
using NeoEdit.MenuFile;
using NeoEdit.MenuFiles;
using NeoEdit.MenuHelp;
using NeoEdit.MenuImage;
using NeoEdit.MenuKeys;
using NeoEdit.MenuMacro;
using NeoEdit.MenuNetwork;
using NeoEdit.MenuNumeric;
using NeoEdit.MenuPosition;
using NeoEdit.MenuRegion;
using NeoEdit.MenuSelect;
using NeoEdit.MenuTable;
using NeoEdit.MenuText;
using NeoEdit.MenuWindow;
using NeoEdit.MenuWindow.Dialogs;
using NeoEdit.Misc;

namespace NeoEdit
{
	partial class Tabs : ITabs
	{
		[DepProp]
		public ObservableCollection<ITextEditor> Items { get { return UIHelper<Tabs>.GetPropValue<ObservableCollection<ITextEditor>>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public ITextEditor TopMost { get { return UIHelper<Tabs>.GetPropValue<ITextEditor>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public TabsLayout Layout { get { return UIHelper<Tabs>.GetPropValue<TabsLayout>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Columns { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public Window WindowParent { get { return UIHelper<Tabs>.GetPropValue<Tabs>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string ActiveCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string InactiveCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string TotalCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string ClipboardCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }

		readonly RunOnceTimer layoutTimer, topMostTimer;

		Action<ITextEditor> ShowItem;
		int itemOrder = 0;

		static Tabs()
		{
			ITabsCreator.CreateTabs = addEmpty => new Tabs(addEmpty);
			ITabsCreator.LoadAllAssemblies = () => LoadAll();

			UIHelper<Tabs>.Register();
			UIHelper<Tabs>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs>.AddCoerce(a => a.TopMost, (obj, value) => (value != null) && (obj.Items?.Contains(value) == true) ? value : null);
			UIHelper<Tabs>.AddCallback(a => a.Layout, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.Rows, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.Columns, (obj, o, n) => obj.layoutTimer.Start());
		}

		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

		readonly RunOnceTimer doActivatedTimer, countsTimer;
		public Tabs(bool addEmpty = false)
		{
			layoutTimer = new RunOnceTimer(DoLayout);
			topMostTimer = new RunOnceTimer(ShowTopMost);
			topMostTimer.AddDependency(layoutTimer);

			Items = new ObservableCollection<ITextEditor>();
			Layout = TabsLayout.Full;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			VerticalAlignment = VerticalAlignment.Stretch;
			Drop += (s, e) => OnDrop(e, null);

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command, multiStatus));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			AllowDrop = true;
			Drop += OnDrop;
			doActivatedTimer = new RunOnceTimer(() => DoActivated());
			countsTimer = new RunOnceTimer(() => UpdateStatusBarText());
			NEClipboard.ClipboardChanged += () => UpdateStatusBarText();
			Activated += OnActivated;

			SizeChanged += (s, e) => layoutTimer.Start();
			scrollBar.ValueChanged += (s, e) => layoutTimer.Start();
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			UpdateStatusBarText();

			if (addEmpty)
				Add(new TextEditor());
		}

		void ItemTabs_TabsChanged()
		{
			Items.ForEach(item => item.InvalidateCanvas());
			UpdateStatusBarText();
		}

		void UpdateStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			ActiveCountText = $"{plural(Items.Where(item => item.Active).Count(), "file")}, {plural(Items.Where(item => item.Active).Sum(item => item.NumSelections), "selection")}";
			InactiveCountText = $"{plural(Items.Where(item => !item.Active).Count(), "file")}, {plural(Items.Where(item => !item.Active).Sum(item => item.NumSelections), "selection")}";
			TotalCountText = $"{plural(Items.Count, "file")}, {plural(Items.Sum(item => item.NumSelections), "selection")}";
			ClipboardCountText = $"{plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}";
		}

		Dictionary<ITextEditor, List<string>> clipboard;
		public List<string> GetClipboard(ITextEditor textEditor)
		{
			if (clipboard == null)
			{
				var empty = new List<string>();
				clipboard = Items.ToDictionary(x => x, x => empty);

				var activeTabs = Items.Where(item => item.Active).ToList();

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

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => Add(new TextEditor(file)));
			e.Handled = true;
		}

		public Macro RecordingMacro { get; set; }
		public Macro MacroPlaying { get; set; }

		internal void RunCommand(NECommand command, bool? multiStatus)
		{
			if (MacroPlaying != null)
				return;

			if (command.GetArea() == "Macro")
				if (PreHandleCommandMacro(command))
					return;

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult, multiStatus))
				return;

			if (RecordingMacro != null)
				RecordingMacro.AddCommand(command, shiftDown, dialogResult, multiStatus);

			HandleCommand(command, shiftDown, dialogResult, multiStatus);
		}

		bool PreHandleCommandMacro(NECommand command)
		{
			switch (command)
			{
				case NECommand.Macro_Record_Quick_1: MacroFunctions.Command_Macro_Record_Quick(this, 1); return true;
				case NECommand.Macro_Record_Quick_2: MacroFunctions.Command_Macro_Record_Quick(this, 2); return true;
				case NECommand.Macro_Record_Quick_3: MacroFunctions.Command_Macro_Record_Quick(this, 3); return true;
				case NECommand.Macro_Record_Quick_4: MacroFunctions.Command_Macro_Record_Quick(this, 4); return true;
				case NECommand.Macro_Record_Quick_5: MacroFunctions.Command_Macro_Record_Quick(this, 5); return true;
				case NECommand.Macro_Record_Quick_6: MacroFunctions.Command_Macro_Record_Quick(this, 6); return true;
				case NECommand.Macro_Record_Quick_7: MacroFunctions.Command_Macro_Record_Quick(this, 7); return true;
				case NECommand.Macro_Record_Quick_8: MacroFunctions.Command_Macro_Record_Quick(this, 8); return true;
				case NECommand.Macro_Record_Quick_9: MacroFunctions.Command_Macro_Record_Quick(this, 9); return true;
				case NECommand.Macro_Record_Quick_10: MacroFunctions.Command_Macro_Record_Quick(this, 10); return true;
				case NECommand.Macro_Record_Quick_11: MacroFunctions.Command_Macro_Record_Quick(this, 11); return true;
				case NECommand.Macro_Record_Quick_12: MacroFunctions.Command_Macro_Record_Quick(this, 12); return true;
				case NECommand.Macro_Record_Record: MacroFunctions.Command_Macro_Record_Record(this); return true;
				case NECommand.Macro_Record_StopRecording: MacroFunctions.Command_Macro_Record_StopRecording(this); return true;
				case NECommand.Macro_Append_Quick_1: MacroFunctions.Command_Macro_Append_Quick(this, 1); return true;
				case NECommand.Macro_Append_Quick_2: MacroFunctions.Command_Macro_Append_Quick(this, 2); return true;
				case NECommand.Macro_Append_Quick_3: MacroFunctions.Command_Macro_Append_Quick(this, 3); return true;
				case NECommand.Macro_Append_Quick_4: MacroFunctions.Command_Macro_Append_Quick(this, 4); return true;
				case NECommand.Macro_Append_Quick_5: MacroFunctions.Command_Macro_Append_Quick(this, 5); return true;
				case NECommand.Macro_Append_Quick_6: MacroFunctions.Command_Macro_Append_Quick(this, 6); return true;
				case NECommand.Macro_Append_Quick_7: MacroFunctions.Command_Macro_Append_Quick(this, 7); return true;
				case NECommand.Macro_Append_Quick_8: MacroFunctions.Command_Macro_Append_Quick(this, 8); return true;
				case NECommand.Macro_Append_Quick_9: MacroFunctions.Command_Macro_Append_Quick(this, 9); return true;
				case NECommand.Macro_Append_Quick_10: MacroFunctions.Command_Macro_Append_Quick(this, 10); return true;
				case NECommand.Macro_Append_Quick_11: MacroFunctions.Command_Macro_Append_Quick(this, 11); return true;
				case NECommand.Macro_Append_Quick_12: MacroFunctions.Command_Macro_Append_Quick(this, 12); return true;
				case NECommand.Macro_Append_Append: MacroFunctions.Command_Macro_Append_Append(this); return true;
				case NECommand.Macro_Play_Quick_1: MacroFunctions.Command_Macro_Play_Quick(this, 1); return true;
				case NECommand.Macro_Play_Quick_2: MacroFunctions.Command_Macro_Play_Quick(this, 2); return true;
				case NECommand.Macro_Play_Quick_3: MacroFunctions.Command_Macro_Play_Quick(this, 3); return true;
				case NECommand.Macro_Play_Quick_4: MacroFunctions.Command_Macro_Play_Quick(this, 4); return true;
				case NECommand.Macro_Play_Quick_5: MacroFunctions.Command_Macro_Play_Quick(this, 5); return true;
				case NECommand.Macro_Play_Quick_6: MacroFunctions.Command_Macro_Play_Quick(this, 6); return true;
				case NECommand.Macro_Play_Quick_7: MacroFunctions.Command_Macro_Play_Quick(this, 7); return true;
				case NECommand.Macro_Play_Quick_8: MacroFunctions.Command_Macro_Play_Quick(this, 8); return true;
				case NECommand.Macro_Play_Quick_9: MacroFunctions.Command_Macro_Play_Quick(this, 9); return true;
				case NECommand.Macro_Play_Quick_10: MacroFunctions.Command_Macro_Play_Quick(this, 10); return true;
				case NECommand.Macro_Play_Quick_11: MacroFunctions.Command_Macro_Play_Quick(this, 11); return true;
				case NECommand.Macro_Play_Quick_12: MacroFunctions.Command_Macro_Play_Quick(this, 12); return true;
				case NECommand.Macro_Play_Play: MacroFunctions.Command_Macro_Play_Play(this); return true;
				case NECommand.Macro_Play_Repeat: MacroFunctions.Command_Macro_Play_Repeat(this); return true;
				case NECommand.Macro_Play_PlayOnCopiedFiles: MacroFunctions.Command_Macro_Play_PlayOnCopiedFiles(this); return true;
				default: return false;
			}
		}

		NEClipboard newClipboard;
		public void AddClipboardStrings(IEnumerable<string> strings, bool? isCut = null)
		{
			newClipboard = newClipboard ?? new NEClipboard();
			newClipboard.Add(NEClipboardList.Create(strings));
			newClipboard.IsCut = isCut;
		}

		bool GetDialogResult(NECommand command, out object dialogResult, bool? multiStatus)
		{
			dialogResult = null;

			bool result;
			switch (command.GetArea())
			{
				case "File": result = GetDialogResultFile(command, ref dialogResult, multiStatus); break;
				case "Macro": result = GetDialogResultMacro(command, ref dialogResult, multiStatus); break;
				case "Window": result = GetDialogResultWindow(command, ref dialogResult, multiStatus); break;
				default: result = false; break;
			}

			if (result)
				return dialogResult != null;

			if (TopMost == null)
				return true;

			return TopMost.GetDialogResult(command, out dialogResult, multiStatus);
		}

		bool GetDialogResultFile(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.File_Open_Open: dialogResult = FileFunctions.Command_File_Open_Open_Dialog(this); return true;
				default: return false;
			}
		}

		bool GetDialogResultMacro(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Macro_Open_Open: dialogResult = FileFunctions.Command_File_Open_Open_Dialog(this, Macro.MacroDirectory); return true;
				default: return false;
			}
		}

		bool GetDialogResultWindow(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Window_CustomGrid: dialogResult = WindowFunctions.Command_Window_Type_Dialog(this); return true;
				default: return false;
			}
		}

		public bool HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			Items.ForEach(te => te.DragFiles = null);

			switch (command.GetArea())
			{
				case "File": HandleCommandFile(command, shiftDown, dialogResult, multiStatus); break;
				case "Diff": HandleCommandDiff(command, shiftDown, dialogResult, multiStatus); break;
				case "Macro": if (HandleCommandMacro(command, shiftDown, dialogResult, multiStatus)) return true; break;
				case "Window": HandleCommandWindow(command, shiftDown, dialogResult, multiStatus); break;
				case "Help": HandleCommandHelp(command, shiftDown, dialogResult, multiStatus); break;
			}

			try
			{
				var answer = new AnswerResult();
				foreach (var textEditorItem in Items.Where(item => item.Active).ToList())
				{
					textEditorItem.HandleCommand(command, shiftDown, dialogResult, multiStatus, answer);
					if (answer.Answer == MessageOptions.Cancel)
						break;
				}
				if (newClipboard != null)
					NEClipboard.Current = newClipboard;
			}
			finally
			{
				clipboard = null;
				newClipboard = null;
			}

			return true;
		}

		void HandleCommandFile(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.File_New_New: FileFunctions.Command_File_New_New(this, shiftDown); break;
				case NECommand.File_New_FromClipboards: FileFunctions.Command_File_New_FromClipboards(this); break;
				case NECommand.File_New_FromClipboardSelections: FileFunctions.Command_File_New_FromClipboardSelections(this); break;
				case NECommand.File_Open_Open: FileFunctions.Command_File_Open_Open(this, dialogResult as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: FileFunctions.Command_File_Open_CopiedCut(this); break;
				case NECommand.File_Operations_DragDrop: FileFunctions.Command_File_Operations_DragDrop(this); break;
				case NECommand.File_MoveToNewWindow: FileFunctions.Command_File_MoveToNewWindow(this); break;
				case NECommand.File_Shell_Integrate: FileFunctions.Command_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: FileFunctions.Command_File_Shell_Unintegrate(); break;
				case NECommand.File_Exit: Close(); break;
			}
		}

		void HandleCommandDiff(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Diff_Diff: DiffFunctions.Command_Diff_Diff(this, shiftDown); break;
				case NECommand.Diff_Select_LeftTab: DiffFunctions.Command_Diff_Select_LeftRightBothTabs(this, true); break;
				case NECommand.Diff_Select_RightTab: DiffFunctions.Command_Diff_Select_LeftRightBothTabs(this, false); break;
				case NECommand.Diff_Select_BothTabs: DiffFunctions.Command_Diff_Select_LeftRightBothTabs(this, null); break;
			}
		}

		bool HandleCommandMacro(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Macro_Open_Quick_1: MacroFunctions.Command_Macro_Open_Quick(this, 1); return true;
				case NECommand.Macro_Open_Quick_2: MacroFunctions.Command_Macro_Open_Quick(this, 2); return true;
				case NECommand.Macro_Open_Quick_3: MacroFunctions.Command_Macro_Open_Quick(this, 3); return true;
				case NECommand.Macro_Open_Quick_4: MacroFunctions.Command_Macro_Open_Quick(this, 4); return true;
				case NECommand.Macro_Open_Quick_5: MacroFunctions.Command_Macro_Open_Quick(this, 5); return true;
				case NECommand.Macro_Open_Quick_6: MacroFunctions.Command_Macro_Open_Quick(this, 6); return true;
				case NECommand.Macro_Open_Quick_7: MacroFunctions.Command_Macro_Open_Quick(this, 7); return true;
				case NECommand.Macro_Open_Quick_8: MacroFunctions.Command_Macro_Open_Quick(this, 8); return true;
				case NECommand.Macro_Open_Quick_9: MacroFunctions.Command_Macro_Open_Quick(this, 9); return true;
				case NECommand.Macro_Open_Quick_10: MacroFunctions.Command_Macro_Open_Quick(this, 10); return true;
				case NECommand.Macro_Open_Quick_11: MacroFunctions.Command_Macro_Open_Quick(this, 11); return true;
				case NECommand.Macro_Open_Quick_12: MacroFunctions.Command_Macro_Open_Quick(this, 12); return true;
				case NECommand.Macro_Open_Open: FileFunctions.Command_File_Open_Open(this, dialogResult as OpenFileDialogResult); return true;
				default: return false;
			}
		}

		void HandleCommandWindow(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Window_NewWindow: WindowFunctions.Command_Window_NewWindow(); break;
				case NECommand.Window_Full: WindowFunctions.Command_Window_Type(this, TabsLayout.Full, null); break;
				case NECommand.Window_Grid: WindowFunctions.Command_Window_Type(this, TabsLayout.Grid, null); break;
				case NECommand.Window_CustomGrid: WindowFunctions.Command_Window_Type(this, TabsLayout.Custom, dialogResult as WindowCustomGridDialog.Result); break;
				case NECommand.Window_ActiveTabs: WindowFunctions.Command_Window_ActiveTabs(this); break;
				case NECommand.Window_FontSize: WindowFunctions.Command_Window_FontSize(this); break;
				case NECommand.Window_Select_TabsWithSelections: WindowFunctions.Command_Window_Select_TabsWithWithoutSelections(this, true); break;
				case NECommand.Window_Select_TabsWithoutSelections: WindowFunctions.Command_Window_Select_TabsWithWithoutSelections(this, false); break;
				case NECommand.Window_Select_ModifiedTabs: WindowFunctions.Command_Window_Select_ModifiedUnmodifiedTabs(this, true); break;
				case NECommand.Window_Select_UnmodifiedTabs: WindowFunctions.Command_Window_Select_ModifiedUnmodifiedTabs(this, false); break;
				case NECommand.Window_Select_TabsWithSelectionsToTop: WindowFunctions.Command_Window_Select_TabsWithSelectionsToTop(this); break;
				case NECommand.Window_Close_TabsWithSelections: WindowFunctions.Command_Window_Close_TabsWithWithoutSelections(this, true); break;
				case NECommand.Window_Close_TabsWithoutSelections: WindowFunctions.Command_Window_Close_TabsWithWithoutSelections(this, false); break;
				case NECommand.Window_Close_ModifiedTabs: WindowFunctions.Command_Window_Close_ModifiedUnmodifiedTabs(this, true); break;
				case NECommand.Window_Close_UnmodifiedTabs: WindowFunctions.Command_Window_Close_ModifiedUnmodifiedTabs(this, false); break;
				case NECommand.Window_Close_ActiveTabs: WindowFunctions.Command_Window_Close_ActiveInactiveTabs(this, true); break;
				case NECommand.Window_Close_InactiveTabs: WindowFunctions.Command_Window_Close_ActiveInactiveTabs(this, false); break;
				case NECommand.Window_WordList: WindowFunctions.Command_Window_WordList(this); break;
			}
		}

		void HandleCommandHelp(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Help_About: HelpFunctions.Command_Help_About(); break;
				case NECommand.Help_Update: HelpFunctions.Command_Help_Update(); break;
				case NECommand.Help_RunGC: HelpFunctions.Command_Help_RunGC(); break;
			}
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

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;
			var altDown = this.altDown;

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;
			e.Handled = HandleKey(key, shiftDown, controlDown, altDown);

			if ((RecordingMacro != null) && (e.Handled))
				RecordingMacro.AddKey(key, shiftDown, controlDown, altDown);
		}

		public bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = false;
			var activeTabs = Items.Where(item => item.Active).ToList();
			var previousData = default(object);
			foreach (var textEditorItems in activeTabs)
				textEditorItems.PreHandleKey(key, shiftDown, controlDown, altDown, ref previousData);
			foreach (var textEditorItems in activeTabs)
				result = textEditorItems.HandleKey(key, shiftDown, controlDown, altDown, previousData) || result;
			return result;
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

			e.Handled = HandleText(e.Text);

			if ((RecordingMacro != null) && (e.Handled))
				RecordingMacro.AddText(e.Text);
		}

		public bool HandleText(string text)
		{
			var result = false;
			foreach (var textEditorItems in Items.Where(item => item.Active).ToList())
				result = textEditorItems.HandleText(text) || result;
			return result;
		}

		public void QueueDoActivated() => doActivatedTimer.Start();

		public void QueueUpdateCounts() => countsTimer.Start();

		void OnActivated(object sender, EventArgs e) => QueueDoActivated();

		void DoActivated()
		{
			if (!IsActive)
				return;

			Activated -= OnActivated;
			try
			{
				var answer = new AnswerResult();
				foreach (var item in Items)
				{
					item.Activated(answer);
					if (answer.Answer == MessageOptions.Cancel)
						break;
				}
			}
			finally { Activated += OnActivated; }
		}

		void ShowTopMost()
		{
			if (TopMost == null)
				return;
			ShowItem?.Invoke(TopMost);
			TopMost.Focus();
		}

		public void SetLayout(TabsLayout layout, int? columns = null, int? rows = null)
		{
			Layout = layout;
			Columns = columns;
			Rows = rows;
			topMostTimer.Start();
		}

		public void Add(ITextEditor item, int? index = null)
		{
			var replace = (!index.HasValue) && (!item.Empty()) && (TopMost != null) && (TopMost.Empty()) ? TopMost : default(ITextEditor);
			if (replace != null)
			{
				replace.Closed();
				Items[Items.IndexOf(replace)] = item;
			}
			else
				Items.Insert(index ?? Items.Count, item);
			TopMost = item;
		}

		public ITextEditor Add(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null, int? index = null)
		{
			var textEditor = new TextEditor(fileName, displayName, bytes, codePage, contentType, modified, line, column, shutdownData);
			Add(textEditor, index);
			return textEditor;
		}

		public Window AddDiff(ITextEditor textEdit1, ITextEditor textEdit2)
		{
			if (textEdit1.ContentType == ParserType.None)
				textEdit1.ContentType = textEdit2.ContentType;
			if (textEdit2.ContentType == ParserType.None)
				textEdit2.ContentType = textEdit1.ContentType;
			Add(textEdit1);
			Add(textEdit2);
			textEdit1.DiffTarget = textEdit2;
			Layout = TabsLayout.Custom;
			Columns = 2;
			return this;
		}

		public Window AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, ParserType contentType1 = ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, ShutdownData shutdownData1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, ParserType contentType2 = ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, ShutdownData shutdownData2 = null)
		{
			var te1 = new TextEditor(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, shutdownData1);
			var te2 = new TextEditor(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, shutdownData2);
			return AddDiff(te1, te2);
		}

		void ItemsChanged()
		{
			ItemTabs_TabsChanged();

			if (Items == null)
				return;

			foreach (var item in Items)
			{
				EnhancedFocusManager.SetIsEnhancedFocusScope(item as DependencyObject, true);
				item.TabsParent = this;
			}

			UpdateTopMost();
			layoutTimer.Start();
		}

		void TopMostChanged()
		{
			if (TopMost == null)
			{
				UpdateTopMost();
				return;
			}

			if (!shiftDown)
				foreach (var item in Items)
					item.Active = false;
			TopMost.Active = true;

			if (!controlDown)
				TopMost.ItemOrder = ++itemOrder;

			Dispatcher.BeginInvoke((Action)(() =>
			{
				UpdateLayout();
				if (TopMost != null)
					TopMost.Focus();
			}));

			topMostTimer.Start();
		}

		public void UpdateTopMost()
		{
			var topMost = TopMost;
			if ((topMost == null) || (!topMost.Active))
				topMost = null;
			if (topMost == null)
				topMost = Items.Where(item => item.Active).OrderByDescending(item => item.ItemOrder).FirstOrDefault();
			if (topMost == null)
				topMost = Items.OrderByDescending(item => item.ItemOrder).FirstOrDefault();
			TopMost = topMost;
		}

		public bool TabIsActive(ITextEditor item) => Items.Where(x => x == item).Select(x => x.Active).DefaultIfEmpty(false).First();

		public int GetIndex(ITextEditor item, bool activeOnly = false)
		{
			var index = Items.Where(x => (!activeOnly) || (x.Active)).Indexes(x => x == item).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void Remove(ITextEditor item)
		{
			Items.Remove(item);
			item.Closed();
		}

		public void RemoveAll()
		{
			var items = Items.ToList();
			Items.Clear();
			foreach (var item in items)
				item.Closed();
		}

		public int ActiveCount => Items.Count(item => item.Active);

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
				if (TopMost != null)
					TopMost.ItemOrder = ++itemOrder;
		}

		void MovePrev()
		{
			var index = Items.IndexOf(TopMost) - 1;
			if (index < 0)
				index = Items.Count - 1;
			if (index >= 0)
				TopMost = Items[index];
		}

		void MoveNext()
		{
			var index = Items.IndexOf(TopMost) + 1;
			if (index >= Items.Count)
				index = 0;
			if (index < Items.Count)
				TopMost = Items[index];
		}

		void MoveTabOrder()
		{
			var ordering = Items.OrderBy(item => item.ItemOrder).ToList();
			var current = ordering.IndexOf(TopMost) - 1;
			if (current == -2) // Not found
				return;
			if (current == -1)
				current = ordering.Count - 1;
			TopMost = ordering[current];
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			for (var source = e.OriginalSource as FrameworkElement; source != null; source = source.Parent as FrameworkElement)
				if (source is TextEditor te)
				{
					TopMost = te;
					break;
				}
		}

		void OnDrop(DragEventArgs e, ITextEditor toItem)
		{
			var fromItems = e.Data.GetData(typeof(List<ITextEditor>)) as List<ITextEditor>;
			if (fromItems == null)
				return;

			var toIndex = Items.IndexOf(toItem);
			fromItems.ForEach(fromItem => fromItem.TabsParent.Items.Remove(fromItem));

			if (toIndex == -1)
				toIndex = Items.Count;
			else
				toIndex = Math.Min(toIndex, Items.Count);

			foreach (var fromItem in fromItems)
			{
				Items.Insert(toIndex, fromItem);
				++toIndex;
				TopMost = fromItem;
				e.Handled = true;
			}
		}

		public void MoveToTop(IEnumerable<ITextEditor> tabs)
		{
			var found = new HashSet<ITextEditor>(tabs);
			var indexes = Items.Indexes(item => found.Contains(item)).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				Items.Move(indexes[ctr], ctr);
		}

		DockPanel GetTabLabel(Tabs tabs, bool tiles, ITextEditor item)
		{
			var dockPanel = new DockPanel { Margin = new Thickness(0, 0, tiles ? 0 : 2, 1), Tag = item };

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p2 ? \"CadetBlue\" : (p1 ? \"LightBlue\" : \"LightGray\")" };
			multiBinding.Bindings.Add(new Binding { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(ITextEditor.Active)) { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TopMost)) { Source = tabs });
			dockPanel.SetBinding(DockPanel.BackgroundProperty, multiBinding);

			dockPanel.MouseLeftButtonDown += (s, e) => tabs.TopMost = item;
			dockPanel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = item.TabsParent.Items.Where(tab => tab.Active).ToList();
					DragDrop.DoDragDrop(s as DockPanel, new DataObject(typeof(List<ITextEditor>), active), DragDropEffects.Move);
				}
			};

			var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0) };
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(ITextEditor.TabLabel)) { Source = item });
			dockPanel.Children.Add(text);

			var closeButton = new Button
			{
				Content = "x",
				BorderThickness = new Thickness(0),
				Style = FindResource(ToolBar.ButtonStyleKey) as Style,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2, 0, 5, 0),
				Foreground = new SolidColorBrush(Color.FromRgb(128, 32, 32)),
				Focusable = false,
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			closeButton.Click += (s, e) =>
			{
				if (item.CanClose())
					tabs.Remove(item);
			};
			dockPanel.Children.Add(closeButton);
			return dockPanel;
		}

		void ClearLayout()
		{
			canvas.Children.Clear();
			foreach (var item in Items)
			{
				var parent = (item as FrameworkElement).Parent;
				if (parent is Panel p)
					p.Children.Clear();
				else if (parent is ContentControl cc)
					cc.Content = null;
				else if (parent != null)
					throw new Exception("Don't know how to disconnect item");
			}
		}

		void DoLayout()
		{
			ClearLayout();
			if (Layout == TabsLayout.Full)
				DoFullLayout();
			else
				DoGridLayout();
			TopMost?.Focus();
		}

		void DoFullLayout()
		{
			if (scrollBar.Visibility != Visibility.Collapsed)
			{
				scrollBar.Visibility = Visibility.Collapsed;
				UpdateLayout();
			}

			var grid = new Grid { Width = canvas.ActualWidth, Height = canvas.ActualHeight, AllowDrop = true };
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var tabLabels = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };

			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var item in Items)
			{
				var tabLabel = GetTabLabel(this, false, item);
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as ITextEditor);
				stackPanel.Children.Add(tabLabel);
			}

			ShowItem = item =>
			{
				var show = stackPanel.Children.OfType<FrameworkElement>().Where(x => x.Tag == item).FirstOrDefault();
				if (show == null)
					return;
				tabLabels.UpdateLayout();
				var left = show.TranslatePoint(new Point(0, 0), tabLabels).X + tabLabels.HorizontalOffset;
				tabLabels.ScrollToHorizontalOffset(Math.Min(left, Math.Max(tabLabels.HorizontalOffset, left + show.ActualWidth - tabLabels.ViewportWidth)));
			};

			tabLabels.Content = stackPanel;
			Grid.SetRow(tabLabels, 0);
			Grid.SetColumn(tabLabels, 1);
			grid.Children.Add(tabLabels);

			var moveLeft = new RepeatButton { Content = "<", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(5, 0, 5, 0) };
			moveLeft.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset - 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = ">", Margin = new Thickness(2, 0, 0, 0), Padding = new Thickness(5, 0, 5, 0) };
			moveRight.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset + 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveRight, 0);
			Grid.SetColumn(moveRight, 2);
			grid.Children.Add(moveRight);

			var contentControl = new ContentControl { FocusVisualStyle = null };
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding(nameof(TopMost)) { Source = this });
			Grid.SetRow(contentControl, 1);
			Grid.SetColumn(contentControl, 0);
			Grid.SetColumnSpan(contentControl, 3);
			grid.Children.Add(contentControl);

			canvas.Children.Add(grid);
		}

		void DoGridLayout()
		{
			int columns, rows;
			if (Layout == TabsLayout.Grid)
			{
				columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Items.Count)), 5));
				rows = Math.Max(1, Math.Min((Items.Count + columns - 1) / columns, 5));
			}
			else if (!Rows.HasValue)
			{
				columns = Math.Max(1, Columns ?? (int)Math.Ceiling(Math.Sqrt(Items.Count)));
				rows = Math.Max(1, (Items.Count + columns - 1) / columns);
			}
			else
			{
				rows = Math.Max(1, Rows.Value);
				columns = Math.Max(1, Columns ?? (Items.Count + rows - 1) / rows);
			}

			var totalRows = (Items.Count + columns - 1) / columns;

			var scrollBarVisibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			if (scrollBar.Visibility != scrollBarVisibility)
			{
				scrollBar.Visibility = scrollBarVisibility;
				UpdateLayout();
			}

			var width = canvas.ActualWidth / columns;
			var height = canvas.ActualHeight / rows;

			scrollBar.ViewportSize = scrollBar.LargeChange = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - scrollBar.ViewportSize;

			for (var ctr = 0; ctr < Items.Count; ++ctr)
			{
				var item = Items[ctr] as TextEditor;
				var top = ctr / columns * height - scrollBar.Value;
				if ((top + height < 0) || (top > canvas.ActualHeight))
					continue;

				var dockPanel = new DockPanel { AllowDrop = true, Margin = new Thickness(0, 0, 2, 2) };
				dockPanel.Drop += (s, e) => OnDrop(e, item);
				var tabLabel = GetTabLabel(this, true, item);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					item.SetValue(DockPanel.DockProperty, Dock.Bottom);
					item.FocusVisualStyle = null;
					dockPanel.Children.Add(item);
				}

				Canvas.SetLeft(dockPanel, ctr % columns * width + 1);
				Canvas.SetTop(dockPanel, top + 1);
				dockPanel.Width = width - 2;
				dockPanel.Height = height - 2;
				canvas.Children.Add(dockPanel);
			}

			ShowItem = item =>
			{
				var index = Items.IndexOf(item);
				if (index == -1)
					return;
				var top = index / columns * height;
				scrollBar.Value = Math.Min(top, Math.Max(scrollBar.Value, top + height - scrollBar.ViewportSize));
			};
		}

		public void NotifyActiveChanged() => ItemTabs_TabsChanged();

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = new AnswerResult();
			var topMost = TopMost;
			foreach (var item in Items)
			{
				TopMost = item;
				if (!item.CanClose(answer))
				{
					e.Cancel = true;
					return;
				}
			}
			TopMost = topMost;
			Items.ToList().ForEach(item => item.Closed());
			base.OnClosing(e);
		}

		System.Windows.Forms.NotifyIcon ni;
		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			if (WindowState == WindowState.Minimized)
			{
				if (Settings.MinimizeToTray)
				{
					ni = new System.Windows.Forms.NotifyIcon
					{
						Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location),
						Visible = true,
					};
					ni.Click += (s, e2) => Restore();
					Hide();
				}
			}
		}

		public bool Restore()
		{
			if (ni == null)
				return false;

			base.Show();
			WindowState = WindowState.Normal;
			ni.Dispose();
			ni = null;
			return true;
		}

		static void LoadAll()
		{
			ContentFunctions.Load();
			DatabaseFunctions.Load();
			DateTimeFunctions.Load();
			DiffFunctions.Load();
			EditFunctions.Load();
			ExpressionFunctions.Load();
			FileFunctions.Load();
			FilesFunctions.Load();
			HelpFunctions.Load();
			ImageFunctions.Load();
			KeysFunctions.Load();
			MacroFunctions.Load();
			NetworkFunctions.Load();
			NumericFunctions.Load();
			PositionFunctions.Load();
			RegionFunctions.Load();
			SelectFunctions.Load();
			TableFunctions.Load();
			TextFunctions.Load();
			WindowFunctions.Load();
		}
	}
}
