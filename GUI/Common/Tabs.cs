using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NeoEdit.GUI.Common
{
	public class Tabs<ItemType> : Grid where ItemType : FrameworkElement
	{
		public enum ViewType
		{
			Tabs,
			Tiles,
		}

		[DepProp]
		public ObservableCollection<ItemType> Items { get { return UIHelper<Tabs<ItemType>>.GetPropValue(() => this.Items); } set { UIHelper<Tabs<ItemType>>.SetPropValue(() => this.Items, value); } }
		[DepProp]
		public ItemType Active { get { return UIHelper<Tabs<ItemType>>.GetPropValue(() => this.Active); } set { UIHelper<Tabs<ItemType>>.SetPropValue(() => this.Active, value); } }
		[DepProp]
		public ViewType View { get { return UIHelper<Tabs<ItemType>>.GetPropValue(() => this.View); } set { UIHelper<Tabs<ItemType>>.SetPropValue(() => this.View, value); } }

		public Func<ItemType, Label> GetLabel { get; set; }

		Dictionary<ItemType, Label> labels;

		static Tabs()
		{
			UIHelper<Tabs<ItemType>>.Register();
			UIHelper<Tabs<ItemType>>.AddCallback(a => a.View, (obj, o, n) => obj.Layout());
			UIHelper<Tabs<ItemType>>.AddCallback(a => a.Active, (obj, o, n) => obj.ActiveChanged());
			UIHelper<Tabs<ItemType>>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.SetActive(e));
			UIHelper<Tabs<ItemType>>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.Layout());
			UIHelper<Tabs<ItemType>>.AddCoerce(a => a.Active, (obj, value) => (value == null) || ((obj.Items != null) && (obj.Items.Contains(value))) ? value : null);
		}

		void ActiveChanged()
		{
			Layout();
			if (Active != null)
				Active.Focus();
		}

		public Tabs()
		{
			Items = new ObservableCollection<ItemType>();
			View = ViewType.Tabs;
			GetLabel = DefaultGetLabel;
			Background = Brushes.Gray;
			Focusable = true;
			AllowDrop = true;
			DragOver += (s, e) => DoDragOver(s, e, null);
		}

		protected virtual Label DefaultGetLabel(ItemType item)
		{
			return new Label { Content = "Title" };
		}

		internal bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			e.Handled = true;
			switch (e.Key)
			{
				case Key.PageUp: if (controlDown) MovePrev(); else e.Handled = false; break;
				case Key.PageDown: if (controlDown) MoveNext(); else e.Handled = false; break;
				default: e.Handled = false; break;
			}
		}

		public void MovePrev()
		{
			var index = Items.IndexOf(Active) - 1;
			if (index < 0)
				index = Items.Count - 1;
			if (index >= 0)
				Active = Items[index];
		}

		public void MoveNext()
		{
			var index = Items.IndexOf(Active) + 1;
			if (index >= Items.Count)
				index = 0;
			if (index < Items.Count)
				Active = Items[index];
		}

		void SetActive(NotifyCollectionChangedEventArgs e)
		{
			if (Items == null)
			{
				Active = null;
				return;
			}

			if (e == null)
			{
				Active = Items.FirstOrDefault();
				return;
			}

			if (Active == null)
			{
				Active = Items.FirstOrDefault();
				return;
			}

			if (e.Action == NotifyCollectionChangedAction.Move)
				return;
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				Active = null;
				return;
			}

			if (e.OldItems == null)
				return;
			int index = e.OldItems.IndexOf(Active);
			if (index == -1)
				return;

			index += e.OldStartingIndex;
			index = Math.Min(index, Items.Count - 1);
			if (index < 0)
				Active = null;
			else
				Active = Items[index];
		}

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnPreviewGotKeyboardFocus(e);
			var focus = e.NewFocus as DependencyObject;
			foreach (var item in Items)
				if (item.IsAncestorOf(focus))
					Active = item;
		}

		Label GetMovableLabel(ItemType item, bool tiled)
		{
			var label = labels[item] = GetLabel(item);
			label.Margin = new Thickness(0, 0, tiled ? 0 : 2, 1);
			label.Background = item == Active ? Brushes.LightBlue : Brushes.LightGray;
			label.AllowDrop = true;
			label.MouseLeftButtonDown += (s, e) => Active = item;
			label.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(label, new DataObject(typeof(ItemType), item), DragDropEffects.Move);
			};

			label.DragOver += (s, e) => DoDragOver(s, e, item);

			return label;
		}

		void DoDragOver(object sender, DragEventArgs e, ItemType toItem)
		{
			e.Handled = true;

			var fromItem = e.Data.GetData(typeof(ItemType)) as ItemType;
			if (toItem == fromItem)
				return;

			var fromTabs = UIHelper.FindParent<Tabs<ItemType>>(fromItem);
			if ((fromTabs == null) || ((fromTabs == this) && (toItem == null)))
				return;

			var fromIndex = fromTabs.Items.IndexOf(fromItem);
			var toIndex = toItem == null ? Items.Count : Items.IndexOf(toItem);

			if (fromTabs == this)
			{
				// Only move tabs when they're in a place that won't immediately move back
				var toLabel = labels[toItem];
				var fromLabel = fromTabs.labels[fromItem];

				var pos = e.GetPosition(toLabel);
				if (fromIndex < toIndex)
				{
					if (pos.X < toLabel.ActualWidth - fromLabel.ActualWidth)
						return;
				}
				else
				{
					if (pos.X > fromLabel.ActualWidth)
						return;
				}
			}

			fromTabs.Items.RemoveAt(fromIndex);
			Items.Insert(toIndex, fromItem);
			Active = fromItem;
		}

		void Layout()
		{
			Children.Clear();
			RowDefinitions.Clear();
			ColumnDefinitions.Clear();
			labels = new Dictionary<ItemType, Label>();

			if ((Items == null) || (Items.Count == 0))
				return;

			if (View == ViewType.Tiles)
				LayoutTiles();
			else
				LayoutTabs();
		}

		void LayoutTiles()
		{
			const double border = 2;

			var columns = (int)Math.Ceiling(Math.Sqrt(Items.Count));
			var rows = (Items.Count + columns - 1) / columns;

			for (var ctr = 0; ctr < columns; ++ctr)
			{
				if (ctr != 0)
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(border) });
				ColumnDefinitions.Add(new ColumnDefinition());
			}

			for (var ctr = 0; ctr < rows; ++ctr)
			{
				if (ctr != 0)
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(border) });
				RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
				RowDefinitions.Add(new RowDefinition());
			}

			int count = 0;
			foreach (var item in Items)
			{
				var column = count % columns * 2;
				var row = count / columns * 3;

				var label = GetMovableLabel(item, true);
				SetColumn(label, column);
				SetRow(label, row);
				Children.Add(label);

				SetColumn(item, column);
				SetRow(item, row + 1);
				Children.Add(item);

				++count;
			}
		}

		void LayoutTabs()
		{
			ColumnDefinitions.Add(new ColumnDefinition());
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
			RowDefinitions.Add(new RowDefinition());

			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var item in Items)
				stackPanel.Children.Add(GetMovableLabel(item, false));
			SetRow(stackPanel, 0);
			Children.Add(stackPanel);

			SetRow(Active, 1);
			SetColumn(Active, 0);
			Children.Add(Active);
		}
	}
}
