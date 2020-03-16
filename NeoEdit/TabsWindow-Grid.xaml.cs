using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		int gridColumns, gridRows;
		double gridWidth, gridHeight;
		List<TabLabel> gridTabLabels;
		List<TabWindow> gridTabWindows;

		bool lastGrid = false;
		Size lastGridSize;
		IReadOnlyOrderedHashSet<Tab> lastGridAllTabs;
		Tab lastGridFocused;
		double lastGridScrollBarValue;
		int? lastGridColumns, lastGridRows, lastGridMaxColumns, lastGridMaxRows;

		void ClearGridLayout()
		{
			if (!lastGrid)
				return;

			canvas.Children.Clear();

			gridColumns = gridRows = 0;
			gridWidth = gridHeight = 0;
			gridTabLabels = null;
			gridTabWindows = null;

			lastGrid = false;
			lastGridSize = default;
			lastGridAllTabs = null;
			lastGridFocused = null;
			lastGridScrollBarValue = 0;
			lastGridColumns = lastGridRows = lastGridMaxColumns = lastGridMaxRows = null;
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
			lastGridAllTabs = Tabs.AllTabs;
			lastGridFocused = Tabs.Focused;
			lastGridScrollBarValue = scrollBar.Value;
			lastGridColumns = Tabs.Columns;
			lastGridRows = Tabs.Rows;
			lastGridMaxColumns = Tabs.MaxColumns;
			lastGridMaxRows = Tabs.MaxRows;
		}

		void CalculateGridParameters()
		{
			if ((lastGridAllTabs != Tabs.AllTabs) || (lastGridColumns != Tabs.Columns) || (lastGridRows != Tabs.Rows) || (lastGridMaxColumns != Tabs.MaxColumns) || (lastGridMaxRows != Tabs.MaxRows))
			{
				int? columns = null, rows = null;
				if (Tabs.Columns.HasValue)
					columns = Math.Max(1, Tabs.Columns.Value);
				if (Tabs.Rows.HasValue)
					rows = Math.Max(1, Tabs.Rows.Value);
				if ((!columns.HasValue) && (!rows.HasValue))
					columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Tabs.AllTabs.Count)), Tabs.MaxColumns ?? int.MaxValue));
				if (!rows.HasValue)
					rows = Math.Max(1, Math.Min((Tabs.AllTabs.Count + columns.Value - 1) / columns.Value, Tabs.MaxRows ?? int.MaxValue));
				if (!columns.HasValue)
					columns = Math.Max(1, Math.Min((Tabs.AllTabs.Count + rows.Value - 1) / rows.Value, Tabs.MaxColumns ?? int.MaxValue));

				gridColumns = columns.Value;
				gridRows = rows.Value;

				var totalRows = (Tabs.AllTabs.Count + gridColumns - 1) / gridColumns;

				scrollBarBorder.Visibility = totalRows > gridRows ? Visibility.Visible : Visibility.Collapsed;
				UpdateLayout();

				gridWidth = canvas.ActualWidth / gridColumns;
				gridHeight = canvas.ActualHeight / gridRows;

				scrollBar.ViewportSize = canvas.ActualHeight;
				scrollBar.Maximum = gridHeight * totalRows - canvas.ActualHeight;

				lastGridAllTabs = null; // Make everything else calculate
			}
		}

		void SetGridScrollPosition()
		{
			scrollBar.ValueChanged -= OnScrollBarValueChanged;

			if ((Tabs.Focused != null) && ((lastGridAllTabs != Tabs.AllTabs) || (lastGridFocused != Tabs.Focused)))
			{
				var atTop = Tabs.AllTabs.IndexOf(Tabs.Focused) / gridColumns * gridHeight;
				scrollBar.Value = Math.Min(atTop, Math.Max(scrollBar.Value, atTop + gridHeight - scrollBar.ViewportSize));
			}

			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));

			scrollBar.ValueChanged += OnScrollBarValueChanged;
		}

		void SetGridLayout()
		{
			if ((lastGridAllTabs == Tabs.AllTabs) && (lastGridScrollBarValue == scrollBar.Value))
			{
				gridTabLabels.ForEach(tabLabel => tabLabel.Refresh(Tabs));
				gridTabWindows.ForEach(tabWindow => tabWindow.DrawAll());
				return;
			}

			canvas.Children.Clear();
			gridTabLabels = new List<TabLabel>();
			gridTabWindows = new List<TabWindow>();
			for (var ctr = 0; ctr < Tabs.AllTabs.Count; ++ctr)
			{
				var top = ctr / gridColumns * gridHeight - scrollBar.Value;
				if ((top + gridHeight < 0) || (top > canvas.ActualHeight))
					continue;

				var border = new Border
				{
					BorderBrush = OutlineBrush,
					Background = BackgroundBrush,
					BorderThickness = new Thickness(2),
					CornerRadius = new CornerRadius(8)
				};
				Canvas.SetLeft(border, ctr % gridColumns * gridWidth);
				Canvas.SetTop(border, top);

				var tabWindow = new TabWindow(Tabs.AllTabs[ctr]);
				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, Tabs.AllTabs[ctr]);
				var tabLabel = CreateTabLabel(Tabs.AllTabs[ctr]);
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
				gridTabWindows.Add(tabWindow);
			}
		}
	}
}
