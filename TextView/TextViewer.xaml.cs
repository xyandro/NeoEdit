﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextView
{
	partial class TextViewer
	{
		[DepProp]
		public string FileName { get { return UIHelper<TextViewer>.GetPropValue<string>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextViewer>.GetPropValue<int>(this); } set { UIHelper<TextViewer>.SetPropValue(this, value); } }

		int xScrollViewportFloor { get { return (int)Math.Floor(xScroll.ViewportSize); } }
		int xScrollViewportCeiling { get { return (int)Math.Ceiling(xScroll.ViewportSize); } }
		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize); } }

		static TextViewer()
		{
			UIHelper<TextViewer>.Register();
			UIHelper<TextViewer>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextViewer>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextViewer>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		RunOnceTimer renderTimer;

		readonly TextData data;
		internal TextViewer(TextData _data)
		{
			InitializeComponent();

			renderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			data = _data;
			FileName = data.FileName;
			if (!File.Exists(FileName))
				throw new Exception(String.Format("File {0} doesn't exist.", FileName));

			UIHelper<TextViewer>.AddCallback(this, Canvas.ActualWidthProperty, () => CalculateBoundaries());
			UIHelper<TextViewer>.AddCallback(this, Canvas.ActualHeightProperty, () => CalculateBoundaries());

			canvas.Render += OnCanvasRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;
		}

		internal Label GetLabel()
		{
			return new Label { Padding = new Thickness(10, 2, 10, 2), Content = Path.GetFileName(FileName) };
		}

		internal void Close()
		{
			data.Close();
		}

		internal void Command_File_CopyPath()
		{
			ClipboardWindow.SetFiles(new List<string> { FileName }, false);
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((xScrollViewportCeiling == 0) || (yScrollViewportCeiling == 0))
				return;

			var startColumn = xScrollValue;
			var endColumn = Math.Min(data.NumColumns, startColumn + xScrollViewportCeiling);
			var numColumns = endColumn - startColumn;

			var startLine = yScrollValue;
			var endLine = Math.Min(data.NumLines, startLine + yScrollViewportCeiling);
			var numLines = endLine - startLine;

			var lines = data.GetLines(startLine, endLine);

			for (var line = 0; line < numLines; ++line)
			{
				var str = lines[line];
				if (str.Length < startColumn)
					continue;

				var y = line * Font.lineHeight;
				str = str.Substring(startColumn, Math.Min(str.Length - startColumn, numColumns));

				var text = Font.GetText(str);
				dc.DrawText(text, new Point(0, y));
			}
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			return false;
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			Focus();
		}

		internal bool HandleText(string text)
		{
			return false;
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.charWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = data.NumColumns - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / Font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			renderTimer.Start();
		}

		public override string ToString()
		{
			return FileName;
		}
	}
}
