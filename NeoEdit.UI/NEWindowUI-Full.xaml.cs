using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI
{
	partial class NEWindowUI
	{
		StackPanel fullFileLabelsPanel;
		Grid fullContentGrid;
		double fullFileLabelIndex;
		double fullFileLabelsWidth;
		List<NEFileLabel> fullFileLabels;

		bool lastFull = false;
		Size lastFullSize;
		IReadOnlyList<INEFile> lastFullNEFiles;
		INEFile lastFullFocused;
		double lastFullFileLabelIndex;

		void ClearFullLayout()
		{
			if (!lastFull)
				return;

			canvas.Children.Clear();

			fullFileLabelsPanel = null;
			fullContentGrid.Children.Clear();
			fullContentGrid = null;
			neFileUIs[0].NEFile = null;
			fullFileLabelsWidth = 0;
			fullFileLabels = null;

			lastFull = false;
			lastFullSize = default;
			lastFullNEFiles = null;
			lastFullFocused = null;
			lastFullFileLabelIndex = 0;
		}

		void DoFullLayout()
		{
			ClearGridLayout();

			if (lastFullSize != canvas.RenderSize)
				ClearFullLayout();

			CreateFullLayout();
			CreateFullFileLabels();
			SetFullContent();

			lastFull = true;
			lastFullSize = canvas.RenderSize;
			lastFullNEFiles = renderParameters.NEFiles;
			lastFullFocused = renderParameters.FocusedFile;
			lastFullFileLabelIndex = fullFileLabelIndex;
		}

		private void CreateFullLayout()
		{
			if (lastFull)
				return;

			SetNEFileUICount(1);

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

			fullFileLabelsPanel = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true };
			Grid.SetRow(fullFileLabelsPanel, 0);
			Grid.SetColumn(fullFileLabelsPanel, 1);
			grid.Children.Add(fullFileLabelsPanel);

			var moveLeft = new RepeatButton { Content = "⮜", Margin = new Thickness(0, 0, 4, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveLeft.Click += (s, e) => { --fullFileLabelIndex; HandleCommand(new ExecuteState(NECommand.Internal_Redraw, Keyboard.Modifiers.ToModifiers())); };
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = "⮞", Margin = new Thickness(4, 0, 0, 0), Foreground = Brushes.White, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Focusable = false };
			moveRight.Click += (s, e) => { ++fullFileLabelIndex; HandleCommand(new ExecuteState(NECommand.Internal_Redraw, Keyboard.Modifiers.ToModifiers())); };
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

			fullFileLabelsWidth = canvas.ActualWidth - 40 - 2;
		}

		NEFileLabel GetFileLabel(Dictionary<int, NEFileLabel> fileLabelDict, int index)
		{
			if (fileLabelDict.ContainsKey(index))
				return fileLabelDict[index];

			var fileLabel = CreateFileLabel(renderParameters.NEFiles.GetIndex(index));
			fileLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			fileLabelDict[index] = fileLabel;
			return fileLabel;
		}

		double GetAtRightIndex(Dictionary<int, NEFileLabel> fileLabelDict, int index)
		{
			var remaining = fullFileLabelsWidth;
			var atRightIndex = 0d;
			while (index >= 0)
			{
				atRightIndex = index;
				var width = GetFileLabel(fileLabelDict, index).DesiredSize.Width;
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

		void CreateFullFileLabels()
		{
			var fileLabelMap = new Dictionary<int, NEFileLabel>();
			if ((renderParameters.FocusedFile != null) && ((lastFullNEFiles != renderParameters.NEFiles) || (lastFullFocused != renderParameters.FocusedFile)))
			{
				var atLeftIndex = renderParameters.NEFiles.FindIndex(renderParameters.FocusedFile);
				var atRightIndex = GetAtRightIndex(fileLabelMap, atLeftIndex);
				fullFileLabelIndex = Math.Min(atLeftIndex, Math.Max(fullFileLabelIndex, atRightIndex));
			}

			if ((lastFullNEFiles != renderParameters.NEFiles) || (lastFullFileLabelIndex != fullFileLabelIndex))
				fullFileLabelIndex = Math.Max(0, Math.Min(fullFileLabelIndex, GetAtRightIndex(fileLabelMap, renderParameters.NEFiles.Count - 1)));

			if ((lastFullNEFiles == renderParameters.NEFiles) && (lastFullFileLabelIndex == fullFileLabelIndex))
			{
				fullFileLabels.ForEach(fileLabel => fileLabel.Refresh(renderParameters));
				return;
			}

			fullFileLabelsPanel.Children.Clear();
			fullFileLabels = new List<NEFileLabel>();
			if (!renderParameters.NEFiles.Any())
				return;

			var remaining = fullFileLabelsWidth;
			var index = (int)fullFileLabelIndex;
			var offset = fullFileLabelIndex - index;
			while (true)
			{
				if ((index >= renderParameters.NEFiles.Count) || (remaining <= 0))
					break;

				var fileLabel = GetFileLabel(fileLabelMap, index);
				if (offset != 0)
				{
					fileLabel.Margin = new Thickness(-fileLabel.DesiredSize.Width * offset, 0, 0, 0);
					offset = 0;
				}
				fullFileLabels.Add(fileLabel);
				fullFileLabelsPanel.Children.Add(fileLabel);

				++index;
				remaining -= fileLabel.DesiredSize.Width;
			}
		}

		void SetFullContent()
		{
			neFileUIs[0].NEFile = renderParameters.FocusedFile;
			if (renderParameters.FocusedFile == null)
			{
				if (lastFullFocused != null)
					fullContentGrid.Children.Clear();
			}
			else
			{
				if (lastFullFocused == null)
					fullContentGrid.Children.Add(neFileUIs[0]);
			}

			neFileUIs[0].DrawAll();
		}
	}
}
