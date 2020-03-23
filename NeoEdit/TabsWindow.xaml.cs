using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Misc;
using NeoEdit.Program.NEClipboards;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		readonly Tabs Tabs;

		static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
		static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));

		static TabsWindow()
		{
			UIHelper<TabsWindow>.Register();

			OutlineBrush.Freeze();
			BackgroundBrush.Freeze();
		}

		public TabsWindow(Tabs tabs)
		{
			Tabs = tabs;

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			activateTabsTimer = new RunOnceTimer(() => ActivateTabs());
			drawTimer = new RunOnceTimer(() => DrawAll());
			NEClipboard.ClipboardChanged += () => statusBar.InvalidateVisual();
			Font.FontSizeChanged += (s, e) => QueueDraw();

			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;
		}

		public bool HandleCommand(ExecuteState state, bool configure = true)
		{
			state.TabsWindow = this;
			if (configure)
				state.Modifiers = Keyboard.Modifiers;
			if (!Tabs.HandleCommand(state, configure))
				return false;

			if ((state.Command == NECommand.File_Exit) && (state.Configuration == null))
				Close();

			return true;
		}

		readonly RunOnceTimer activateTabsTimer, drawTimer;
		public void QueueActivateTabs()
		{
			if (Helpers.IsDebugBuild)
				return;
			Dispatcher.Invoke(() => activateTabsTimer.Start());
		}

		public void QueueDraw() => Dispatcher.Invoke(() => drawTimer.Start());

		void OnActivated(object sender, EventArgs e)
		{
			Tabs.LastActivated = DateTime.Now;
			QueueActivateTabs();
		}

		void ActivateTabs()
		{
			if (!IsActive)
				return;

			HandleCommand(new ExecuteState(NECommand.Internal_Activate));
		}

		void OnScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => QueueDraw();

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			QueueDraw();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			try { SetPosition(Settings.WindowPosition); } catch { }
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => Tabs.AddTab(new Tab(file)));
			e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;

			e.Handled = HandleCommand(new ExecuteState(NECommand.Internal_Key) { Key = key });
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			HandleCommand(new ExecuteState(NECommand.Internal_Text) { Text = e.Text });
			e.Handled = true;
		}

		//TODO
		//protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		//{
		//	for (var source = e.OriginalSource as FrameworkElement; source != null; source = source.Parent as FrameworkElement)
		//		if (source is TabData tab)
		//		{
		//			if (HandleClick(tab))
		//			{
		//				e.Handled = true;
		//				return;
		//			}
		//			break;
		//		}
		//	base.OnPreviewMouseLeftButtonDown(e);
		//}

		void OnDrop(DragEventArgs e, Tab toTab)
		{
			//TODO
			//var fromTabs = e.Data.GetData(typeof(List<Tab>)) as List<Tab>;
			//if (fromTabs == null)
			//	return;

			//var toIndex = GetTabIndex(toTab);
			//fromTabs.ForEach(fromTab => fromTab.TabsParent.RemoveTab(fromTab));

			//if (toIndex == -1)
			//	toIndex = Tabs.Count;
			//else
			//	toIndex = Math.Min(toIndex, Tabs.Count);

			//foreach (var fromTab in fromTabs)
			//{
			//	AddTab(fromTab, toIndex);
			//	++toIndex;
			//	e.Handled = true;
			//}
		}

		TabLabel CreateTabLabel(Tab tab)
		{
			var tabLabel = new TabLabel(tab);
			tabLabel.MouseLeftButtonDown += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_MouseActivate) { Configuration = tab });
			tabLabel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = Tabs.SortedActiveTabs.ToList();
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<Tab>), active), DragDropEffects.Move);
				}
			};
			tabLabel.CloseClicked += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_CloseTab) { Configuration = tab });

			tabLabel.Refresh(Tabs);
			return tabLabel;
		}

		readonly List<TabWindow> tabWindows = new List<TabWindow>();
		void SetTabWindowCount(int desiredCount)
		{
			while (tabWindows.Count < desiredCount)
				tabWindows.Add(new TabWindow());
			tabWindows.RemoveRange(desiredCount, tabWindows.Count - desiredCount);
		}

		void OnStatusBarRender(object s, DrawingContext dc)
		{
			const string Separator = "  |  ";

			var status = new List<string>();
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";
			status.Add($"Active: {plural(Tabs.UnsortedActiveTabsCount, "file")}, {plural(Tabs.UnsortedActiveTabs.Sum(tab => tab.Selections.Count), "selection")}");
			status.Add($"Inactive: {plural(Tabs.AllTabs.Except(Tabs.UnsortedActiveTabs).Count(), "file")}, {plural(Tabs.AllTabs.Except(Tabs.UnsortedActiveTabs).Sum(tab => tab.Selections.Count), "selection")}");
			status.Add($"Total: {plural(Tabs.AllTabs.Count(), "file")}, {plural(Tabs.AllTabs.Sum(tab => tab.Selections.Count), "selection")}");
			status.Add($"Clipboard: {plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}");
			status.Add($"Keys/Values: {string.Join(" / ", Tabs.keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");

			var text = new FormattedText(string.Join(Separator, status), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Brushes.White, 1);
			var pos = 0;
			for (var ctr = 0; ctr < status.Count - 1; ++ctr)
			{
				pos += status[ctr].Length;
				text.SetForegroundBrush(Brushes.Gray, pos, Separator.Length);
				pos += Separator.Length;
			}
			dc.DrawText(text, new Point(6, 1));
		}

		void SetMenuCheckboxes()
		{
			bool? GetMultiStatus(Func<Tab, bool> func)
			{
				var results = Tabs.UnsortedActiveTabs.Select(func).Distinct().Take(2).ToList();
				if (results.Count != 1)
					return default;
				return results[0];
			}

			menu.menu_File_DontExitOnClose.MultiStatus = Settings.DontExitOnClose;
			menu.menu_Edit_EscapeClearsSelections.MultiStatus = Settings.EscapeClearsSelections;
			menu.menu_Macro_Visualize.MultiStatus = Tabs.MacroVisualize;
			menu.menu_Window_Font_ShowSpecial.MultiStatus = Font.ShowSpecialChars;

			menu.menu_File_AutoRefresh.MultiStatus = GetMultiStatus(x => x.AutoRefresh);
			menu.menu_File_Encrypt.MultiStatus = GetMultiStatus(x => !string.IsNullOrWhiteSpace(x.AESKey));
			menu.menu_File_Compress.MultiStatus = GetMultiStatus(x => x.Compressed);
			menu.menu_Edit_Navigate_JumpBy_Words.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Words);
			menu.menu_Edit_Navigate_JumpBy_Numbers.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Numbers);
			menu.menu_Edit_Navigate_JumpBy_Paths.MultiStatus = GetMultiStatus(x => x.JumpBy == JumpByType.Paths);
			menu.menu_Diff_IgnoreWhitespace.MultiStatus = GetMultiStatus(x => x.DiffIgnoreWhitespace);
			menu.menu_Diff_IgnoreCase.MultiStatus = GetMultiStatus(x => x.DiffIgnoreCase);
			menu.menu_Diff_IgnoreNumbers.MultiStatus = GetMultiStatus(x => x.DiffIgnoreNumbers);
			menu.menu_Diff_IgnoreLineEndings.MultiStatus = GetMultiStatus(x => x.DiffIgnoreLineEndings);
			menu.menu_Content_Type_None.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.None);
			menu.menu_Content_Type_Balanced.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.Balanced);
			menu.menu_Content_Type_Columns.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.Columns);
			menu.menu_Content_Type_CPlusPlus.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CPlusPlus);
			menu.menu_Content_Type_CSharp.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CSharp);
			menu.menu_Content_Type_CSV.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.CSV);
			menu.menu_Content_Type_ExactColumns.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.ExactColumns);
			menu.menu_Content_Type_HTML.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.HTML);
			menu.menu_Content_Type_JSON.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.JSON);
			menu.menu_Content_Type_SQL.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.SQL);
			menu.menu_Content_Type_TSV.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.TSV);
			menu.menu_Content_Type_XML.MultiStatus = GetMultiStatus(x => x.ContentType == ParserType.XML);
			menu.menu_Content_HighlightSyntax.MultiStatus = GetMultiStatus(x => x.HighlightSyntax);
			menu.menu_Content_StrictParsing.MultiStatus = GetMultiStatus(x => x.StrictParsing);
			menu.menu_Content_KeepSelections.MultiStatus = GetMultiStatus(x => x.KeepSelections);
			menu.menu_Window_ViewBinary.MultiStatus = GetMultiStatus(x => x.ViewBinary);
		}

		void DrawAll()
		{
			statusBar.InvalidateVisual();
			SetMenuCheckboxes();
			Title = $"{(Tabs.Focused == null ? "" : $"{Tabs.Focused.DisplayName ?? Tabs.Focused.FileName ?? "Untitled"} - ")}NeoEdit{(Helpers.IsAdministrator() ? " (Administrator)" : "")}";

			if ((Tabs.Columns == 1) && (Tabs.Rows == 1))
				DoFullLayout();
			else
				DoGridLayout();

		}

		public void Move(Tab tab, int newIndex)
		{
			Tabs.RemoveTab(tab);
			Tabs.InsertTab(tab, newIndex);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			e.Cancel = !HandleCommand(new ExecuteState(NECommand.File_Exit) { Configuration = true });
		}

		protected override void OnClosed(EventArgs e)
		{
			try { Settings.WindowPosition = GetPosition(); } catch { }
			base.OnClosed(e);
		}

		public bool GotoTab(string fileName, int? line, int? column, int? index)
		{
			var tab = Tabs.AllTabs.FirstOrDefault(x => x.FileName == fileName);
			if (tab == null)
				return false;
			Activate();
			Tabs.ClearAllActive();
			Tabs.SetActive(tab);
			Tabs.Focused = tab;
			//TODO
			//tab.Execute_File_Refresh();
			//tab.Goto(line, column, index);
			return true;
		}

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		public void SetForeground()
		{
			Activate();
			Show();
			SetForegroundWindow(new WindowInteropHelper(this).Handle);
		}
	}
}
