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

namespace NeoEdit.GUI.Controls
{
	public class Tabs<ItemType> : UserControl where ItemType : FrameworkElement
	{
		public enum ViewType
		{
			Tabs,
			Tiles,
		}

		class ItemData : DependencyObject
		{
			[DepProp]
			public ItemType Item { get { return UIHelper<ItemData>.GetPropValue<ItemType>(this); } set { UIHelper<ItemData>.SetPropValue(this, value); } }
			[DepProp]
			public int ItemOrder { get { return UIHelper<ItemData>.GetPropValue<int>(this); } set { UIHelper<ItemData>.SetPropValue(this, value); } }
			[DepProp]
			public bool Active { get { return UIHelper<ItemData>.GetPropValue<bool>(this); } set { UIHelper<ItemData>.SetPropValue(this, value); } }

			static ItemData() { UIHelper<ItemData>.Register(); }
		}

		[DepProp]
		public ObservableCollection<ItemType> Items { get { return UIHelper<Tabs<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ItemType> Active { get { return UIHelper<Tabs<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemType TopMost { get { return UIHelper<Tabs<ItemType>>.GetPropValue<ItemType>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ViewType View { get { return UIHelper<Tabs<ItemType>>.GetPropValue<ViewType>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabelPath { get { return UIHelper<Tabs<ItemType>>.GetPropValue<string>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<ItemData> Data { get { return UIHelper<Tabs<ItemType>>.GetPropValue<ObservableCollection<ItemData>>(this); } set { UIHelper<Tabs<ItemType>>.SetPropValue(this, value); } }

		static Tabs()
		{
			UIHelper<Tabs<ItemType>>.Register();
			UIHelper<Tabs<ItemType>>.AddCallback(a => a.TabLabelPath, (obj, o, n) => obj.SetupLayout());
			UIHelper<Tabs<ItemType>>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs<ItemType>>.AddObservableCallback(a => a.Active, (obj, s, e) => obj.ActiveChanged());
			UIHelper<Tabs<ItemType>>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs<ItemType>>.AddCoerce(a => a.TopMost, (obj, value) => (value == null) || ((obj.Items != null) && (obj.Items.Contains(value))) ? value : null);
		}

		int itemOrder = 0;
		public Tabs()
		{
			SetupLayout();

			Data = new ObservableCollection<ItemData>();
			Items = new ObservableCollection<ItemType>();
			Active = new ObservableCollection<ItemType>();

			View = ViewType.Tabs;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			Drop += (s, e) => OnDrop(e, null);
		}

		void ItemsChanged()
		{
			if (Items == null)
				Data = new ObservableCollection<ItemData>();
			else
				Data = new ObservableCollection<ItemData>(Items.Distinct().Select(item => Data.FirstOrDefault(itemData => itemData.Item == item) ?? new ItemData { Item = item, ItemOrder = ++itemOrder }));

			foreach (var itemData in Data)
				EnhancedFocusManager.SetIsEnhancedFocusScope(itemData.Item, true);

			UpdateActive();
		}

		void ActiveChanged()
		{
			foreach (var data in Data)
			{
				var newValue = Active.Contains(data.Item);
				if (data.Active == newValue)
					continue;
				data.Active = newValue;
			}
		}

		void TopMostChanged()
		{
			var item = Data.FirstOrDefault(itemData => itemData.Item == TopMost);
			if (item == null)
			{
				UpdateTopMost();
				return;
			}

			if (!shiftDown)
				foreach (var itemData in Data)
					itemData.Active = false;

			TopMost = item.Item;
			item.Active = true;
			if (!controlDown)
				item.ItemOrder = ++itemOrder;

			Dispatcher.BeginInvoke((Action)(() =>
			{
				UpdateLayout();
				TopMost.Focus();
			}));
		}

		void UpdateActive()
		{
			Active = new ObservableCollection<ItemType>(Data.Where(itemData => itemData.Active).Select(itemData => itemData.Item));
			UpdateTopMost();
		}

		void UpdateTopMost()
		{
			var item = Data.Where(itemData => itemData.Active).OrderByDescending(itemData => itemData.ItemOrder).FirstOrDefault();
			if (item == null)
				item = Data.OrderByDescending(itemData => itemData.ItemOrder).FirstOrDefault();
			if (item == null)
			{
				TopMost = null;
				return;
			}

			item.Active = true;
			TopMost = item.Item;
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

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
				TopMostChanged();
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
			var ordering = Data.OrderBy(itemData => itemData.ItemOrder).Select(itemData => itemData.Item).ToList();
			var current = ordering.IndexOf(TopMost);
			if (current == -1)
				return;
			--current;
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

		void OnDrop(DragEventArgs e, Label toLabel)
		{
			e.Handled = true;

			var fromLabel = e.Data.GetData(typeof(Label)) as Label;
			if ((fromLabel == null) || (toLabel == fromLabel))
				return;

			var toData = toLabel == null ? null : toLabel.DataContext as ItemData;
			var fromData = fromLabel.DataContext as ItemData;

			var fromTabs = UIHelper.FindParent<Tabs<ItemType>>(fromData.Item);
			if ((fromTabs == null) || ((fromTabs == this) && (toLabel == null)))
				return;

			var fromIndex = fromTabs.Data.IndexOf(fromData);
			var toIndex = toLabel == null ? Items.Count : Data.IndexOf(toData);

			fromTabs.Data.RemoveAt(fromIndex);
			Data.Insert(toIndex, fromData);
			Items = new ObservableCollection<ItemType>(Data.Select(itemData => itemData.Item));
			if (fromTabs != this)
				fromTabs.Items = new ObservableCollection<ItemType>(fromTabs.Data.Select(itemData => itemData.Item));
		}

		class AllItemsControl : ItemsControl
		{
			public AllItemsControl() { Focusable = false; }
			protected override bool IsItemItsOwnContainerOverride(object item) { return false; }
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

		FrameworkElementFactory GetLabel(ViewType view)
		{
			var label = new FrameworkElementFactory(typeof(Label));
			if (view == ViewType.Tiles)
				label.SetValue(DockPanel.DockProperty, Dock.Top);
			label.SetBinding(Label.ContentProperty, new Binding("Item." + TabLabelPath));
			label.SetValue(Label.PaddingProperty, new Thickness(10, 2, 10, 2));
			label.SetValue(Label.MarginProperty, new Thickness(0, 0, view == ViewType.Tabs ? 2 : 0, 1));

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "[0] == [2] ? 'CadetBlue' : ([1] ? 'LightBlue' : 'LightGray')" };
			multiBinding.Bindings.Add(new Binding("Item"));
			multiBinding.Bindings.Add(new Binding("Active"));
			multiBinding.Bindings.Add(new Binding("TopMost") { Source = this });
			label.SetBinding(Label.BackgroundProperty, multiBinding);

			label.SetValue(Label.AllowDropProperty, true);
			label.AddHandler(Label.MouseLeftButtonDownEvent, (MouseButtonEventHandler)((s, e) => TopMost = ((s as Label).DataContext as ItemData).Item));
			label.AddHandler(Label.MouseMoveEvent, (MouseEventHandler)((s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(s as Label, new DataObject(typeof(Label), s as Label), DragDropEffects.Move);
			}));

			label.AddHandler(Label.DropEvent, (DragEventHandler)((s, e) => OnDrop(e, s as Label)));

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
						itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding("Data") { Source = this });
						itemsControl.SetValue(AllItemsControl.ItemTemplateProperty, new DataTemplate { VisualTree = GetLabel(ViewType.Tabs) });

						{
							var itemsPanel = new ItemsPanelTemplate();
							{
								var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
								stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
								itemsPanel.VisualTree = stackPanel;
							}
							itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, itemsPanel);
						}
						dockPanel.AppendChild(itemsControl);
					}
					{
						var itemsControl = new FrameworkElementFactory(typeof(AllItemsControl));
						itemsControl.SetValue(DockPanel.DockProperty, Dock.Bottom);
						itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding("Data") { Source = this });
						{
							var itemTemplate = new DataTemplate();
							{
								var contentControl = new FrameworkElementFactory(typeof(ContentControl));
								contentControl.SetBinding(ContentControl.ContentProperty, new Binding("Item"));
								var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "[0] == [1] ? True : False" };
								multiBinding.Bindings.Add(new Binding("Item"));
								multiBinding.Bindings.Add(new Binding("TopMost") { Source = this });
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
					itemsControl.SetBinding(AllItemsControl.ItemsSourceProperty, new Binding("Data") { Source = this });
					{
						var itemTemplate = new DataTemplate();
						{
							var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
							dockPanel.SetValue(DockPanel.MarginProperty, new Thickness(0, 0, 2, 2));
							dockPanel.AppendChild(GetLabel(ViewType.Tiles));
							{
								var contentControl = new FrameworkElementFactory(typeof(ContentControl));
								contentControl.SetValue(DockPanel.DockProperty, Dock.Bottom);
								contentControl.SetBinding(ContentControl.ContentProperty, new Binding("Item"));
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
							uniformGrid.SetBinding(UniformGrid.ColumnsProperty, new Binding("Items.Count") { Source = this, Converter = new ColumnCountConverter() });
							uniformGrid.SetValue(UniformGrid.MarginProperty, new Thickness(0, 0, -2, -2));
							itemsPanel.VisualTree = uniformGrid;
						}
						itemsControl.SetValue(AllItemsControl.ItemsPanelProperty, itemsPanel);
					}
					tilesTemplate.VisualTree = itemsControl;
					itemsControl.SetValue(Window.BackgroundProperty, Brushes.Gray);
				}

				var dataTrigger = new DataTrigger { Binding = new Binding("View") { Source = this }, Value = ViewType.Tiles };
				dataTrigger.Setters.Add(new Setter(AllItemsControl.TemplateProperty, tilesTemplate));
				style.Triggers.Add(dataTrigger);
			}
			Style = style;
		}
	}
}
