using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Converters;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Highlighting;
using NeoEdit.Program.Misc;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		const double Spacing = 2;
		static double LineHeight => Font.FontSize + Spacing;

		class PreviousStruct
		{
			public NECommand Command { get; set; }
			public bool ShiftDown { get; set; }
			public object DialogResult { get; set; }
			public bool? MultiStatus { get; set; }
		}

		public TextEditorData TextEditorData { get; } = new TextEditorData();

		[DepProp]
		public string DisplayName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool AutoRefresh { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public ParserType ContentType { get { return UIHelper<TextEditor>.GetPropValue<ParserType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<TextEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string AESKey { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool Compressed { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEnding { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreWhitespace { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreCase { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreNumbers { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreLineEndings { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsDiff { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffEncodingMismatch { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int TextEditorOrder { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool KeepSelections { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool HighlightSyntax { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool StrictParsing { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public JumpByType JumpBy { get { return UIHelper<TextEditor>.GetPropValue<JumpByType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); jumpBy = JumpBy; } }
		[DepProp]
		public bool ViewValues { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public IList<byte> ViewValuesData { get { return UIHelper<TextEditor>.GetPropValue<IList<byte>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool ViewValuesHasSel { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		public TabsWindow TabsParent { get; set; }

		int currentSelectionField;
		public int CurrentSelection { get => currentSelectionField; set { currentSelectionField = value; canvasRenderTimer.Start(); statusBarRenderTimer.Start(); } }
		public int NumSelections => Selections.Count;
		JumpByType jumpBy;

		bool watcherFileModified = false;

		AnswerResult savedAnswers => TabsParent.savedAnswers;

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

		int xScrollViewportFloor => (int)Math.Floor(xScroll.ViewportSize);
		int xScrollViewportCeiling => (int)Math.Ceiling(xScroll.ViewportSize);
		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize);
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize);

		public bool HasSelections => Selections.Any();

		static TextEditor()
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

			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.ContentType, (obj, o, n) => obj.canvasRenderTimer.Start());
			//TODO UIHelper<TextEditor>.AddCallback(a => a.CodePage, (obj, o, n) => obj.CalculateDiff());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.HighlightSyntax, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		public RangeList Selections { get; private set; } = new RangeList(new List<Range>());
		public void SetSelections(List<Range> selections, bool deOverlap = true)
		{
			Selections = new RangeList(selections, deOverlap);
			EnsureVisible();
			canvasRenderTimer.Start();
			TabsParent?.QueueUpdateStatusBar();
		}

		readonly Dictionary<int, RangeList> regionsList = Enumerable.Range(1, 9).ToDictionary(num => num, num => new RangeList(new List<Range>()));
		public IReadOnlyDictionary<int, RangeList> Regions => regionsList;
		public void SetRegions(int region, List<Range> regions)
		{
			regionsList[region] = new RangeList(regions);
			canvasRenderTimer.Start();
			TabsParent?.QueueUpdateStatusBar();
		}

		RunOnceTimer canvasRenderTimer, statusBarRenderTimer;
		List<PropertyChangeNotifier> localCallbacks;
		public UndoRedo undoRedo { get; }
		static ThreadSafeRandom random = new ThreadSafeRandom();
		public DateTime fileLastWrite { get; set; }
		int mouseClickCount = 0;
		public List<string> DragFiles { get; set; }
		CacheValue modifiedChecksum = new CacheValue();
		public string DiffIgnoreCharacters { get; set; }
		PreviousStruct previous = null;
		FileSystemWatcher watcher = null;
		ShutdownData shutdownData;

		internal TextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, int? index = null, ShutdownData shutdownData = null)
		{
			EnhancedFocusManager.SetIsEnhancedFocusScope(this, true);

			fileName = fileName?.Trim('"');
			this.shutdownData = shutdownData;

			InitializeComponent();
			canvasRenderTimer = new RunOnceTimer(() => { canvas.InvalidateVisual(); statusBar.InvalidateVisual(); });
			statusBarRenderTimer = new RunOnceTimer(() => statusBar.InvalidateVisual());
			AutoRefresh = KeepSelections = HighlightSyntax = true;
			JumpBy = JumpByType.Words;

			SetupTabLabel();

			AllowDrop = true;
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			//Drop += OnDrop;

			undoRedo = new UndoRedo();

			//TODO OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			//TODO Goto(line, column, index);

			localCallbacks = UIHelper<TextEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;
			// TODO statusBar.Render += OnStatusBarRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				canvasRenderTimer.Start();
			};

			FontSizeChanged(null, null);
			Font.FontSizeChanged += FontSizeChanged;
			Font.ShowSpecialCharsChanged += (s, e) => InvalidateCanvas();
		}

		public void InvalidateCanvas()
		{
			canvas.InvalidateVisual();
			statusBar.InvalidateVisual();
		}

		void FontSizeChanged(object sender, EventArgs e) => CalculateBoundaries();

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.CharWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = TextEditorData.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / LineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = TextEditorData.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScrollValue = yScrollValue;

			//TODO yScroll.DiffList = DataQwer.GetDiffRanges();

			LineEnding = TextEditorData.OnlyEnding;

			canvasRenderTimer.Start();
		}

		public bool CanClose()
		{
			if (!IsModified)
				return true;

			if (!savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(CanClose)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultCancel = MessageOptions.Cancel,
				}.Show(false);

			if (savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.No))
				return true;
			//if (savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.Yes))
			//{
			//	Command_File_Save_Save();
			//	return !IsModified;
			//}
			return false;
		}

		void ClearWatcher()
		{
			if (watcher != null)
			{
				watcher.Dispose();
				watcher = null;
			}
		}

		public void Closed()
		{
			Font.FontSizeChanged -= FontSizeChanged;
			ClearWatcher();
			shutdownData?.OnShutdown();
		}

		bool ConfirmContinueWhenCannotEncode()
		{
			if (!savedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(ConfirmContinueWhenCannotEncode)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "The specified encoding cannot fully represent the data. Continue anyway?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show();
			return savedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.Yes);
		}

		public void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
				return;

			var range = Selections[CurrentSelection];
			var lineMin = TextEditorData.GetPositionLine(range.Start);
			var lineMax = TextEditorData.GetPositionLine(range.End);
			var indexMin = TextEditorData.GetPositionIndex(range.Start, lineMin);
			var indexMax = TextEditorData.GetPositionIndex(range.End, lineMax);

			if (centerVertically)
			{
				yScrollValue = (lineMin + lineMax - yScrollViewportFloor) / 2;
				if (centerHorizontally)
					xScrollValue = (TextEditorData.GetColumnFromIndex(lineMin, indexMin) + TextEditorData.GetColumnFromIndex(lineMax, indexMax) - xScrollViewportFloor) / 2;
				else
					xScrollValue = 0;
			}

			var line = TextEditorData.GetPositionLine(range.Cursor);
			var index = TextEditorData.GetPositionIndex(range.Cursor, line);
			var x = TextEditorData.GetColumnFromIndex(line, index);
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));

			statusBarRenderTimer.Start();
		}

		public void HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, object preResult)
		{
			var start = DateTime.UtcNow;
			if (command != NECommand.Macro_RepeatLastAction)
			{
				previous = new PreviousStruct
				{
					Command = command,
					ShiftDown = shiftDown,
					DialogResult = dialogResult,
					MultiStatus = multiStatus,
				};
			}

			//switch (command)
			//{
			//}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			// TODO
			//if ((command != NECommand.Macro_TimeNextAction) && (timeNext))
			//{
			//	timeNext = false;
			//	new Message(TabsParent)
			//	{
			//		Title = "Timer",
			//		Text = $"Elapsed time: {elapsed:n} ms",
			//		Options = MessageOptions.Ok,
			//	}.Show();
			//}
		}

		void MouseHandler(Point mousePos, int clickCount, bool selecting)
		{
			//TODO
			//var sels = Selections.ToList();
			//var line = Math.Max(0, Math.Min(DataQwer.NumLines - 1, (int)(mousePos.Y / LineHeight) + yScrollValue));
			//var column = Math.Max(0, Math.Min(DataQwer.GetLineColumnsLength(line), (int)(mousePos.X / Font.CharWidth) + xScrollValue));
			//var index = DataQwer.GetIndexFromColumn(line, column, true);
			//var position = DataQwer.GetPosition(line, index);
			//var mouseRange = CurrentSelection < sels.Count ? sels[CurrentSelection] : null;

			//var currentSelection = default(Range);
			//if (selecting)
			//{
			//	if (mouseRange != null)
			//	{
			//		sels.Remove(mouseRange);
			//		var anchor = mouseRange.Anchor;
			//		if (clickCount != 1)
			//		{
			//			if (position < anchor)
			//				position = GetPrevWord(position + 1);
			//			else
			//				position = GetNextWord(position);

			//			if ((mouseRange.Cursor <= anchor) != (position <= anchor))
			//			{
			//				if (position <= anchor)
			//					anchor = GetNextWord(anchor);
			//				else
			//					anchor = GetPrevWord(anchor);
			//			}
			//		}

			//		currentSelection = new Range(position, anchor);
			//	}
			//}
			//else
			//{
			//	if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
			//		sels.Clear();

			//	if (clickCount == 1)
			//		currentSelection = new Range(position);
			//	else
			//	{
			//		if (mouseRange != null)
			//			sels.Remove(mouseRange);
			//		currentSelection = new Range(GetNextWord(position), GetPrevWord(Math.Min(position + 1, DataQwer.MaxPosition)));
			//	}
			//}

			//if (currentSelection != null)
			//	sels.Add(currentSelection);
			//SetSelections(sels);
			//if (currentSelection != null)
			//	CurrentSelection = Selections.IndexOf(currentSelection);
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (DragFiles != null)
			{
				DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, DragFiles.ToArray()), DragDropEffects.Copy);
				DragFiles = null;
				return;
			}

			mouseClickCount = e.ClickCount;
			MouseHandler(e.GetPosition(canvas), e.ClickCount, (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None);
			canvas.CaptureMouse();
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			MouseHandler(e.GetPosition(canvas), mouseClickCount, true);
			e.Handled = true;
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
			drawBounds.StartLine = yScrollValue;
			drawBounds.EndLine = Math.Min(TextEditorData.NumLines, drawBounds.StartLine + yScrollViewportCeiling);
			drawBounds.StartColumn = xScrollValue;
			drawBounds.EndColumn = Math.Min(TextEditorData.MaxColumn + 1, drawBounds.StartColumn + xScrollViewportCeiling);

			var lines = Enumerable.Range(drawBounds.StartLine, drawBounds.EndLine - drawBounds.StartLine);
			drawBounds.LineRanges = lines.ToDictionary(line => line, line => new Range(TextEditorData.GetPosition(line, 0), TextEditorData.GetPosition(line, TextEditorData.GetLineLength(line) + 1)));
			drawBounds.StartIndexes = lines.ToDictionary(line => line, line => TextEditorData.GetIndexFromColumn(line, drawBounds.StartColumn, true));
			drawBounds.EndIndexes = lines.ToDictionary(line => line, line => TextEditorData.GetIndexFromColumn(line, drawBounds.EndColumn, true));
			return drawBounds;
		}

		void RenderCarets(DrawingContext dc, DrawBounds drawBounds)
		{
			for (var selectionCtr = 0; selectionCtr < TextEditorData.Selections.Count; ++selectionCtr)
			{
				var range = TextEditorData.Selections[selectionCtr];

				if ((range.End < drawBounds.ScreenStart) || (range.Start > drawBounds.ScreenEnd))
					continue;

				var startLine = TextEditorData.GetPositionLine(range.Start);
				var endLine = TextEditorData.GetPositionLine(range.End);
				var cursorLine = range.Cursor == range.Start ? startLine : endLine;
				startLine = Math.Max(drawBounds.StartLine, startLine);
				endLine = Math.Min(drawBounds.EndLine, endLine + 1);

				if ((cursorLine < startLine) || (cursorLine >= endLine))
					continue;

				if (selectionCtr == CurrentSelection)
					dc.DrawRoundedRectangle(highlightRowBrush, lightlightRowPen, new Rect(-2, drawBounds.Y(cursorLine), canvas.ActualWidth + 4, Font.FontSize), 4, 4);

				var cursor = TextEditorData.GetPositionIndex(range.Cursor, cursorLine);
				if ((cursor >= drawBounds.StartIndexes[cursorLine]) && (cursor <= drawBounds.EndIndexes[cursorLine]))
				{
					cursor = TextEditorData.GetColumnFromIndex(cursorLine, cursor);
					for (var pass = selectionCtr == CurrentSelection ? 2 : 1; pass > 0; --pass)
						dc.DrawRectangle(caretBrush, null, new Rect(drawBounds.X(cursor) - 1, drawBounds.Y(cursorLine), 2, LineHeight));
				}
			}
		}

		void RenderIndicators(DrawingContext dc, DrawBounds drawBounds, Range visibleCursor, List<Range> ranges, Brush brush, Pen pen, double leftSpacing, double rightSpacing)
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
			var startLine = TextEditorData.GetPositionLine(range.Start);
			var startColumn = TextEditorData.GetColumnFromIndex(startLine, TextEditorData.GetPositionIndex(range.Start, startLine));

			var endLine = TextEditorData.GetPositionLine(range.End);
			var endColumn = TextEditorData.GetColumnFromIndex(endLine, TextEditorData.GetPositionIndex(range.End, endLine));
			if ((endLine != startLine) && (endColumn == 0))
			{
				--endLine;
				endColumn = TextEditorData.GetLineColumnsLength(endLine) + 1;
			}

			var points = new List<Point>();

			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine) + Font.FontSize / 2));
			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine)));
			for (var line = startLine; ; ++line)
			{
				var done = line == endLine;
				if ((line >= drawBounds.StartLine - 1) && ((line < drawBounds.EndLine)))
				{
					var length = done ? endColumn : TextEditorData.GetLineColumnsLength(line) + 1;
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

			var highlightDictionary = HighlightSyntax ? Highlight.Get(ContentType)?.GetDictionary() : null;

			var startColumn = Math.Max(drawBounds.StartColumn - HighlightRegexSize, 0);
			var startOffset = drawBounds.StartColumn - startColumn;
			var endColumn = drawBounds.EndColumn + HighlightRegexSize;
			for (var line = drawBounds.StartLine; line < drawBounds.EndLine; ++line)
			{
				var lineColumns = TextEditorData.GetLineColumns(line, startColumn, endColumn);
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
			if ((TextEditorData == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0) || (!canvas.IsVisible))
				return;

			var drawBounds = GetDrawBounds();
			var visibleCursor = (TextEditorData.CurrentSelection >= 0) && (TextEditorData.CurrentSelection < TextEditorData.Selections.Count) ? TextEditorData.Selections[TextEditorData.CurrentSelection] : null;

			for (var ctr = 1; ctr <= 9; ++ctr)
				RenderIndicators(dc, drawBounds, null, TextEditorData.GetRegions(ctr), null, regionPen[ctr], -2, 2);
			if (TextEditorData.Selections.Any(range => range.HasSelection))
				RenderIndicators(dc, drawBounds, visibleCursor, TextEditorData.Selections, selectionBrush, selectionPen, -1, 1);
			else
				RenderCarets(dc, drawBounds);
			RenderDiff(dc, drawBounds);
			RenderText(dc, drawBounds);
		}

		//TODO
		//void OnDrop(object sender, DragEventArgs e)
		//{
		//	// If we get a file list, return and let the tabs window handle it
		//	var fileList = e.Data.GetData("FileDrop") as string[];
		//	if (fileList != null)
		//		return;

		//	if (Selections.Count != 1)
		//		throw new Exception("Must have one selection.");

		//	var data = (e.Data.GetData("UnicodeText") ?? e.Data.GetData("Text") ?? e.Data.GetData(typeof(string))) as string;
		//	if (data == null)
		//		return;

		//	ReplaceSelections(data);
		//	e.Handled = true;
		//}

		public void Activated()
		{
			if (!watcherFileModified)
				return;

			watcherFileModified = false;
			//TODO Command_File_Refresh();
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"(p0 ?? FileName(p1) ?? ""[Untitled]"")t+(p2?""*"":"""")t+(p3?"" (Diff""t+(p4?"" - Encoding mismatch"":"""")t+"")"":"""")" };
			multiBinding.Bindings.Add(new Binding(nameof(DisplayName)) { Source = this });
			multiBinding.Bindings.Add(new Binding(nameof(FileName)) { Source = this });
			multiBinding.Bindings.Add(new Binding(nameof(IsModified)) { Source = this });
			multiBinding.Bindings.Add(new Binding(nameof(IsDiff)) { Source = this });
			multiBinding.Bindings.Add(new Binding(nameof(DiffEncodingMismatch)) { Source = this });
			SetBinding(UIHelper<TextEditor>.GetProperty(a => a.TabLabel), multiBinding);
		}

		public override string ToString() => FileName ?? DisplayName;

		public void UpdateViewValue(byte[] value, int? size)
		{
			//TODO
			//var sels = Selections.Select(range => Range.FromIndex(range.Start, size ?? range.Length)).ToList();
			//var values = Enumerable.Repeat(Coder.BytesToString(value, CodePage), sels.Count).ToList();
			//Replace(sels, values);
		}
	}
}
