using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		int gridColumns, gridRows;
		double gridWidth, gridHeight;
		List<TabLabel> gridTabLabels;

		bool lastGrid = false;
		Size lastGridSize;
		int lastGridAllTabsHash;
		Tab lastGridFocused;
		double lastGridScrollBarValue;
		int? lastGridColumns, lastGridRows, lastGridMaxColumns, lastGridMaxRows;

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
			lastGridAllTabsHash = 0;
			lastGridFocused = null;
			lastGridScrollBarValue = 0;
			lastGridColumns = lastGridRows = lastGridMaxColumns = lastGridMaxRows = null;
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
			lastGridAllTabsHash = Tabs.AllTabsHash;
			lastGridFocused = Tabs.Focused;
			lastGridScrollBarValue = scrollBar.Value;
			lastGridColumns = Tabs.Columns;
			lastGridRows = Tabs.Rows;
			lastGridMaxColumns = Tabs.MaxColumns;
			lastGridMaxRows = Tabs.MaxRows;
		}

		void CalculateGridParameters()
		{
			if ((lastGridAllTabsHash != Tabs.AllTabsHash) || (lastGridColumns != Tabs.Columns) || (lastGridRows != Tabs.Rows) || (lastGridMaxColumns != Tabs.MaxColumns) || (lastGridMaxRows != Tabs.MaxRows))
			{
				int? columns = null, rows = null;
				if (Tabs.Columns.HasValue)
					columns = Math.Max(1, Tabs.Columns.Value);
				if (Tabs.Rows.HasValue)
					rows = Math.Max(1, Tabs.Rows.Value);
				if ((!columns.HasValue) && (!rows.HasValue))
					columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Tabs.AllTabs.Count())), Tabs.MaxColumns ?? int.MaxValue));
				if (!rows.HasValue)
					rows = Math.Max(1, Math.Min((Tabs.AllTabs.Count() + columns.Value - 1) / columns.Value, Tabs.MaxRows ?? int.MaxValue));
				if (!columns.HasValue)
					columns = Math.Max(1, Math.Min((Tabs.AllTabs.Count() + rows.Value - 1) / rows.Value, Tabs.MaxColumns ?? int.MaxValue));

				gridColumns = columns.Value;
				gridRows = rows.Value;

				var totalRows = (Tabs.AllTabs.Count() + gridColumns - 1) / gridColumns;

				scrollBarBorder.Visibility = totalRows > gridRows ? Visibility.Visible : Visibility.Collapsed;
				UpdateLayout();

				gridWidth = canvas.ActualWidth / gridColumns;
				gridHeight = canvas.ActualHeight / gridRows;

				scrollBar.ViewportSize = gridRows;
				scrollBar.Maximum = totalRows - scrollBar.ViewportSize;

				lastGridAllTabsHash = 0; // Make everything else calculate

				SetTabWindowCount(gridColumns * gridRows);
			}
		}

		void SetGridScrollPosition()
		{
			scrollBar.ValueChanged -= OnScrollBarValueChanged;

			if ((Tabs.Focused != null) && ((lastGridAllTabsHash != Tabs.AllTabsHash) || (lastGridFocused != Tabs.Focused)))
			{
				var atTop = Tabs.AllTabs.FindIndex(Tabs.Focused) / gridColumns * gridHeight;
				scrollBar.Value = Math.Min(atTop, Math.Max(scrollBar.Value, atTop + gridHeight - scrollBar.ViewportSize));
			}

			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));

			scrollBar.ValueChanged += OnScrollBarValueChanged;
		}

		void SetGridLayout()
		{
			if ((lastGridAllTabsHash == Tabs.AllTabsHash) && (lastGridScrollBarValue == scrollBar.Value))
			{
				gridTabLabels.ForEach(tabLabel => tabLabel.Refresh(Tabs));
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
					if (tabIndex >= Tabs.AllTabs.Count())
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
					tabWindow.Tab = Tabs.AllTabs.GetIndex(tabIndex++);
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
