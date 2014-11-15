using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	partial class Chart
	{
		List<Tuple<string, string, long>> data;
		List<Brush> brushes;
		public Chart(List<Tuple<string, string, long>> _data)
		{
			InitializeComponent();
			data = _data;

			var random = new Random(1);
			var brushInfo = typeof(Brushes).GetProperties();
			brushes = Enumerable.Range(0, brushInfo.Length).Select(ctr => brushInfo[ctr].GetValue(null, null) as Brush).OrderBy(brush => random.Next()).ToList();
			while (brushes.Count < data.Count)
				brushes.AddRange(brushes);
			brushes = brushes.Take(data.Count).ToList();

			for (var ctr = 0; ctr < data.Count; ++ctr)
				legend.Children.Add(GetKey(data[ctr].Item1, brushes[ctr], data[ctr].Item3));

			UIHelper<Chart>.AddCallback(scroller, ScrollViewer.ViewportWidthProperty, () => RecalculateLegend());
			UIHelper<Chart>.AddCallback(scroller, ScrollViewer.ViewportHeightProperty, () => RecalculateLegend());
			UIHelper<Chart>.AddCallback(pieChart, ActualWidthProperty, () => RecalculatePieChart());
			UIHelper<Chart>.AddCallback(pieChart, ActualHeightProperty, () => RecalculatePieChart());
		}

		Point GetPoint(Point center, double radius, double angle)
		{
			return center + new Vector(Math.Sin(angle), -Math.Cos(angle)) * radius;
		}

		string GetSize(long size)
		{
			if (size < 1024)
				return size.ToString();

			double mySize = size;
			var suffix = new string[] { "", "KB", "MB", "GB", "TB", "PB", "EB" };
			var useSuffix = 0;
			while (mySize >= 1024)
			{
				mySize /= 1024;
				++useSuffix;
			}
			return String.Format("{0:0.0}{1}", mySize, suffix[useSuffix]);
		}

		Path GetArc(Point center, double radius, double startAngle, double endAngle, Brush fillBrush, string name, long size)
		{
			var pathGeometry = new PathGeometry();
			pathGeometry.Figures.Add(new PathFigure(center, new List<PathSegment>
			{
				new LineSegment { Point = GetPoint(center, radius, startAngle) },
				new ArcSegment { Size = new Size(radius, radius), Point = GetPoint(center, radius, endAngle), SweepDirection = SweepDirection.Clockwise, IsLargeArc = (endAngle - startAngle) > Math.PI },
			}, true));
			return new Path { Fill = fillBrush, Stroke = Brushes.Black, Data = pathGeometry, ToolTip = name + ": " + GetSize(size) };
		}

		Grid GetKey(string name, Brush brush, long size)
		{
			var key = new Grid { ToolTip = GetSize(size) };
			key.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
			key.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
			key.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
			key.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });

			var keyRect = new Rectangle { Width = 16, Height = 16, Fill = brush };
			Grid.SetRow(keyRect, 0);
			Grid.SetColumn(keyRect, 0);
			key.Children.Add(keyRect);

			var keyText = new TextBlock { Text = name };
			Grid.SetRow(keyText, 0);
			Grid.SetColumn(keyText, 2);
			key.Children.Add(keyText);

			return key;
		}

		int oldRows = -1;
		public void RecalculateLegend()
		{
			if (data.Count < 2)
				return;

			double rowSpacing = 18;
			var rows = Math.Max(1, (int)(scroller.ViewportHeight / rowSpacing));
			var columns = (data.Count + rows - 1) / rows;

			if (rows == oldRows)
				return;
			oldRows = rows;

			legend.RowDefinitions.Clear();
			for (var row = 0; row < rows; ++row)
				legend.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowSpacing) });

			legend.ColumnDefinitions.Clear();
			for (var column = 0; column < columns; ++column)
			{
				if (column != 0)
					legend.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
				legend.ColumnDefinitions.Add(new ColumnDefinition());
			}

			for (var ctr = 0; ctr < data.Count; ++ctr)
			{
				Grid.SetColumn(legend.Children[ctr], ctr / rows * 2);
				Grid.SetRow(legend.Children[ctr], ctr % rows);
			}
		}

		public void RecalculatePieChart()
		{
			if (data.Count < 2)
				return;

			pieChart.Children.Clear();

			var radius = Math.Min(pieChart.ActualWidth, pieChart.ActualHeight) / 2;
			if (radius <= 0)
				return;
			var center = new Point(radius, radius);

			double start = 0;
			var total = data.Sum(num => num.Item3);
			for (var ctr = 0; ctr < data.Count; ++ctr)
			{
				var item = data[ctr];
				var key = legend.Children[ctr] as FrameworkElement;
				var brush = brushes[ctr];

				var end = start + (double)item.Item3 / total * 2 * Math.PI;
				var arc = GetArc(center, radius, start, end, brush, item.Item1, item.Item3);
				start = end;
				pieChart.Children.Add(arc);

				var style = new Style();
				style.Setters.Add(new Setter { Property = Path.RenderTransformProperty, Value = null });
				var trigger = new DataTrigger { Binding = new Binding("IsMouseOver") { Source = arc }, Value = true };
				trigger.Setters.Add(new Setter { Property = Path.RenderTransformProperty, Value = new ScaleTransform(1.05, 1.05, center.X, center.Y) });
				style.Triggers.Add(trigger);
				trigger = new DataTrigger { Binding = new Binding("IsMouseOver") { Source = key }, Value = true };
				trigger.Setters.Add(new Setter { Property = Path.RenderTransformProperty, Value = new ScaleTransform(1.05, 1.05, center.X, center.Y) });
				style.Triggers.Add(trigger);
				arc.Style = style;

				style = new Style();
				style.Setters.Add(new Setter { Property = Canvas.BackgroundProperty, Value = null });
				trigger = new DataTrigger { Binding = new Binding("IsMouseOver") { Source = arc }, Value = true };
				trigger.Setters.Add(new Setter { Property = Canvas.BackgroundProperty, Value = Brushes.LightGray });
				style.Triggers.Add(trigger);
				trigger = new DataTrigger { Binding = new Binding("IsMouseOver") { Source = key }, Value = true };
				trigger.Setters.Add(new Setter { Property = Canvas.BackgroundProperty, Value = Brushes.LightGray });
				style.Triggers.Add(trigger);
				key.Style = style;
			}
		}
	}
}
