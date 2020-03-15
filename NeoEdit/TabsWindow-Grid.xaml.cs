using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		void ClearGridLayout()
		{
		}

		void DoGridLayout(bool setFocus)
		{
			ClearFullLayout();

			canvas.Children.Clear();
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

			var totalRows = (Tabs.AllTabs.Count + columns.Value - 1) / columns.Value;

			scrollBar.Visibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			UpdateLayout();

			var width = canvas.ActualWidth / columns.Value;
			var height = canvas.ActualHeight / rows.Value;

			scrollBar.ViewportSize = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - canvas.ActualHeight;
			scrollBar.ValueChanged -= OnScrollBarValueChanged;
			if ((setFocus) && (Tabs.Focused != null))
			{
				var index = Tabs.AllTabs.Indexes(tab => tab == Tabs.Focused).DefaultIfEmpty(-1).First();
				if (index != -1)
				{
					var top = index / columns.Value * height;
					scrollBar.Value = Math.Min(top, Math.Max(scrollBar.Value, top + height - scrollBar.ViewportSize));
				}
			}
			scrollBar.Value = Math.Max(0, Math.Min(scrollBar.Value, scrollBar.Maximum));
			scrollBar.ValueChanged += OnScrollBarValueChanged;

			for (var ctr = 0; ctr < Tabs.AllTabs.Count; ++ctr)
			{
				var top = ctr / columns.Value * height - scrollBar.Value;
				if ((top + height < 0) || (top > canvas.ActualHeight))
					continue;

				var border = new Border
				{
					BorderBrush = OutlineBrush,
					Background = BackgroundBrush,
					BorderThickness = new Thickness(2),
					CornerRadius = new CornerRadius(8)
				};
				Canvas.SetLeft(border, ctr % columns.Value * width);
				Canvas.SetTop(border, top);

				var tabWindow = new TabWindow(Tabs.AllTabs[ctr]);
				var dockPanel = new DockPanel { AllowDrop = true };
				dockPanel.Drop += (s, e) => OnDrop(e, Tabs.AllTabs[ctr]);
				var tabLabel = GetTabLabel(Tabs.AllTabs[ctr]);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					tabWindow.SetValue(DockPanel.DockProperty, Dock.Bottom);
					tabWindow.FocusVisualStyle = null;
					dockPanel.Children.Add(tabWindow);
				}
				tabWindow.DrawAll();

				border.Child = dockPanel;

				border.Width = width;
				border.Height = height;
				canvas.Children.Add(border);
			}
		}
	}
}
