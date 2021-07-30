using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI
{
	partial class NEWindowUI
	{
		int gridColumns, gridRows;
		double gridWidth, gridHeight;
		List<NEFileLabel> gridFileLabels;

		bool lastGrid = false;
		Size lastGridSize;
		IReadOnlyList<INEFile> lastGridNEFiles;
		INEFile lastGridFocused;
		double lastGridScrollBarValue;
		WindowLayout lastWindowLayout;

		void ClearGridLayout()
		{
			if (!lastGrid)
				return;

			DisconnectNEFileUIs();
			canvas.Children.Clear();

			gridColumns = gridRows = 0;
			gridWidth = gridHeight = 0;
			gridFileLabels = null;

			lastGrid = false;
			lastGridSize = default;
			lastGridNEFiles = null;
			lastGridFocused = null;
			lastGridScrollBarValue = 0;
			lastWindowLayout = new WindowLayout();
		}

		void DisconnectNEFileUIs()
		{
			foreach (var neFileUI in neFileUIs)
			{
				if (neFileUI.NEFile == null)
					continue;
				(neFileUI.Parent as DockPanel).Children.Clear();
				neFileUI.NEFile = null;
			}
		}

		void DoGridLayout()
		{
			ClearFullLayout();

			if (lastGridSize != canvas.RenderSize)
				ClearGridLayout();

			CalculateGridParameters();
			SetGridScrollPosition();
			SetGridLayout();

			lastGrid = true;
			lastGridSize = canvas.RenderSize;
			lastGridNEFiles = renderParameters.NEFiles;
			lastGridFocused = renderParameters.FocusedFile;
			lastGridScrollBarValue = scrollBar.Value;
			lastWindowLayout = renderParameters.WindowLayout;
		}

		void CalculateGridParameters()
		{
			if ((lastGridNEFiles != renderParameters.NEFiles) || (lastWindowLayout != renderParameters.WindowLayout))
			{
				int? columns = null, rows = null;
				if (renderParameters.WindowLayout.Columns.HasValue)
					columns = Math.Max(1, renderParameters.WindowLayout.Columns.Value);
				if (renderParameters.WindowLayout.Rows.HasValue)
					rows = Math.Max(1, renderParameters.WindowLayout.Rows.Value);
				if ((!columns.HasValue) && (!rows.HasValue))
					columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(renderParameters.NEFiles.Count)), renderParameters.WindowLayout.MaxColumns ?? int.MaxValue));
				if (!rows.HasValue)
					rows = Math.Max(1, Math.Min((renderParameters.NEFiles.Count + columns.Value - 1) / columns.Value, renderParameters.WindowLayout.MaxRows ?? int.MaxValue));
				if (!columns.HasValue)
					columns = Math.Max(1, Math.Min((renderParameters.NEFiles.Count + rows.Value - 1) / rows.Value, renderParameters.WindowLayout.MaxColumns ?? int.MaxValue));

				gridColumns = columns.Value;
				gridRows = rows.Value;

				var totalRows = (renderParameters.NEFiles.Count + gridColumns - 1) / gridColumns;

				scrollBarBorder.Visibility = totalRows > gridRows ? Visibility.Visible : Visibility.Collapsed;
				UpdateLayout();

				gridWidth = canvas.ActualWidth / gridColumns;
				gridHeight = canvas.ActualHeight / gridRows;

				scrollBar.ViewportSize = gridRows;
				scrollBar.Maximum = totalRows - scrollBar.ViewportSize;

				lastGridNEFiles = null; // Make everything else calculate

				SetNEFileUICount(gridColumns * gridRows);
			}
		}

		void SetGridScrollPosition()
		{
			scrollBar.ValueChanged -= OnScrollBarValueChanged;

			if ((renderParameters.FocusedFile != null) && ((lastGridNEFiles != renderParameters.NEFiles) || (lastGridFocused != renderParameters.FocusedFile)))
			{
				var atTop = renderParameters.NEFiles.FindIndex(renderParameters.FocusedFile) / gridColumns;
				scrollBar.Value = Math.Min(atTop, Math.Max(scrollBar.Value, atTop + 1 - scrollBar.ViewportSize));
			}

			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));

			scrollBar.ValueChanged += OnScrollBarValueChanged;
		}

		void SetGridLayout()
		{
			if ((lastGridNEFiles == renderParameters.NEFiles) && (lastGridScrollBarValue == scrollBar.Value))
			{
				gridFileLabels.ForEach(fileLabel => fileLabel.Refresh(renderParameters));
				neFileUIs.ForEach(neFileUI => neFileUI.DrawAll());
				return;
			}

			canvas.Children.Clear();
			gridFileLabels = new List<NEFileLabel>();
			DisconnectNEFileUIs();
			var neFileUIIndex = 0;
			var fileIndex = (int)(scrollBar.Value + 0.5) * gridColumns;
			for (var row = 0; row < gridRows; ++row)
				for (var column = 0; column < gridColumns; ++column)
				{
					if (fileIndex >= renderParameters.NEFiles.Count)
						break;

					var top = row * gridHeight;

					var border = new Border
					{
						BorderBrush = OutlineBrush,
						Background = BackgroundBrush,
						BorderThickness = new Thickness(2),
						CornerRadius = new CornerRadius(8)
					};
					Canvas.SetLeft(border, column * gridWidth);
					Canvas.SetTop(border, top);

					var neFileUI = neFileUIs[neFileUIIndex++];
					neFileUI.NEFile = renderParameters.NEFiles.GetIndex(fileIndex++);
					var dockPanel = new DockPanel { AllowDrop = true };
					dockPanel.Drop += (s, e) => OnDrop(e, neFileUI.NEFile);
					var fileLabel = CreateFileLabel(neFileUI.NEFile);
					DockPanel.SetDock(fileLabel, Dock.Top);
					dockPanel.Children.Add(fileLabel);
					neFileUI.SetValue(DockPanel.DockProperty, Dock.Bottom);
					neFileUI.FocusVisualStyle = null;
					dockPanel.Children.Add(neFileUI);
					neFileUI.DrawAll();

					border.Child = dockPanel;

					border.Width = gridWidth;
					border.Height = gridHeight;
					canvas.Children.Add(border);

					gridFileLabels.Add(fileLabel);
				}
		}
	}
}
