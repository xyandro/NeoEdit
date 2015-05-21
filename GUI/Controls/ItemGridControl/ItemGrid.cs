﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.GUI.Misc;

namespace NeoEdit.GUI.Controls.ItemGridControl
{
	public class ItemGrid<ItemType> : Grid where ItemType : DependencyObject
	{
		public delegate void ItemGridEventHandler(object sender);
		ItemGridEventHandler accept = s => { };
		public event ItemGridEventHandler Accept { add { accept += value; } remove { accept -= value; } }
		ItemGridEventHandler selectionChanged = s => { };
		public event ItemGridEventHandler SelectionChanged { add { selectionChanged += value; } remove { selectionChanged -= value; } }

		[DepProp]
		public ObservableCollection<ItemType> Items { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ItemType> SortedItems { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ItemGridColumn> Columns { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ObservableCollection<ItemGridColumn>>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemGridColumn SortColumn { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ItemGridColumn>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool SortAscending { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<bool>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemType Focused { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ItemType>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool FocusColumns { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<bool>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ItemGridColumn FocusedColumn { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ItemGridColumn>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ItemType> Selected { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }
		[DepProp(Default = 500)]
		public int TextInputDelay { get { return UIHelper<ItemGrid<ItemType>>.GetPropValue<int>(this); } set { UIHelper<ItemGrid<ItemType>>.SetPropValue(this, value); } }

		static readonly Brush buttonBrush;
		static readonly Style buttonStyle;
		static ItemGrid()
		{
			UIHelper<ItemGrid<ItemType>>.Register();
			UIHelper<ItemGrid<ItemType>>.AddObservableCallback(a => a.Items, (obj, s, e) => { obj.verifyTimer.Start(); obj.sortTimer.Start(); });
			UIHelper<ItemGrid<ItemType>>.AddObservableCallback(a => a.Columns, (obj, s, e) => obj.verifyTimer.Start());
			UIHelper<ItemGrid<ItemType>>.AddObservableCallback(a => a.Selected, (obj, s, e) => { obj.verifyTimer.Start(); obj.selectionTimer.Start(); });
			UIHelper<ItemGrid<ItemType>>.AddCallback(a => a.SortColumn, (obj, s, e) => { obj.SortAscending = obj.SortColumn != null ? obj.SortColumn.SortAscending : true; obj.verifyTimer.Start(); obj.sortTimer.Start(); });
			UIHelper<ItemGrid<ItemType>>.AddCallback(a => a.SortAscending, (obj, s, e) => obj.sortTimer.Start());
			UIHelper<ItemGrid<ItemType>>.AddCallback(a => a.Focused, (obj, s, e) => { obj.verifyTimer.Start(); obj.showFocus = true; });
			UIHelper<ItemGrid<ItemType>>.AddCallback(a => a.FocusColumns, (obj, s, e) => obj.drawTimer.Start());
			UIHelper<ItemGrid<ItemType>>.AddCallback(a => a.FocusedColumn, (obj, s, e) => obj.verifyTimer.Start());

			buttonBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208));
			buttonBrush.Freeze();
			buttonStyle = new Style(typeof(Button));
			buttonStyle.Setters.Add(new Setter(Button.FocusableProperty, false));
			buttonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(0)));
		}

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
			if ((Focused == null) || (SortedItems == null))
				return;
			var idx = SortedItems.IndexOf(Focused);
			if (idx == -1)
				return;
			lastFocusedIndex = idx;
		}

		const double headerHeight = 21.96;
		const double rowHeight = 19.96;

		RunOnceTimer verifyTimer, sortTimer, drawTimer, selectionTimer;

		ScrollViewer xScroll;
		ScrollBar yScroll;
		Grid contents;
		public ItemGrid()
		{
			Focusable = true;
			KeyboardNavigation.SetIsTabStop(this, true);
			FocusVisualStyle = null;

			ColumnDefinitions.Add(new ColumnDefinition());
			ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			RowDefinitions.Add(new RowDefinition());

			xScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Visible, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
			contents = new Grid();
			xScroll.Content = contents;
			Children.Add(xScroll);

			yScroll = new ScrollBar { Focusable = false };
			Grid.SetColumn(yScroll, 1);
			Children.Add(yScroll);

			verifyTimer = new RunOnceTimer(() => VerifyParameters());
			sortTimer = new RunOnceTimer(() => Sort());
			drawTimer = new RunOnceTimer(() => Redraw());
			selectionTimer = new RunOnceTimer(() => selectionChanged(this));
			sortTimer.AddDependency(verifyTimer);
			drawTimer.AddDependency(sortTimer, verifyTimer);
			selectionTimer.AddDependency(verifyTimer);

			Items = new ObservableCollection<ItemType>();
			Columns = new ObservableCollection<ItemGridColumn>();
			Selected = new ObservableCollection<ItemType>();

			yScroll.SizeChanged += (s, e) => drawTimer.Start();
			yScroll.ValueChanged += (s, e) => drawTimer.Start();
			PreviewMouseWheel += (s, e) => yScroll.Value -= e.Delta / 10;
		}

		void VerifyParameters()
		{
			if (Columns != null)
			{
				if (Columns.Count == 0)
					SortColumn = FocusedColumn = null;
				else
				{
					if ((SortColumn == null) || (!Columns.Contains(SortColumn)))
						SortColumn = Columns.First();

					if ((FocusedColumn == null) || (!Columns.Contains(FocusedColumn)))
						FocusedColumn = Columns.First();
				}
			}

			if (Items != null)
			{
				if (Selected != null)
				{
					Selected.Where(item => !Items.Contains(item)).ToList().ForEach(item => Selected.Remove(item));
					Selected.GroupBy(item => item).SelectMany(group => group.Skip(1).Select(a => group.Key)).ToList().ForEach(item => Selected.Remove(item));
				}

				if (!Items.Contains(Focused))
					Focused = null;

				SetLastFocusedIndex();
			}

			verifyTimer.Stop();
			drawTimer.Start();
		}

		class Comparer : IComparer<ItemType>
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

			public int Compare(ItemType o1, ItemType o2)
			{
				var val1 = o1.GetValue(prop);
				var val2 = o2.GetValue(prop);

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
				return 0;
			}
		}

		void Sort()
		{
			if ((Columns == null) || (Items == null) || (SortColumn == null))
				return;

			SortedItems = new ObservableCollection<ItemType>(Items.OrderBy(a => a, new Comparer(SortColumn.DepProp, SortAscending, (Items.Count <= 500) && (SortColumn.NumericStrings))));
			sortTimer.Stop();
			verifyTimer.Stop(); // Since verify is a dependency of sort, this was already done

			lastTextInputTime = null;
			SetLastFocusedIndex();
			drawTimer.Start();
		}

		public void ResetScroll()
		{
			yScroll.Value = 0;
			lastFocusedIndex = 0;
		}

		public void SyncItems(IEnumerable<ItemType> items, DependencyProperty prop)
		{
			var scrollPos = yScroll.Value;
			var focused = Focused == null ? null : Focused.GetValue(prop) as IComparable;
			var selected = Selected.Select(item => item.GetValue(prop) as IComparable).ToList();

			Items.Clear();
			foreach (var item in items)
				Items.Add(item);

			Selected.Clear();
			foreach (var item in Items)
			{
				var itemValue = item.GetValue(prop) as IComparable;
				if (focused == itemValue)
					Focused = item;
				if (selected.Contains(itemValue))
					Selected.Add(item);
			}

			yScroll.Value = scrollPos;
		}

		public void BringFocusedIntoView()
		{
			showFocus = true;
			drawTimer.Start();
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altDown { get { return (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None; } }
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		WeakReference<ItemType> lastShiftSel;
		void MoveFocus(int offset, bool relative, bool select = false)
		{
			if (!SortedItems.Any())
				return;

			if (relative)
				if (!FocusedIndex.HasValue)
					offset = lastFocusedIndex;
				else
					offset += FocusedIndex.Value;

			offset = Math.Max(0, Math.Min(offset, SortedItems.Count - 1));
			Focused = SortedItems[offset];

			if (!controlDown)
			{
				Selected.Clear();
				select = true;
			}

			if (shiftDown)
			{
				ItemType lastSel;
				if ((lastShiftSel == null) || (!lastShiftSel.TryGetTarget(out lastSel)))
					lastSel = null;
				var lastSelIndex = Math.Max(0, SortedItems.IndexOf(lastSel));
				var start = Math.Min(lastSelIndex, offset);
				var end = Math.Max(lastSelIndex, offset);
				for (var ctr = start; ctr <= end; ++ctr)
					Selected.Add(SortedItems[ctr]);
			}
			else if (select)
			{
				if (Selected.Contains(Focused))
					Selected.Remove(Focused);
				else if (Focused != null)
				{
					Selected.Add(Focused);
					lastShiftSel = new WeakReference<ItemType>(Focused);
				}
			}
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			Focus();
			if (e.ClickCount == 2)
				accept(this);
			else
			{
				MoveFocus((int)((e.GetPosition(contents).Y - headerHeight) / rowHeight + yScroll.Value), false, true);
				e.Handled = true;
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keys = new KeySet
			{
				{ Key.Escape, () => lastTextInputTime = null },
				{ Key.Enter, () => accept(this) },
			};

			if (keys.Run(e))
			{
				e.Handled = true;
				return;
			}

			if (altDown)
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.System:
						switch (e.SystemKey)
						{
							case Key.Down:
							case Key.Up:
								if ((Selected.Count == 0) || (SortedItems.Count == 0) || (Focused == null))
									break;

								var index = FocusedIndex.Value;
								var offset = e.SystemKey == Key.Up ? -1 : 1;
								while (true)
								{
									index += offset;
									if (index < 0)
										index = SortedItems.Count - 1;
									if (index >= SortedItems.Count)
										index = 0;
									if (Selected.Contains(SortedItems[index]))
									{
										Focused = SortedItems[index];
										break;
									}
								}
								break;
							case Key.Space:
								Focused = SortedItems.FirstOrDefault(item => Selected.Contains(item));
								break;
							default: e.Handled = false; break;
						}
						break;
					default: e.Handled = false; break;
				}
				return;
			}

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
					if (yScroll.Value == FocusedIndex)
						MoveFocus((int)-yScroll.LargeChange, true);
					else
						MoveFocus((int)yScroll.Value, false);
					break;
				case Key.PageDown:
					if (yScroll.Value + yScroll.LargeChange == FocusedIndex)
						MoveFocus((int)yScroll.LargeChange, true);
					else
						MoveFocus((int)(yScroll.Value + yScroll.LargeChange), false);
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

			var now = DateTime.UtcNow;
			if ((!lastTextInputTime.HasValue) || ((now - lastTextInputTime.Value).TotalMilliseconds > TextInputDelay))
				lastTextInput = "";
			lastTextInput += e.Text;
			lastTextInputTime = now;

			var start = FocusedIndex ?? 0;
			var index = start;
			while (true)
			{
				if (SortedItems[index].GetValue(SortColumn.DepProp).ToString().StartsWith(lastTextInput, StringComparison.OrdinalIgnoreCase))
				{
					MoveFocus(index, false, true);
					break;
				}
				++index;
				if (index >= SortedItems.Count)
					index = 0;
				if (index == start)
					break;
			}
		}

		bool showFocus = false;
		void Redraw()
		{
			if (SortedItems == null)
				return;

			yScroll.ViewportSize = Math.Max(0, Math.Ceiling((xScroll.ViewportHeight - headerHeight) / rowHeight));
			yScroll.LargeChange = Math.Max(0, Math.Floor((xScroll.ViewportHeight - headerHeight) / rowHeight) - 1);
			yScroll.Minimum = 0;
			yScroll.Maximum = SortedItems.Count - yScroll.ViewportSize + 1;

			if (showFocus)
			{
				var index = FocusedIndex;
				if (index.HasValue)
					yScroll.Value = Math.Max(Math.Min(index.Value, yScroll.Value), index.Value - yScroll.LargeChange);
				showFocus = false;
			}

			if (drawTimer.Started)
				return;

			contents.Children.Clear();
			contents.ColumnDefinitions.Clear();
			contents.RowDefinitions.Clear();

			if ((xScroll.ViewportHeight <= 0) || (Columns == null) || (Columns.Count == 0))
				return;

			contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			foreach (var column in Columns)
			{
				var header = column.Header;
				if (column == SortColumn)
					header += SortAscending ? " \u25b5" : " \u25bf";

				var button = new Button { Content = header, Style = buttonStyle, AllowDrop = true };
				button.Click += (s, e) =>
				{
					if (controlDown)
						Columns.Remove(column);
					else if (SortColumn != column)
						SortColumn = column;
					else
						SortAscending = !SortAscending;
				};

				Point? dragStart = null;
				button.PreviewMouseLeftButtonDown += (s, e) => { dragStart = e.GetPosition(button); };
				button.PreviewMouseLeftButtonUp += (s, e) => dragStart = null;
				button.PreviewMouseMove += (s, e) =>
				{
					if ((dragStart.HasValue) && ((e.GetPosition(button) - dragStart.Value).Length > SystemParameters.MinimumHorizontalDragDistance))
					{
						dragStart = null;
						DragDrop.DoDragDrop(button, new DataObject(typeof(ItemGridColumn), column), DragDropEffects.Move);
					}
				};
				button.Drop += (s, e) =>
				{
					var column2 = e.Data.GetData(typeof(ItemGridColumn)) as ItemGridColumn;
					var fromIndex = Columns.IndexOf(column2);
					var toIndex = Columns.IndexOf(column);
					if ((fromIndex == toIndex) || (fromIndex == -1) || (toIndex == -1))
						return;

					Columns.Move(fromIndex, toIndex);
				};
				Grid.SetColumn(button, contents.ColumnDefinitions.Count);
				contents.Children.Add(button);
				contents.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			}

			var start = (int)(yScroll.Value);
			var rows = Math.Min((int)yScroll.ViewportSize, SortedItems.Count - start);
			for (var ctr = 0; ctr < rows; ++ctr)
			{
				var item = SortedItems[start + ctr];

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

				for (var col = 0; col < Columns.Count; ++col)
				{
					var column = Columns[col];
					UIElement element = null;

					if (column.DepProp.PropertyType == typeof(BitmapSource))
					{
						var source = item.GetValue(column.DepProp) as BitmapSource;
						if (source != null)
							element = new Image { Source = source, Height = 20 };
					}
					else
					{
						var textBlock = new TextBlock { Padding = new Thickness(10, 2, 10, 2), HorizontalAlignment = column.HorizontalAlignment };
						textBlock.SetBinding(TextBlock.TextProperty, new Binding(column.DepProp.Name) { Source = item, Converter = StringFormatConverter.Converter, ConverterParameter = column.StringFormat });
						element = textBlock;
					}

					if (element == null)
						continue;

					Grid.SetRow(element, contents.RowDefinitions.Count);
					Grid.SetColumn(element, col);
					contents.Children.Add(element);
				}
				contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			}
		}
	}
}