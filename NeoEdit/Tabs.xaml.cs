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
using NeoEdit.Program.Controls;
using NeoEdit.Program.Converters;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Misc;
using NeoEdit.Program.NEClipboards;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> Items { get { return UIHelper<Tabs>.GetPropValue<ObservableCollection<TextEditor>>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor TopMost { get { return UIHelper<Tabs>.GetPropValue<TextEditor>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Columns { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxColumns { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxRows { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
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
		[DepProp]
		public string KeysValuesCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		public DateTime LastActivated { get; private set; }

		readonly RunOnceTimer layoutTimer, topMostTimer;

		Action<TextEditor> ShowItem;
		int itemOrder = 0;

		static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));

		static Tabs()
		{
			UIHelper<Tabs>.Register();
			UIHelper<Tabs>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs>.AddCoerce(a => a.TopMost, (obj, value) => (value != null) && (obj.Items?.Contains(value) == true) ? value : null);
			UIHelper<Tabs>.AddCallback(a => a.Rows, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.Columns, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.MaxRows, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.MaxColumns, (obj, o, n) => obj.layoutTimer.Start());
			OutlineBrush.Freeze();
			BackgroundBrush.Freeze();
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

			Items = new ObservableCollection<TextEditor>();
			Rows = Columns = 1;
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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			try { SetPosition(Settings.WindowPosition); } catch { }
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
			KeysValuesCountText = $"{string.Join(" / ", keysAndValues.Select(l => $"{l.Sum(x => x.Count):n0}"))}";
		}

		Dictionary<TextEditor, List<string>> clipboard;
		public List<string> GetClipboard(TextEditor textEditor)
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

		static List<List<List<string>>> keysAndValues = Enumerable.Range(0, 10).Select(x => new List<List<string>>()).ToList();
		static List<Dictionary<string, int>> keysHash = new List<Dictionary<string, int>>();
		List<Dictionary<TextEditor, List<string>>> keysAndValuesLookup = keysAndValues.Select(x => default(Dictionary<TextEditor, List<string>>)).ToList();
		Dictionary<TextEditor, Dictionary<string, int>> keysHashLookup;
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

			UpdateStatusBarText();
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
					keysAndValuesLookup[kvIndex] = Items.ToDictionary(item => item, item => keysAndValues[kvIndex][0]);
					if (kvIndex == 0)
						keysHashLookup = Items.ToDictionary(item => item, item => keysHash[0]);
				}
				else
				{
					var activeTabs = Items.Where(item => item.Active).ToList();
					if (keysAndValues[kvIndex].Count != activeTabs.Count)
					{
						if (throwOnException)
							throw new Exception("Tab count doesn't match keys count");
						return false;
					}
					keysAndValuesLookup[kvIndex] = activeTabs.Select((item, index) => new { item, index }).ToDictionary(obj => obj.item, obj => keysAndValues[kvIndex][obj.index]);
					if (kvIndex == 0)
						keysHashLookup = activeTabs.Select((item, index) => new { item, index }).ToDictionary(obj => obj.item, obj => keysHash[obj.index]);
				}
			}

			return true;
		}

		public List<string> GetKeysAndValues(TextEditor textEditor, int index, bool throwOnException = true)
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

		public Dictionary<string, int> GetKeysHash(TextEditor textEditor)
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
			fileList.ForEach(file => Add(new TextEditor(file)));
			e.Handled = true;
		}

		public Macro RecordingMacro { get; set; }
		public Macro MacroPlaying { get; set; }

		internal void RunCommand(NECommand command, bool? multiStatus)
		{
			if (MacroPlaying != null)
				return;

			switch (command)
			{
				case NECommand.Macro_Record_Quick_1: Command_Macro_Record_Quick(1); return;
				case NECommand.Macro_Record_Quick_2: Command_Macro_Record_Quick(2); return;
				case NECommand.Macro_Record_Quick_3: Command_Macro_Record_Quick(3); return;
				case NECommand.Macro_Record_Quick_4: Command_Macro_Record_Quick(4); return;
				case NECommand.Macro_Record_Quick_5: Command_Macro_Record_Quick(5); return;
				case NECommand.Macro_Record_Quick_6: Command_Macro_Record_Quick(6); return;
				case NECommand.Macro_Record_Quick_7: Command_Macro_Record_Quick(7); return;
				case NECommand.Macro_Record_Quick_8: Command_Macro_Record_Quick(8); return;
				case NECommand.Macro_Record_Quick_9: Command_Macro_Record_Quick(9); return;
				case NECommand.Macro_Record_Quick_10: Command_Macro_Record_Quick(10); return;
				case NECommand.Macro_Record_Quick_11: Command_Macro_Record_Quick(11); return;
				case NECommand.Macro_Record_Quick_12: Command_Macro_Record_Quick(12); return;
				case NECommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case NECommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case NECommand.Macro_Append_Quick_1: Command_Macro_Append_Quick(1); return;
				case NECommand.Macro_Append_Quick_2: Command_Macro_Append_Quick(2); return;
				case NECommand.Macro_Append_Quick_3: Command_Macro_Append_Quick(3); return;
				case NECommand.Macro_Append_Quick_4: Command_Macro_Append_Quick(4); return;
				case NECommand.Macro_Append_Quick_5: Command_Macro_Append_Quick(5); return;
				case NECommand.Macro_Append_Quick_6: Command_Macro_Append_Quick(6); return;
				case NECommand.Macro_Append_Quick_7: Command_Macro_Append_Quick(7); return;
				case NECommand.Macro_Append_Quick_8: Command_Macro_Append_Quick(8); return;
				case NECommand.Macro_Append_Quick_9: Command_Macro_Append_Quick(9); return;
				case NECommand.Macro_Append_Quick_10: Command_Macro_Append_Quick(10); return;
				case NECommand.Macro_Append_Quick_11: Command_Macro_Append_Quick(11); return;
				case NECommand.Macro_Append_Quick_12: Command_Macro_Append_Quick(12); return;
				case NECommand.Macro_Append_Append: Command_Macro_Append_Append(); return;
				case NECommand.Macro_Play_Quick_1: Command_Macro_Play_Quick(1); return;
				case NECommand.Macro_Play_Quick_2: Command_Macro_Play_Quick(2); return;
				case NECommand.Macro_Play_Quick_3: Command_Macro_Play_Quick(3); return;
				case NECommand.Macro_Play_Quick_4: Command_Macro_Play_Quick(4); return;
				case NECommand.Macro_Play_Quick_5: Command_Macro_Play_Quick(5); return;
				case NECommand.Macro_Play_Quick_6: Command_Macro_Play_Quick(6); return;
				case NECommand.Macro_Play_Quick_7: Command_Macro_Play_Quick(7); return;
				case NECommand.Macro_Play_Quick_8: Command_Macro_Play_Quick(8); return;
				case NECommand.Macro_Play_Quick_9: Command_Macro_Play_Quick(9); return;
				case NECommand.Macro_Play_Quick_10: Command_Macro_Play_Quick(10); return;
				case NECommand.Macro_Play_Quick_11: Command_Macro_Play_Quick(11); return;
				case NECommand.Macro_Play_Quick_12: Command_Macro_Play_Quick(12); return;
				case NECommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
				case NECommand.Macro_Play_Repeat: Command_Macro_Play_Repeat(); return;
				case NECommand.Macro_Play_PlayOnCopiedFiles: Command_Macro_Play_PlayOnCopiedFiles(); return;
			}

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult, multiStatus))
				return;

			if (RecordingMacro != null)
				RecordingMacro.AddCommand(command, shiftDown, dialogResult, multiStatus);

			HandleCommand(command, shiftDown, dialogResult, multiStatus);
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

			var result = true;
			switch (command)
			{
				case NECommand.File_Open_Open: dialogResult = Command_File_Open_Open_Dialog(); break;
				case NECommand.Macro_Open_Open: dialogResult = Command_File_Open_Open_Dialog(Macro.MacroDirectory); break;
				case NECommand.Window_CustomGrid: dialogResult = Command_Window_CustomGrid_Dialog(); break;
				default: result = false; break;
			}

			if (result)
				return dialogResult != null;

			if (TopMost == null)
				return true;

			return TopMost.GetDialogResult(command, out dialogResult, multiStatus);
		}

		public bool HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			Items.ForEach(te => te.DragFiles = null);

			switch (command)
			{
				case NECommand.File_New_New: Command_File_New_New(shiftDown); break;
				case NECommand.File_New_FromClipboards: Command_File_New_FromClipboards(); break;
				case NECommand.File_New_FromClipboardSelections: Command_File_New_FromClipboardSelections(); break;
				case NECommand.File_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case NECommand.File_Operations_DragDrop: Command_File_Operations_DragDrop(); break;
				case NECommand.File_MoveToNewWindow: Command_File_MoveToNewWindow(); break;
				case NECommand.File_Shell_Integrate: Command_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: Command_File_Shell_Unintegrate(); break;
				case NECommand.File_Exit: Close(); break;
				case NECommand.Diff_Diff: Command_Diff_Diff(shiftDown); break;
				case NECommand.Diff_Select_LeftTab: Command_Diff_Select_LeftRightBothTabs(true); break;
				case NECommand.Diff_Select_RightTab: Command_Diff_Select_LeftRightBothTabs(false); break;
				case NECommand.Diff_Select_BothTabs: Command_Diff_Select_LeftRightBothTabs(null); break;
				case NECommand.Macro_Open_Quick_1: Command_Macro_Open_Quick(1); return true;
				case NECommand.Macro_Open_Quick_2: Command_Macro_Open_Quick(2); return true;
				case NECommand.Macro_Open_Quick_3: Command_Macro_Open_Quick(3); return true;
				case NECommand.Macro_Open_Quick_4: Command_Macro_Open_Quick(4); return true;
				case NECommand.Macro_Open_Quick_5: Command_Macro_Open_Quick(5); return true;
				case NECommand.Macro_Open_Quick_6: Command_Macro_Open_Quick(6); return true;
				case NECommand.Macro_Open_Quick_7: Command_Macro_Open_Quick(7); return true;
				case NECommand.Macro_Open_Quick_8: Command_Macro_Open_Quick(8); return true;
				case NECommand.Macro_Open_Quick_9: Command_Macro_Open_Quick(9); return true;
				case NECommand.Macro_Open_Quick_10: Command_Macro_Open_Quick(10); return true;
				case NECommand.Macro_Open_Quick_11: Command_Macro_Open_Quick(11); return true;
				case NECommand.Macro_Open_Quick_12: Command_Macro_Open_Quick(12); return true;
				case NECommand.Macro_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); return true;
				case NECommand.Window_NewWindow: Command_Window_NewWindow(); break;
				case NECommand.Window_Full: Command_Window_Full(); break;
				case NECommand.Window_Grid: Command_Window_Grid(); break;
				case NECommand.Window_CustomGrid: Command_Window_CustomGrid(dialogResult as WindowCustomGridDialog.Result); break;
				case NECommand.Window_ActiveTabs: Command_Window_ActiveTabs(); break;
				case NECommand.Window_Font_Size: Command_Window_Font_Size(); break;
				case NECommand.Window_Select_TabsWithSelections: Command_Window_Select_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Select_TabsWithoutSelections: Command_Window_Select_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Select_ModifiedTabs: Command_Window_Select_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Select_UnmodifiedTabs: Command_Window_Select_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Select_InactiveTabs: Command_Window_Select_InactiveTabs(); break;
				case NECommand.Window_Select_TabsWithSelectionsToTop: Command_Window_Select_TabsWithSelectionsToTop(); break;
				case NECommand.Window_Close_TabsWithSelections: Command_Window_Close_TabsWithWithoutSelections(true); break;
				case NECommand.Window_Close_TabsWithoutSelections: Command_Window_Close_TabsWithWithoutSelections(false); break;
				case NECommand.Window_Close_ModifiedTabs: Command_Window_Close_ModifiedUnmodifiedTabs(true); break;
				case NECommand.Window_Close_UnmodifiedTabs: Command_Window_Close_ModifiedUnmodifiedTabs(false); break;
				case NECommand.Window_Close_ActiveTabs: Command_Window_Close_ActiveInactiveTabs(true); break;
				case NECommand.Window_Close_InactiveTabs: Command_Window_Close_ActiveInactiveTabs(false); break;
				case NECommand.Window_WordList: Command_Window_WordList(); break;
				case NECommand.Help_About: Command_Help_About(); break;
				case NECommand.Help_Update: Command_Help_Update(); break;
				case NECommand.Help_RunGC: Command_Help_RunGC(); break;
			}

			try
			{
				var preResult = default(object);
				foreach (var textEditorItem in Items.Where(item => item.Active).ToList())
					textEditorItem.PreHandleCommand(command, ref preResult);

				var answer = new AnswerResult();
				foreach (var textEditorItem in Items.Where(item => item.Active).ToList())
				{
					textEditorItem.HandleCommand(command, shiftDown, dialogResult, multiStatus, answer, preResult);
					if (answer.Answer.HasFlag(MessageOptions.Cancel))
						break;
				}
				if (newClipboard != null)
					NEClipboard.Current = newClipboard;
				SetupNewKeys();
			}
			finally
			{
				clipboard = null;
				newClipboard = null;

				for (var ctr = 0; ctr < keysAndValues.Count; ++ctr)
					keysAndValuesLookup[ctr] = null;
				keysHashLookup = null;
				newKeysAndValues = null;
			}

			return true;
		}

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
					if (answer.Answer.HasFlag(MessageOptions.Cancel))
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

		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null)
		{
			Columns = columns;
			Rows = rows;
			MaxColumns = maxColumns;
			MaxRows = maxRows;
			topMostTimer.Start();
		}

		public void Add(TextEditor item, int? index = null, bool canReplace = true)
		{
			var replace = (canReplace) && (!index.HasValue) && (!item.Empty()) && (TopMost != null) && (TopMost.Empty()) ? TopMost : default(TextEditor);
			if (replace != null)
			{
				replace.Closed();
				Items[Items.IndexOf(replace)] = item;
			}
			else
				Items.Insert(index ?? Items.Count, item);
			TopMost = item;
		}

		public TextEditor Add(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null, int? index = null, bool canReplace = true)
		{
			var textEditor = new TextEditor(fileName, displayName, bytes, codePage, contentType, modified, line, column, shutdownData);
			Add(textEditor, index, canReplace);
			return textEditor;
		}

		public Window AddDiff(TextEditor textEdit1, TextEditor textEdit2)
		{
			if (textEdit1.ContentType == ParserType.None)
				textEdit1.ContentType = textEdit2.ContentType;
			if (textEdit2.ContentType == ParserType.None)
				textEdit2.ContentType = textEdit1.ContentType;
			Add(textEdit1);
			Add(textEdit2);
			textEdit1.DiffTarget = textEdit2;
			SetLayout(maxColumns: 2);
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

		public bool TabIsActive(TextEditor item) => Items.Where(x => x == item).Select(x => x.Active).DefaultIfEmpty(false).First();

		public int GetIndex(TextEditor item, bool activeOnly = false)
		{
			var index = Items.Where(x => (!activeOnly) || (x.Active)).Indexes(x => x == item).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void Remove(TextEditor item)
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

		void OnDrop(DragEventArgs e, TextEditor toItem)
		{
			var fromItems = e.Data.GetData(typeof(List<TextEditor>)) as List<TextEditor>;
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

		public void MoveToTop(IEnumerable<TextEditor> tabs)
		{
			var found = new HashSet<TextEditor>(tabs);
			var indexes = Items.Indexes(item => found.Contains(item)).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				Items.Move(indexes[ctr], ctr);
		}

		DockPanel GetTabLabel(Tabs tabs, bool tiles, TextEditor item, out BindingBase colorBinding)
		{
			var dockPanel = new DockPanel { Height = 20, Margin = new Thickness(0, 0, tiles ? 0 : 2, 0), Tag = item };

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p2 ? \"#c0c0c0\" : (p1 ? \"#a0a0a0\" : \"#606060\")" };
			multiBinding.Bindings.Add(new Binding { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TextEditor.Active)) { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TopMost)) { Source = tabs });
			colorBinding = multiBinding;

			dockPanel.MouseLeftButtonDown += (s, e) => tabs.TopMost = item;
			dockPanel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = item.TabsParent.Items.Where(tab => tab.Active).ToList();
					DragDrop.DoDragDrop(s as DockPanel, new DataObject(typeof(List<TextEditor>), active), DragDropEffects.Move);
				}
			};

			var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0), Foreground = Brushes.White };
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextEditor.TabLabel)) { Source = item });
			dockPanel.Children.Add(text);

			var closeButton = new Button
			{
				Content = "🗙",
				BorderThickness = new Thickness(0),
				Style = FindResource(ToolBar.ButtonStyleKey) as Style,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2, 0, 5, 0),
				Foreground = Brushes.Red,
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
			if ((Columns == 1) && (Rows == 1))
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
			foreach (var item in Items)
			{
				var tabLabel = GetTabLabel(this, false, item, out var colorBinding);
				tabLabel.SetBinding(DockPanel.BackgroundProperty, colorBinding);
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as TextEditor);
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

			var moveLeft = new RepeatButton { Width = 20, Height = 20, Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent };
			moveLeft.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset - 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Width = 20, Height = 20, Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent };
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
				columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Items.Count)), MaxColumns ?? int.MaxValue));
			if (!rows.HasValue)
				rows = Math.Max(1, Math.Min((Items.Count + columns.Value - 1) / columns.Value, MaxRows ?? int.MaxValue));
			if (!columns.HasValue)
				columns = Math.Max(1, Math.Min((Items.Count + rows.Value - 1) / rows.Value, MaxColumns ?? int.MaxValue));

			var totalRows = (Items.Count + columns.Value - 1) / columns.Value;

			var scrollBarVisibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			if (scrollBar.Visibility != scrollBarVisibility)
			{
				scrollBar.Visibility = scrollBarVisibility;
				UpdateLayout();
			}

			var width = canvas.ActualWidth / columns.Value;
			var height = canvas.ActualHeight / rows.Value;

			scrollBar.ViewportSize = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - canvas.ActualHeight;

			for (var ctr = 0; ctr < Items.Count; ++ctr)
			{
				var item = Items[ctr] as TextEditor;
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

				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, item);
				var tabLabel = GetTabLabel(this, true, item, out var colorBinding);
				border.SetBinding(Border.BorderBrushProperty, colorBinding);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					item.SetValue(DockPanel.DockProperty, Dock.Bottom);
					item.FocusVisualStyle = null;
					dockPanel.Children.Add(item);
				}

				border.Child = dockPanel;

				border.Width = width;
				border.Height = height;
				canvas.Children.Add(border);
			}

			ShowItem = item =>
			{
				var index = Items.IndexOf(item);
				if (index == -1)
					return;
				var top = index / columns.Value * height;
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

			try { Settings.WindowPosition = GetPosition(); } catch { }
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

			Show();
			WindowState = WindowState.Normal;
			ni.Dispose();
			ni = null;
			return true;
		}
	}
}
