using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI
{
	partial class TabsWindow
	{
		int gridColumns, gridRows;
		double gridWidth, gridHeight;
		List<TabLabel> gridTabLabels;

		bool lastGrid = false;
		Size lastGridSize;
		IReadOnlyList<ITab> lastGridAllTabs;
		ITab lastGridFocused;
		double lastGridScrollBarValue;
		WindowLayout lastWindowLayout;

		void ClearGridLayout()
		{
			if (!lastGrid)
				return;

			DisconnectTabWindows();
			canvas.Children.Clear();

			gridColumns = gridRows = 0;
			gridWidth = gridHeight = 0;
			gridTabLabels = null;

			lastGrid = false;
			lastGridSize = default;
			lastGridAllTabs = null;
			lastGridFocused = null;
			lastGridScrollBarValue = 0;
			lastWindowLayout = new WindowLayout();
		}

		void DisconnectTabWindows()
		{
			foreach (var tabWindow in tabWindows)
			{
				if (tabWindow.Tab == null)
					continue;
				(tabWindow.Parent as DockPanel).Children.Clear();
				tabWindow.Tab = null;
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
			lastGridAllTabs = renderParameters.AllTabs;
			lastGridFocused = renderParameters.FocusedTab;
			lastGridScrollBarValue = scrollBar.Value;
			lastWindowLayout = renderParameters.WindowLayout;
		}

		void CalculateGridParameters()
		{
			if ((lastGridAllTabs != renderParameters.AllTabs) || (lastWindowLayout != renderParameters.WindowLayout))
			{
				int? columns = null, rows = null;
				if (renderParameters.WindowLayout.Columns.HasValue)
					columns = Math.Max(1, renderParameters.WindowLayout.Columns.Value);
				if (renderParameters.WindowLayout.Rows.HasValue)
					rows = Math.Max(1, renderParameters.WindowLayout.Rows.Value);
				if ((!columns.HasValue) && (!rows.HasValue))
					columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(renderParameters.AllTabs.Count)), renderParameters.WindowLayout.MaxColumns ?? int.MaxValue));
				if (!rows.HasValue)
					rows = Math.Max(1, Math.Min((renderParameters.AllTabs.Count + columns.Value - 1) / columns.Value, renderParameters.WindowLayout.MaxRows ?? int.MaxValue));
				if (!columns.HasValue)
					columns = Math.Max(1, Math.Min((renderParameters.AllTabs.Count + rows.Value - 1) / rows.Value, renderParameters.WindowLayout.MaxColumns ?? int.MaxValue));

				gridColumns = columns.Value;
				gridRows = rows.Value;

				var totalRows = (renderParameters.AllTabs.Count + gridColumns - 1) / gridColumns;

				scrollBarBorder.Visibility = totalRows > gridRows ? Visibility.Visible : Visibility.Collapsed;
				UpdateLayout();

				gridWidth = canvas.ActualWidth / gridColumns;
				gridHeight = canvas.ActualHeight / gridRows;

				scrollBar.ViewportSize = gridRows;
				scrollBar.Maximum = totalRows - scrollBar.ViewportSize;

				lastGridAllTabs = null; // Make everything else calculate

				SetTabWindowCount(gridColumns * gridRows);
			}
		}

		void SetGridScrollPosition()
		{
			scrollBar.ValueChanged -= OnScrollBarValueChanged;

			if ((renderParameters.FocusedTab != null) && ((lastGridAllTabs != renderParameters.AllTabs) || (lastGridFocused != renderParameters.FocusedTab)))
			{
				var atTop = renderParameters.AllTabs.FindIndex(renderParameters.FocusedTab) / gridColumns * gridHeight;
				scrollBar.Value = Math.Min(atTop, Math.Max(scrollBar.Value, atTop + gridHeight - scrollBar.ViewportSize));
			}

			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));

			scrollBar.ValueChanged += OnScrollBarValueChanged;
		}

		void SetGridLayout()
		{
			if ((lastGridAllTabs == renderParameters.AllTabs) && (lastGridScrollBarValue == scrollBar.Value))
			{
				gridTabLabels.ForEach(tabLabel => tabLabel.Refresh(renderParameters));
				tabWindows.ForEach(tabWindow => tabWindow.DrawAll());
				return;
			}

			canvas.Children.Clear();
			gridTabLabels = new List<TabLabel>();
			DisconnectTabWindows();
			var tabWindowsIndex = 0;
			var tabIndex = (int)(scrollBar.Value + 0.5) * gridColumns;
			for (var row = 0; row < gridRows; ++row)
				for (var column = 0; column < gridColumns; ++column)
				{
					if (tabIndex >= renderParameters.AllTabs.Count)
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

					var tabWindow = tabWindows[tabWindowsIndex++];
					tabWindow.Tab = renderParameters.AllTabs.GetIndex(tabIndex++);
					var dockPanel = new DockPanel { AllowDrop = true };
					dockPanel.Drop += (s, e) => OnDrop(e, tabWindow.Tab);
					var tabLabel = CreateTabLabel(tabWindow.Tab);
					DockPanel.SetDock(tabLabel, Dock.Top);
					dockPanel.Children.Add(tabLabel);
					tabWindow.SetValue(DockPanel.DockProperty, Dock.Bottom);
					tabWindow.FocusVisualStyle = null;
					dockPanel.Children.Add(tabWindow);
					tabWindow.DrawAll();

					border.Child = dockPanel;

					border.Width = gridWidth;
					border.Height = gridHeight;
					canvas.Children.Add(border);

					gridTabLabels.Add(tabLabel);
				}
		}
	}
}
