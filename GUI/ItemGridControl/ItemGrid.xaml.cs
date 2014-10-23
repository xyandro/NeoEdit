using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.ItemGridControl
{
	public partial class ItemGrid : Grid
	{
		RoutedEventHandler accept = (s, e) => { };
		public event RoutedEventHandler Accept { add { accept += value; } remove { accept -= value; } }

		public static DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(IEnumerable<DependencyObject>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnItemsCollectionChanged(e.OldValue)));
		public static DependencyProperty SortedItemsProperty = DependencyProperty.Register("SortedItems", typeof(ListCollectionView), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnSortedItemsCollectionChanged(e.OldValue)));
		public static DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ObservableHashSet<ItemGridColumn>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnColumnsCollectionChanged(e.OldValue)));
		public static DependencyProperty SortColumnProperty = DependencyProperty.Register("SortColumn", typeof(ItemGridColumn), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnSortColumnChanged()));
		public static DependencyProperty SortAscendingProperty = DependencyProperty.Register("SortAscending", typeof(bool), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnSortAscendingChanged()));
		public static DependencyProperty FocusedProperty = DependencyProperty.Register("Focused", typeof(DependencyObject), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnFocusedChanged()));
		public static DependencyProperty FocusColumnsProperty = DependencyProperty.Register("FocusColumns", typeof(bool), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnFocusColumnsChanged()));
		public static DependencyProperty FocusedColumnProperty = DependencyProperty.Register("FocusedColumn", typeof(ItemGridColumn), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnFocusedColumnChanged()));
		public static DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(ObservableCollection<DependencyObject>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).OnSelectedCollectionChanged(e.OldValue)));
		public static DependencyProperty TextInputColumnProperty = DependencyProperty.Register("TextInputColumn", typeof(ItemGridColumn), typeof(ItemGrid));
		public static DependencyProperty TextInputDelayProperty = DependencyProperty.Register("TextInputDelay", typeof(int), typeof(ItemGrid), new PropertyMetadata(500));

		public IEnumerable<DependencyObject> Items { get { return (IEnumerable<DependencyObject>)GetValue(ItemsProperty) ?? new List<DependencyObject>(); } set { SetValue(ItemsProperty, value); } }
		public ListCollectionView SortedItems { get { return (ListCollectionView)GetValue(SortedItemsProperty); } set { SetValue(SortedItemsProperty, value); } }
		public ObservableHashSet<ItemGridColumn> Columns { get { return (ObservableHashSet<ItemGridColumn>)GetValue(ColumnsProperty); } set { SetValue(ColumnsProperty, value); } }
		public ItemGridColumn SortColumn { get { return (ItemGridColumn)GetValue(SortColumnProperty); } set { SetValue(SortColumnProperty, value); } }
		public bool SortAscending { get { return (bool)GetValue(SortAscendingProperty); } set { SetValue(SortAscendingProperty, value); } }
		public DependencyObject Focused { get { return (DependencyObject)GetValue(FocusedProperty); } set { SetValue(FocusedProperty, value); } }
		public bool FocusColumns { get { return (bool)GetValue(FocusColumnsProperty); } set { SetValue(FocusColumnsProperty, value); } }
		public ItemGridColumn FocusedColumn { get { return (ItemGridColumn)GetValue(FocusedColumnProperty); } set { SetValue(FocusedColumnProperty, value); } }
		public ObservableCollection<DependencyObject> Selected { get { return (ObservableCollection<DependencyObject>)GetValue(SelectedProperty); } set { SetValue(SelectedProperty, value); } }
		public ItemGridColumn TextInputColumn { get { return (ItemGridColumn)GetValue(TextInputColumnProperty); } set { SetValue(TextInputColumnProperty, value); } }
		public int TextInputDelay { get { return (int)GetValue(TextInputDelayProperty); } set { SetValue(TextInputDelayProperty, value); } }

		public int? FocusedIndex
		{
			get
			{
				var idx = SortedItems.IndexOf(Focused);
				if (idx == -1)
					return null;
				return idx;
			}
		}
		public int FocusedColumnIndex { get { return Columns.IndexOf(FocusedColumn); } }

		int lastFocusedIndex = -1;
		void SetLastFocusedIndex()
		{
			if (Focused == null)
				return;
			var idx = SortedItems.IndexOf(Focused);
			if (idx == -1)
				return;
			lastFocusedIndex = idx;
		}

		public ItemGridColumn this[string column] { get { return Columns.FirstOrDefault(col => col.Header == column); } }
		const double headerHeight = 21.96;
		const double rowHeight = 19.96;

		RunOnceTimer drawTimer, sortTimer;

		public ItemGrid()
		{
			InitializeComponent();

			drawTimer = new RunOnceTimer(() => Redraw());
			sortTimer = new RunOnceTimer(() => Sort());
			drawTimer.AddDependency(sortTimer);

			Columns = new ObservableHashSet<ItemGridColumn>();
			Selected = new ObservableCollection<DependencyObject>();

			DependencyPropertyDescriptor.FromProperty(ScrollViewer.ViewportHeightProperty, typeof(ScrollViewer)).AddValueChanged(scroller, (o, e) => drawTimer.Start());
			scroll.ValueChanged += (s, e) => drawTimer.Start();
			PreviewMouseWheel += (s, e) => scroll.Value -= e.Delta / 10;
		}

		void OnItemsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= OnItemsChanged;

			var newValue = Items as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += OnItemsChanged;

			OnItemsChanged(null, null);
		}

		void OnSortedItemsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= OnSortedItemsChanged;

			var newValue = SortedItems as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += OnSortedItemsChanged;

			OnSortedItemsChanged(null, null);
		}

		void OnColumnsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= OnColumnsChanged;

			var newValue = Columns as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += OnColumnsChanged;

			OnColumnsChanged(null, null);
		}

		void OnSelectedCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= OnSelectedChanged;

			var newValue = Selected as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += OnSelectedChanged;

			OnSelectedChanged(null, null);
		}

		void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			sortTimer.Start();
		}

		void OnSortedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnFocusedChanged(false);
			OnSelectedChanged(null, null);
			drawTimer.Start();
			lastTextInputTime = null;
		}

		void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnSortColumnChanged();
			OnFocusedColumnChanged();
			drawTimer.Start();
		}

		void OnSelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Selected.Where(item => !SortedItems.Contains(item)).ToList().ForEach(item => Selected.Remove(item));
			drawTimer.Start();
		}

		void OnSortColumnChanged()
		{
			if (Columns.Count == 0)
				return;

			if (!Columns.Contains(SortColumn))
			{
				SortColumn = Columns.First();
				return; // Recursive
			}

			SortAscending = SortColumn.SortAscending;
			OnSortAscendingChanged();
		}

		void OnSortAscendingChanged()
		{
			sortTimer.Start();
		}

		void OnFocusedChanged(bool show = true)
		{
			if ((Focused != null) && (!SortedItems.Contains(Focused)))
			{
				Focused = null;
				return; // Recursive
			}

			SetLastFocusedIndex();
			if (show)
			{
				ShowFocus();
				drawTimer.Start();
			}
		}

		void OnFocusColumnsChanged()
		{
			drawTimer.Start();
		}

		void OnFocusedColumnChanged()
		{
			if (Columns.Count == 0)
				return;

			if (!Columns.Contains(FocusedColumn))
			{
				FocusedColumn = Columns.First();
				return; // Recursive
			}

			drawTimer.Start();
		}

		class Comparer : IComparer
		{
			readonly DependencyProperty prop;
			readonly bool ascending, numericStrings;
			public Comparer(DependencyProperty _prop, bool _ascending, bool _numericStrings)
			{
				prop = _prop;
				ascending = _ascending;
				numericStrings = _numericStrings;
			}

			string SortStr(string str)
			{
				return Regex.Replace(str, @"\d+", match => new string('0', Math.Max(0, 20 - match.Value.Length)) + match.Value);
			}

			public int Compare(object o1, object o2)
			{
				var val1 = (o1 as DependencyObject).GetValue(prop);
				var val2 = (o2 as DependencyObject).GetValue(prop);

				if ((val1 == null) && (val2 == null))
					return 0;
				if (val1 == null)
					return 1;
				if (val2 == null)
					return -1;

				if (val1.GetType() != val2.GetType())
					throw new Exception("Different types");

				var mult = ascending ? 1 : -1;

				if ((val1.GetType() == typeof(string)) && (numericStrings))
					return mult * SortStr((string)val1).CompareTo(SortStr((string)val2));
				if (val1 is IComparable)
					return mult * (val1 as IComparable).CompareTo(val2);

				throw new Exception("Unable to compare");
			}
		}

		void Sort()
		{
			if ((Columns.Contains(SortColumn)) && (SortedItems != null))
			{
				SortedItems.CustomSort = new Comparer(SortColumn.DepProp, SortAscending, (SortedItems.Count <= 500) && (SortColumn.NumericStrings));
				SetLastFocusedIndex();
				drawTimer.Start();
			}
		}

		bool showFocus = false;
		public void ShowFocus()
		{
			showFocus = true;
			drawTimer.Start();
		}

		public void ResetScroll()
		{
			scroll.Value = 0;
			lastFocusedIndex = 0;
		}

		public void SyncItems(IEnumerable<DependencyObject> items, DependencyProperty prop)
		{
			var scrollPos = scroll.Value;
			IComparable focused = null;
			if (Focused != null)
				focused = Focused.GetValue(prop) as IComparable;
			var selected = Selected.Select(item => item.GetValue(prop) as IComparable).ToList();

			Items = new ObservableCollection<DependencyObject>(items);
			var itemsByKey = Items.ToDictionary(item => item.GetValue(prop), item => item);
			if ((focused != null) && (itemsByKey.ContainsKey(focused)))
				Focused = itemsByKey[focused];
			Selected = new ObservableCollection<DependencyObject>(itemsByKey.Where(item => selected.Contains(item.Key)).Select(item => item.Value));

			scroll.Value = scrollPos;
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altDown { get { return (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None; } }
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		WeakReference<DependencyObject> lastShiftSel;
		void MoveFocus(int offset, bool relative, bool select = false)
		{
			if (relative)
				if (!FocusedIndex.HasValue)
					offset = lastFocusedIndex;
				else
					offset += FocusedIndex.Value;

			offset = Math.Max(0, Math.Min(offset, SortedItems.Count - 1));
			Focused = SortedItems.GetItemAt(offset) as DependencyObject;

			if (!controlDown)
			{
				Selected.Clear();
				select = true;
			}

			if (shiftDown)
			{
				DependencyObject lastSel;
				if ((lastShiftSel == null) || (!lastShiftSel.TryGetTarget(out lastSel)))
					lastSel = null;
				var lastSelIndex = Math.Max(0, SortedItems.IndexOf(lastSel));
				var start = Math.Min(lastSelIndex, offset);
				var end = Math.Max(lastSelIndex, offset);
				for (var ctr = start; ctr <= end; ++ctr)
					Selected.Add(SortedItems.GetItemAt(ctr) as DependencyObject);
			}
			else if (select)
			{
				if (Selected.Contains(Focused))
					Selected.Remove(Focused);
				else if (Focused != null)
				{
					Selected.Add(Focused);
					lastShiftSel = new WeakReference<DependencyObject>(Focused);
				}
			}

			ShowFocus();
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Focus();
			base.OnMouseLeftButtonDown(e);
			if (e.ClickCount == 2)
				accept(this, new RoutedEventArgs());
			else
			{
				MoveFocus((int)((e.GetPosition(contents).Y - headerHeight) / rowHeight + scroll.Value), false, true);
				e.Handled = true;
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keys = new KeySet
			{
				{ ModifierKeys.Control, Key.A, () => SortedItems.Cast<DependencyObject>().ToList().ForEach(item => Selected.Add(item)) },
				{ Key.Escape, () => lastTextInputTime = null },
				{ Key.Enter, () => accept(this, new RoutedEventArgs()) },
			};

			if (keys.Run(e))
			{
				e.Handled = true;
				return;
			}

			if (altDown)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Left: FocusedColumn = Columns[Math.Max(0, FocusedColumnIndex - 1)]; break;
				case Key.Right: FocusedColumn = Columns[Math.Min(Columns.Count - 1, FocusedColumnIndex + 1)]; break;
				case Key.Up: MoveFocus(-1, true); break;
				case Key.Down: MoveFocus(1, true); break;
				case Key.Home: MoveFocus(0, false); break;
				case Key.End: MoveFocus(SortedItems.Count - 1, false); break;
				case Key.PageUp:
					if (scroll.Value == FocusedIndex)
						MoveFocus((int)-scroll.LargeChange, true);
					else
						MoveFocus((int)scroll.Value, false);
					break;
				case Key.PageDown:
					if (scroll.Value + scroll.LargeChange == FocusedIndex)
						MoveFocus((int)scroll.LargeChange, true);
					else
						MoveFocus((int)(scroll.Value + scroll.LargeChange), false);
					break;
				case Key.Space: MoveFocus(0, true, true); break;
				default: e.Handled = false; break;
			}
		}

		DateTime? lastTextInputTime;
		string lastTextInput;
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			e.Handled = true;

			if ((Columns.Count == 0) || (SortedItems.Count == 0))
				return;

			if (!Columns.Contains(TextInputColumn))
				TextInputColumn = Columns.First();

			var now = DateTime.UtcNow;
			if ((lastTextInputTime.HasValue) && ((now - lastTextInputTime.Value).TotalMilliseconds <= TextInputDelay))
				lastTextInput += e.Text;
			else
				lastTextInput = e.Text;
			lastTextInputTime = now;

			var start = FocusedIndex ?? 0;
			var index = start;
			while (true)
			{
				if ((SortedItems.GetItemAt(index) as DependencyObject).GetValue(TextInputColumn.DepProp).ToString().StartsWith(lastTextInput, StringComparison.OrdinalIgnoreCase))
				{
					MoveFocus(index, false, true);
					break;
				}
				++index;
				if (index >= Items.Count())
					index = 0;
				if (index == start)
					break;
			}
		}

		void Redraw()
		{
			if (SortedItems == null)
				return;

			scroll.ViewportSize = Math.Max(0, Math.Ceiling((scroller.ViewportHeight - headerHeight) / rowHeight));
			scroll.LargeChange = Math.Max(0, Math.Floor((scroller.ViewportHeight - headerHeight) / rowHeight) - 1);
			scroll.Minimum = 0;
			scroll.Maximum = SortedItems.Count - scroll.ViewportSize + 1;

			if (showFocus)
			{
				var index = FocusedIndex;
				if (index.HasValue)
					scroll.Value = Math.Max(Math.Min(index.Value, scroll.Value), index.Value - scroll.LargeChange);
				showFocus = false;
			}

			if (drawTimer.Started())
				return;

			contents.Children.Clear();
			contents.ColumnDefinitions.Clear();
			contents.RowDefinitions.Clear();

			if ((scroller.ViewportHeight <= 0) || (Columns == null) || (Columns.Count == 0))
				return;

			contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			foreach (var column in Columns)
			{
				var header = column.Header;
				if (column == SortColumn)
					header += SortAscending ? " \u25b5" : " \u25bf";

				var button = new Button { Content = header };
				button.Click += (s, e) =>
				{
					if (SortColumn != column)
						SortColumn = column;
					else
						SortAscending = !SortAscending;
				};
				Grid.SetColumn(button, contents.ColumnDefinitions.Count);
				contents.Children.Add(button);
				contents.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			}

			var start = (int)(scroll.Value);
			var rows = Math.Min((int)scroll.ViewportSize, SortedItems.Count - start);
			for (var ctr = 0; ctr < rows; ++ctr)
			{
				var item = SortedItems.GetItemAt(start + ctr);

				if (Selected.Contains(item))
				{
					var rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(208, 227, 252)) };
					Grid.SetRow(rect, contents.RowDefinitions.Count);
					Grid.SetColumnSpan(rect, Columns.Count);
					contents.Children.Add(rect);
				}

				if (item == Focused)
				{
					var rect = new Rectangle { Stroke = new SolidColorBrush(Color.FromRgb(125, 162, 206)) };
					if (FocusColumns)
						Grid.SetColumn(rect, FocusedColumnIndex);
					else
						Grid.SetColumnSpan(rect, Columns.Count);
					Grid.SetRow(rect, contents.RowDefinitions.Count);
					contents.Children.Add(rect);
				}

				var col = 0;
				foreach (var column in Columns)
				{
					var textBlock = new TextBlock { Padding = new Thickness(10, 2, 10, 2), HorizontalAlignment = column.HorizontalAlignment };
					textBlock.SetBinding(TextBlock.TextProperty, new Binding(column.DepProp.Name) { Source = item, Converter = StringFormatConverter.Converter, ConverterParameter = column.StringFormat });
					Grid.SetRow(textBlock, contents.RowDefinitions.Count);
					Grid.SetColumn(textBlock, col++);
					contents.Children.Add(textBlock);
				}
				contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			}
		}
	}
}
