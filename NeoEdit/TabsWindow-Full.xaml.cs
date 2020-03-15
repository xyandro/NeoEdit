using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		bool lastFull = false;
		Size lastSize;
		IReadOnlyList<Tab> prevTabs;
		StackPanel tabLabelsStackPanel;
		ScrollViewer tabLabelsScrollViewer;
		Grid contentGrid;

		void ClearFullLayout()
		{
			lastFull = default;
			lastSize = default;
			prevTabs = default;
			tabLabelsStackPanel = default;
			tabLabelsScrollViewer = default;
			contentGrid = default;
		}

		void DoFullLayout(bool setFocus)
		{
			ClearGridLayout();

			if (lastSize != canvas.RenderSize)
			{
				lastSize = canvas.RenderSize;
				lastFull = false;
			}

			if (!lastFull)
			{
				lastFull = true;
				prevTabs = null;

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
				grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				grid.RowDefinitions.Add(new RowDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

				tabLabelsScrollViewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Focusable = false };

				tabLabelsStackPanel = new StackPanel { Orientation = Orientation.Horizontal };

				tabLabelsScrollViewer.Content = tabLabelsStackPanel;
				Grid.SetRow(tabLabelsScrollViewer, 0);
				Grid.SetColumn(tabLabelsScrollViewer, 1);
				grid.Children.Add(tabLabelsScrollViewer);

				var moveLeft = new RepeatButton { Width = 20, Height = 20, Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
				moveLeft.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset - 50, tabLabelsScrollViewer.ScrollableWidth)));
				Grid.SetRow(moveLeft, 0);
				Grid.SetColumn(moveLeft, 0);
				grid.Children.Add(moveLeft);

				var moveRight = new RepeatButton { Width = 20, Height = 20, Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
				moveRight.Click += (s, e) => tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabelsScrollViewer.HorizontalOffset + 50, tabLabelsScrollViewer.ScrollableWidth)));
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

			if (prevTabs != Tabs.AllTabs)
			{
				prevTabs = Tabs.AllTabs;
				tabLabelsStackPanel.Children.Clear();
				foreach (var tab in Tabs.AllTabs)
				{
					var tabLabel = GetTabLabel(tab);
					tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as Tab);
					tabLabelsStackPanel.Children.Add(tabLabel);
				}
			}
			else
			{
				tabLabelsStackPanel.Children.OfType<Border>().ForEach(SetColor);
			}

			if ((setFocus) && (Tabs.Focused != null))
			{
				var show = tabLabelsStackPanel.Children.OfType<FrameworkElement>().Where(x => x.Tag == Tabs.Focused).FirstOrDefault();
				if (show != null)
				{
					tabLabelsScrollViewer.UpdateLayout();
					var left = show.TranslatePoint(new Point(0, 0), tabLabelsScrollViewer).X + tabLabelsScrollViewer.HorizontalOffset;
					tabLabelsScrollViewer.ScrollToHorizontalOffset(Math.Min(left, Math.Max(tabLabelsScrollViewer.HorizontalOffset, left + show.ActualWidth - tabLabelsScrollViewer.ViewportWidth)));
				}
			}

			contentGrid.Children.Clear();
			if (Tabs.Focused != null)
			{
				var tabWindow = new TabWindow(Tabs.Focused);
				contentGrid.Children.Add(tabWindow);
				tabWindow.DrawAll();
			}
		}
	}
}
