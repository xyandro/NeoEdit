using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.GUI.Controls
{
	public enum TabsLayout
	{
		Full,
		Grid,
		Custom,
	}

	public class Tabs<ItemType, CommandType> : UserControl where ItemType : TabsControl<ItemType, CommandType>
	{
		public delegate void TabsChangedDelegate();
		public event TabsChangedDelegate TabsChanged;

		[DepProp]
		public ObservableCollection<ItemType> Items { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<ObservableCollection<ItemType>>(this); } private set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemType TopMost { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<ItemType>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public TabsLayout Layout { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<TabsLayout>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public int? Columns { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<int?>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<int?>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public TabsWindow<ItemType, CommandType> WindowParent { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<TabsWindow<ItemType, CommandType>>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }

		readonly RunOnceTimer layoutTimer, topMostTimer;

		static Tabs()
		{
			UIHelper<Tabs<ItemType, CommandType>>.Register();
			UIHelper<Tabs<ItemType, CommandType>>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs<ItemType, CommandType>>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs<ItemType, CommandType>>.AddCoerce(a => a.TopMost, (obj, value) => (value != null) && (obj.Items?.Contains(value) == true) ? value : null);
			UIHelper<Tabs<ItemType, CommandType>>.AddCallback(a => a.Layout, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs<ItemType, CommandType>>.AddCallback(a => a.Rows, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs<ItemType, CommandType>>.AddCallback(a => a.Columns, (obj, o, n) => obj.layoutTimer.Start());
		}

		readonly Canvas canvas;
		readonly ScrollBar scrollBar;
		Action<ItemType> ShowItem;
		int itemOrder = 0;
		public Tabs()
		{
			layoutTimer = new RunOnceTimer(DoLayout);
			topMostTimer = new RunOnceTimer(ShowTopMost);
			topMostTimer.AddDependency(layoutTimer);

			SetupLayout(out canvas, out scrollBar);
			SizeChanged += (s, e) => layoutTimer.Start();
			scrollBar.ValueChanged += (s, e) => layoutTimer.Start();
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			Items = new ObservableCollection<ItemType>();
			Layout = TabsLayout.Full;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			VerticalAlignment = VerticalAlignment.Stretch;
			Drop += (s, e) => OnDrop(e, null);
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

		public ItemType CreateTab(ItemType item, int? index = null)
		{
			var replace = (!index.HasValue) && (!item.Empty()) && (TopMost != null) && (TopMost.Empty()) ? TopMost : default(ItemType);
			if (replace != null)
				Items[Items.IndexOf(replace)] = item;
			else
				Items.Insert(index ?? Items.Count, item);
			TopMost = item;
			return replace;
		}

		public void ShowActiveTabsDialog()
		{
			ActiveTabsDialog<ItemType, CommandType>.Run(this);
			UpdateTopMost();
		}

		void ItemsChanged()
		{
			TabsChanged?.Invoke();

			if (Items == null)
				return;

			foreach (var item in Items)
			{
				EnhancedFocusManager.SetIsEnhancedFocusScope(item, true);
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

		void UpdateTopMost()
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

		public bool IsActive(ItemType item) => Items.Where(x => x == item).Select(x => x.Active).DefaultIfEmpty(false).First();

		public int GetIndex(ItemType item, bool activeOnly = false)
		{
			var index = Items.Where(x => (!activeOnly) || (x.Active)).Indexes(x => x == item).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void Remove(ItemType item)
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

		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

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
			base.OnMouseLeftButtonDown(e);
			var source = e.OriginalSource as DependencyObject;
			foreach (var item in Items)
				if (item.IsAncestorOf(source))
					TopMost = item;
		}

		void OnDrop(DragEventArgs e, ItemType toItem)
		{
			var fromItems = e.Data.GetData(typeof(List<ItemType>)) as List<ItemType>;
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

		public void MoveToTop(IEnumerable<ItemType> tabs)
		{
			var found = new HashSet<ItemType>(tabs);
			var indexes = Items.Indexes(item => found.Contains(item)).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				Items.Move(indexes[ctr], ctr);
		}

		DockPanel GetTabLabel(Tabs<ItemType, CommandType> tabs, bool tiles, ItemType item)
		{
			var dockPanel = new DockPanel { Margin = new Thickness(0, 0, tiles ? 0 : 2, 1), Tag = item };

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p2 ? \"CadetBlue\" : (p1 ? \"LightBlue\" : \"LightGray\")" };
			multiBinding.Bindings.Add(new Binding { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TabsControl<ItemType, CommandType>.Active)) { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(Tabs<ItemType, CommandType>.TopMost)) { Source = tabs });
			dockPanel.SetBinding(DockPanel.BackgroundProperty, multiBinding);

			dockPanel.MouseLeftButtonDown += (s, e) => tabs.TopMost = item;
			dockPanel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = (item.TabsParent as Tabs<ItemType, CommandType>).Items.Where(tab => tab.Active).ToList();
					DragDrop.DoDragDrop(s as DockPanel, new DataObject(typeof(List<ItemType>), active), DragDropEffects.Move);
				}
			};

			var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0) };
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(TabsControl<ItemType, CommandType>.TabLabel)) { Source = item });
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

		void SetupLayout(out Canvas canvas, out ScrollBar scrollBar)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Content = grid;

			canvas = new Canvas { Background = Brushes.Gray, ClipToBounds = true };
			Grid.SetRow(canvas, 0);
			Grid.SetColumn(canvas, 0);
			grid.Children.Add(canvas);

			scrollBar = new ScrollBar();
			Grid.SetRow(scrollBar, 0);
			Grid.SetColumn(scrollBar, 1);
			grid.Children.Add(scrollBar);
		}

		void ClearLayout()
		{
			canvas.Children.Clear();
			foreach (var item in Items)
			{
				var parent = item.Parent;
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
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as ItemType);
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
				var item = Items[ctr];
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

		internal void NotifyActiveChanged() => TabsChanged?.Invoke();
	}
}
