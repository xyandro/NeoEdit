using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		bool lastFull = false;
		Size lastSize;
		StackPanel tabLabelsStackPanel;
		Grid contentGrid;
		IReadOnlyOrderedHashSet<Tab> lastAllTabs;
		Tab lastFocused;
		double lastTabLabelIndex, tabLabelIndex;
		TabWindow tabWindow;

		void ClearFullLayout()
		{
			if (!lastFull)
				return;

			contentGrid.Children.Clear();

			lastFull = false;
			lastSize = default;
			tabLabelsStackPanel = null;
			contentGrid = null;
			lastAllTabs = null;
			lastTabLabelIndex = 0;
			tabLabelIndex = 0;
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

			tabLabelsStackPanel = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true };
			Grid.SetRow(tabLabelsStackPanel, 0);
			Grid.SetColumn(tabLabelsStackPanel, 1);
			grid.Children.Add(tabLabelsStackPanel);

			var moveLeft = new RepeatButton { Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveLeft.Click += (s, e) => { --tabLabelIndex; QueueDraw(); };
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveRight.Click += (s, e) => { ++tabLabelIndex; QueueDraw(); };
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
			if ((lastAllTabs == Tabs.AllTabs) && (lastFocused == Tabs.Focused) && (lastTabLabelIndex == tabLabelIndex))
			{
				tabLabelsStackPanel.Children.OfType<TabLabel>().ForEach(tabLabel => tabLabel.Refresh(Tabs));
				return;
			}

			lastAllTabs = Tabs.AllTabs;

			tabLabelsStackPanel.Children.Clear();
			if (!lastAllTabs.Any())
				return;

			var tabLabels = new Dictionary<int, TabLabel>();
			TabLabel FetchTabLabel(int index)
			{
				if (tabLabels.ContainsKey(index))
					return tabLabels[index];

				var tabLabel = GetTabLabel(lastAllTabs[index]);
				tabLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				tabLabels[index] = tabLabel;
				return tabLabel;
			}

			if ((lastFocused != Tabs.Focused) && (Tabs.Focused != null))
			{
				var atLeftIndex = lastAllTabs.IndexOf(Tabs.Focused);
				var remaining = canvas.ActualWidth - 40 - 3;
				var atRightIndex = 0d;
				for (var ctr = atLeftIndex; ctr >= 0; --ctr)
				{
					atRightIndex = ctr;
					var width = FetchTabLabel(ctr).DesiredSize.Width;
					remaining -= width;
					if (remaining <= 0)
					{
						atRightIndex -= remaining / width;
						break;
					}
				}

				tabLabelIndex = Math.Min(atLeftIndex, Math.Max(tabLabelIndex, atRightIndex));
			}

			tabLabelIndex = Math.Max(0, Math.Min(tabLabelIndex, lastAllTabs.Count - 1));

			lastTabLabelIndex = tabLabelIndex;
			{
				var remaining = canvas.ActualWidth - 40 - 3;
				var index = (int)tabLabelIndex;
				var offset = tabLabelIndex - index;
				while (true)
				{
					if ((index >= lastAllTabs.Count) || (remaining <= 0))
						break;
					var tabLabel = FetchTabLabel(index);
					remaining -= tabLabel.DesiredSize.Width;
					if (offset != 0)
					{
						tabLabel.Margin = new Thickness(-tabLabel.DesiredSize.Width * offset, 0, 0, 0);
						offset = 0;
					}
					tabLabelsStackPanel.Children.Add(tabLabel);
					++index;
				}
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
