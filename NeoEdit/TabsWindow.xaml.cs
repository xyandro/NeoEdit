using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
			Tabs = new Tabs();
			if (addEmpty)
				HandleCommand(new ExecuteState(NECommand.File_New_New));

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => HandleCommand(new ExecuteState(command) { MultiStatus = multiStatus }));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			activateTabsTimer = new RunOnceTimer(() => ActivateTabs());
			NEClipboard.ClipboardChanged += () => SetStatusBarText();

			scrollBar.ValueChanged += OnScrollBarValueChanged;
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			if (addEmpty)
				HandleCommand(new ExecuteState(NECommand.File_New_New));
		}

		public bool HandleCommand(ExecuteState executeState, bool configure = true) => Tabs.HandleCommand(executeState, configure);

		readonly RunOnceTimer activateTabsTimer;
		public void QueueActivateTabs() => Dispatcher.Invoke(() => activateTabsTimer.Start());

		public void SetLayout(int? columns = null, int? rows = null, int? maxColumns = null, int? maxRows = null) => Tabs.SetLayout(columns, rows, maxColumns, maxRows);

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

		void OnScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => DrawAll();

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

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => AddTab(new Tab(file)));
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

			e.Handled = Tabs.HandleCommand(new ExecuteState(NECommand.Internal_Key) { Key = key });
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

		bool HandleClick(Tab tab)
		{
			if (!shiftDown)
				HandleCommand(new ExecuteState(NECommand.Internal_MouseActivate) { Configuration = tab });
			//TODO
			//else if (Focused != tab)
			//{
			//	Focused = tab;
			//	return true;
			//}
			return false;
		}

		public void AddTab(Tab tab, int? index = null, bool canReplace = true) => Tabs.AddTab(tab, index, canReplace);

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

		void SetColor(Border border)
		{
			if (border.Tag is Tab tab)
			{
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
			}
		}

		Border GetTabLabel(Tab tab)
		{
			var border = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(2), BorderThickness = new Thickness(2), Tag = tab };
			border.MouseLeftButtonDown += (s, e) => HandleClick(tab);
			border.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = Tabs.ActiveTabs.ToList();
					DragDrop.DoDragDrop(s as DependencyObject, new DataObject(typeof(List<Tab>), active), DragDropEffects.Move);
				}
			};

			SetColor(border);

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
			closeButton.Click += (s, e) =>
			{
				if (tab.CanClose())
					RemoveTab(tab);
			};
			Grid.SetRow(closeButton, 0);
			Grid.SetColumn(closeButton, 1);
			grid.Children.Add(closeButton);

			border.Child = grid;
			return border;
		}

		public IReadOnlyList<Tab> ActiveTabs => Tabs.ActiveTabs;

		public int? Columns => Tabs.Columns;

		public int? Rows => Tabs.Rows;

		public int? MaxColumns => Tabs.MaxColumns;

		public int? MaxRows => Tabs.MaxRows;

		void SetStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";
			statusBar.Items.Clear();
			statusBar.Items.Add($"Active: {plural(ActiveTabs.Count(), "file")}, {plural(ActiveTabs.Sum(tab => tab.Selections.Count), "selection")}");
			statusBar.Items.Add(new Separator());
			statusBar.Items.Add($"Inactive: {plural(Tabs.AllTabs.Except(ActiveTabs).Count(), "file")}, {plural(Tabs.AllTabs.Except(ActiveTabs).Sum(tab => tab.Selections.Count), "selection")}");
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

		public void RemoveTab(Tab tab, bool close = true) => Tabs.RemoveTab(tab, close);

		Tab Focused => Tabs.Focused;

		public void DrawAll(bool setFocus = false)
		{
			SetStatusBarText();
			SetMenuCheckboxes();
			Title = $"{(Focused == null ? "" : $"{Focused.DisplayName ?? Focused.FileName ?? "Untitled"} - ")}NeoEdit{(Helpers.IsAdministrator() ? " (Administrator)" : "")}";

			if ((Columns == 1) && (Rows == 1))
				DoFullLayout(setFocus);
			else
				DoGridLayout(setFocus);

		}

		Size lastSize;
		bool lastFull = false;
		IReadOnlyList<Tab> prevTabs;
		StackPanel tabLabelsStackPanel;
		ScrollViewer tabLabelsScrollViewer;
		Grid contentGrid;
		void DoFullLayout(bool setFocus)
		{
			if (lastSize != canvas.RenderSize)
			{
				lastSize = canvas.RenderSize;
				lastFull = false;
			}

			if (!lastFull)
			{
				lastFull = true;
				prevTabs = null;

				canvas.Children.Clear();

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

				tabLabelsScrollViewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };

				tabLabelsStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

				tabLabelsScrollViewer.Content = tabLabelsStackPanel;
				Grid.SetRow(tabLabelsScrollViewer, 0);
				Grid.SetColumn(tabLabelsScrollViewer, 1);
				grid.Children.Add(tabLabelsScrollViewer);

				var moveLeft = new RepeatButton { Width = 20, Height = 20, Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
				moveLeft.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset - 50, tabLabelsScrollViewer.ScrollableWidth)));
				Grid.SetRow(moveLeft, 0);
				Grid.SetColumn(moveLeft, 0);
				grid.Children.Add(moveLeft);

				var moveRight = new RepeatButton { Width = 20, Height = 20, Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
				moveRight.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset + 50, tabLabelsScrollViewer.ScrollableWidth)));
				Grid.SetRow(moveRight, 0);
				Grid.SetColumn(moveRight, 2);
				grid.Children.Add(moveRight);

				contentGrid = new Grid();
				Grid.SetRow(contentGrid, 1);
				Grid.SetColumn(contentGrid, 0);
				Grid.SetColumnSpan(contentGrid, 3);
				grid.Children.Add(contentGrid);

				outerBorder.Child = grid;
				canvas.Children.Add(outerBorder);
			}

			if (prevTabs != Tabs.AllTabs)
			{
				prevTabs = Tabs.AllTabs;
				tabLabelsStackPanel.Children.Clear();
				foreach (var tab in Tabs.AllTabs)
				{
					var tabLabel = GetTabLabel(tab);
					tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as Tab);
					tabLabelsStackPanel.Children.Add(tabLabel);
				}
			}
			else
			{
				tabLabelsStackPanel.Children.OfType<Border>().ForEach(SetColor);
			}

			if ((setFocus) && (Focused != null))
			{
				var show = tabLabelsStackPanel.Children.OfType<FrameworkElement>().Where(x => x.Tag == Focused).FirstOrDefault();
				if (show != null)
				{
					tabLabelsScrollViewer.UpdateLayout();
					var left = show.TranslatePoint(new Point(0, 0), tabLabelsScrollViewer).X + tabLabelsScrollViewer.HorizontalOffset;
					tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Min(left, Math.Max(tabLabelsScrollViewer.HorizontalOffset, left + show.ActualWidth - tabLabelsScrollViewer.ViewportWidth)));
				}
			}

			contentGrid.Children.Clear();
			if (Focused != null)
			{
				var tabWindow = new TabWindow(Focused);
				contentGrid.Children.Add(tabWindow);
				tabWindow.DrawAll();
			}
		}

		void DoGridLayout(bool setFocus)
		{
			canvas.Children.Clear();
			int? columns = null, rows = null;
			if (Columns.HasValue)
				columns = Math.Max(1, Columns.Value);
			if (Rows.HasValue)
				rows = Math.Max(1, Rows.Value);
			if ((!columns.HasValue) && (!rows.HasValue))
				columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Tabs.AllTabs.Count)), MaxColumns ?? int.MaxValue));
			if (!rows.HasValue)
				rows = Math.Max(1, Math.Min((Tabs.AllTabs.Count + columns.Value - 1) / columns.Value, MaxRows ?? int.MaxValue));
			if (!columns.HasValue)
				columns = Math.Max(1, Math.Min((Tabs.AllTabs.Count + rows.Value - 1) / rows.Value, MaxColumns ?? int.MaxValue));

			var totalRows = (Tabs.AllTabs.Count + columns.Value - 1) / columns.Value;

			scrollBar.Visibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			UpdateLayout();

			var width = canvas.ActualWidth / columns.Value;
			var height = canvas.ActualHeight / rows.Value;

			scrollBar.ViewportSize = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - canvas.ActualHeight;
			scrollBar.ValueChanged -= OnScrollBarValueChanged;
			if ((setFocus) && (Focused != null))
			{
				var index = Tabs.AllTabs.Indexes(tab => tab == Focused).DefaultIfEmpty(-1).First();
				if (index != -1)
				{
					var top = index / columns.Value * height;
					scrollBar.Value = Math.Min(top, Math.Max(scrollBar.Value, top + height - scrollBar.ViewportSize));
				}
			}
			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));
			scrollBar.ValueChanged += OnScrollBarValueChanged;

			for (var ctr = 0; ctr < Tabs.AllTabs.Count; ++ctr)
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

				var tabWindow = new TabWindow(Tabs.AllTabs[ctr]);
				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, Tabs.AllTabs[ctr]);
				var tabLabel = GetTabLabel(Tabs.AllTabs[ctr]);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					tabWindow.SetValue(DockPanel.DockProperty, Dock.Bottom);
					tabWindow.FocusVisualStyle = null;
					dockPanel.Children.Add(tabWindow);
				}
				tabWindow.DrawAll();

				border.Child = dockPanel;

				border.Width = width;
				border.Height = height;
				canvas.Children.Add(border);
			}
		}

		public void Move(Tab tab, int newIndex)
		{
			RemoveTab(tab);
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

		public int GetTabIndex(Tab tab, bool activeOnly = false) => Tabs.GetTabIndex(tab, activeOnly);
	}
}
