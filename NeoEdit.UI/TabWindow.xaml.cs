using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.UI.Controls;
using NeoEdit.UI.Highlighting;

namespace NeoEdit.UI
{
	partial class TabWindow
	{
		static internal readonly Brush caretBrush = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255));
		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(96, 38, 132, 255));
		static internal readonly Pen selectionPen = new Pen(new SolidColorBrush(Color.FromArgb(96, 38, 132, 255)), 2);
		static internal readonly Dictionary<int, Pen> regionPen = new Dictionary<int, Pen>
		{
			[1] = new Pen(new SolidColorBrush(Color.FromRgb(248, 118, 109)), 2),
			[2] = new Pen(new SolidColorBrush(Color.FromRgb(124, 174, 0)), 2),
			[3] = new Pen(new SolidColorBrush(Color.FromRgb(0, 191, 196)), 2),
			[4] = new Pen(new SolidColorBrush(Color.FromRgb(199, 124, 255)), 2),
			[5] = new Pen(new SolidColorBrush(Color.FromRgb(245, 53, 139)), 2),
			[6] = new Pen(new SolidColorBrush(Color.FromRgb(238, 138, 98)), 2),
			[7] = new Pen(new SolidColorBrush(Color.FromRgb(182, 62, 95)), 2),
			[8] = new Pen(new SolidColorBrush(Color.FromRgb(105, 47, 111)), 2),
			[9] = new Pen(new SolidColorBrush(Color.FromRgb(237, 223, 184)), 2),
		};
		static internal readonly Brush diffLineBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Pen diffLinePen = new Pen(new SolidColorBrush(Color.FromRgb(120, 102, 3)), 2);
		static internal readonly Brush diffColBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Brush highlightRowBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
		static internal readonly Pen lightlightRowPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);

		static TabWindow()
		{
			caretBrush.Freeze();
			selectionBrush.Freeze();
			selectionPen.Freeze();
			regionPen.Values.ForEach(brush => brush.Freeze());
			diffLineBrush.Freeze();
			diffLinePen.Freeze();
			diffColBrush.Freeze();
			highlightRowBrush.Freeze();
			lightlightRowPen.Freeze();
		}

		public TabsWindow TabsWindow { get; }
		public ITab Tab { get; set; }

		const double Spacing = 2;
		static double LineHeight => Font.FontSize + Spacing;

		internal TabWindow(TabsWindow tabsWindow)
		{
			TabsWindow = tabsWindow;
			EnhancedFocusManager.SetIsEnhancedFocusScope(this, true);
			InitializeComponent();
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			//TODO Drop += OnDrop;
		}

		void OnMouseWheel(object sender, MouseWheelEventArgs e) => yScroll.Value -= e.Delta / 40;

		void ScrollChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => TabsWindow.HandleCommand(new ExecuteState(NECommand.Internal_Scroll) { Configuration = (Tab, (int)xScroll.Value, (int)yScroll.Value) });

		public void DrawAll()
		{
			if (Tab == null)
				return;

			SetupViewBinary();
			canvas.InvalidateVisual();
			statusBar.InvalidateVisual();
		}

		void SetScrollBarsParameters()
		{
			xScroll.ValueChanged -= ScrollChanged;
			yScroll.ValueChanged -= ScrollChanged;

			xScroll.ViewportSize = canvas.ActualWidth / Font.CharWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Tab.MaxColumn - Math.Floor(xScroll.ViewportSize);
			xScroll.SmallChange = 1;
			xScroll.Value = Math.Max(xScroll.Minimum, Math.Min(Tab.StartColumn, xScroll.Maximum));

			yScroll.ViewportSize = canvas.ActualHeight / LineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Tab.NumLines - Math.Floor(yScroll.ViewportSize);
			yScroll.SmallChange = 1;
			yScroll.Value = Math.Max(yScroll.Minimum, Math.Min(Tab.StartRow, yScroll.Maximum));

			//TODO yScroll.DiffList = DataQwer.GetDiffRanges();

			xScroll.ValueChanged += ScrollChanged;
			yScroll.ValueChanged += ScrollChanged;
		}

		void SetupViewBinary()
		{
			if (!Tab.ViewBinary)
			{
				viewBinaryControl.Visibility = Visibility.Collapsed;
				return;
			}

			viewBinaryControl.Visibility = Visibility.Visible;
			Tab.GetViewBinaryData(out var data, out var hasSel);
			viewBinary.SetData(data, hasSel, Tab.ViewBinaryCodePages, Tab.ViewBinarySearches);
		}

		class DrawBounds
		{
			public int StartLine { get; set; }
			public int EndLine { get; set; }
			public int StartColumn { get; set; }
			public int EndColumn { get; set; }
			Dictionary<int, Range> lineRanges;
			public Dictionary<int, Range> LineRanges
			{
				get => lineRanges; set
				{
					lineRanges = value;
					ScreenStart = lineRanges.First().Value.Start;
					ScreenEnd = lineRanges.Last().Value.End + 1;
				}
			}
			public int ScreenStart { get; private set; }
			public int ScreenEnd { get; private set; }
			public Dictionary<int, int> StartIndexes { get; set; }
			public Dictionary<int, int> EndIndexes { get; set; }
			public double X(int column) => (column - StartColumn) * Font.CharWidth;
			public double Y(int line) => (line - StartLine) * LineHeight;
		}

		DrawBounds GetDrawBounds()
		{
			var drawBounds = new DrawBounds();
			drawBounds.StartLine = (int)yScroll.Value;
			drawBounds.EndLine = Math.Min(Tab.NumLines, drawBounds.StartLine + (int)Math.Ceiling(canvas.ActualHeight / LineHeight));
			drawBounds.StartColumn = (int)xScroll.Value;
			drawBounds.EndColumn = Math.Min(Tab.MaxColumn + 1, drawBounds.StartColumn + (int)Math.Ceiling(canvas.ActualWidth / Font.CharWidth));

			var lines = Enumerable.Range(drawBounds.StartLine, drawBounds.EndLine - drawBounds.StartLine);
			drawBounds.LineRanges = lines.ToDictionary(line => line, line => new Range(Tab.GetPosition(line, 0), Tab.GetPosition(line, Tab.GetLineLength(line) + 1)));
			drawBounds.StartIndexes = lines.ToDictionary(line => line, line => Tab.GetIndexFromColumn(line, drawBounds.StartColumn, true));
			drawBounds.EndIndexes = lines.ToDictionary(line => line, line => Tab.GetIndexFromColumn(line, drawBounds.EndColumn, true));
			return drawBounds;
		}

		void RenderCarets(DrawingContext dc, DrawBounds drawBounds)
		{
			for (var selectionCtr = 0; selectionCtr < Tab.Selections.Count; ++selectionCtr)
			{
				var range = Tab.Selections[selectionCtr];

				if ((range.End < drawBounds.ScreenStart) || (range.Start > drawBounds.ScreenEnd))
					continue;

				var startLine = Tab.GetPositionLine(range.Start);
				var endLine = Tab.GetPositionLine(range.End);
				var cursorLine = range.Cursor == range.Start ? startLine : endLine;
				startLine = Math.Max(drawBounds.StartLine, startLine);
				endLine = Math.Min(drawBounds.EndLine, endLine + 1);

				if ((cursorLine < startLine) || (cursorLine >= endLine))
					continue;

				if (selectionCtr == Tab.CurrentSelection)
					dc.DrawRoundedRectangle(highlightRowBrush, lightlightRowPen, new Rect(-2, drawBounds.Y(cursorLine), canvas.ActualWidth + 4, Font.FontSize), 4, 4);

				var cursor = Tab.GetPositionIndex(range.Cursor, cursorLine);
				if ((cursor >= drawBounds.StartIndexes[cursorLine]) && (cursor <= drawBounds.EndIndexes[cursorLine]))
				{
					cursor = Tab.GetColumnFromIndex(cursorLine, cursor);
					for (var pass = selectionCtr == Tab.CurrentSelection ? 2 : 1; pass > 0; --pass)
						dc.DrawRectangle(caretBrush, null, new Rect(drawBounds.X(cursor) - 1, drawBounds.Y(cursorLine), 2, LineHeight));
				}
			}
		}

		void RenderIndicators(DrawingContext dc, DrawBounds drawBounds, Range visibleCursor, IReadOnlyList<Range> ranges, Brush brush, Pen pen, double leftSpacing, double rightSpacing)
		{
			var radius = Math.Min(4, Font.FontSize / 2 - 1);

			foreach (var range in ranges)
			{
				if ((range.End < drawBounds.ScreenStart) || (range.Start > drawBounds.ScreenEnd))
					continue;

				var points = GetIndicatorPoints(range, drawBounds, leftSpacing, rightSpacing);
				var geometry = CreateIndicatorGeometry(points, radius, brush != null);
				for (var pass = range == visibleCursor ? 2 : 1; pass > 0; --pass)
					dc.DrawGeometry(brush, pen, geometry);
			}
		}

		List<Point> GetIndicatorPoints(Range range, DrawBounds drawBounds, double leftSpacing, double rightSpacing)
		{
			var startLine = Tab.GetPositionLine(range.Start);
			var startColumn = Tab.GetColumnFromIndex(startLine, Tab.GetPositionIndex(range.Start, startLine));

			var endLine = Tab.GetPositionLine(range.End);
			var endColumn = Tab.GetColumnFromIndex(endLine, Tab.GetPositionIndex(range.End, endLine));
			if ((endLine != startLine) && (endColumn == 0))
			{
				--endLine;
				endColumn = Tab.GetLineColumnsLength(endLine) + 1;
			}

			var points = new List<Point>();

			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine) + Font.FontSize / 2));
			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine)));
			for (var line = startLine; ; ++line)
			{
				var done = line == endLine;
				if ((line >= drawBounds.StartLine - 1) && ((line < drawBounds.EndLine)))
				{
					var length = done ? endColumn : Tab.GetLineColumnsLength(line) + 1;
					points.Add(new Point(drawBounds.X(length) + rightSpacing, drawBounds.Y(line)));
					points.Add(new Point(drawBounds.X(length) + rightSpacing, drawBounds.Y(line) + LineHeight));
				}
				if (done)
					break;
			}
			if (endLine != startLine)
			{
				points.Add(new Point(leftSpacing, points[points.Count - 1].Y));
				points.Add(new Point(leftSpacing, drawBounds.Y(startLine) + LineHeight));
			}
			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, points[points.Count - 1].Y));
			points.Add(points[0]);
			return points;
		}

		static StreamGeometry CreateIndicatorGeometry(List<Point> points, double radius, bool fill)
		{
			int CompareValue(double v1, double v2)
			{
				var result = v1.CompareTo(v2);
				if (result != 0)
					result /= Math.Abs(result);
				return result;
			}

			var geometry = new StreamGeometry();
			using (var ctx = geometry.Open())
			{
				for (var ctr = 0; ctr < points.Count - 1; ++ctr)
				{
					if (ctr == 0)
					{
						ctx.BeginFigure(points[ctr], fill, true);
						continue;
					}

					var prevX = CompareValue(points[ctr - 1].X, points[ctr].X);
					var prevY = CompareValue(points[ctr - 1].Y, points[ctr].Y);
					var nextX = CompareValue(points[ctr].X, points[ctr + 1].X);
					var nextY = CompareValue(points[ctr].Y, points[ctr + 1].Y);

					if (((prevX == 0) != (prevY == 0)) && ((nextX == 0) != (nextY == 0)) && ((prevX == 0) != (nextX == 0)))
					{
						ctx.LineTo(points[ctr] + new Vector(prevX * radius, prevY * radius), true, true);
						ctx.ArcTo(points[ctr] + new Vector(-nextX * radius, -nextY * radius), new Size(radius, radius), 0, false, (prevX == 0 ? prevY != nextX : prevX == nextY) ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true, true);
					}
					else
						ctx.LineTo(points[ctr], true, true);
				}
			}
			geometry.Freeze();
			return geometry;
		}

		void RenderDiff(DrawingContext dc, DrawBounds drawBounds)
		{
			//int? startDiff = null;
			//for (var line = drawBounds.StartLine; ; ++line)
			//{
			//	var done = line == drawBounds.EndLine;

			//	var matchType = done ? TextData.DiffType.Match : DataQwer.GetLineDiffType(line);
			//	if (matchType != TextData.DiffType.Match)
			//	{
			//		startDiff = startDiff ?? line;

			//		if (!matchType.HasFlag(TextData.DiffType.HasGap))
			//		{
			//			startDiff = startDiff ?? line;

			//			var map = DataQwer.GetLineColumnMap(line, true);
			//			foreach (var tuple in DataQwer.GetLineColumnDiffs(line))
			//			{
			//				var start = map[tuple.Item1];
			//				var end = map[tuple.Item2];
			//				if (end >= start)
			//					dc.DrawRectangle(diffColBrush, null, new Rect(drawBounds.X(start) - 1, drawBounds.Y(line), (end - start) * Font.CharWidth + 2, Font.FontSize));
			//			}
			//		}
			//	}

			//	if ((startDiff.HasValue) && (matchType == TextData.DiffType.Match))
			//	{
			//		dc.DrawRoundedRectangle(diffLineBrush, diffLinePen, new Rect(-2, drawBounds.Y(startDiff.Value), canvas.ActualWidth + 4, drawBounds.Y(line) - drawBounds.Y(startDiff.Value) - Spacing + 1), 4, 4);
			//		startDiff = null;
			//	}

			//	if (done)
			//		break;
			//}
		}

		void RenderText(DrawingContext dc, DrawBounds drawBounds)
		{
			const int HighlightRegexSize = 500;

			var highlightDictionary = Tab.HighlightSyntax ? Highlight.Get(Tab.ContentType)?.GetDictionary() : null;

			var startColumn = Math.Max(drawBounds.StartColumn - HighlightRegexSize, 0);
			var startOffset = drawBounds.StartColumn - startColumn;
			var endColumn = drawBounds.EndColumn + HighlightRegexSize;
			for (var line = drawBounds.StartLine; line < drawBounds.EndLine; ++line)
			{
				var lineColumns = Tab.GetLineColumns(line, startColumn, endColumn);
				if (lineColumns.Length <= startOffset)
					continue;

				var text = Font.GetText(lineColumns.Substring(startOffset));

				if (highlightDictionary != null)
				{
					foreach (var entry in highlightDictionary)
						foreach (Match match in entry.Key.Matches(lineColumns))
						{
							var start = match.Index - startOffset;
							var end = start + match.Length;
							if (end < 0)
								continue;
							start = Math.Max(0, start);
							text.SetForegroundBrush(entry.Value, start, end - start);
						}
				}

				dc.DrawText(text, new Point(0, drawBounds.Y(line)));
			}
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if (!TabsWindow.Drawing)
				return;

			Tab.SetTabSize((int)Math.Floor(canvas.ActualWidth / Font.CharWidth), (int)Math.Floor(canvas.ActualHeight / LineHeight));
			SetScrollBarsParameters();

			var drawBounds = GetDrawBounds();
			var visibleCursor = (Tab.CurrentSelection >= 0) && (Tab.CurrentSelection < Tab.Selections.Count) ? Tab.Selections[Tab.CurrentSelection] : null;

			for (var region = 1; region <= 9; ++region)
			{
				var multiplier = (region - 1) / 8d * 4.5 + 2;
				RenderIndicators(dc, drawBounds, null, Tab.GetRegions(region), null, regionPen[region], -2 * multiplier, 2 * multiplier);
			}
			if (Tab.Selections.Any(range => range.HasSelection))
				RenderIndicators(dc, drawBounds, visibleCursor, Tab.Selections, selectionBrush, selectionPen, -1, 1);
			else
				RenderCarets(dc, drawBounds);
			RenderDiff(dc, drawBounds);
			RenderText(dc, drawBounds);
		}

		void OnStatusBarRender(object s, DrawingContext dc)
		{
			if (!TabsWindow.Drawing)
				return;

			const string Separator = "  |  ";

			var status = Tab.GetStatusBar();
			var text = new FormattedText(string.Join(Separator, status), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Brushes.White, 1);
			var pos = 0;
			for (var ctr = 0; ctr < status.Count - 1; ++ctr)
			{
				pos += status[ctr].Length;
				text.SetForegroundBrush(Brushes.Gray, pos, Separator.Length);
				pos += Separator.Length;
			}
			dc.DrawText(text, new Point(2, 1));
		}

		int mouseClickCount;
		void HandleMouse(Point position, bool? selecting)
		{
			canvas.CaptureMouse();
			var line = (int)(position.Y / LineHeight + yScroll.Value);
			var column = (int)(position.X / Font.CharWidth + xScroll.Value);
			TabsWindow.HandleCommand(new ExecuteState(NECommand.Internal_Mouse) { Configuration = (Tab, line, column, mouseClickCount, selecting) });
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			canvas.CaptureMouse();
			mouseClickCount = e.ClickCount;
			HandleMouse(e.GetPosition(canvas), null);
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			HandleMouse(e.GetPosition(canvas), true);
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			canvas.ReleaseMouseCapture();
			e.Handled = true;
		}
	}
}
