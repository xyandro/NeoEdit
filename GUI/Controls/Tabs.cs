using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.GUI.Controls
{
	public class Tabs<ItemType, CommandType> : UserControl where ItemType : TabsControl<ItemType, CommandType>
	{
		[DepProp]
		public ObservableCollection<ItemType> Items { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<ObservableCollection<ItemType>>(this); } private set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemType TopMost { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<ItemType>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<Tabs<ItemType, CommandType>>.GetPropValue<bool>(this); } set { UIHelper<Tabs<ItemType, CommandType>>.SetPropValue(this, value); } }
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
			Tiles = false;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			VerticalAlignment = VerticalAlignment.Stretch;
			Drop += (s, e) => OnDrop(e, null);
		}

		public void CreateTab(ItemType item)
		{
			if ((!item.Empty()) && (TopMost != null) && (TopMost.Empty()))
				Items[Items.IndexOf(TopMost)] = item;
			else
				Items.Add(item);
			TopMost = item;
		}

		public void ShowActiveTabsDialog()
		{
			ActiveTabsDialog<ItemType, CommandType>.Run(this);
			UpdateTopMost();
		}

		void ItemsChanged()
		{
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

		public int GetIndex(ItemType item)
		{
			var index = Items.IndexOf(item);
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

		bool controlDown => (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None;
		bool shiftDown => (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None;

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			if (controlDown)
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

		void OnDrop(DragEventArgs e, TabLabel toLabel)
		{
			var fromLabel = e.Data.GetData(typeof(TabLabel)) as TabLabel;
			if ((fromLabel == null) || (toLabel == fromLabel))
				return;

			var toData = toLabel?.DataContext as ItemType;
			var fromData = fromLabel.DataContext as ItemType;

			var fromTabs = fromData.TabsParent;
			if ((fromTabs == null) || ((fromTabs == this) && (toLabel == null)))
				return;

			var fromIndex = fromTabs.Items.IndexOf(fromData);
			var toIndex = toLabel == null ? Items.Count : Items.IndexOf(toData);

			fromTabs.Items.RemoveAt(fromIndex);
			Items.Insert(toIndex, fromData);

			TopMost = fromData;

			e.Handled = true;
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

		class ColumnCountConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var count = value as int?;
				return count.HasValue ? (int)Math.Ceiling(Math.Sqrt(count.Value)) : 1;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		}

		FrameworkElementFactory GetTabLabel(bool tiles)
		{
			var label = new FrameworkElementFactory(typeof(TabLabel));
			if (tiles)
				label.SetValue(DockPanel.DockProperty, Dock.Top);
			label.SetBinding(TabLabel.TextProperty, new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.TabLabel).Name));
			label.SetValue(TabLabel.PaddingProperty, new Thickness(10, 2, 10, 2));
			label.SetValue(TabLabel.MarginProperty, new Thickness(0, 0, tiles ? 0 : 2, 1));

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "[0] == [2] ? \"CadetBlue\" : ([1] ? \"LightBlue\" : \"LightGray\")" };
			multiBinding.Bindings.Add(new Binding());
			multiBinding.Bindings.Add(new Binding(UIHelper<TabsControl<ItemType, CommandType>>.GetProperty(a => a.Active).Name));
			multiBinding.Bindings.Add(new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.TopMost).Name) { Source = this });
			label.SetBinding(TabLabel.BackgroundProperty, multiBinding);

			label.SetValue(TabLabel.AllowDropProperty, true);
			label.AddHandler(TabLabel.MouseLeftButtonDownEvent, (MouseButtonEventHandler)((s, e) => TopMost = (s as TabLabel).DataContext as ItemType));
			label.AddHandler(TabLabel.MouseMoveEvent, (MouseEventHandler)((s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(s as TabLabel, new DataObject(typeof(TabLabel), s as TabLabel), DragDropEffects.Move);
			}));

			label.AddHandler(TabLabel.DropEvent, (DragEventHandler)((s, e) => OnDrop(e, s as TabLabel)));

			return label;
		}

		void SetupLayout()
		{
			var style = new Style();
			{
				var tabsTemplate = new ControlTemplate();
				{
					var dockPanel = new FrameworkElementFactory(typeof(DockPanel));

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
								var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "[0] == [1]" };
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

					tabsTemplate.VisualTree = dockPanel;

				}

				style.Setters.Add(new Setter(AllItemsControl.TemplateProperty, tabsTemplate));
			}

			{
				var tilesTemplate = new ControlTemplate();
				{
					var itemsControl = new FrameworkElementFactory(typeof(AllItemsControl));
					itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items).Name) { Source = this });
					{
						var itemTemplate = new DataTemplate();
						{
							var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
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
							uniformGrid.SetBinding(UniformGrid.ColumnsProperty, new Binding($"{UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Items)}.Count") { Source = this, Converter = new ColumnCountConverter() });
							uniformGrid.SetValue(UniformGrid.MarginProperty, new Thickness(0, 0, -2, -2));
							itemsPanel.VisualTree = uniformGrid;
						}
						itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, itemsPanel);
					}
					tilesTemplate.VisualTree = itemsControl;
					itemsControl.SetValue(Window.BackgroundProperty, Brushes.Gray);
				}

				var dataTrigger = new DataTrigger { Binding = new Binding(UIHelper<Tabs<ItemType, CommandType>>.GetProperty(a => a.Tiles).Name) { Source = this }, Value = true };
				dataTrigger.Setters.Add(new Setter(AllItemsControl.TemplateProperty, tilesTemplate));
				style.Triggers.Add(dataTrigger);
			}
			Style = style;
		}
	}
}
