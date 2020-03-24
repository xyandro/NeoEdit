using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI
{
	partial class TabsWindow
	{
		StackPanel fullTabLabelsPanel;
		Grid fullContentGrid;
		double fullTabLabelIndex;
		double fullTabLabelsWidth;
		List<TabLabel> fullTabLabels;

		bool lastFull = false;
		Size lastFullSize;
		int lastFullAllTabsHash;
		ITab lastFullFocused;
		double lastFullTabLabelIndex;

		void ClearFullLayout()
		{
			if (!lastFull)
				return;

			canvas.Children.Clear();

			fullTabLabelsPanel = null;
			fullContentGrid.Children.Clear();
			fullContentGrid = null;
			tabWindows[0].Tab = null;
			fullTabLabelsWidth = 0;
			fullTabLabels = null;

			lastFull = false;
			lastFullSize = default;
			lastFullAllTabsHash = 0;
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
			lastFullAllTabsHash = Tabs.AllTabsHash;
			lastFullFocused = Tabs.FocusedITab;
			lastFullTabLabelIndex = fullTabLabelIndex;
		}

		private void CreateFullLayout()
		{
			if (lastFull)
				return;

			SetTabWindowCount(1);

			if (scrollBarBorder.Visibility != Visibility.Collapsed)
			{
				scrollBarBorder.Visibility = Visibility.Collapsed;
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

			var tabLabel = CreateTabLabel(Tabs.AllITabs.GetIndex(index));
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
			if ((Tabs.FocusedITab != null) && ((lastFullAllTabsHash != Tabs.AllTabsHash) || (lastFullFocused != Tabs.FocusedITab)))
			{
				var atLeftIndex = Tabs.AllITabs.FindIndex(Tabs.FocusedITab);
				var atRightIndex = GetAtRightIndex(tabLabelMap, atLeftIndex);
				fullTabLabelIndex = Math.Min(atLeftIndex, Math.Max(fullTabLabelIndex, atRightIndex));
			}

			if ((lastFullAllTabsHash != Tabs.AllTabsHash) || (lastFullTabLabelIndex != fullTabLabelIndex))
				fullTabLabelIndex = Math.Max(0, Math.Min(fullTabLabelIndex, GetAtRightIndex(tabLabelMap, Tabs.AllITabs.Count() - 1)));

			if ((lastFullAllTabsHash == Tabs.AllTabsHash) && (lastFullTabLabelIndex == fullTabLabelIndex))
			{
				fullTabLabels.ForEach(tabLabel => tabLabel.Refresh(Tabs));
				return;
			}

			fullTabLabelsPanel.Children.Clear();
			fullTabLabels = new List<TabLabel>();
			if (!Tabs.AllITabs.Any())
				return;

			var remaining = fullTabLabelsWidth;
			var index = (int)fullTabLabelIndex;
			var offset = fullTabLabelIndex - index;
			while (true)
			{
				if ((index >= Tabs.AllITabs.Count()) || (remaining <= 0))
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
			tabWindows[0].Tab = Tabs.FocusedITab;
			if (Tabs.FocusedITab == null)
			{
				if (lastFullFocused != null)
					fullContentGrid.Children.Clear();
			}
			else
			{
				if (lastFullFocused == null)
					fullContentGrid.Children.Add(tabWindows[0]);
			}

			tabWindows[0].DrawAll();
		}
	}
}
