using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Misc;
using NeoEdit.Program.NEClipboards;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		readonly Tabs Tabs;
		public DateTime LastActivated => Tabs.LastActivated;
		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null) => Tabs.SetLayout(columns, rows, maxColumns, maxRows);
		public void AddTab(Tab tab, bool canReplace = true) => Tabs.AddTab(tab, canReplace: canReplace);


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

		public TabsWindow(bool addEmpty = false)
		{
			Tabs = new Tabs();

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			activateTabsTimer = new RunOnceTimer(() => ActivateTabs());
			drawTimer = new RunOnceTimer(() => DrawAll());
			NEClipboard.ClipboardChanged += () => SetStatusBarText();

			scrollBar.ValueChanged += OnScrollBarValueChanged;
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			if (addEmpty)
				HandleCommand(new ExecuteState(NECommand.File_New_New));
		}

		public bool HandleCommand(ExecuteState state, bool configure = true)
		{
			state.TabsWindow = this;
			state.Modifiers = Keyboard.Modifiers;
			return Tabs.HandleCommand(state, configure);
		}

		readonly RunOnceTimer activateTabsTimer, drawTimer;
		public void QueueActivateTabs() => Dispatcher.Invoke(() => activateTabsTimer.Start());

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

		Border GetTabLabel(Tab tab, bool getSize = false)
		{
			var border = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(2), BorderThickness = new Thickness(2), Tag = tab };
			border.MouseLeftButtonDown += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_MouseActivate) { Configuration = tab });
			border.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = Tabs.SortedActiveTabs.ToList();
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<Tab>), active), DragDropEffects.Move);
				}
			};

			if (Tabs.Focused == tab)
			{
				border.BorderBrush = FocusedWindowBorderBrush;
				border.Background = FocusedWindowBackgroundBrush;
			}
			else if (Tabs.IsActive(tab))
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
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tab.TabLabel)) { Source = tab });
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
			closeButton.Click += (s, e) => HandleCommand(new ExecuteState(NECommand.Internal_CloseTab) { Configuration = tab });
			Grid.SetRow(closeButton, 0);
			Grid.SetColumn(closeButton, 1);
			grid.Children.Add(closeButton);

			border.Child = grid;

			if (getSize)
				border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			return border;
		}

		void SetStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";
			statusBar.Items.Clear();
			statusBar.Items.Add($"Active: {plural(Tabs.UnsortedActiveTabs.Count, "file")}, {plural(Tabs.UnsortedActiveTabs.Sum(tab => tab.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Inactive: {plural(Tabs.AllTabs.Except(Tabs.UnsortedActiveTabs).Count(), "file")}, {plural(Tabs.AllTabs.Except(Tabs.UnsortedActiveTabs).Sum(tab => tab.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Total: {plural(Tabs.AllTabs.Count, "file")}, {plural(Tabs.AllTabs.Sum(tab => tab.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Clipboard: {plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Keys/Values: {string.Join(" / ", Tabs.keysAndValues.Select(l => $"{l.Sum(x => x.Values.Count):n0}"))}");

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

		void DrawAll(bool setFocus = false)
		{
			SetStatusBarText();
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
		//	Tabs.ToList().ForEach(tab => tab.Closed());
		//	base.OnClosing(e);

		//	try { Settings.WindowPosition = GetPosition(); } catch { }
		//}

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
	}
}
