﻿using System;
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
		IReadOnlyList<ITab> lastFullAllTabs;
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
			lastFullAllTabs = null;
			lastFullFocused = null;
			lastFullTabLabelIndex = 0;
		}

		void DoFullLayout(RenderParameters renderParameters)
		{
			ClearGridLayout();

			if (lastFullSize != canvas.RenderSize)
				ClearFullLayout();

			CreateFullLayout();
			CreateFullTabLabels(renderParameters);
			SetFullContent(renderParameters);

			lastFull = true;
			lastFullSize = canvas.RenderSize;
			lastFullAllTabs = renderParameters.AllTabs;
			lastFullFocused = renderParameters.FocusedTab;
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
			moveLeft.Click += (s, e) => { --fullTabLabelIndex; HandleCommand(new ExecuteState(NECommand.Internal_Redraw)); };
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveRight.Click += (s, e) => { ++fullTabLabelIndex; HandleCommand(new ExecuteState(NECommand.Internal_Redraw)); };
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

		TabLabel GetTabLabel(RenderParameters renderParameters, Dictionary<int, TabLabel> tabLabelDict, int index)
		{
			if (tabLabelDict.ContainsKey(index))
				return tabLabelDict[index];

			var tabLabel = CreateTabLabel(renderParameters.AllTabs.GetIndex(index), renderParameters);
			tabLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			tabLabelDict[index] = tabLabel;
			return tabLabel;
		}

		double GetAtRightIndex(RenderParameters renderParameters, Dictionary<int, TabLabel> tabLabelDict, int index)
		{
			var remaining = fullTabLabelsWidth;
			var atRightIndex = 0d;
			while (index >= 0)
			{
				atRightIndex = index;
				var width = GetTabLabel(renderParameters, tabLabelDict, index).DesiredSize.Width;
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

		void CreateFullTabLabels(RenderParameters renderParameters)
		{
			var tabLabelMap = new Dictionary<int, TabLabel>();
			if ((renderParameters.FocusedTab != null) && ((lastFullAllTabs != renderParameters.AllTabs) || (lastFullFocused != renderParameters.FocusedTab)))
			{
				var atLeftIndex = renderParameters.AllTabs.FindIndex(renderParameters.FocusedTab);
				var atRightIndex = GetAtRightIndex(renderParameters, tabLabelMap, atLeftIndex);
				fullTabLabelIndex = Math.Min(atLeftIndex, Math.Max(fullTabLabelIndex, atRightIndex));
			}

			if ((lastFullAllTabs != renderParameters.AllTabs) || (lastFullTabLabelIndex != fullTabLabelIndex))
				fullTabLabelIndex = Math.Max(0, Math.Min(fullTabLabelIndex, GetAtRightIndex(renderParameters, tabLabelMap, renderParameters.AllTabs.Count - 1)));

			if ((lastFullAllTabs == renderParameters.AllTabs) && (lastFullTabLabelIndex == fullTabLabelIndex))
			{
				fullTabLabels.ForEach(tabLabel => tabLabel.Refresh(renderParameters.ActiveTabs, renderParameters.FocusedTab));
				return;
			}

			fullTabLabelsPanel.Children.Clear();
			fullTabLabels = new List<TabLabel>();
			if (!renderParameters.AllTabs.Any())
				return;

			var remaining = fullTabLabelsWidth;
			var index = (int)fullTabLabelIndex;
			var offset = fullTabLabelIndex - index;
			while (true)
			{
				if ((index >= renderParameters.AllTabs.Count) || (remaining <= 0))
					break;

				var tabLabel = GetTabLabel(renderParameters, tabLabelMap, index);
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

		void SetFullContent(RenderParameters renderParameters)
		{
			tabWindows[0].Tab = renderParameters.FocusedTab;
			if (renderParameters.FocusedTab == null)
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
