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
		StackPanel fullTabLabelsPanel;
		Grid fullContentGrid;
		TabWindow fullTabWindow;
		double fullTabLabelIndex;
		double fullTabLabelsWidth;
		List<TabLabel> fullTabLabels;

		bool lastFull = false;
		Size lastFullSize;
		IReadOnlyOrderedHashSet<Tab> lastFullAllTabs;
		Tab lastFullFocused;
		double lastFullTabLabelIndex;

		void ClearFullLayout()
		{
			if (!lastFull)
				return;

			canvas.Children.Clear();

			fullTabLabelsPanel = null;
			fullContentGrid.Children.Clear();
			fullContentGrid = null;
			fullTabWindow = null;
			fullTabLabelsWidth = 0;
			fullTabLabels = null;

			lastFull = false;
			lastFullSize = default;
			lastFullAllTabs = null;
			lastFullFocused = null;
			lastFullTabLabelIndex = 0;
		}

		void DoFullLayout()
		{
			ClearGridLayout();

			if (lastFullSize != canvas.RenderSize)
				ClearFullLayout();

			CreateFullLayout();
			CreateFullTabLabels();
			SetFullContent();

			lastFull = true;
			lastFullSize = canvas.RenderSize;
			lastFullAllTabs = Tabs.AllTabs;
			lastFullFocused = Tabs.Focused;
			lastFullTabLabelIndex = fullTabLabelIndex;
		}

		private void CreateFullLayout()
		{
			if (lastFull)
				return;

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

			fullTabLabelsPanel = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true };
			Grid.SetRow(fullTabLabelsPanel, 0);
			Grid.SetColumn(fullTabLabelsPanel, 1);
			grid.Children.Add(fullTabLabelsPanel);

			var moveLeft = new RepeatButton { Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveLeft.Click += (s, e) => { --fullTabLabelIndex; QueueDraw(); };
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveRight.Click += (s, e) => { ++fullTabLabelIndex; QueueDraw(); };
			Grid.SetRow(moveRight, 0);
			Grid.SetColumn(moveRight, 2);
			grid.Children.Add(moveRight);

			fullContentGrid = new Grid();
			Grid.SetRow(fullContentGrid, 1);
			Grid.SetColumn(fullContentGrid, 0);
			Grid.SetColumnSpan(fullContentGrid, 3);
			grid.Children.Add(fullContentGrid);

			outerBorder.Child = grid;
			canvas.Children.Add(outerBorder);

			fullTabLabelsWidth = canvas.ActualWidth - 40 - 2;
		}

		TabLabel GetTabLabel(Dictionary<int, TabLabel> tabLabelDict, int index)
		{
			if (tabLabelDict.ContainsKey(index))
				return tabLabelDict[index];

			var tabLabel = CreateTabLabel(Tabs.AllTabs[index]);
			tabLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			tabLabelDict[index] = tabLabel;
			return tabLabel;
		}

		double GetAtRightIndex(Dictionary<int, TabLabel> tabLabelDict, int index)
		{
			var remaining = fullTabLabelsWidth;
			var atRightIndex = 0d;
			while (index >= 0)
			{
				atRightIndex = index;
				var width = GetTabLabel(tabLabelDict, index).DesiredSize.Width;
				remaining -= width;
				if (remaining <= 0)
				{
					atRightIndex -= remaining / width;
					break;
				}
				--index;
			}
			return atRightIndex;
		}

		void CreateFullTabLabels()
		{
			var tabLabelMap = new Dictionary<int, TabLabel>();
			if ((Tabs.Focused != null) && ((lastFullAllTabs != Tabs.AllTabs) || (lastFullFocused != Tabs.Focused)))
			{
				var atLeftIndex = Tabs.AllTabs.IndexOf(Tabs.Focused);
				var atRightIndex = GetAtRightIndex(tabLabelMap, atLeftIndex);
				fullTabLabelIndex = Math.Min(atLeftIndex, Math.Max(fullTabLabelIndex, atRightIndex));
			}

			if ((lastFullAllTabs != Tabs.AllTabs) || (lastFullTabLabelIndex != fullTabLabelIndex))
				fullTabLabelIndex = Math.Max(0, Math.Min(fullTabLabelIndex, GetAtRightIndex(tabLabelMap, Tabs.AllTabs.Count - 1)));

			if ((lastFullAllTabs == Tabs.AllTabs) && (lastFullTabLabelIndex == fullTabLabelIndex))
			{
				fullTabLabels.ForEach(tabLabel => tabLabel.Refresh(Tabs));
				return;
			}

			fullTabLabelsPanel.Children.Clear();
			fullTabLabels = new List<TabLabel>();
			if (!Tabs.AllTabs.Any())
				return;

			var remaining = fullTabLabelsWidth;
			var index = (int)fullTabLabelIndex;
			var offset = fullTabLabelIndex - index;
			while (true)
			{
				if ((index >= Tabs.AllTabs.Count) || (remaining <= 0))
					break;

				var tabLabel = GetTabLabel(tabLabelMap, index);
				if (offset != 0)
				{
					tabLabel.Margin = new Thickness(-tabLabel.DesiredSize.Width * offset, 0, 0, 0);
					offset = 0;
				}
				fullTabLabels.Add(tabLabel);
				fullTabLabelsPanel.Children.Add(tabLabel);

				++index;
				remaining -= tabLabel.DesiredSize.Width;
			}
		}

		void SetFullContent()
		{
			if (lastFullFocused != Tabs.Focused)
			{
				fullTabWindow = null;
				fullContentGrid.Children.Clear();

				if (Tabs.Focused != null)
				{
					fullTabWindow = new TabWindow(Tabs.Focused);
					fullContentGrid.Children.Add(fullTabWindow);
				}
			}

			fullTabWindow?.DrawAll();
		}
	}
}
