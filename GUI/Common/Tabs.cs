using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NeoEdit.GUI.Common
{
	public class Tabs<ItemType> : Grid where ItemType : UIElement
	{
		public enum ViewType
		{
			Tabs,
			Tiles,
		}

		[DepProp]
		public ObservableCollection<ItemType> Items { get { return uiHelper.GetPropValue<ObservableCollection<ItemType>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ItemType Active { get { return uiHelper.GetPropValue<ItemType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ViewType View { get { return uiHelper.GetPropValue<ViewType>(); } set { uiHelper.SetPropValue(value); } }

		public Func<ItemType, Label> GetLabel { get; set; }

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

		readonly UIHelper<Tabs<ItemType>> uiHelper;
		public Tabs()
		{
			uiHelper = new UIHelper<Tabs<ItemType>>(this);
			Items = new ObservableCollection<ItemType>();
			View = ViewType.Tabs;
			GetLabel = DefaultGetLabel;
			Background = Brushes.Gray;
			Focusable = true;
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
			var label = GetLabel(item);
			label.Margin = new Thickness(0, 0, tiled ? 0 : 2, 1);
			label.Background = item == Active ? Brushes.LightBlue : Brushes.LightGray;
			label.AllowDrop = true;
			label.MouseLeftButtonDown += (s, e) => Active = item;
			label.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
					DragDrop.DoDragDrop(label, new DataObject(typeof(ItemType), item), DragDropEffects.Move);
			};

			label.Drop += (s, e) =>
			{
				var item2 = e.Data.GetData(typeof(ItemType)) as ItemType;
				var fromIndex = Items.IndexOf(item2);
				var toIndex = Items.IndexOf(item);
				if ((fromIndex == toIndex) || (fromIndex == -1) || (toIndex == -1))
					return;

				Items.RemoveAt(fromIndex);
				Items.Insert(toIndex, item2);
				Active = item2;
			};

			return label;
		}

		void Layout()
		{
			Children.Clear();
			RowDefinitions.Clear();
			ColumnDefinitions.Clear();

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
