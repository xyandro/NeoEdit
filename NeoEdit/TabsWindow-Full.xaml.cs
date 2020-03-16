using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		bool lastFull = false;
		Size lastSize;
		Canvas tabLabelsCanvas;
		Grid contentGrid;
		IReadOnlyOrderedHashSet<Tab> lastActiveTabs;
		Tab lastFocused;
		TabWindow tabWindow;

		void ClearFullLayout()
		{
			if (!lastFull)
				return;

			contentGrid.Children.Clear();

			lastFull = false;
			lastSize = default;
			tabLabelsCanvas = null;
			contentGrid = null;
			lastActiveTabs = null;
			lastFocused = null;
		}

		void DoFullLayout()
		{
			ClearGridLayout();

			if (lastSize != canvas.RenderSize)
			{
				ClearFullLayout();
				lastSize = canvas.RenderSize;
			}

			CreateFullControls();
			CreateTabLabels();
			SetContent();
		}

		private void CreateFullControls()
		{
			if (lastFull)
				return;

			lastFull = true;

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
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

			tabLabelsCanvas = new Canvas { ClipToBounds = true };
			Grid.SetRow(tabLabelsCanvas, 0);
			Grid.SetColumn(tabLabelsCanvas, 1);
			grid.Children.Add(tabLabelsCanvas);

			var moveLeft = new RepeatButton { Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			//moveLeft.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset - 50, tabLabelsScrollViewer.ScrollableWidth)));
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			//moveRight.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset + 50, tabLabelsScrollViewer.ScrollableWidth)));
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

		void CreateTabLabels()
		{
			if (lastActiveTabs == Tabs.UnsortedActiveTabs)
				return;

			lastActiveTabs = Tabs.UnsortedActiveTabs;

			tabLabelsCanvas.Children.Clear();
			if (lastActiveTabs.Count == 0)
				return;

			var focusedIndex = Tabs.Focused == null ? 0 : Tabs.AllTabs.IndexOf(Tabs.Focused);
			var tabLabels = new List<Border>();

			var left = double.MaxValue;
			var right = double.MinValue;
			var leftIndex = focusedIndex - 1;
			var rightIndex = focusedIndex;
			while (true)
			{
				if ((rightIndex < Tabs.AllTabs.Count) && (right <= canvas.ActualWidth - 40))
				{
					var tabLabel = GetTabLabel(Tabs.AllTabs[rightIndex], true);
					tabLabels.Add(tabLabel);
					if (right == double.MinValue)
					{
						left = (canvas.ActualWidth - 40 - tabLabel.DesiredSize.Width) / 2;
						right = left + tabLabel.DesiredSize.Width;
					}
					else
						right += tabLabel.DesiredSize.Width;
					++rightIndex;
				}
				else if (rightIndex == Tabs.AllTabs.Count)
				{
					++rightIndex; // Don't want to do this again
					var offset = Math.Max(canvas.ActualWidth - 40 - 3 - right, 0);
					left += offset;
					right += offset;
				}
				else if ((leftIndex >= 0) && (left >= 0))
				{
					var tabLabel = GetTabLabel(Tabs.AllTabs[leftIndex], true);
					tabLabels.Insert(0, tabLabel);
					left -= tabLabel.DesiredSize.Width;
					--leftIndex;
				}
				else if (leftIndex == -1)
				{
					--leftIndex; // Don't want to do this again
					var offset = Math.Max(0, left);
					left -= offset;
					right -= offset;
				}
				else
					break;
			}

			foreach (var tabLabel in tabLabels)
			{
				Canvas.SetLeft(tabLabel, left);
				tabLabelsCanvas.Children.Add(tabLabel);
				left += tabLabel.DesiredSize.Width;
			}
		}

		void SetContent()
		{
			if (lastFocused != Tabs.Focused)
			{
				lastFocused = Tabs.Focused;

				contentGrid.Children.Clear();
				tabWindow = null;
				if (lastFocused != null)
				{
					tabWindow = new TabWindow(lastFocused);
					contentGrid.Children.Add(tabWindow);
				}
			}
			tabWindow?.DrawAll();
		}
	}
}
