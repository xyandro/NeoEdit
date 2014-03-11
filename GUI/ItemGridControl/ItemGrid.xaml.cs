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
using System.Windows.Threading;

namespace NeoEdit.GUI.ItemGridControl
{
	public partial class ItemGrid : Grid
	{
		public delegate void AcceptEvent();
		AcceptEvent accept = () => { };
		public event AcceptEvent Accept
		{
			add { accept += value; }
			remove { accept -= value; }
		}

		public static DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(IEnumerable<DependencyObject>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).ItemsCollectionChanged(e.OldValue)));
		public static DependencyProperty SortedItemsProperty = DependencyProperty.Register("SortedItems", typeof(ListCollectionView), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).SortedItemsCollectionChanged(e.OldValue)));
		public static DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ObservableHashSet<ItemGridColumn>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).ColumnsCollectionChanged(e.OldValue)));
		public static DependencyProperty SortColumnProperty = DependencyProperty.Register("SortColumn", typeof(ItemGridColumn), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).SortColumnChanged()));
		public static DependencyProperty SortAscendingProperty = DependencyProperty.Register("SortAscending", typeof(bool), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).SortAscendingChanged()));
		public static DependencyProperty FocusedProperty = DependencyProperty.Register("Focused", typeof(DependencyObject), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).FocusedChanged()));
		public static DependencyProperty FocusColumnsProperty = DependencyProperty.Register("FocusColumns", typeof(bool), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).FocusColumnsChanged()));
		public static DependencyProperty FocusedColumnProperty = DependencyProperty.Register("FocusedColumn", typeof(ItemGridColumn), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).FocusedColumnChanged()));
		public static DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(ObservableCollection<DependencyObject>), typeof(ItemGrid), new PropertyMetadata((d, e) => (d as ItemGrid).SelectedCollectionChanged(e.OldValue)));
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

		public int FocusedIndex { get { return SortedItems.IndexOf(Focused); } }
		public int FocusedColumnIndex { get { return Columns.IndexOf(FocusedColumn); } }

		public ItemGridColumn this[string column] { get { return Columns.FirstOrDefault(col => col.Header == column); } }
		const double headerHeight = 21.96;
		const double rowHeight = 19.96;

		public ItemGrid()
		{
			InitializeComponent();
			Columns = new ObservableHashSet<ItemGridColumn>();
			Selected = new ObservableCollection<DependencyObject>();
			DependencyPropertyDescriptor.FromProperty(ScrollViewer.ViewportHeightProperty, typeof(ScrollViewer)).AddValueChanged(scroller, (o, e) => OnSizeChanged());
			scroll.ValueChanged += (s, e) => InvalidateDraw();
			PreviewMouseWheel += (s, e) => scroll.Value -= e.Delta / 10;
		}

		void OnSizeChanged()
		{
			scroll.ViewportSize = Math.Max(0, Math.Ceiling((scroller.ViewportHeight - headerHeight) / rowHeight));
			scroll.LargeChange = Math.Max(0, Math.Floor((scroller.ViewportHeight - headerHeight) / rowHeight) - 1);
			scroll.Minimum = 0;
			scroll.Maximum = SortedItems.Count - scroll.ViewportSize + 1;
			InvalidateDraw();
		}

		void ItemsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= ItemsChanged;

			var newValue = SortedItems as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += ItemsChanged;

			ItemsChanged(null, null);
		}

		void SortedItemsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= SortedItemsChanged;

			var newValue = SortedItems as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += SortedItemsChanged;

			SortedItemsChanged(null, null);
		}

		void ColumnsCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= ColumnsChanged;

			var newValue = Columns as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += ColumnsChanged;

			ColumnsChanged(null, null);
		}

		void SelectedCollectionChanged(object _oldValue)
		{
			var oldValue = _oldValue as INotifyCollectionChanged;
			if (oldValue != null)
				oldValue.CollectionChanged -= SelectedChanged;

			var newValue = Selected as INotifyCollectionChanged;
			if (newValue != null)
				newValue.CollectionChanged += SelectedChanged;

			SelectedChanged(null, null);
		}

		void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateSort();
		}

		void SortedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			FocusedChanged(false);
			SelectedChanged(null, null);
			InvalidateDraw();
		}

		void ColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			SortColumnChanged();
			FocusedColumnChanged();
			InvalidateDraw();
		}

		void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Selected.Where(item => !SortedItems.Contains(item)).ToList().ForEach(item => Selected.Remove(item));
			InvalidateDraw();
		}

		void SortColumnChanged()
		{
			if (Columns.Count == 0)
				return;

			if (!Columns.Contains(SortColumn))
			{
				SortColumn = Columns.First();
				return; // Recursive
			}

			SortAscending = SortColumn.SortAscending;
			SortAscendingChanged();
		}

		void SortAscendingChanged()
		{
			InvalidateSort();
		}

		void FocusedChanged(bool show = true)
		{
			if (SortedItems.Count == 0)
				return;

			if (!SortedItems.Contains(Focused))
			{
				MoveFocus(0, false, true);
				return; // Recursive
			}

			if (show)
			{
				ShowFocus();
				InvalidateDraw();
			}
		}

		void FocusColumnsChanged()
		{
			InvalidateDraw();
		}

		void FocusedColumnChanged()
		{
			if (Columns.Count == 0)
				return;

			if (!Columns.Contains(FocusedColumn))
			{
				FocusedColumn = Columns.First();
				return; // Recursive
			}

			InvalidateDraw();
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

				if (val1.GetType() == typeof(int))
					return mult * ((int)val1).CompareTo((int)val2);
				if (val1.GetType() == typeof(long))
					return mult * ((long)val1).CompareTo((long)val2);
				if (val1.GetType() == typeof(double))
					return mult * ((double)val1).CompareTo((double)val2);
				if (val1.GetType() == typeof(DateTime))
					return mult * ((DateTime)val1).CompareTo((DateTime)val2);
				if (val1.GetType() == typeof(string))
					if (numericStrings)
						return mult * SortStr((string)val1).CompareTo(SortStr((string)val2));
					else
						return mult * ((string)val1).CompareTo((string)val2);

				throw new Exception("Unable to compare");
			}
		}

		public void Sort()
		{
			if ((Columns.Contains(SortColumn)) && (SortedItems != null))
				SortedItems.CustomSort = new Comparer(SortColumn.DepProp, SortAscending, SortColumn.NumericStrings);
		}

		void ShowFocus()
		{
			if (Items.Count() == 0)
				return;

			var index = FocusedIndex;
			scroll.Value = Math.Min(index, Math.Max(scroll.Value, index - scroll.LargeChange));
			scroll.Value = Math.Max(Math.Min(index, scroll.Value), index - scroll.LargeChange);
		}

		DispatcherTimer drawTimer = null;
		void InvalidateDraw()
		{
			if (drawTimer != null)
				return;

			drawTimer = new DispatcherTimer();
			drawTimer.Tick += (s, e) =>
			{
				drawTimer.Stop();
				drawTimer = null;
				Redraw();
			};
			drawTimer.Start();
		}

		DispatcherTimer sortTimer = null;
		void InvalidateSort()
		{
			if (sortTimer != null)
				return;

			sortTimer = new DispatcherTimer();
			sortTimer.Tick += (s, e) =>
			{
				sortTimer.Stop();
				sortTimer = null;
				Sort();
			};
			sortTimer.Start();
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		bool controlOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Control; } }

		WeakReference<DependencyObject> lastShiftSel;
		void MoveFocus(int offset, bool relative, bool select = false)
		{
			if (relative)
				offset += FocusedIndex;
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
				else
				{
					Selected.Add(Focused);
					lastShiftSel = new WeakReference<DependencyObject>(Focused);
				}
			}

			ShowFocus();
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			if (e.ClickCount == 2)
				accept();
			else
			{
				MoveFocus((int)((e.GetPosition(contents).Y - headerHeight) / rowHeight + scroll.Value), false, true);
				e.Handled = true;
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

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
				case Key.A:
					if (controlOnly)
						SortedItems.Cast<DependencyObject>().ToList().ForEach(item => Selected.Add(item));
					else
						e.Handled = false;
					break;
				case Key.Escape: lastTextInputTime = null; break;
				case Key.Enter: accept(); break;
				default: e.Handled = false; break;
			}
		}

		DateTime? lastTextInputTime;
		string lastTextInput;
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);

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

			var start = FocusedIndex;
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
					var label = new Label { Padding = new Thickness(10, 2, 10, 2), HorizontalAlignment = column.HorizontalAlignment };
					label.SetBinding(Label.ContentProperty, new Binding(column.DepProp.Name) { Source = item, Converter = StringFormatConverter.Converter, ConverterParameter = column.StringFormat });
					Grid.SetRow(label, contents.RowDefinitions.Count);
					Grid.SetColumn(label, col++);
					contents.Children.Add(label);
				}
				contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			}
		}
	}
}
