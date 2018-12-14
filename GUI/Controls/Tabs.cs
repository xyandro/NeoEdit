﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
		public double TabsScroll { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<double>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public double TabsScrollMax { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<double>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public TabsWindow<ItemType, CommandType> WindowParent { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<TabsWindow<ItemType, CommandType>>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }

		static Tabs()
		{
			UIHelper<Tabs<ItemType, CommandType>>.Register();
			UIHelper<Tabs<ItemType, CommandType>>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs<ItemType, CommandType>>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs<ItemType, CommandType>>.AddCoerce(a => a.TopMost, (obj, value) => (value != null) && (obj.Items?.Contains(value) == true) ? value : null);
		}

		int itemOrder = 0;
		public Tabs()
		{
			SetupLayout();

			Items = new ObservableCollection<ItemType>();
			Layout = TabsLayout.Full;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			VerticalAlignment = VerticalAlignment.Stretch;
			Drop += (s, e) => OnDrop(e, null);
		}

		public void SetLayout(TabsLayout layout, int? columns = null, int? rows = null)
		{
			Layout = layout;
			Columns = columns;
			Rows = rows;
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

		void OnDrop(DragEventArgs e, DockPanel toPanel)
		{
			var fromItems = e.Data.GetData(typeof(List<ItemType>)) as List<ItemType>;
			if (fromItems == null)
				return;

			var toIndex = Items.IndexOf(toPanel?.DataContext as ItemType);
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

		class AllItemsControl : ItemsControl
		{
			public AllItemsControl() { Focusable = false; }
			protected override bool IsItemItsOwnContainerOverride(object item) => false;
		}

		class TabLabel : TextBlock
		{
			public static readonly RoutedEvent ItemMatchEvent = EventManager.RegisterRoutedEvent("ItemMatch", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TabLabel));

			[DepProp]
			public object Item { get { return UIHelper<TabLabel>.GetPropValue<object>(this); } private set { UIHelper<TabLabel>.SetPropValue(this, value); } }

			public event RoutedEventHandler ItemMatch
			{
				add { AddHandler(ItemMatchEvent, value); }
				remove { RemoveHandler(ItemMatchEvent, value); }
			}

			static TabLabel() { UIHelper<TabLabel>.Register(); }

			PropertyChangeNotifier notifier;
			public TabLabel()
			{
				Focusable = false;
				notifier = new PropertyChangeNotifier(this, UIHelper<TabLabel>.GetProperty(a => a.Item), () =>
				{
					if (Item == DataContext)
						Dispatcher.BeginInvoke((Action)(() => RaiseEvent(new RoutedEventArgs(ItemMatchEvent))));
				});
			}
		}

		public void MoveToTop(IEnumerable<ItemType> tabs)
		{
			var found = new HashSet<ItemType>(tabs);
			var indexes = Items.Indexes(item => found.Contains(item)).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				Items.Move(indexes[ctr], ctr);
		}

		class ColumnCountConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				var count = (int)values[0];
				var columns = (int?)values[1];
				var rows = (int?)values[2];

				return columns ?? (rows.HasValue ? 0 : (int)Math.Ceiling(Math.Sqrt(count)));
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		}

		class RowCountConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				var count = (int)values[0];
				var columns = (int?)values[1];
				var rows = (int?)values[2];

				return rows ?? 0;
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		}

		FrameworkElementFactory GetTabLabel(bool tiles)
		{
			var dp = new FrameworkElementFactory(typeof(DockPanel));
			dp.SetValue(DockPanel.DockProperty, Dock.Top);
			dp.SetValue(DockPanel.MarginProperty, new Thickness(0, 0, tiles ? 0 : 2, 1));

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p2 ? \"CadetBlue\" : (p1 ? \"LightBlue\" : \"LightGray\")" };
			multiBinding.Bindings.Add(new Binding());
			multiBinding.Bindings.Add(new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.Active).Name));
			multiBinding.Bindings.Add(new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TopMost).Name) { Source = this });
			dp.SetBinding(DockPanel.BackgroundProperty, multiBinding);

			dp.AddHandler(DockPanel.MouseLeftButtonDownEvent, (MouseButtonEventHandler)((s, e) => TopMost = (s as DockPanel).DataContext as ItemType));
			dp.AddHandler(DockPanel.MouseMoveEvent, (MouseEventHandler)((s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = (((s as DockPanel).DataContext as ItemType).TabsParent as Tabs<ItemType, CommandType>).Items.Where(tab => tab.Active).ToList();
					DragDrop.DoDragDrop(s as DockPanel, new DataObject(typeof(List<ItemType>), active), DragDropEffects.Move);
				}
			}));

			{
				var label = new FrameworkElementFactory(typeof(TabLabel));
				label.SetBinding(TabLabel.TextProperty, new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.TabLabel).Name));
				label.SetValue(TabLabel.VerticalAlignmentProperty, VerticalAlignment.Center);
				label.SetValue(TabLabel.MarginProperty, new Thickness(10, 0, 2, 0));
				dp.AppendChild(label);
			}

			{
				var button = new FrameworkElementFactory(typeof(Button));
				button.SetValue(Button.ContentProperty, "x");
				button.SetValue(Button.BorderThicknessProperty, new Thickness(0));
				button.SetValue(Button.StyleProperty, FindResource(ToolBar.ButtonStyleKey));
				button.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Center);
				button.SetValue(Button.MarginProperty, new Thickness(2, 0, 5, 0));
				button.SetValue(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(128, 32, 32)));
				button.SetValue(Button.FocusableProperty, false);
				button.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Right);
				button.AddHandler(Button.ClickEvent, (RoutedEventHandler)((s, e) =>
				{
					var item = ((s as Button).Parent as DockPanel).DataContext as ItemType;
					if (item.CanClose())
						Remove(item);
				}));
				dp.AppendChild(button);
			}

			return dp;
		}

		void SetupLayout()
		{
			var style = new Style();
			{
				var fullTemplate = new ControlTemplate();
				{
					var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
					dockPanel.SetValue(DockPanel.AllowDropProperty, true);
					dockPanel.AddHandler(DockPanel.DropEvent, (DragEventHandler)((s, e) => OnDrop(e, s as DockPanel)));

					{
						var itemsControl = new FrameworkElementFactory(typeof(AllItemsControl));
						itemsControl.SetValue(DockPanel.DockProperty, Dock.Top);
						itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items).Name) { Source = this });
						{
							var notifierLabel = GetTabLabel(false);
							notifierLabel.SetBinding(UIHelper<TabLabel>.GetProperty(a => a.Item), new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TopMost).Name) { Source = this });
							notifierLabel.AddHandler(TabLabel.ItemMatchEvent, (RoutedEventHandler)((s, e) =>
							{
								var label = s as TabLabel;
								var scrollViewer = UIHelper.FindParent<ScrollViewer>(label);
								if (scrollViewer == null)
									return;
								var left = label.TranslatePoint(new Point(0, 0), scrollViewer).X + TabsScroll;
								var right = label.TranslatePoint(new Point(label.ActualWidth, 0), scrollViewer).X + TabsScroll;
								TabsScroll = Math.Min(left, Math.Max(TabsScroll, right - scrollViewer.ViewportWidth));
								TabsScroll = Math.Max(0, Math.Min(TabsScroll, TabsScrollMax));
							}));
							var itemTemplate = new DataTemplate { VisualTree = notifierLabel };
							itemsControl.SetValue(AllItemsControl.ItemTemplateProperty, itemTemplate);
						}

						{
							var itemsPanel = new ItemsPanelTemplate();
							{
								var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
								stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
								itemsPanel.VisualTree = stackPanel;
							}
							itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, itemsPanel);
							{
								var template = new ControlTemplate();
								{
									var dockPanel2 = new FrameworkElementFactory(typeof(DockPanel));

									{
										var repeatButton = new FrameworkElementFactory(typeof(RepeatButton));
										repeatButton.SetValue(DockPanel.DockProperty, Dock.Left);
										repeatButton.SetValue(RepeatButton.ContentProperty, "<");
										repeatButton.SetValue(RepeatButton.MarginProperty, new Thickness(0, 0, 4, 0));
										repeatButton.SetValue(RepeatButton.PaddingProperty, new Thickness(5, 0, 5, 0));
										repeatButton.AddHandler(RepeatButton.ClickEvent, (RoutedEventHandler)((s, e) => TabsScroll = Math.Max(0, Math.Min(TabsScroll - 50, TabsScrollMax))));
										dockPanel2.AppendChild(repeatButton);
									}
									{
										var repeatButton = new FrameworkElementFactory(typeof(RepeatButton));
										repeatButton.SetValue(DockPanel.DockProperty, Dock.Right);
										repeatButton.SetValue(RepeatButton.ContentProperty, ">");
										repeatButton.SetValue(RepeatButton.MarginProperty, new Thickness(2, 0, 0, 0));
										repeatButton.SetValue(RepeatButton.PaddingProperty, new Thickness(5, 0, 5, 0));
										repeatButton.AddHandler(RepeatButton.ClickEvent, (RoutedEventHandler)((s, e) => TabsScroll = Math.Max(0, Math.Min(TabsScroll + 50, TabsScrollMax))));
										dockPanel2.AppendChild(repeatButton);
									}
									{
										var scrollViewer = new FrameworkElementFactory(typeof(BindableScrollViewer));
										scrollViewer.SetValue(BindableScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
										scrollViewer.SetValue(BindableScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
										scrollViewer.SetBinding(UIHelper<BindableScrollViewer>.GetProperty(a => a.HorizontalPosition), new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TabsScroll).Name) { Source = this, Mode = BindingMode.TwoWay });
										scrollViewer.SetBinding(UIHelper<BindableScrollViewer>.GetProperty(a => a.HorizontalMax), new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TabsScrollMax).Name) { Source = this, Mode = BindingMode.OneWayToSource });
										scrollViewer.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
										dockPanel2.AppendChild(scrollViewer);
									}
									template.VisualTree = dockPanel2;
								}
								itemsControl.SetValue(AllItemsControl.TemplateProperty, template);
							}
						}
						dockPanel.AppendChild(itemsControl);
					}
					{
						var itemsControl = new FrameworkElementFactory(typeof(AllItemsControl));
						itemsControl.SetValue(DockPanel.DockProperty, Dock.Bottom);
						itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items).Name) { Source = this });
						{
							var itemTemplate = new DataTemplate();
							{
								var contentControl = new FrameworkElementFactory(typeof(ContentControl));
								contentControl.SetBinding(ContentControl.ContentProperty, new Binding());
								var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p1" };
								multiBinding.Bindings.Add(new Binding());
								multiBinding.Bindings.Add(new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TopMost).Name) { Source = this });
								contentControl.SetBinding(ContentControl.VisibilityProperty, multiBinding);
								contentControl.SetValue(ContentControl.FocusVisualStyleProperty, null);
								itemTemplate.VisualTree = contentControl;
							}
							itemsControl.SetValue(AllItemsControl.ItemTemplateProperty, itemTemplate);
						}
						itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, new ItemsPanelTemplate { VisualTree = new FrameworkElementFactory(typeof(Grid)) });
						dockPanel.AppendChild(itemsControl);
					}
					dockPanel.SetValue(Window.BackgroundProperty, Brushes.Gray);

					fullTemplate.VisualTree = dockPanel;

				}

				style.Setters.Add(new Setter(AllItemsControl.TemplateProperty, fullTemplate));
			}

			{
				var gridTemplate = new ControlTemplate();

				{
					var itemsControl = new FrameworkElementFactory(typeof(AllItemsControl));
					itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items).Name) { Source = this });
					{
						var itemTemplate = new DataTemplate();
						{
							var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
							dockPanel.SetValue(DockPanel.AllowDropProperty, true);
							dockPanel.AddHandler(DockPanel.DropEvent, (DragEventHandler)((s, e) => OnDrop(e, s as DockPanel)));
							dockPanel.SetValue(DockPanel.MarginProperty, new Thickness(0, 0, 2, 2));
							dockPanel.AppendChild(GetTabLabel(true));
							{
								var contentControl = new FrameworkElementFactory(typeof(ContentControl));
								contentControl.SetValue(DockPanel.DockProperty, Dock.Bottom);
								contentControl.SetValue(ContentControl.ContentProperty, new Binding());
								contentControl.SetValue(ContentControl.FocusVisualStyleProperty, null);
								dockPanel.AppendChild(contentControl);
							}
							itemTemplate.VisualTree = dockPanel;
						}
						itemsControl.SetValue(AllItemsControl.ItemTemplateProperty, itemTemplate);
					}
					{
						var itemsPanel = new ItemsPanelTemplate();
						{
							var uniformGrid = new FrameworkElementFactory(typeof(UniformGrid));

							var columnsBinding = new MultiBinding { Converter = new ColumnCountConverter() };
							columnsBinding.Bindings.Add(new Binding($"{UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items)}.Count") { Source = this });
							columnsBinding.Bindings.Add(new Binding(nameof(Columns)) { Source = this });
							columnsBinding.Bindings.Add(new Binding(nameof(Rows)) { Source = this });
							uniformGrid.SetBinding(UniformGrid.ColumnsProperty, columnsBinding);

							var rowsBinding = new MultiBinding { Converter = new RowCountConverter() };
							rowsBinding.Bindings.Add(new Binding($"{UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items)}.Count") { Source = this });
							rowsBinding.Bindings.Add(new Binding(nameof(Columns)) { Source = this });
							rowsBinding.Bindings.Add(new Binding(nameof(Rows)) { Source = this });
							uniformGrid.SetBinding(UniformGrid.RowsProperty, rowsBinding);

							uniformGrid.SetValue(UniformGrid.MarginProperty, new Thickness(0, 0, -2, -2));
							itemsPanel.VisualTree = uniformGrid;
						}
						itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, itemsPanel);
					}
					gridTemplate.VisualTree = itemsControl;
					itemsControl.SetValue(Window.BackgroundProperty, Brushes.Gray);
				}

				var dataTrigger = new DataTrigger { Binding = new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Layout).Name) { Source = this }, Value = TabsLayout.Grid };
				dataTrigger.Setters.Add(new Setter(AllItemsControl.TemplateProperty, gridTemplate));
				style.Triggers.Add(dataTrigger);
			}
			Style = style;
		}

		internal void NotifyActiveChanged() => TabsChanged?.Invoke();
	}
}
