﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common.NEClipboards;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextView.Dialogs;

namespace NeoEdit.TextView
{
	public class TabsControl : TabsControl<TextViewer, TextViewCommand> { }

	partial class TextViewer : IDisposable
	{
		[DepProp]
		public string FileName { get { return UIHelper<TextViewer>.GetPropValue<string>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int selCursorLine { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int selCursorColumn { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int selHighlightLine { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int selHighlightColumn { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }

		int xScrollViewportFloor => (int)Math.Floor(xScroll.ViewportSize);
		int xScrollViewportCeiling => (int)Math.Ceiling(xScroll.ViewportSize);
		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize);
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize);

		static TextViewer()
		{
			UIHelper<TextViewer>.Register();
			UIHelper<TextViewer>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.selCursorLine, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.selCursorColumn, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.selHighlightLine, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.selHighlightColumn, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextViewer>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextViewer>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.selCursorLine, (obj, value) => (int)Math.Max(0, Math.Min(obj.data.NumLines, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.selCursorColumn, (obj, value) => (int)Math.Max(0, Math.Min(obj.data.NumColumns, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.selHighlightLine, (obj, value) => (int)Math.Max(0, Math.Min(obj.data.NumLines, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.selHighlightColumn, (obj, value) => (int)Math.Max(0, Math.Min(obj.data.NumColumns, value)));
		}

		RunOnceTimer renderTimer;

		readonly TextData data;
		List<PropertyChangeNotifier> localCallbacks;
		internal TextViewer(TextData _data)
		{
			InitializeComponent();

			SetBinding(UIHelper<TabsControl<TextViewer, TextViewCommand>>.GetProperty(a => a.TabLabel), new Binding("FileName") { Converter = new NEExpressionConverter(), ConverterParameter = @"FileName(p0)", Source = this });

			renderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			data = _data;
			FileName = data.FileName;
			if (!File.Exists(FileName))
				throw new Exception($"File {FileName} doesn't exist.");

			localCallbacks = UIHelper<TextViewer>.GetLocalCallbacks(this);

			canvas.Render += OnCanvasRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Font.FontSizeChanged += FontSizeChanged;
		}

		public override void Closed()
		{
			Font.FontSizeChanged -= FontSizeChanged;
			base.Closed();
		}

		public void Dispose() => data.Dispose();

		void FontSizeChanged(double newSize)
		{
			CalculateBoundaries();
			canvas.InvalidateVisual();
		}

		internal void Command_File_CopyPath() => NEClipboard.Current = NEClipboard.Create(FileName, false);

		internal void Command_File_Split()
		{
			var result = SplitDialog.Run(WindowParent, data);
			if (result == null)
				return;

			data.SplitFile(result.SplitData);
		}

		internal void Command_Edit_Copy()
		{
			int selStartLine, selStartColumn, selEndLine, selEndColumn;
			GetSel(out selStartLine, out selStartColumn, out selEndLine, out selEndColumn);
			var estimate = data.GetSizeEstimate(selStartLine, Math.Min(selEndLine + 1, data.NumLines));
			if (estimate >= 524288)
			{
				if (new Message(WindowParent)
				{
					Title = "Confirm",
					Text = $"The data you are attempting to copy is roughly {estimate:n0} bytes.  Are you sure you want to do this?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.No,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			var lines = data.GetLines(selStartLine, Math.Min(selEndLine + 1, data.NumLines), false);
			var result = "";
			for (var line = selStartLine; (line <= selEndLine) && (line - selStartLine < lines.Count); ++line)
			{
				var str = lines[line - selStartLine];
				if (line == selEndLine)
					str = str.Substring(0, Math.Min(str.Length, selEndColumn));
				if (line == selStartLine)
					str = str.Substring(Math.Min(str.Length, selStartColumn));
				result += str;
			}
			if (result.Length != 0)
				NEClipboard.Current = NEClipboard.Create(result);
		}

		void GetSel(out int startLine, out int startColumn, out int endLine, out int endColumn)
		{
			if ((selCursorLine < selHighlightLine) || ((selCursorLine == selHighlightLine) && (selCursorColumn <= selHighlightColumn)))
			{
				startLine = selCursorLine;
				startColumn = selCursorColumn;
				endLine = selHighlightLine;
				endColumn = selHighlightColumn;
			}
			else
			{
				startLine = selHighlightLine;
				startColumn = selHighlightColumn;
				endLine = selCursorLine;
				endColumn = selCursorColumn;
			}
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((xScrollViewportCeiling == 0) || (yScrollViewportCeiling == 0))
				return;

			var startColumn = xScrollValue;
			var endColumn = startColumn + xScrollViewportCeiling;
			var numColumns = endColumn - startColumn;

			var startLine = yScrollValue;
			var endLine = startLine + yScrollViewportCeiling;
			var numLines = endLine - startLine;

			var lines = data.GetLines(Math.Min(startLine, data.NumLines), Math.Min(endLine, data.NumLines));

			int selStartLine, selStartColumn, selEndLine, selEndColumn;
			GetSel(out selStartLine, out selStartColumn, out selEndLine, out selEndColumn);

			for (var line = startLine; line < endLine; ++line)
			{
				var y = (line - startLine) * Font.FontSize;

				if ((line >= selStartLine) && (line <= selEndLine))
				{
					var startCol = line == selStartLine ? selStartColumn : 0;
					var endCol = line == selEndLine ? selEndColumn : data.NumColumns;

					startCol = Math.Max(0, Math.Min(xScrollViewportCeiling, startCol - xScrollValue));
					endCol = Math.Max(0, Math.Min(xScrollViewportCeiling, endCol - xScrollValue));

					if (startCol != endCol)
						dc.DrawRectangle(Misc.selectionBrush, null, new Rect(startCol * Font.CharWidth, y, (endCol - startCol) * Font.CharWidth, Font.FontSize));
				}

				if (selCursorLine == line)
				{
					var column = selCursorColumn - xScrollValue;
					if ((column >= 0) && (column < xScrollViewportCeiling))
						dc.DrawRectangle(Brushes.Black, null, new Rect(column * Font.CharWidth, y, 2, Font.FontSize));
				}

				if (line - startLine >= lines.Count)
					continue;

				var str = lines[line - startLine];
				if (str.Length < startColumn)
					continue;

				str = str.Substring(startColumn, Math.Min(str.Length - startColumn, numColumns));
				var text = Font.GetText(str);
				dc.DrawText(text, new Point(0, y));
			}
		}

		void EnsureVisible()
		{
			yScrollValue = Math.Min(selCursorLine, Math.Max(selCursorLine - yScrollViewportFloor + 1, yScrollValue));
			xScrollValue = Math.Min(selCursorColumn, Math.Max(selCursorColumn - xScrollViewportFloor + 1, xScrollValue));
		}

		void MoveCursor(bool shiftDown, int lineChange = 0, int columnChange = 0, bool lineRel = true, bool columnRel = true)
		{
			if (lineRel)
				lineChange += selCursorLine;
			selCursorLine = lineChange;

			if (columnRel)
				columnChange += selCursorColumn;
			selCursorColumn = columnChange;

			if (!shiftDown)
			{
				selHighlightLine = selCursorLine;
				selHighlightColumn = selCursorColumn;
			}

			EnsureVisible();
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			switch (key)
			{
				case Key.Up: MoveCursor(shiftDown, lineChange: -1); break;
				case Key.Down: MoveCursor(shiftDown, lineChange: 1); break;
				case Key.Left:
					if (controlDown)
						MoveCursor(shiftDown, columnChange: -10);
					else
						MoveCursor(shiftDown, columnChange: -1);
					break;
				case Key.Right:
					if (controlDown)
						MoveCursor(shiftDown, columnChange: 20);
					else
						MoveCursor(shiftDown, columnChange: 1);
					break;
				case Key.Home:
					if (controlDown)
						MoveCursor(shiftDown, 0, 0, false, false);
					else
						MoveCursor(shiftDown, columnChange: 0, columnRel: false);
					break;
				case Key.End:
					if (controlDown)
						MoveCursor(shiftDown, data.NumLines, 0, false, false);
					else
						MoveCursor(shiftDown, columnChange: data.NumColumns, columnRel: false);
					break;
				case Key.PageUp:
					MoveCursor(shiftDown, lineChange: -yScrollViewportFloor + 1);
					break;
				case Key.PageDown:
					MoveCursor(shiftDown, lineChange: yScrollViewportFloor - 1);
					break;
				default: return false;
			}
			return true;
		}

		internal bool HandleText(string text) => false;

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.CharWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = data.NumColumns - xScrollViewportFloor + 1;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / Font.FontSize;
			yScroll.Minimum = 0;
			yScroll.Maximum = data.NumLines - yScrollViewportFloor + 1;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			renderTimer.Start();
		}

		public override string ToString() => FileName;
	}
}
