using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Converters;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Highlighting;
using NeoEdit.Program.Misc;
using NeoEdit.Program.Parsing;
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

		public TextData Data { get; } = new TextData();

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
		public bool IncludeInlineVariables { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public JumpByType JumpBy { get { return UIHelper<TextEditor>.GetPropValue<JumpByType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); jumpBy = JumpBy; } }
		[DepProp]
		public bool ViewValues { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public IList<byte> ViewValuesData { get { return UIHelper<TextEditor>.GetPropValue<IList<byte>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool ViewValuesHasSel { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string ViewValuesFindValue { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		public TabsWindow TabsParent { get; set; }
		public bool IsActive => TabsParent.TabIsActive(this);

		public bool CanClose() => CanClose(new AnswerResult());

		int currentSelectionField;
		public int CurrentSelection { get => currentSelectionField; set { currentSelectionField = value; canvasRenderTimer.Start(); statusBarRenderTimer.Start(); } }
		public int NumSelections => Selections.Count;
		public List<string> Clipboard => TabsParent.GetClipboard(this);
		JumpByType jumpBy;

		bool watcherFileModified = false;

		TextEditor diffTarget;
		public TextEditor DiffTarget
		{
			get { return diffTarget; }
			set
			{
				if (value == this)
					value = null;

				IsDiff = false;

				if (diffTarget != null)
				{
					diffTarget.IsDiff = false;

					var thisXScrollValue = xScrollValue;
					var thisYScrollValue = Data.GetDiffLine(yScrollValue);
					var targetXScrollValue = diffTarget.xScrollValue;
					var targetYScrollValue = diffTarget.Data.GetDiffLine(diffTarget.yScrollValue);
					BindingOperations.ClearBinding(this, UIHelper<TextEditor>.GetProperty(a => a.xScrollValue));
					BindingOperations.ClearBinding(this, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					BindingOperations.ClearBinding(diffTarget, UIHelper<TextEditor>.GetProperty(a => a.xScrollValue));
					BindingOperations.ClearBinding(diffTarget, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					xScrollValue = thisXScrollValue;
					yScrollValue = thisYScrollValue;
					diffTarget.xScrollValue = targetXScrollValue;
					diffTarget.yScrollValue = targetYScrollValue;

					Data.ClearDiff();
					diffTarget.Data.ClearDiff();
					CalculateBoundaries();
					diffTarget.CalculateBoundaries();
					diffTarget.diffTarget = null;
					diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					diffTarget = value as TextEditor;
					(value as TextEditor).diffTarget = this;

					var diffXScrollValue = diffTarget.xScrollValue;
					var diffYScrollValue = diffTarget.yScrollValue;
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.xScrollValue), new Binding(nameof(xScrollValue)) { Source = value, Mode = BindingMode.TwoWay });
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue), new Binding(nameof(yScrollValue)) { Source = value, Mode = BindingMode.TwoWay });
					IsDiff = diffTarget.IsDiff = true;
					CalculateDiff();
					xScrollValue = diffXScrollValue;
					yScrollValue = diffTarget.Data.GetNonDiffLine(diffYScrollValue);
				}
			}
		}

		static internal readonly Brush caretBrush = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255));
		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(96, 38, 132, 255));
		static internal readonly Pen selectionPen = new Pen(new SolidColorBrush(Color.FromArgb(96, 38, 132, 255)), 2);
		static internal readonly Brush searchBrush = new SolidColorBrush(Color.FromRgb(101, 51, 6));
		static internal readonly Pen searchPen = new Pen(new SolidColorBrush(Color.FromRgb(201, 102, 12)), 2);
		static internal readonly Dictionary<int, Pen> regionPen = new Dictionary<int, Pen>
		{
			[1] = new Pen(new SolidColorBrush(Color.FromRgb(245, 53, 139)), 2),
			[2] = new Pen(new SolidColorBrush(Color.FromRgb(238, 138, 98)), 2),
			[3] = new Pen(new SolidColorBrush(Color.FromRgb(182, 62, 95)), 2),
			[4] = new Pen(new SolidColorBrush(Color.FromRgb(105, 47, 111)), 2),
			[5] = new Pen(new SolidColorBrush(Color.FromRgb(237, 223, 184)), 2),
			[6] = new Pen(new SolidColorBrush(Color.FromRgb(248, 118, 109)), 2),
			[7] = new Pen(new SolidColorBrush(Color.FromRgb(124, 174, 0)), 2),
			[8] = new Pen(new SolidColorBrush(Color.FromRgb(0, 191, 196)), 2),
			[9] = new Pen(new SolidColorBrush(Color.FromRgb(199, 124, 255)), 2),
		};
		static internal readonly Brush diffLineBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Pen diffLinePen = new Pen(new SolidColorBrush(Color.FromRgb(120, 102, 3)), 2);
		static internal readonly Brush diffColBrush = new SolidColorBrush(Color.FromArgb(128, 239, 203, 5));
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
			searchBrush.Freeze();
			searchPen.Freeze();
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
			UIHelper<TextEditor>.AddCallback(a => a.CodePage, (obj, o, n) => obj.CalculateDiff());
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

		public RangeList Searches { get; private set; } = new RangeList(new List<Range>());
		public void SetSearches(List<Range> searches)
		{
			Searches = new RangeList(searches.Where(range => range.HasSelection).ToList());
			canvasRenderTimer.Start();
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

		internal TextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null)
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
			Drop += OnDrop;

			undoRedo = new UndoRedo();

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column);

			localCallbacks = UIHelper<TextEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;
			statusBar.Render += OnStatusBarRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				canvasRenderTimer.Start();
			};

			FontSizeChanged(Font.FontSize);
			Font.FontSizeChanged += FontSizeChanged;
			Font.ShowSpecialCharsChanged += (s, e) => InvalidateCanvas();
		}

		public void InvalidateCanvas()
		{
			canvas.InvalidateVisual();
			statusBar.InvalidateVisual();
		}

		void FontSizeChanged(double fontSize) => CalculateBoundaries();

		public int BeginOffset => Data.GetOffset(0, 0);
		public int EndOffset => Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		public Range BeginRange => new Range(BeginOffset);
		Range EndRange => new Range(EndOffset);
		public Range FullRange => new Range(EndOffset, BeginOffset);
		public string AllText => GetString(FullRange);

		void BlockSelDown()
		{
			var sels = new List<Range>();
			foreach (var range in Selections)
			{
				var cursorLine = Data.GetOffsetLine(range.Cursor);
				var highlightLine = Data.GetOffsetLine(range.Anchor);
				var cursorIndex = Data.GetOffsetIndex(range.Cursor, cursorLine);
				var highlightIndex = Data.GetOffsetIndex(range.Anchor, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Data.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Data.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Data.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Data.GetLineLength(highlightLine)));

				sels.Add(new Range(Data.GetOffset(cursorLine, cursorIndex), Data.GetOffset(highlightLine, highlightIndex)));
			}
			SetSelections(Selections.Concat(sels).ToList());
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<Range>();
			foreach (var range in Selections.ToList())
			{
				var startLine = Data.GetOffsetLine(range.Start);
				var endLine = Data.GetOffsetLine(range.End);
				var startIndex = Data.GetOffsetIndex(range.Start, startLine);
				var endIndex = Data.GetOffsetIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Data.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Data.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Data.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Data.GetLineLength(endLine)));

				var prevLineRange = new Range(Data.GetOffset(startLine, startIndex), Data.GetOffset(endLine, endIndex));
				if (found.Contains(prevLineRange.ToString()))
					sels.Add(prevLineRange);
				else
					sels.Add(range);
			}

			SetSelections(sels);
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.CharWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Data.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / LineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScrollValue = yScrollValue;

			yScroll.DiffList = Data.GetDiffRanges();

			LineEnding = Data.OnlyEnding;

			canvasRenderTimer.Start();
		}

		public void CalculateDiff()
		{
			if (diffTarget == null)
				return;

			diffTarget.DiffIgnoreWhitespace = DiffIgnoreWhitespace;
			diffTarget.DiffIgnoreCase = DiffIgnoreCase;
			diffTarget.DiffIgnoreNumbers = DiffIgnoreNumbers;
			diffTarget.DiffIgnoreLineEndings = DiffIgnoreLineEndings;
			diffTarget.DiffIgnoreCharacters = DiffIgnoreCharacters;
			DiffEncodingMismatch = diffTarget.DiffEncodingMismatch = CodePage != diffTarget.CodePage;

			var left = TabsParent.GetTabIndex(this) < DiffTarget.TabsParent.GetTabIndex(DiffTarget) ? this : DiffTarget;
			var right = left == this ? DiffTarget : this;
			TextData.CalculateDiff(left.Data, right.Data, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);

			CalculateBoundaries();
			diffTarget.CalculateBoundaries();
		}

		public bool CanClose(AnswerResult answer)
		{
			if (!IsModified)
				return true;

			if (!answer[nameof(CanClose)].HasFlag(MessageOptions.All))
				answer[nameof(CanClose)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();

			if (answer[nameof(CanClose)].HasFlag(MessageOptions.Cancel))
				return false;
			if (answer[nameof(CanClose)].HasFlag(MessageOptions.No))
				return true;
			if (answer[nameof(CanClose)].HasFlag(MessageOptions.Yes))
			{
				Command_File_Save_Save(answer);
				return !IsModified;
			}
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
			DiffTarget = null;
			Font.FontSizeChanged -= FontSizeChanged;
			ClearWatcher();
			shutdownData?.OnShutdown();
		}

		bool ConfirmContinueWhenCannotEncode()
		{
			return new Message(TabsParent)
			{
				Title = "Confirm",
				Text = "The specified encoding cannot fully represent the data. Continue anyway?",
				Options = MessageOptions.YesNo,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.No,
			}.Show().HasFlag(MessageOptions.Yes);
		}

		public bool Empty() => (FileName == null) && (!IsModified) && (BeginOffset == EndOffset);

		public void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
				return;

			var range = Selections[CurrentSelection];
			var lineMin = Data.GetOffsetLine(range.Start);
			var lineMax = Data.GetOffsetLine(range.End);
			var indexMin = Data.GetOffsetIndex(range.Start, lineMin);
			var indexMax = Data.GetOffsetIndex(range.End, lineMax);

			if (centerVertically)
			{
				yScrollValue = (lineMin + lineMax - yScrollViewportFloor) / 2;
				if (centerHorizontally)
					xScrollValue = (Data.GetColumnFromIndex(lineMin, indexMin) + Data.GetColumnFromIndex(lineMax, indexMax) - xScrollViewportFloor) / 2;
				else
					xScrollValue = 0;
			}

			var line = Data.GetOffsetLine(range.Cursor);
			var index = Data.GetOffsetIndex(range.Cursor, line);
			var x = Data.GetColumnFromIndex(line, index);
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));

			statusBarRenderTimer.Start();
		}

		public List<T> GetFixedExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateList<T>(GetVariables(), Selections.Count());

		public WordSkipType GetWordSkipType(int line, int index)
		{
			if ((index < 0) || (index >= Data.GetLineLength(line)))
				return WordSkipType.Space;

			var c = Data[line, index];
			switch (jumpBy)
			{
				case JumpByType.Words:
				case JumpByType.Numbers:
					if (char.IsWhiteSpace(c))
						return WordSkipType.Space;
					else if ((char.IsLetterOrDigit(c)) || (c == '_') || ((jumpBy == JumpByType.Numbers) && ((c == '.') || (c == '-'))))
						return WordSkipType.Char;
					else
						return WordSkipType.Symbol;
				case JumpByType.Paths:
					if (c == '\\')
						return WordSkipType.Path;
					return WordSkipType.Char;
				default:
					return WordSkipType.Space;
			}
		}

		public int GetNextWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line) - 1);
			while (true)
			{
				if (index >= Data.GetLineLength(line))
				{
					++line;
					if (line >= Data.NumLines)
						return EndOffset;
					index = -1;
				}

				++index;
				var current = GetWordSkipType(line, index);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(line, index);
			}
		}

		public int GetPrevWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line));
			while (true)
			{
				if (index < 0)
				{
					--line;
					if (line < 0)
						return BeginOffset;
					index = Data.GetLineLength(line);
					continue;
				}

				--index;
				var current = GetWordSkipType(line, index);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(line, index + 1);
			}
		}

		public List<string> GetSelectionStrings() => Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();

		public string GetString(Range range) => Data.GetString(range.Start, range.Length);

		public List<T> GetVariableExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateList<T>(GetVariables());

		public NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var fileName = FileName;
			var displayName = DisplayName;

			var results = new NEVariables();

			var strs = default(List<string>);
			var initializeStrs = new NEVariableInitializer(() => strs = Selections.Select(range => GetString(range)).ToList());
			results.Add(NEVariable.List("x", "Selection", () => strs, initializeStrs));
			results.Add(NEVariable.Constant("xn", "Selection count", () => Selections.Count));
			results.Add(NEVariable.List("xl", "Selection length", () => Selections.Select(range => range.Length)));
			results.Add(NEVariable.Constant("xlmin", "Selection min length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xlmax", "Selection max length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xmin", "Selection numeric min", () => Selections.AsParallel().Select(range => GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xmax", "Selection numeric max", () => Selections.AsParallel().Select(range => GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xtmin", "Selection text min", () => Selections.AsParallel().Select(range => GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).First()));
			results.Add(NEVariable.Constant("xtmax", "Selection text max", () => Selections.AsParallel().Select(range => GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).Last()));

			foreach (var pair in Regions)
			{
				var regions = default(List<string>);
				var initializeRegions = new NEVariableInitializer(() => regions = pair.Value.Select(range => GetString(range)).ToList());
				results.Add(NEVariable.List($"r{pair.Key}", $"Region {pair.Key}", () => regions, initializeRegions));
				results.Add(NEVariable.Constant($"r{pair.Key}n", $"Region {pair.Key} count", () => pair.Value.Count));
				results.Add(NEVariable.List($"r{pair.Key}l", $"Region {pair.Key} length", () => pair.Value.Select(range => range.Length)));
				results.Add(NEVariable.Constant($"r{pair.Key}lmin", $"Region {pair.Key} min length", () => pair.Value.Select(range => range.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"r{pair.Key}lmax", $"Region {pair.Key} max length", () => pair.Value.Select(range => range.Length).DefaultIfEmpty(0).Max()));
			}

			results.Add(NEVariable.Series("y", "One-based index", index => index + 1));
			results.Add(NEVariable.Series("z", "Zero-based index", index => index));

			if (Clipboard.Count == 1)
			{
				results.Add(NEVariable.Constant("c", "Clipboard", () => Clipboard[0]));
				results.Add(NEVariable.Constant("cl", "Clipboard length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard[0].Length));
			}
			else
			{
				results.Add(NEVariable.List("c", "Clipboard", () => Clipboard));
				results.Add(NEVariable.List("cl", "Clipboard length", () => Clipboard.Select(str => str.Length)));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}
			results.Add(NEVariable.Constant("cn", "Clipboard count", () => Clipboard.Count));

			results.Add(NEVariable.Constant("f", "Filename", () => fileName));
			results.Add(NEVariable.Constant("d", "Display name", () => displayName));

			var lineStarts = default(List<int>);
			var initializeLineStarts = new NEVariableInitializer(() => lineStarts = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => lineStarts, initializeLineStarts));
			var lineEnds = default(List<int>);
			var initializeLineEnds = new NEVariableInitializer(() => lineEnds = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => lineEnds, initializeLineEnds));

			var colStarts = default(List<int>);
			var initializeColStarts = new NEVariableInitializer(() => colStarts = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.Start, lineStarts[index] - 1) + 1).ToList(), initializeLineStarts);
			results.Add(NEVariable.List("col", "Selection column start", () => colStarts, initializeColStarts));
			var colEnds = default(List<int>);
			var initializeColEnds = new NEVariableInitializer(() => colEnds = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.End, lineEnds[index] - 1) + 1).ToList(), initializeLineEnds);
			results.Add(NEVariable.List("colend", "Selection column end", () => colEnds, initializeColEnds));

			var posStarts = default(List<int>);
			var initializePosStarts = new NEVariableInitializer(() => posStarts = Selections.Select(range => range.Start).ToList());
			results.Add(NEVariable.List("pos", "Selection position start", () => posStarts, initializePosStarts));
			var posEnds = default(List<int>);
			var initializePosEnds = new NEVariableInitializer(() => posEnds = Selections.Select(range => range.End).ToList());
			results.Add(NEVariable.List("posend", "Selection position end", () => posEnds, initializePosEnds));

			for (var ctr = 0; ctr < 10; ++ctr)
			{
				var name = ctr == 0 ? "k" : $"v{ctr}";
				var desc = ctr == 0 ? "Keys" : $"Values {ctr}";
				var values = TabsParent.GetKeysAndValues(this, ctr, false);
				if (values == null)
					continue;
				results.Add(NEVariable.List(name, desc, () => values));
				results.Add(NEVariable.Constant($"{name}n", $"{desc} count", () => values.Count));
				results.Add(NEVariable.List($"{name}l", $"{desc} length", () => values.Select(str => str.Length)));
				results.Add(NEVariable.Constant($"{name}lmin", $"{desc} min length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"{name}lmax", $"{desc} max length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}

			if (Coder.IsImage(CodePage))
			{
				results.Add(NEVariable.Constant("width", "Image width", () => GetBitmap().Width));
				results.Add(NEVariable.Constant("height", "Image height", () => GetBitmap().Height));
			}

			var nonNulls = default(List<Tuple<double, int>>);
			double lineStart = 0, lineIncrement = 0, geoStart = 0, geoIncrement = 0;
			var initializeNonNulls = new NEVariableInitializer(() => nonNulls = Selections.AsParallel().AsOrdered().Select((range, index) => new { str = GetString(range), index }).NonNullOrWhiteSpace(obj => obj.str).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList());
			var initializeLineSeries = new NEVariableInitializer(() =>
			{
				if (nonNulls.Count == 0)
					lineStart = lineIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					lineStart = nonNulls[0].Item1;
					lineIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					lineIncrement = (last.Item1 - first.Item1) / (last.Item2 - first.Item2);
					lineStart = first.Item1 - lineIncrement * first.Item2;
				}
			}, initializeNonNulls);
			var initializeGeoSeries = new NEVariableInitializer(() =>
			{
				if (nonNulls.Count == 0)
					geoStart = geoIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					geoStart = nonNulls[0].Item1;
					geoIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					geoIncrement = Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
					geoStart = first.Item1 / Math.Pow(geoIncrement, first.Item2);
				}
			}, initializeNonNulls);
			results.Add(NEVariable.Constant("linestart", "Linear series start", () => lineStart, initializeLineSeries));
			results.Add(NEVariable.Constant("lineincrement", "Linear series increment", () => lineIncrement, initializeLineSeries));
			results.Add(NEVariable.Constant("geostart", "Geometric series start", () => geoStart, initializeGeoSeries));
			results.Add(NEVariable.Constant("geoincrement", "Geometric series increment", () => geoIncrement, initializeGeoSeries));

			if (IncludeInlineVariables)
				GetInlineVariables().NonNullOrEmpty(inlineVar => inlineVar.Name).Where(inlineVar => !results.Contains(inlineVar.Name)).ForEach(inlineVar => results.Add(NEVariable.Constant(inlineVar.Name, "Inline variable", inlineVar.Value)));

			return results;
		}

		void Goto(int? line, int? column)
		{
			var useLine = Math.Max(0, Math.Min(line ?? 1, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(useLine, Math.Max(0, (column ?? 1) - 1), true);
			SetSelections(Selections.Concat(new Range(Data.GetOffset(useLine, index))).ToList());
		}

		public bool GetDialogResult(NECommand command, out object dialogResult, bool? multiStatus)
		{
			dialogResult = null;

			switch (command)
			{
				case NECommand.File_Save_SaveAsByExpression: dialogResult = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Copy_CopyToByExpression: dialogResult = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Operations_RenameByExpression: dialogResult = Command_File_Operations_RenameByExpression_Dialog(); break;
				case NECommand.File_Operations_SetDisplayName: dialogResult = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Encoding_Encoding: dialogResult = Command_File_Encoding_Encoding_Dialog(); break;
				case NECommand.File_Encoding_ReopenWithEncoding: dialogResult = Command_File_Encoding_ReopenWithEncoding_Dialog(); break;
				case NECommand.File_Encoding_LineEndings: dialogResult = Command_File_Encoding_LineEndings_Dialog(); break;
				case NECommand.File_Encrypt: dialogResult = Command_File_Encrypt_Dialog(multiStatus); break;
				case NECommand.Edit_Find_Find: dialogResult = Command_Edit_Find_Find_Dialog(); break;
				case NECommand.Edit_Find_MassFind: dialogResult = Command_Edit_Find_MassFind_Dialog(); break;
				case NECommand.Edit_Find_Binary: dialogResult = Command_Edit_Find_Binary_Dialog(); break;
				case NECommand.Edit_Find_Replace: dialogResult = Command_Edit_Find_Replace_Dialog(); break;
				case NECommand.Edit_Rotate: dialogResult = Command_Edit_Rotate_Dialog(); break;
				case NECommand.Edit_Repeat: dialogResult = Command_Edit_Repeat_Dialog(); break;
				case NECommand.Edit_URL_Absolute: dialogResult = Command_Edit_URL_Absolute_Dialog(); break;
				case NECommand.Edit_Data_Hash: dialogResult = Command_Edit_Data_Hash_Dialog(); break;
				case NECommand.Edit_Data_Compress: dialogResult = Command_Edit_Data_Compress_Dialog(); break;
				case NECommand.Edit_Data_Decompress: dialogResult = Command_Edit_Data_Decompress_Dialog(); break;
				case NECommand.Edit_Data_Encrypt: dialogResult = Command_Edit_Data_Encrypt_Dialog(); break;
				case NECommand.Edit_Data_Decrypt: dialogResult = Command_Edit_Data_Decrypt_Dialog(); break;
				case NECommand.Edit_Data_Sign: dialogResult = Command_Edit_Data_Sign_Dialog(); break;
				case NECommand.Edit_Sort: dialogResult = Command_Edit_Sort_Dialog(); break;
				case NECommand.Edit_Convert: dialogResult = Command_Edit_Convert_Dialog(); break;
				case NECommand.Edit_ModifyRegions: dialogResult = Command_Edit_ModifyRegions_Dialog(); break;
				case NECommand.Diff_IgnoreCharacters: dialogResult = Command_Diff_IgnoreCharacters_Dialog(); break;
				case NECommand.Diff_Fix_Whitespace: dialogResult = Command_Diff_Fix_Whitespace_Dialog(); break;
				case NECommand.Files_Name_MakeAbsolute: dialogResult = Command_Files_Name_MakeAbsolute_Dialog(); break;
				case NECommand.Files_Name_MakeRelative: dialogResult = Command_Files_Name_MakeRelative_Dialog(); break;
				case NECommand.Files_Name_GetUnique: dialogResult = Command_Files_Name_GetUnique_Dialog(); break;
				case NECommand.Files_Set_Size: dialogResult = Command_Files_Set_Size_Dialog(); break;
				case NECommand.Files_Set_Time_Write: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_Access: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_Create: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_All: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Attributes: dialogResult = Command_Files_Set_Attributes_Dialog(); break;
				case NECommand.Files_Find_Binary: dialogResult = Command_Files_Find_Binary_Dialog(); break;
				case NECommand.Files_Find_Text: dialogResult = Command_Files_Find_Text_Dialog(); break;
				case NECommand.Files_Find_MassFind: dialogResult = Command_Files_Find_MassFind_Dialog(); break;
				case NECommand.Files_Insert: dialogResult = Command_Files_Insert_Dialog(); break;
				case NECommand.Files_Create_FromExpressions: dialogResult = Command_Files_Create_FromExpressions_Dialog(); break;
				case NECommand.Files_Select_ByVersionControlStatus: dialogResult = Command_Files_Select_ByVersionControlStatus_Dialog(); break;
				case NECommand.Files_Hash: dialogResult = Command_Files_Hash_Dialog(); break;
				case NECommand.Files_Sign: dialogResult = Command_Files_Sign_Dialog(); break;
				case NECommand.Files_Operations_Copy: dialogResult = Command_Files_Operations_CopyMove_Dialog(false); break;
				case NECommand.Files_Operations_Move: dialogResult = Command_Files_Operations_CopyMove_Dialog(true); break;
				case NECommand.Files_Operations_Encoding: dialogResult = Command_Files_Operations_Encoding_Dialog(); break;
				case NECommand.Files_Operations_SplitFile: dialogResult = Command_Files_Operations_SplitFile_Dialog(); break;
				case NECommand.Files_Operations_CombineFiles: dialogResult = Command_Files_Operations_CombineFiles_Dialog(); break;
				case NECommand.Expression_Expression: dialogResult = Command_Expression_Expression_Dialog(); break;
				case NECommand.Expression_Copy: dialogResult = Command_Expression_Copy_Dialog(); break;
				case NECommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				case NECommand.Expression_InlineVariables_Solve: dialogResult = Command_Expression_InlineVariables_Solve_Dialog(); break;
				case NECommand.Text_Select_Trim: dialogResult = Command_Text_Select_Trim_Dialog(); break;
				case NECommand.Text_Select_ByWidth: dialogResult = Command_Text_Select_ByWidth_Dialog(); break;
				case NECommand.Text_Select_WholeWord: dialogResult = Command_Text_Select_WholeBoundedWord_Dialog(true); break;
				case NECommand.Text_Select_BoundedWord: dialogResult = Command_Text_Select_WholeBoundedWord_Dialog(false); break;
				case NECommand.Text_Width: dialogResult = Command_Text_Width_Dialog(); break;
				case NECommand.Text_Trim: dialogResult = Command_Text_Trim_Dialog(); break;
				case NECommand.Text_Unicode: dialogResult = Command_Text_Unicode_Dialog(); break;
				case NECommand.Text_RandomText: dialogResult = Command_Text_RandomText_Dialog(); break;
				case NECommand.Text_ReverseRegEx: dialogResult = Command_Text_ReverseRegEx_Dialog(); break;
				case NECommand.Text_FirstDistinct: dialogResult = Command_Text_FirstDistinct_Dialog(); break;
				case NECommand.Numeric_ConvertBase: dialogResult = Command_Numeric_ConvertBase_Dialog(); break;
				case NECommand.Numeric_Series_Linear: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(true); break;
				case NECommand.Numeric_Series_Geometric: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(false); break;
				case NECommand.Numeric_Scale: dialogResult = Command_Numeric_Scale_Dialog(); break;
				case NECommand.Numeric_Floor: dialogResult = Command_Numeric_Floor_Dialog(); break;
				case NECommand.Numeric_Ceiling: dialogResult = Command_Numeric_Ceiling_Dialog(); break;
				case NECommand.Numeric_Round: dialogResult = Command_Numeric_Round_Dialog(); break;
				case NECommand.Numeric_Limit: dialogResult = Command_Numeric_Limit_Dialog(); break;
				case NECommand.Numeric_Cycle: dialogResult = Command_Numeric_Cycle_Dialog(); break;
				case NECommand.Numeric_RandomNumber: dialogResult = Command_Numeric_RandomNumber_Dialog(); break;
				case NECommand.Numeric_CombinationsPermutations: dialogResult = Command_Numeric_CombinationsPermutations_Dialog(); break;
				case NECommand.Numeric_MinMaxValues: dialogResult = Command_Numeric_MinMaxValues_Dialog(); break;
				case NECommand.DateTime_Convert: dialogResult = Command_DateTime_Convert_Dialog(); break;
				case NECommand.Image_GrabColor: dialogResult = Command_Image_GrabColor_Dialog(); break;
				case NECommand.Image_GrabImage: dialogResult = Command_Image_GrabImage_Dialog(); break;
				case NECommand.Image_AdjustColor: dialogResult = Command_Image_AdjustColor_Dialog(); break;
				case NECommand.Image_AddColor: dialogResult = Command_Image_AddOverlayColor_Dialog(true); break;
				case NECommand.Image_OverlayColor: dialogResult = Command_Image_AddOverlayColor_Dialog(false); break;
				case NECommand.Image_Size: dialogResult = Command_Image_Size_Dialog(); break;
				case NECommand.Image_Crop: dialogResult = Command_Image_Crop_Dialog(); break;
				case NECommand.Image_Rotate: dialogResult = Command_Image_Rotate_Dialog(); break;
				case NECommand.Image_GIF_Animate: dialogResult = Command_Image_GIF_Animate_Dialog(); break;
				case NECommand.Image_GIF_Split: dialogResult = Command_Image_GIF_Split_Dialog(); break;
				case NECommand.Table_Convert: dialogResult = Command_Table_Convert_Dialog(); break;
				case NECommand.Table_TextToTable: dialogResult = Command_Table_TextToTable_Dialog(); break;
				case NECommand.Table_EditTable: dialogResult = Command_Table_EditTable_Dialog(); break;
				case NECommand.Table_AddColumn: dialogResult = Command_Table_AddColumn_Dialog(); break;
				case NECommand.Table_Select_RowsByExpression: dialogResult = Command_Table_Select_RowsByExpression_Dialog(); break;
				case NECommand.Table_Join: dialogResult = Command_Table_Join_Dialog(); break;
				case NECommand.Table_Database_GenerateInserts: dialogResult = Command_Table_Database_GenerateInserts_Dialog(); break;
				case NECommand.Table_Database_GenerateUpdates: dialogResult = Command_Table_Database_GenerateUpdates_Dialog(); break;
				case NECommand.Table_Database_GenerateDeletes: dialogResult = Command_Table_Database_GenerateDeletes_Dialog(); break;
				case NECommand.Position_Goto_Lines: dialogResult = Command_Position_Goto_Dialog(GotoType.Line); break;
				case NECommand.Position_Goto_Columns: dialogResult = Command_Position_Goto_Dialog(GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: dialogResult = Command_Position_Goto_Dialog(GotoType.Index); break;
				case NECommand.Position_Goto_Positions: dialogResult = Command_Position_Goto_Dialog(GotoType.Position); break;
				case NECommand.Content_Ancestor: dialogResult = Command_Content_Ancestor_Dialog(); break;
				case NECommand.Content_Attributes: dialogResult = Command_Content_Attributes_Dialog(); break;
				case NECommand.Content_WithAttribute: dialogResult = Command_Content_WithAttribute_Dialog(); break;
				case NECommand.Content_Children_WithAttribute: dialogResult = Command_Content_Children_WithAttribute_Dialog(); break;
				case NECommand.Content_Descendants_WithAttribute: dialogResult = Command_Content_Descendants_WithAttribute_Dialog(); break;
				case NECommand.Network_AbsoluteURL: dialogResult = Command_Network_AbsoluteURL_Dialog(); break;
				case NECommand.Network_FetchFile: dialogResult = Command_Network_FetchFile_Dialog(); break;
				case NECommand.Network_FetchStream: dialogResult = Command_Network_FetchStream_Dialog(); break;
				case NECommand.Network_FetchPlaylist: dialogResult = Command_Network_FetchPlaylist_Dialog(); break;
				case NECommand.Network_Ping: dialogResult = Command_Network_Ping_Dialog(); break;
				case NECommand.Network_ScanPorts: dialogResult = Command_Network_ScanPorts_Dialog(); break;
				case NECommand.Network_WCF_GetConfig: dialogResult = Command_Network_WCF_GetConfig_Dialog(); break;
				case NECommand.Network_WCF_InterceptCalls: dialogResult = Command_Network_WCF_InterceptCalls_Dialog(); break;
				case NECommand.Database_Connect: dialogResult = Command_Database_Connect_Dialog(); break;
				case NECommand.Database_Examine: Command_Database_Examine_Dialog(); break;
				case NECommand.Select_Limit: dialogResult = Command_Select_Limit_Dialog(); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: dialogResult = Command_Select_Repeats_ByCount_Dialog(); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: dialogResult = Command_Select_Repeats_ByCount_Dialog(); break;
				case NECommand.Select_Split: dialogResult = Command_Select_Split_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		public void PreHandleCommand(NECommand command, ref object preResult)
		{
			switch (command)
			{
				case NECommand.Edit_Paste_Paste: Command_Pre_Edit_Paste_Paste(ref preResult); break;
				case NECommand.Edit_Paste_RotatePaste: Command_Pre_Edit_Paste_Paste(ref preResult); break;
				case NECommand.Select_RepeatsCaseSensitive_TabsMatch: Command_Pre_Select_Repeats_TabsMatchMismatch(ref preResult, true); break;
				case NECommand.Select_RepeatsCaseSensitive_TabsMismatch: Command_Pre_Select_Repeats_TabsMatchMismatch(ref preResult, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_TabsMatch: Command_Pre_Select_Repeats_TabsMatchMismatch(ref preResult, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_TabsMismatch: Command_Pre_Select_Repeats_TabsMatchMismatch(ref preResult, false); break;
				case NECommand.Select_Selection_ToggleAnchor: Command_Pre_Select_Selection_ToggleAnchor(ref preResult); break;
			}
		}

		bool timeNext = false;
		public void HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer, object preResult)
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

			switch (command)
			{
				case NECommand.File_New_FromSelections: Command_File_New_FromSelections(); break;
				case NECommand.File_Open_Selected: Command_File_Open_Selected(); break;
				case NECommand.File_Save_Save: Command_File_Save_Save(answer); break;
				case NECommand.File_Save_SaveAs: Command_File_SaveCopy_SaveCopy(answer); break;
				case NECommand.File_Save_SaveAsClipboard: Command_File_SaveCopy_SaveCopyClipboard(answer); break;
				case NECommand.File_Save_SaveAsByExpression: Command_File_SaveCopy_SaveCopyByExpression(dialogResult as GetExpressionDialog.Result, answer); break;
				case NECommand.File_Copy_CopyTo: Command_File_SaveCopy_SaveCopy(answer, true); break;
				case NECommand.File_Copy_CopyToClipboard: Command_File_SaveCopy_SaveCopyClipboard(answer, true); break;
				case NECommand.File_Copy_CopyToByExpression: Command_File_SaveCopy_SaveCopyByExpression(dialogResult as GetExpressionDialog.Result, answer, true); break;
				case NECommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case NECommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case NECommand.File_Copy_DisplayName: Command_File_Copy_DisplayName(); break;
				case NECommand.File_Operations_Rename: Command_File_Operations_Rename(answer); break;
				case NECommand.File_Operations_RenameClipboard: Command_File_Operations_RenameClipboard(answer); break;
				case NECommand.File_Operations_RenameByExpression: Command_File_Operations_RenameByExpression(dialogResult as GetExpressionDialog.Result, answer); break;
				case NECommand.File_Operations_Delete: Command_File_Operations_Delete(answer); break;
				case NECommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case NECommand.File_Operations_CommandPrompt: Command_File_Operations_CommandPrompt(); break;
				case NECommand.File_Operations_VCSDiff: Command_File_Operations_VCSDiff(); break;
				case NECommand.File_Operations_SetDisplayName: Command_File_Operations_SetDisplayName(dialogResult as GetExpressionDialog.Result); break;
				case NECommand.File_Close: Command_File_Close(answer); break;
				case NECommand.File_Refresh: Command_File_Refresh(answer); break;
				case NECommand.File_AutoRefresh: Command_File_AutoRefresh(multiStatus); break;
				case NECommand.File_Revert: Command_File_Revert(answer); break;
				case NECommand.File_Insert_Files: Command_File_Insert_Files(); break;
				case NECommand.File_Insert_CopiedCut: Command_File_Insert_CopiedCut(); break;
				case NECommand.File_Insert_Selected: Command_File_Insert_Selected(); break;
				case NECommand.File_Encoding_Encoding: Command_File_Encoding_Encoding(dialogResult as EncodingDialog.Result); break;
				case NECommand.File_Encoding_ReopenWithEncoding: Command_File_Encoding_ReopenWithEncoding(dialogResult as EncodingDialog.Result, answer); break;
				case NECommand.File_Encoding_LineEndings: Command_File_Encoding_LineEndings(dialogResult as FileEncodingLineEndingsDialog.Result); break;
				case NECommand.File_Encrypt: Command_File_Encrypt(dialogResult as string); break;
				case NECommand.File_Compress: Command_File_Compress(multiStatus); break;
				case NECommand.Edit_Undo: Command_Edit_Undo(); break;
				case NECommand.Edit_Redo: Command_Edit_Redo(); break;
				case NECommand.Edit_Copy_Copy: Command_Edit_Copy_CutCopy(false); break;
				case NECommand.Edit_Copy_Cut: Command_Edit_Copy_CutCopy(true); break;
				case NECommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(shiftDown, false, preResult); break;
				case NECommand.Edit_Paste_RotatePaste: Command_Edit_Paste_Paste(true, true, preResult); break;
				case NECommand.Edit_Find_Find: Command_Edit_Find_Find(shiftDown, dialogResult as EditFindFindDialog.Result); break;
				case NECommand.Edit_Find_Next: Command_Edit_Find_NextPrevious(true, shiftDown); break;
				case NECommand.Edit_Find_Previous: Command_Edit_Find_NextPrevious(false, shiftDown); break;
				case NECommand.Edit_Find_Selected: Command_Edit_Find_Selected(shiftDown); break;
				case NECommand.Edit_Find_MassFind: Command_Edit_Find_MassFind(dialogResult as EditFindMassFindDialog.Result); break;
				case NECommand.Edit_Find_Binary: Command_Edit_Find_Binary(dialogResult as EditFindBinaryDialog.Result); break;
				case NECommand.Edit_Find_Replace: Command_Edit_Find_Replace(dialogResult as EditFindReplaceDialog.Result); break;
				case NECommand.Edit_Find_ClearSearchResults: Command_Edit_Find_ClearSearchResults(); break;
				case NECommand.Edit_CopyDown: Command_Edit_CopyDown(); break;
				case NECommand.Edit_Rotate: Command_Edit_Rotate(dialogResult as EditRotateDialog.Result); break;
				case NECommand.Edit_Repeat: Command_Edit_Repeat(dialogResult as EditRepeatDialog.Result); break;
				case NECommand.Edit_Markup_Escape: Command_Edit_Markup_Escape(); break;
				case NECommand.Edit_Markup_Unescape: Command_Edit_Markup_Unescape(); break;
				case NECommand.Edit_RegEx_Escape: Command_Edit_RegEx_Escape(); break;
				case NECommand.Edit_RegEx_Unescape: Command_Edit_RegEx_Unescape(); break;
				case NECommand.Edit_URL_Escape: Command_Edit_URL_Escape(); break;
				case NECommand.Edit_URL_Unescape: Command_Edit_URL_Unescape(); break;
				case NECommand.Edit_URL_Absolute: Command_Edit_URL_Absolute(dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Edit_Data_Hash: Command_Edit_Data_Hash(dialogResult as EditDataHashDialog.Result); break;
				case NECommand.Edit_Data_Compress: Command_Edit_Data_Compress(dialogResult as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Decompress: Command_Edit_Data_Decompress(dialogResult as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Encrypt: Command_Edit_Data_Encrypt(dialogResult as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Decrypt: Command_Edit_Data_Decrypt(dialogResult as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Sign: Command_Edit_Data_Sign(dialogResult as EditDataSignDialog.Result); break;
				case NECommand.Edit_Sort: Command_Edit_Sort(dialogResult as EditSortDialog.Result); break;
				case NECommand.Edit_Convert: Command_Edit_Convert(dialogResult as EditConvertDialog.Result); break;
				case NECommand.Edit_ModifyRegions: Command_Edit_ModifyRegions(dialogResult as EditModifyRegionsDialog.Result); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Select_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Select, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Select_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Select, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Select_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Select, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Select_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Select, 9); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Set_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Set, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Set_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Set, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Set_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Set, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Set_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Set, 9); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Add_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Add, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Add_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Add, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Add_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Add, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Add_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Add, 9); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Remove_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Remove, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Remove_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Remove, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Remove_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Remove, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Remove_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Modify_Remove, 9); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Next_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Next, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Next_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Next, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Next_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Next, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Next_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Next, 9); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Previous_Region6: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Previous, 6); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Previous_Region7: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Previous, 7); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Previous_Region8: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Previous, 8); break;
				case NECommand.Edit_ModifyRegionsShortcuts_Previous_Region9: Command_Edit_ModifyRegionsShortcuts(EditModifyRegionsDialog.Action.Select_Previous, 9); break;
				case NECommand.Edit_Navigate_WordLeft: Command_Edit_Navigate_WordLeftRight(false, shiftDown); break;
				case NECommand.Edit_Navigate_WordRight: Command_Edit_Navigate_WordLeftRight(true, shiftDown); break;
				case NECommand.Edit_Navigate_AllLeft: Command_Edit_Navigate_AllLeft(shiftDown); break;
				case NECommand.Edit_Navigate_AllRight: Command_Edit_Navigate_AllRight(shiftDown); break;
				case NECommand.Edit_Navigate_JumpBy_Words: Command_Edit_Navigate_JumpBy(JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: Command_Edit_Navigate_JumpBy(JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: Command_Edit_Navigate_JumpBy(JumpByType.Paths); break;
				case NECommand.Diff_Selections: Command_Diff_Selections(); break;
				case NECommand.Diff_SelectedFiles: Command_Diff_SelectedFiles(); break;
				case NECommand.Diff_VCSNormalFiles: Command_Diff_Diff_VCSNormalFiles(); break;
				case NECommand.Diff_Regions_Region1: Command_Diff_Regions_Region(1); break;
				case NECommand.Diff_Regions_Region2: Command_Diff_Regions_Region(2); break;
				case NECommand.Diff_Regions_Region3: Command_Diff_Regions_Region(3); break;
				case NECommand.Diff_Regions_Region4: Command_Diff_Regions_Region(4); break;
				case NECommand.Diff_Regions_Region5: Command_Diff_Regions_Region(5); break;
				case NECommand.Diff_Regions_Region6: Command_Diff_Regions_Region(6); break;
				case NECommand.Diff_Regions_Region7: Command_Diff_Regions_Region(7); break;
				case NECommand.Diff_Regions_Region8: Command_Diff_Regions_Region(8); break;
				case NECommand.Diff_Regions_Region9: Command_Diff_Regions_Region(9); break;
				case NECommand.Diff_Break: Command_Diff_Break(); break;
				case NECommand.Diff_IgnoreWhitespace: Command_Diff_IgnoreWhitespace(multiStatus); break;
				case NECommand.Diff_IgnoreCase: Command_Diff_IgnoreCase(multiStatus); break;
				case NECommand.Diff_IgnoreNumbers: Command_Diff_IgnoreNumbers(multiStatus); break;
				case NECommand.Diff_IgnoreLineEndings: Command_Diff_IgnoreLineEndings(multiStatus); break;
				case NECommand.Diff_IgnoreCharacters: Command_Diff_IgnoreCharacters(dialogResult as DiffIgnoreCharactersDialog.Result); break;
				case NECommand.Diff_Reset: Command_Diff_Reset(); break;
				case NECommand.Diff_Next: Command_Diff_NextPrevious(true, shiftDown); break;
				case NECommand.Diff_Previous: Command_Diff_NextPrevious(false, shiftDown); break;
				case NECommand.Diff_CopyLeft: Command_Diff_CopyLeftRight(true); break;
				case NECommand.Diff_CopyRight: Command_Diff_CopyLeftRight(false); break;
				case NECommand.Diff_Fix_Whitespace: Command_Diff_Fix_Whitespace(dialogResult as DiffFixWhitespaceDialog.Result); break;
				case NECommand.Diff_Fix_Case: Command_Diff_Fix_Case(); break;
				case NECommand.Diff_Fix_Numbers: Command_Diff_Fix_Numbers(); break;
				case NECommand.Diff_Fix_LineEndings: Command_Diff_Fix_LineEndings(); break;
				case NECommand.Diff_Fix_Encoding: Command_Diff_Fix_Encoding(); break;
				case NECommand.Diff_Select_Match: Command_Diff_Select_MatchDiff(true); break;
				case NECommand.Diff_Select_Diff: Command_Diff_Select_MatchDiff(false); break;
				case NECommand.Files_Name_Simplify: Command_Files_Name_Simplify(); break;
				case NECommand.Files_Name_MakeAbsolute: Command_Files_Name_MakeAbsolute(dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_MakeRelative: Command_Files_Name_MakeRelative(dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_GetUnique: Command_Files_Name_GetUnique(dialogResult as FilesNamesGetUniqueDialog.Result); break;
				case NECommand.Files_Name_Sanitize: Command_Files_Name_Sanitize(); break;
				case NECommand.Files_Get_Size: Command_Files_Get_Size(); break;
				case NECommand.Files_Get_Time_Write: Command_Files_Get_Time(TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: Command_Files_Get_Time(TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: Command_Files_Get_Time(TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: Command_Files_Get_Attributes(); break;
				case NECommand.Files_Get_Version_File: Command_Files_Get_Version_File(); break;
				case NECommand.Files_Get_Version_Product: Command_Files_Get_Version_Product(); break;
				case NECommand.Files_Get_Version_Assembly: Command_Files_Get_Version_Assembly(); break;
				case NECommand.Files_Get_Children: Command_Files_Get_ChildrenDescendants(false); break;
				case NECommand.Files_Get_Descendants: Command_Files_Get_ChildrenDescendants(true); break;
				case NECommand.Files_Get_VersionControlStatus: Command_Files_Get_VersionControlStatus(); break;
				case NECommand.Files_Set_Size: Command_Files_Set_Size(dialogResult as FilesSetSizeDialog.Result); break;
				case NECommand.Files_Set_Time_Write: Command_Files_Set_Time(TimestampType.Write, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Access: Command_Files_Set_Time(TimestampType.Access, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Create: Command_Files_Set_Time(TimestampType.Create, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_All: Command_Files_Set_Time(TimestampType.All, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Attributes: Command_Files_Set_Attributes(dialogResult as FilesSetAttributesDialog.Result); break;
				case NECommand.Files_Find_Binary: Command_Files_Find_Binary(dialogResult as FilesFindBinaryDialog.Result, answer); break;
				case NECommand.Files_Find_Text: Command_Files_Find_Text(dialogResult as FilesFindTextDialog.Result, answer); break;
				case NECommand.Files_Find_MassFind: Command_Files_Find_MassFind(dialogResult as FilesFindMassFindDialog.Result, answer); break;
				case NECommand.Files_Insert: Command_Files_Insert(dialogResult as FilesInsertDialog.Result); break;
				case NECommand.Files_Create_Files: Command_Files_Create_Files(); break;
				case NECommand.Files_Create_Directories: Command_Files_Create_Directories(); break;
				case NECommand.Files_Create_FromExpressions: Command_Files_Create_FromExpressions(dialogResult as FilesCreateFromExpressionsDialog.Result); break;
				case NECommand.Files_Select_Name_Directory: Command_Files_Select_Name(GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: Command_Files_Select_Name(GetPathType.FileName); break;
				case NECommand.Files_Select_Name_FileNamewoExtension: Command_Files_Select_Name(GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: Command_Files_Select_Name(GetPathType.Extension); break;
				case NECommand.Files_Select_Name_Next: Command_Files_Select_Name_Next(); break;
				case NECommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case NECommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case NECommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case NECommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case NECommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case NECommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case NECommand.Files_Select_MatchDepth: Command_Files_Select_MatchDepth(); break;
				case NECommand.Files_Select_CommonAncestor: Command_Files_Select_CommonAncestor(); break;
				case NECommand.Files_Select_ByVersionControlStatus: Command_Files_Select_ByVersionControlStatus(dialogResult as FilesSelectByVersionControlStatusDialog.Result); break;
				case NECommand.Files_Hash: Command_Files_Hash(dialogResult as FilesHashDialog.Result); break;
				case NECommand.Files_Sign: Command_Files_Sign(dialogResult as FilesSignDialog.Result); break;
				case NECommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(dialogResult as FilesOperationsCopyMoveDialog.Result, false); break;
				case NECommand.Files_Operations_Move: Command_Files_Operations_CopyMove(dialogResult as FilesOperationsCopyMoveDialog.Result, true); break;
				case NECommand.Files_Operations_Delete: Command_Files_Operations_Delete(); break;
				case NECommand.Files_Operations_DragDrop: Command_Files_Operations_DragDrop(); break;
				case NECommand.Files_Operations_Explore: Command_Files_Operations_Explore(); break;
				case NECommand.Files_Operations_CommandPrompt: Command_Files_Operations_CommandPrompt(); break;
				case NECommand.Files_Operations_RunCommand_Parallel: Command_Files_Operations_RunCommand_Parallel(); break;
				case NECommand.Files_Operations_RunCommand_Sequential: Command_Files_Operations_RunCommand_Sequential(); break;
				case NECommand.Files_Operations_RunCommand_Shell: Command_Files_Operations_RunCommand_Shell(); break;
				case NECommand.Files_Operations_Encoding: Command_Files_Operations_Encoding(dialogResult as FilesOperationsEncodingDialog.Result); break;
				case NECommand.Files_Operations_SplitFile: Command_Files_Operations_SplitFile(dialogResult as FilesOperationsSplitFileDialog.Result); break;
				case NECommand.Files_Operations_CombineFiles: Command_Files_Operations_CombineFiles(dialogResult as FilesOperationsCombineFilesDialog.Result); break;
				case NECommand.Expression_Expression: Command_Expression_Expression(dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_Copy: Command_Expression_Copy(dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_EvaluateSelected: Command_Expression_EvaluateSelected(); break;
				case NECommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_InlineVariables_Add: Command_Expression_InlineVariables_Add(); break;
				case NECommand.Expression_InlineVariables_Calculate: Command_Expression_InlineVariables_Calculate(); break;
				case NECommand.Expression_InlineVariables_Solve: Command_Expression_InlineVariables_Solve(dialogResult as ExpressionSolveDialog.Result, answer); break;
				case NECommand.Expression_InlineVariables_IncludeInExpressions: Command_Expression_InlineVariables_IncludeInExpressions(multiStatus); break;
				case NECommand.Text_Select_Trim: Command_Text_Select_Trim(dialogResult as TextTrimDialog.Result); break;
				case NECommand.Text_Select_ByWidth: Command_Text_Select_ByWidth(dialogResult as TextWidthDialog.Result); break;
				case NECommand.Text_Select_WholeWord: Command_Text_Select_WholeBoundedWord(dialogResult as TextSelectWholeBoundedWordDialog.Result, true); break;
				case NECommand.Text_Select_BoundedWord: Command_Text_Select_WholeBoundedWord(dialogResult as TextSelectWholeBoundedWordDialog.Result, false); break;
				case NECommand.Text_Select_Min_Text: Command_Text_Select_MinMax_Text(false); break;
				case NECommand.Text_Select_Min_Length: Command_Text_Select_MinMax_Length(false); break;
				case NECommand.Text_Select_Max_Text: Command_Text_Select_MinMax_Text(true); break;
				case NECommand.Text_Select_Max_Length: Command_Text_Select_MinMax_Length(true); break;
				case NECommand.Text_Case_Upper: Command_Text_Case_Upper(); break;
				case NECommand.Text_Case_Lower: Command_Text_Case_Lower(); break;
				case NECommand.Text_Case_Proper: Command_Text_Case_Proper(); break;
				case NECommand.Text_Case_Toggle: Command_Text_Case_Toggle(); break;
				case NECommand.Text_Length: Command_Text_Length(); break;
				case NECommand.Text_Width: Command_Text_Width(dialogResult as TextWidthDialog.Result); break;
				case NECommand.Text_Trim: Command_Text_Trim(dialogResult as TextTrimDialog.Result); break;
				case NECommand.Text_SingleLine: Command_Text_SingleLine(); break;
				case NECommand.Text_Unicode: Command_Text_Unicode(dialogResult as TextUnicodeDialog.Result); break;
				case NECommand.Text_GUID: Command_Text_GUID(); break;
				case NECommand.Text_RandomText: Command_Text_RandomText(dialogResult as TextRandomTextDialog.Result); break;
				case NECommand.Text_LoremIpsum: Command_Text_LoremIpsum(); break;
				case NECommand.Text_ReverseRegEx: Command_Text_ReverseRegEx(dialogResult as TextReverseRegExDialog.Result); break;
				case NECommand.Text_FirstDistinct: Command_Text_FirstDistinct(dialogResult as TextFirstDistinctDialog.Result); break;
				case NECommand.Text_RepeatCount: Command_Text_RepeatCount(); break;
				case NECommand.Text_RepeatIndex: Command_Text_RepeatIndex(); break;
				case NECommand.Numeric_Select_Min: Command_Numeric_Select_MinMax(false); break;
				case NECommand.Numeric_Select_Max: Command_Numeric_Select_MinMax(true); break;
				case NECommand.Numeric_Select_Fraction_Whole: Command_Numeric_Select_Fraction_Whole(); break;
				case NECommand.Numeric_Select_Fraction_Fraction: Command_Numeric_Select_Fraction_Fraction(); break;
				case NECommand.Numeric_Hex_ToHex: Command_Numeric_Hex_ToHex(); break;
				case NECommand.Numeric_Hex_FromHex: Command_Numeric_Hex_FromHex(); break;
				case NECommand.Numeric_ConvertBase: Command_Numeric_ConvertBase(dialogResult as NumericConvertBaseDialog.Result); break;
				case NECommand.Numeric_Series_ZeroBased: Command_Numeric_Series_ZeroBased(); break;
				case NECommand.Numeric_Series_OneBased: Command_Numeric_Series_OneBased(); break;
				case NECommand.Numeric_Series_Linear: Command_Numeric_Series_LinearGeometric(dialogResult as NumericSeriesDialog.Result, true); break;
				case NECommand.Numeric_Series_Geometric: Command_Numeric_Series_LinearGeometric(dialogResult as NumericSeriesDialog.Result, false); break;
				case NECommand.Numeric_Scale: Command_Numeric_Scale(dialogResult as NumericScaleDialog.Result); break;
				case NECommand.Numeric_Add_Sum: Command_Numeric_Add_Sum(); break;
				case NECommand.Numeric_Add_ForwardSum: Command_Numeric_Add_ForwardReverseSum(true, false); break;
				case NECommand.Numeric_Add_ReverseSum: Command_Numeric_Add_ForwardReverseSum(false, false); break;
				case NECommand.Numeric_Add_UndoForwardSum: Command_Numeric_Add_ForwardReverseSum(true, true); break;
				case NECommand.Numeric_Add_UndoReverseSum: Command_Numeric_Add_ForwardReverseSum(false, true); break;
				case NECommand.Numeric_Add_Increment: Command_Numeric_Add_IncrementDecrement(true); break;
				case NECommand.Numeric_Add_Decrement: Command_Numeric_Add_IncrementDecrement(false); break;
				case NECommand.Numeric_Add_AddClipboard: Command_Numeric_Add_AddSubtractClipboard(true); break;
				case NECommand.Numeric_Add_SubtractClipboard: Command_Numeric_Add_AddSubtractClipboard(false); break;
				case NECommand.Numeric_Fraction_Whole: Command_Numeric_Fraction_Whole(); break;
				case NECommand.Numeric_Fraction_Fraction: Command_Numeric_Fraction_Fraction(); break;
				case NECommand.Numeric_Fraction_Simplify: Command_Numeric_Fraction_Simplify(); break;
				case NECommand.Numeric_Absolute: Command_Numeric_Absolute(); break;
				case NECommand.Numeric_Floor: Command_Numeric_Floor(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Ceiling: Command_Numeric_Ceiling(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Round: Command_Numeric_Round(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Limit: Command_Numeric_Limit(dialogResult as NumericLimitDialog.Result); break;
				case NECommand.Numeric_Cycle: Command_Numeric_Cycle(dialogResult as NumericCycleDialog.Result); break;
				case NECommand.Numeric_Trim: Command_Numeric_Trim(); break;
				case NECommand.Numeric_Factor: Command_Numeric_Factor(); break;
				case NECommand.Numeric_RandomNumber: Command_Numeric_RandomNumber(dialogResult as NumericRandomNumberDialog.Result); break;
				case NECommand.Numeric_CombinationsPermutations: Command_Numeric_CombinationsPermutations(dialogResult as NumericCombinationsPermutationsDialog.Result); break;
				case NECommand.Numeric_MinMaxValues: Command_Numeric_MinMaxValues(dialogResult as NumericMinMaxValuesDialog.Result); break;
				case NECommand.DateTime_Now: Command_DateTime_Now(); break;
				case NECommand.DateTime_UtcNow: Command_DateTime_UtcNow(); break;
				case NECommand.DateTime_Convert: Command_DateTime_Convert(dialogResult as DateTimeConvertDialog.Result); break;
				case NECommand.Image_GrabColor: Command_Image_GrabColor(dialogResult as ImageGrabColorDialog.Result); break;
				case NECommand.Image_GrabImage: Command_Image_GrabImage(dialogResult as ImageGrabImageDialog.Result); break;
				case NECommand.Image_AdjustColor: Command_Image_AdjustColor(dialogResult as ImageAdjustColorDialog.Result); break;
				case NECommand.Image_AddColor: Command_Image_AddColor(dialogResult as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_OverlayColor: Command_Image_OverlayColor(dialogResult as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_Size: Command_Image_Size(dialogResult as ImageSizeDialog.Result); break;
				case NECommand.Image_Crop: Command_Image_Crop(dialogResult as ImageCropDialog.Result); break;
				case NECommand.Image_FlipHorizontal: Command_Image_FlipHorizontal(); break;
				case NECommand.Image_FlipVertical: Command_Image_FlipVertical(); break;
				case NECommand.Image_Rotate: Command_Image_Rotate(dialogResult as ImageRotateDialog.Result); break;
				case NECommand.Image_GIF_Animate: Command_Image_GIF_Animate(dialogResult as ImageGIFAnimateDialog.Result); break;
				case NECommand.Image_GIF_Split: Command_Image_GIF_Split(dialogResult as ImageGIFSplitDialog.Result); break;
				case NECommand.Table_DetectType: Command_Table_DetectType(); break;
				case NECommand.Table_Convert: Command_Table_Convert(dialogResult as TableConvertDialog.Result); break;
				case NECommand.Table_TextToTable: Command_Table_TextToTable(dialogResult as TableTextToTableDialog.Result); break;
				case NECommand.Table_LineSelectionsToTable: Command_Table_LineSelectionsToTable(); break;
				case NECommand.Table_RegionSelectionsToTable_Region1: Command_Table_RegionSelectionsToTable_Region(1); break;
				case NECommand.Table_RegionSelectionsToTable_Region2: Command_Table_RegionSelectionsToTable_Region(2); break;
				case NECommand.Table_RegionSelectionsToTable_Region3: Command_Table_RegionSelectionsToTable_Region(3); break;
				case NECommand.Table_RegionSelectionsToTable_Region4: Command_Table_RegionSelectionsToTable_Region(4); break;
				case NECommand.Table_RegionSelectionsToTable_Region5: Command_Table_RegionSelectionsToTable_Region(5); break;
				case NECommand.Table_RegionSelectionsToTable_Region6: Command_Table_RegionSelectionsToTable_Region(6); break;
				case NECommand.Table_RegionSelectionsToTable_Region7: Command_Table_RegionSelectionsToTable_Region(7); break;
				case NECommand.Table_RegionSelectionsToTable_Region8: Command_Table_RegionSelectionsToTable_Region(8); break;
				case NECommand.Table_RegionSelectionsToTable_Region9: Command_Table_RegionSelectionsToTable_Region(9); break;
				case NECommand.Table_EditTable: Command_Table_EditTable(dialogResult as TableEditTableDialog.Result); break;
				case NECommand.Table_AddHeaders: Command_Table_AddHeaders(); break;
				case NECommand.Table_AddRow: Command_Table_AddRow(); break;
				case NECommand.Table_AddColumn: Command_Table_AddColumn(dialogResult as TableAddColumnDialog.Result); break;
				case NECommand.Table_Select_RowsByExpression: Command_Table_Select_RowsByExpression(dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Table_SetJoinSource: Command_Table_SetJoinSource(); break;
				case NECommand.Table_Join: Command_Table_Join(dialogResult as TableJoinDialog.Result); break;
				case NECommand.Table_Transpose: Command_Table_Transpose(); break;
				case NECommand.Table_Database_GenerateInserts: Command_Table_Database_GenerateInserts(dialogResult as TableDatabaseGenerateInsertsDialog.Result); break;
				case NECommand.Table_Database_GenerateUpdates: Command_Table_Database_GenerateUpdates(dialogResult as TableDatabaseGenerateUpdatesDialog.Result); break;
				case NECommand.Table_Database_GenerateDeletes: Command_Table_Database_GenerateDeletes(dialogResult as TableDatabaseGenerateDeletesDialog.Result); break;
				case NECommand.Position_Goto_Lines: Command_Position_Goto(GotoType.Line, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Columns: Command_Position_Goto(GotoType.Column, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Indexes: Command_Position_Goto(GotoType.Index, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Positions: Command_Position_Goto(GotoType.Position, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Copy_Lines: Command_Position_Copy(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Command_Position_Copy(GotoType.Column, !shiftDown); break;
				case NECommand.Position_Copy_Indexes: Command_Position_Copy(GotoType.Index, !shiftDown); break;
				case NECommand.Position_Copy_Positions: Command_Position_Copy(GotoType.Position, false); break;
				case NECommand.Content_Type_SetFromExtension: Command_Content_Type_SetFromExtension(); break;
				case NECommand.Content_Type_None: Command_Content_Type(ParserType.None); break;
				case NECommand.Content_Type_Balanced: Command_Content_Type(ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: Command_Content_Type(ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: Command_Content_Type(ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: Command_Content_Type(ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: Command_Content_Type(ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: Command_Content_Type(ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: Command_Content_Type(ParserType.HTML); break;
				case NECommand.Content_Type_JSON: Command_Content_Type(ParserType.JSON); break;
				case NECommand.Content_Type_SQL: Command_Content_Type(ParserType.SQL); break;
				case NECommand.Content_Type_TSV: Command_Content_Type(ParserType.TSV); break;
				case NECommand.Content_Type_XML: Command_Content_Type(ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: Command_Content_HighlightSyntax(multiStatus); break;
				case NECommand.Content_StrictParsing: Command_Content_StrictParsing(multiStatus); break;
				case NECommand.Content_Reformat: Command_Content_Reformat(); break;
				case NECommand.Content_Comment: Command_Content_Comment(); break;
				case NECommand.Content_Uncomment: Command_Content_Uncomment(); break;
				case NECommand.Content_TogglePosition: Command_Content_TogglePosition(shiftDown); break;
				case NECommand.Content_Current: Command_Content_Current(); break;
				case NECommand.Content_Parent: Command_Content_Parent(); break;
				case NECommand.Content_Ancestor: Command_Content_Ancestor(dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Attributes: Command_Content_Attributes(dialogResult as ContentAttributesDialog.Result); break;
				case NECommand.Content_WithAttribute: Command_Content_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Children_Children: Command_Content_Children_Children(); break;
				case NECommand.Content_Children_SelfAndChildren: Command_Content_Children_SelfAndChildren(); break;
				case NECommand.Content_Children_First: Command_Content_Children_First(); break;
				case NECommand.Content_Children_WithAttribute: Command_Content_Children_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Descendants_Descendants: Command_Content_Descendants_Descendants(); break;
				case NECommand.Content_Descendants_SelfAndDescendants: Command_Content_Descendants_SelfAndDescendants(); break;
				case NECommand.Content_Descendants_First: Command_Content_Descendants_First(); break;
				case NECommand.Content_Descendants_WithAttribute: Command_Content_Descendants_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Navigate_Up: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Up, shiftDown); break;
				case NECommand.Content_Navigate_Down: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Down, shiftDown); break;
				case NECommand.Content_Navigate_Left: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Left, shiftDown); break;
				case NECommand.Content_Navigate_Right: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Right, shiftDown); break;
				case NECommand.Content_Navigate_Home: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Home, shiftDown); break;
				case NECommand.Content_Navigate_End: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.End, shiftDown); break;
				case NECommand.Content_Navigate_PgUp: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgUp, shiftDown); break;
				case NECommand.Content_Navigate_PgDn: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgDn, shiftDown); break;
				case NECommand.Content_Navigate_Row: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: Command_Content_KeepSelections(multiStatus); break;
				case NECommand.Network_AbsoluteURL: Command_Network_AbsoluteURL(dialogResult as NetworkAbsoluteURLDialog.Result); break;
				case NECommand.Network_Fetch: Command_Network_Fetch(); break;
				case NECommand.Network_FetchHex: Command_Network_Fetch(Coder.CodePage.Hex); break;
				case NECommand.Network_FetchFile: Command_Network_FetchFile(dialogResult as NetworkFetchFileDialog.Result); break;
				case NECommand.Network_FetchStream: Command_Network_FetchStream(dialogResult as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_FetchPlaylist: Command_Network_FetchPlaylist(dialogResult as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_Lookup_IP: Command_Network_Lookup_IP(); break;
				case NECommand.Network_Lookup_HostName: Command_Network_Lookup_HostName(); break;
				case NECommand.Network_AdaptersInfo: Command_Network_AdaptersInfo(); break;
				case NECommand.Network_Ping: Command_Network_Ping(dialogResult as NetworkPingDialog.Result); break;
				case NECommand.Network_ScanPorts: Command_Network_ScanPorts(dialogResult as NetworkScanPortsDialog.Result); break;
				case NECommand.Network_WCF_GetConfig: Command_Network_WCF_GetConfig(dialogResult as NetworkWCFGetConfig.Result); break;
				case NECommand.Network_WCF_Execute: Command_Network_WCF_Execute(); break;
				case NECommand.Network_WCF_InterceptCalls: Command_Network_WCF_InterceptCalls(dialogResult as NetworkWCFInterceptCallsDialog.Result); break;
				case NECommand.Network_WCF_ResetClients: Command_Network_WCF_ResetClients(); break;
				case NECommand.Database_Connect: Command_Database_Connect(dialogResult as DatabaseConnectDialog.Result); break;
				case NECommand.Database_ExecuteQuery: Command_Database_ExecuteQuery(); break;
				case NECommand.Database_GetSproc: Command_Database_GetSproc(); break;
				case NECommand.Keys_Set_KeysCaseSensitive: Command_Keys_Set(0, true); break;
				case NECommand.Keys_Set_KeysCaseInsensitive: Command_Keys_Set(0, false); break;
				case NECommand.Keys_Set_Values1: Command_Keys_Set(1); break;
				case NECommand.Keys_Set_Values2: Command_Keys_Set(2); break;
				case NECommand.Keys_Set_Values3: Command_Keys_Set(3); break;
				case NECommand.Keys_Set_Values4: Command_Keys_Set(4); break;
				case NECommand.Keys_Set_Values5: Command_Keys_Set(5); break;
				case NECommand.Keys_Set_Values6: Command_Keys_Set(6); break;
				case NECommand.Keys_Set_Values7: Command_Keys_Set(7); break;
				case NECommand.Keys_Set_Values8: Command_Keys_Set(8); break;
				case NECommand.Keys_Set_Values9: Command_Keys_Set(9); break;
				case NECommand.Keys_Add_Keys: Command_Keys_Add(0); break;
				case NECommand.Keys_Add_Values1: Command_Keys_Add(1); break;
				case NECommand.Keys_Add_Values2: Command_Keys_Add(2); break;
				case NECommand.Keys_Add_Values3: Command_Keys_Add(3); break;
				case NECommand.Keys_Add_Values4: Command_Keys_Add(4); break;
				case NECommand.Keys_Add_Values5: Command_Keys_Add(5); break;
				case NECommand.Keys_Add_Values6: Command_Keys_Add(6); break;
				case NECommand.Keys_Add_Values7: Command_Keys_Add(7); break;
				case NECommand.Keys_Add_Values8: Command_Keys_Add(8); break;
				case NECommand.Keys_Add_Values9: Command_Keys_Add(9); break;
				case NECommand.Keys_Remove_Keys: Command_Keys_Remove(0); break;
				case NECommand.Keys_Remove_Values1: Command_Keys_Remove(1); break;
				case NECommand.Keys_Remove_Values2: Command_Keys_Remove(2); break;
				case NECommand.Keys_Remove_Values3: Command_Keys_Remove(3); break;
				case NECommand.Keys_Remove_Values4: Command_Keys_Remove(4); break;
				case NECommand.Keys_Remove_Values5: Command_Keys_Remove(5); break;
				case NECommand.Keys_Remove_Values6: Command_Keys_Remove(6); break;
				case NECommand.Keys_Remove_Values7: Command_Keys_Remove(7); break;
				case NECommand.Keys_Remove_Values8: Command_Keys_Remove(8); break;
				case NECommand.Keys_Remove_Values9: Command_Keys_Remove(9); break;
				case NECommand.Keys_Replace_Values1: Command_Keys_Replace(1); break;
				case NECommand.Keys_Replace_Values2: Command_Keys_Replace(2); break;
				case NECommand.Keys_Replace_Values3: Command_Keys_Replace(3); break;
				case NECommand.Keys_Replace_Values4: Command_Keys_Replace(4); break;
				case NECommand.Keys_Replace_Values5: Command_Keys_Replace(5); break;
				case NECommand.Keys_Replace_Values6: Command_Keys_Replace(6); break;
				case NECommand.Keys_Replace_Values7: Command_Keys_Replace(7); break;
				case NECommand.Keys_Replace_Values8: Command_Keys_Replace(8); break;
				case NECommand.Keys_Replace_Values9: Command_Keys_Replace(9); break;
				case NECommand.Select_All: Command_Select_All(); break;
				case NECommand.Select_Nothing: Command_Select_Nothing(); break;
				case NECommand.Select_Limit: Command_Select_Limit(dialogResult as SelectLimitDialog.Result); break;
				case NECommand.Select_Lines: Command_Select_Lines(); break;
				case NECommand.Select_WholeLines: Command_Select_WholeLines(); break;
				case NECommand.Select_Rectangle: Command_Select_Rectangle(); break;
				case NECommand.Select_Invert: Command_Select_Invert(); break;
				case NECommand.Select_Join: Command_Select_Join(); break;
				case NECommand.Select_Empty: Command_Select_Empty(true); break;
				case NECommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case NECommand.Select_ToggleOpenClose: Command_Select_ToggleOpenClose(shiftDown); break;
				case NECommand.Select_RepeatsCaseSensitive_Unique: Command_Select_Repeats_Unique(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Duplicates: Command_Select_Repeats_Duplicates(true); break;
				case NECommand.Select_RepeatsCaseSensitive_MatchPrevious: Command_Select_Repeats_MatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_NonMatchPrevious: Command_Select_Repeats_NonMatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_RepeatedLines: Command_Select_Repeats_RepeatedLines(true); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: Command_Select_Repeats_ByCount(dialogResult as SelectByCountDialog.Result, true); break;
				case NECommand.Select_RepeatsCaseSensitive_TabsMatch: Command_Select_Repeats_TabsMatchMismatch(preResult, true); break;
				case NECommand.Select_RepeatsCaseSensitive_TabsMismatch: Command_Select_Repeats_TabsMatchMismatch(preResult, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Unique: Command_Select_Repeats_Unique(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Duplicates: Command_Select_Repeats_Duplicates(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_MatchPrevious: Command_Select_Repeats_MatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_NonMatchPrevious: Command_Select_Repeats_NonMatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_RepeatedLines: Command_Select_Repeats_RepeatedLines(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: Command_Select_Repeats_ByCount(dialogResult as SelectByCountDialog.Result, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_TabsMatch: Command_Select_Repeats_TabsMatchMismatch(preResult, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_TabsMismatch: Command_Select_Repeats_TabsMatchMismatch(preResult, false); break;
				case NECommand.Select_Split: Command_Select_Split(dialogResult as SelectSplitDialog.Result); break;
				case NECommand.Select_Selection_First: Command_Select_Selection_First(); break;
				case NECommand.Select_Selection_CenterVertically: Command_Select_Selection_CenterVertically(); break;
				case NECommand.Select_Selection_Center: Command_Select_Selection_Center(); break;
				case NECommand.Select_Selection_ToggleAnchor: Command_Select_Selection_ToggleAnchor(preResult); break;
				case NECommand.Select_Selection_Next: Command_Select_Selection_NextPrevious(true); break;
				case NECommand.Select_Selection_Previous: Command_Select_Selection_NextPrevious(false); break;
				case NECommand.Select_Selection_Single: Command_Select_Selection_Single(); break;
				case NECommand.Select_Selection_Remove: Command_Select_Selection_Remove(); break;
				case NECommand.Select_Selection_RemoveBeforeCurrent: Command_Select_Selection_RemoveBeforeCurrent(); break;
				case NECommand.Select_Selection_RemoveAfterCurrent: Command_Select_Selection_RemoveAfterCurrent(); break;
				case NECommand.Macro_RepeatLastAction: Command_Macro_RepeatLastAction(); break;
				case NECommand.Macro_TimeNextAction: timeNext = !timeNext; break;
				case NECommand.Window_TabIndex: Command_Window_TabIndex(false); break;
				case NECommand.Window_ActiveTabIndex: Command_Window_TabIndex(true); break;
				case NECommand.Window_ViewValues: Command_Window_ViewValues(multiStatus); break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != NECommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message(TabsParent)
				{
					Title = "Timer",
					Text = $"Elapsed time: {elapsed:n} ms",
					Options = MessageOptions.Ok,
				}.Show();
			}
		}

		public void PreHandleKey(Key key, bool shiftDown, bool controlDown, bool altDown, ref object previousData)
		{
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					previousData = (previousData as bool? ?? false) || (Selections.Any(range => range.HasSelection));
					break;
			}
		}

		public bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown, object previousData)
		{
			var ret = true;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if ((bool)previousData)
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var offset = range.Start;
							var anchor = range.Anchor;

							if (controlDown)
							{
								if (key == Key.Back)
									offset = GetPrevWord(offset);
								else
									offset = GetNextWord(offset);
							}
							else if ((shiftDown) && (key == Key.Delete))
							{
								var line = Data.GetOffsetLine(offset);
								offset = Data.GetOffset(line, 0);
								anchor = offset + Data.GetLineLength(line) + Data.GetEndingLength(line);
							}
							else
							{
								var line = Data.GetOffsetLine(offset);
								var index = Data.GetOffsetIndex(offset, line);

								if (key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										return null;
									index = Data.GetLineLength(line);
								}
								if (index > Data.GetLineLength(line))
								{
									++line;
									if (line >= Data.NumLines)
										return null;
									index = 0;
								}

								offset = Data.GetOffset(line, index);
							}

							return new Range(offset, anchor);
						}).Where(range => range != null).ToList(), null);
					}
					break;
				case Key.Escape:
					SetSearches(new List<Range>());
					DragFiles = null;
					ViewValuesFindValue = null;
					if (Settings.EscapeClearsSelections)
					{
						HandleCommand(NECommand.Select_Selection_Single, false, null, null, null, null);
						if (!Selections.Any())
						{
							var pos = Data.GetOffset(Math.Max(0, Math.Min(yScrollValue, Data.NumLines - 1)), 0);
							SetSelections(new List<Range> { new Range(pos) });
						}
					}
					break;
				case Key.Left:
					{
						SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, int.MaxValue, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Right:
					{
						SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.End);
							else if ((index == Data.GetLineLength(line)) && (line != Data.NumLines - 1))
								return MoveCursor(range, 1, 0, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!controlDown)
							SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList());
						else if (!shiftDown)
							yScrollValue += mult;
						else if (key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, BeginOffset, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(BeginRange);
						SetSelections(sels);
					}
					else
					{
						var sels = new List<Range>();
						bool changed = false;
						foreach (var selection in Selections)
						{
							var line = Data.GetOffsetLine(selection.Cursor);
							var index = Data.GetOffsetIndex(selection.Cursor, line);

							int first;
							var end = Data.GetLineLength(line);
							for (first = 0; first < end; ++first)
							{
								if (!char.IsWhiteSpace(Data[line, first]))
									break;
							}
							if (first == end)
								first = 0;

							if (first != index)
								changed = true;
							sels.Add(MoveCursor(selection, 0, first, shiftDown, indexRel: false));
						}
						if (!changed)
						{
							sels = sels.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, shiftDown, indexRel: false)).ToList();
							xScrollValue = 0;
						}
						SetSelections(sels);
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, EndOffset, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(EndRange);
						SetSelections(sels);
					}
					else
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, shiftDown, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList());
					}
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList());
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (Data.GetOffsetLine(range.Start) == Data.GetOffsetLine(Math.Max(range.Start, range.End - 1)))))
						{
							if (!shiftDown)
								HandleText("\t");
							else
							{
								var tabs = Selections.AsParallel().AsOrdered().Where(range => (range.Start != 0) && (Data.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, null);
							}
							break;
						}

						var selLines = Selections.AsParallel().AsOrdered().Where(a => a.HasSelection).Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy().ToDictionary(line => line, line => Data.GetOffset(line, 0));
						int offset;
						string replace;
						if (shiftDown)
						{
							offset = 1;
							replace = "";
							lines = lines.Where(entry => (Data.GetLineLength(entry.Key) != 0) && (Data[entry.Key, 0] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							offset = 0;
							replace = "\t";
							lines = lines.Where(entry => Data.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, offset)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert);
					}
					break;
				case Key.Enter:
					HandleText(Data.DefaultEnding);
					break;
				default: ret = false; break;
			}

			return ret;
		}

		public bool HandleText(string text)
		{
			if (text.Length == 0)
				return true;

			ReplaceSelections(text, false, tryJoinUndo: true);
			return true;
		}

		void MouseHandler(Point mousePos, int clickCount, bool selecting)
		{
			var sels = Selections.ToList();
			var line = Math.Max(0, Math.Min(Data.NumLines - 1, (int)(mousePos.Y / LineHeight) + yScrollValue));
			var column = Math.Max(0, Math.Min(Data.GetLineColumnsLength(line), (int)(mousePos.X / Font.CharWidth) + xScrollValue));
			var index = Data.GetIndexFromColumn(line, column, true);
			var offset = Data.GetOffset(line, index);
			var mouseRange = CurrentSelection < sels.Count ? sels[CurrentSelection] : null;

			var currentSelection = default(Range);
			if (selecting)
			{
				if (mouseRange != null)
				{
					sels.Remove(mouseRange);
					var anchor = mouseRange.Anchor;
					if (clickCount != 1)
					{
						if (offset < anchor)
							offset = GetPrevWord(offset + 1);
						else
							offset = GetNextWord(offset);

						if ((mouseRange.Cursor <= anchor) != (offset <= anchor))
						{
							if (offset <= anchor)
								anchor = GetNextWord(anchor);
							else
								anchor = GetPrevWord(anchor);
						}
					}

					currentSelection = new Range(offset, anchor);
				}
			}
			else
			{
				if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
					sels.Clear();

				if (clickCount == 1)
					currentSelection = new Range(offset);
				else
				{
					if (mouseRange != null)
						sels.Remove(mouseRange);
					currentSelection = new Range(GetNextWord(offset), GetPrevWord(Math.Min(offset + 1, EndOffset)));
				}
			}

			if (currentSelection != null)
				sels.Add(currentSelection);
			SetSelections(sels);
			if (currentSelection != null)
				CurrentSelection = Selections.IndexOf(currentSelection);
		}

		public Range MoveCursor(Range range, int cursor, bool selecting)
		{
			cursor = Math.Max(BeginOffset, Math.Min(cursor, EndOffset));
			if (selecting)
				if (range.Cursor == cursor)
					return range;
				else
					return new Range(cursor, range.Anchor);

			if ((range.Cursor == cursor) && (range.Anchor == cursor))
				return range;
			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool selecting, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(range.Cursor);
				var startIndex = Data.GetOffsetIndex(range.Cursor, startLine);

				if (lineRel)
					line = Data.SkipDiffGaps(line + startLine, line > 0 ? 1 : -1);
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data.GetLineLength(line)));
			return MoveCursor(range, Data.GetOffset(line, index), selecting);
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
			drawBounds.EndLine = Math.Min(Data.NumLines, drawBounds.StartLine + yScrollViewportCeiling);
			drawBounds.StartColumn = xScrollValue;
			drawBounds.EndColumn = Math.Min(Data.MaxColumn + 1, drawBounds.StartColumn + xScrollViewportCeiling);

			var lines = Enumerable.Range(drawBounds.StartLine, drawBounds.EndLine - drawBounds.StartLine);
			drawBounds.LineRanges = lines.ToDictionary(line => line, line => new Range(Data.GetOffset(line, 0), Data.GetOffset(line, Data.GetLineLength(line) + 1)));
			drawBounds.StartIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, drawBounds.StartColumn, true));
			drawBounds.EndIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, drawBounds.EndColumn, true));
			return drawBounds;
		}

		void RenderCarets(DrawingContext dc, DrawBounds drawBounds)
		{
			for (var selectionCtr = 0; selectionCtr < Selections.Count; ++selectionCtr)
			{
				var range = Selections[selectionCtr];

				if ((range.End < drawBounds.ScreenStart) || (range.Start > drawBounds.ScreenEnd))
					continue;

				var startLine = Data.GetOffsetLine(range.Start);
				var endLine = Data.GetOffsetLine(range.End);
				var cursorLine = range.Cursor == range.Start ? startLine : endLine;
				startLine = Math.Max(drawBounds.StartLine, startLine);
				endLine = Math.Min(drawBounds.EndLine, endLine + 1);

				if ((cursorLine < startLine) || (cursorLine >= endLine))
					continue;

				if (selectionCtr == CurrentSelection)
					dc.DrawRoundedRectangle(highlightRowBrush, lightlightRowPen, new Rect(-2, drawBounds.Y(cursorLine), canvas.ActualWidth + 4, Font.FontSize), 4, 4);

				var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
				if ((cursor >= drawBounds.StartIndexes[cursorLine]) && (cursor <= drawBounds.EndIndexes[cursorLine]))
				{
					cursor = Data.GetColumnFromIndex(cursorLine, cursor);
					for (var pass = selectionCtr == CurrentSelection ? 2 : 1; pass > 0; --pass)
						dc.DrawRectangle(caretBrush, null, new Rect(drawBounds.X(cursor) - 1, drawBounds.Y(cursorLine), 2, LineHeight));
				}
			}
		}

		void RenderIndicators(DrawingContext dc, DrawBounds drawBounds, Range visibleCursor, RangeList ranges, Brush brush, Pen pen, double leftSpacing, double rightSpacing)
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
			var startLine = Data.GetOffsetLine(range.Start);
			var startColumn = Data.GetColumnFromIndex(startLine, Data.GetOffsetIndex(range.Start, startLine));

			var endLine = Data.GetOffsetLine(range.End);
			var endColumn = Data.GetColumnFromIndex(endLine, Data.GetOffsetIndex(range.End, endLine));
			if ((endLine != startLine) && (endColumn == 0))
			{
				--endLine;
				endColumn = Data.GetLineColumnsLength(endLine) + 1;
			}

			var points = new List<Point>();

			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine) + Font.FontSize / 2));
			points.Add(new Point(drawBounds.X(startColumn) + leftSpacing, drawBounds.Y(startLine)));
			for (var line = startLine; ; ++line)
			{
				var done = line == endLine;
				if ((line >= drawBounds.StartLine) && ((line < drawBounds.EndLine)))
				{
					var length = done ? endColumn : Data.GetLineColumnsLength(line) + 1;
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
			int? startDiff = null;
			for (var line = drawBounds.StartLine; ; ++line)
			{
				var done = line == drawBounds.EndLine;

				var matchType = done ? TextData.DiffType.Match : Data.GetLineDiffType(line);
				if (matchType != TextData.DiffType.Match)
				{
					startDiff = startDiff ?? line;

					if (!matchType.HasFlag(TextData.DiffType.HasGap))
					{
						startDiff = startDiff ?? line;

						var map = Data.GetLineColumnMap(line, true);
						foreach (var tuple in Data.GetLineColumnDiffs(line))
						{
							var start = map[tuple.Item1];
							var end = map[tuple.Item2];
							if (end >= start)
								dc.DrawRectangle(diffColBrush, null, new Rect(drawBounds.X(start) - 1, drawBounds.Y(line), (end - start) * Font.CharWidth + 2, Font.FontSize));
						}
					}
				}

				if ((startDiff.HasValue) && (matchType == TextData.DiffType.Match))
				{
					dc.DrawRoundedRectangle(diffLineBrush, diffLinePen, new Rect(-2, drawBounds.Y(startDiff.Value), canvas.ActualWidth + 4, drawBounds.Y(line) - drawBounds.Y(startDiff.Value) - Spacing + 1), 4, 4);
					startDiff = null;
				}

				if (done)
					break;
			}
		}

		void RenderText(DrawingContext dc, DrawBounds drawBounds)
		{
			const int HighlightRegexSize = 500;

			var highlightDictionary = HighlightSyntax ? Highlight.Get(ContentType)?.GetDictionary() : null;

			for (var line = drawBounds.StartLine; line < drawBounds.EndLine; ++line)
			{
				var lineColumns = Data.GetLineColumns(line);
				if (lineColumns.Length <= drawBounds.StartColumn)
					continue;

				var str = lineColumns.Substring(drawBounds.StartColumn, Math.Min(drawBounds.EndColumn, lineColumns.Length) - drawBounds.StartColumn);
				var text = Font.GetText(str);

				if (highlightDictionary != null)
				{
					var highlightStart = Math.Max(drawBounds.StartColumn - HighlightRegexSize, 0);
					var highlightCount = Math.Min(drawBounds.EndColumn - drawBounds.StartColumn + HighlightRegexSize, lineColumns.Length - highlightStart);
					var highlightStr = lineColumns.Substring(highlightStart, highlightCount);

					foreach (var entry in highlightDictionary)
						foreach (Match match in entry.Key.Matches(highlightStr))
						{
							var start = match.Index + highlightStart - drawBounds.StartColumn;
							var count = match.Length;
							if (start < 0)
							{
								count += start;
								start = 0;
							}
							count = Math.Min(count, str.Length - start);
							if (count <= 0)
								continue;
							text.SetForegroundBrush(entry.Value, start, count);
						}
				}

				dc.DrawText(text, new Point(0, drawBounds.Y(line)));
			}
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0) || (!canvas.IsVisible))
				return;

			var drawBounds = GetDrawBounds();
			var visibleCursor = (CurrentSelection >= 0) && (CurrentSelection < Selections.Count) ? Selections[CurrentSelection] : null;

			RenderIndicators(dc, drawBounds, null, Searches, searchBrush, searchPen, -1, 1);
			foreach (var region in Regions)
				RenderIndicators(dc, drawBounds, null, region.Value, null, regionPen[region.Key], -2, 2);
			if (Selections.Any(range => range.HasSelection))
				RenderIndicators(dc, drawBounds, visibleCursor, Selections, selectionBrush, selectionPen, -1, 1);
			else
				RenderCarets(dc, drawBounds);
			RenderDiff(dc, drawBounds);
			RenderText(dc, drawBounds);
		}

		void OnStatusBarRender(object sender, DrawingContext dc)
		{
			var sb = new List<string>();

			ViewValuesData = null;
			ViewValuesHasSel = false;

			if ((CurrentSelection < 0) || (CurrentSelection >= Selections.Count))
			{
				sb.Add("Selection 0/0");
				sb.Add("Col");
				sb.Add("In");
				sb.Add("Pos");
			}
			else
			{
				var range = Selections[CurrentSelection];
				var startLine = Data.GetOffsetLine(range.Start);
				var endLine = Data.GetOffsetLine(range.End);
				var indexMin = Data.GetOffsetIndex(range.Start, startLine);
				var indexMax = Data.GetOffsetIndex(range.End, endLine);
				var lineMin = Data.GetDiffLine(startLine);
				var lineMax = Data.GetDiffLine(endLine);
				var columnMin = Data.GetColumnFromIndex(startLine, indexMin);
				var columnMax = Data.GetColumnFromIndex(endLine, indexMax);
				var posMin = range.Start;
				var posMax = range.End;

				try
				{
					ViewValuesData = Coder.StringToBytes(Data.GetString(range.Start, Math.Min(range.HasSelection ? range.Length : 100, Data.NumChars - range.Start)), CodePage);
					ViewValuesHasSel = range.HasSelection;
				}
				catch { }

				sb.Add($"Selection {CurrentSelection + 1:n0}/{NumSelections:n0}");
				sb.Add($"Col {lineMin + 1:n0}:{columnMin + 1:n0}{((lineMin == lineMax) && (columnMin == columnMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{columnMax + 1:n0}")}");
				sb.Add($"In {lineMin + 1:n0}:{indexMin + 1:n0}{((lineMin == lineMax) && (indexMin == indexMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{indexMax + 1:n0}")}");
				sb.Add($"Pos {posMin:n0}{(posMin == posMax ? "" : $"-{posMax:n0} ({posMax - posMin:n0})")}");
			}

			sb.Add($"Regions {string.Join(" / ", Regions.ToDictionary(pair => pair.Key, pair => pair.Value.Count).OrderBy(pair => pair.Key).Select(pair => $"{pair.Value:n0}"))}");
			sb.Add($"Database {DBName}");

			var tf = SystemFonts.MessageFontFamily.GetTypefaces().Where(x => (x.Weight == FontWeights.Normal) && (x.Style == FontStyles.Normal)).First();
			dc.DrawText(new FormattedText(string.Join(" │ ", sb), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, tf, SystemFonts.MessageFontSize, Brushes.White, 1), new Point(2, 2));
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			// If we get a file list, return and let the tabs window handle it
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList != null)
				return;

			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = (e.Data.GetData("UnicodeText") ?? e.Data.GetData("Text") ?? e.Data.GetData(typeof(string))) as string;
			if (data == null)
				return;

			ReplaceSelections(data);
			e.Handled = true;
		}

		public void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, bool keepUndo = false)
		{
			SetFileName(fileName);
			if (ContentType == ParserType.None)
				ContentType = contentType;
			DisplayName = displayName;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}

			FileSaver.HandleDecrypt(TabsParent, ref bytes, out var aesKey);
			AESKey = aesKey;

			bytes = FileSaver.Decompress(bytes, out var compressed);
			Compressed = compressed;

			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			CodePage = codePage;

			var data = Coder.BytesToString(bytes, codePage, true);
			Replace(new List<Range> { FullRange }, new List<string> { data });

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanExactlyEncode(bytes, CodePage);

			if (!keepUndo)
				undoRedo.Clear();
			SetModifiedFlag(isModified);
		}

		public List<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsParallel().AsOrdered().Select(range => fileName.RelativeChild(GetString(range))).ToList();
		}

		public void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();

			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Anchor - ranges[ctr].End;
			}

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: undoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: undoRedo.AddUndo(textCanvasUndoRedo, IsModified); break;
			}

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);
			SetModifiedFlag();
			CalculateDiff();

			var translateMap = RangeList.GetTranslateMap(ranges, strs, new RangeList[] { Selections, Searches }.Concat(Regions.Values).ToArray());
			SetSelections(Selections.Translate(translateMap));
			Regions.Keys.ToList().ForEach(key => SetRegions(key, Regions[key].Translate(translateMap)));
			var searchLens = Searches.Select(range => range.Length).ToList();
			SetSearches(Searches.Translate(translateMap));
			SetSearches(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());

			CalculateBoundaries();
		}

		public void ReplaceOneWithMany(List<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var ending = addNewLines ?? strs.Any(str => !str.EndsWith(Data.DefaultEnding)) ? Data.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var offset = Selections.Single().Start;
			ReplaceSelections(string.Join("", strs));

			var sels = new List<Range>();
			foreach (var str in strs)
			{
				sels.Add(Range.FromIndex(offset, str.Length - ending.Length));
				offset += str.Length;
			}
			SetSelections(sels);
		}

		public void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType, tryJoinUndo);

		public void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections.ToList(), strs, replaceType, tryJoinUndo);

			if (highlight)
				SetSelections(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				SetSelections(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		public void Save(string fileName, AnswerResult answer, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Data.NumChars >> 20) < 50) && (!VerifyCanEncode(answer)))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = false;
					File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(Data.GetBytes(CodePage), Compressed), AESKey));
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = true;
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (!new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "Save failed. Remove read-only flag?",
						Options = MessageOptions.YesNo,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show().HasFlag(MessageOptions.Yes))
						throw;
					new FileInfo(fileName).IsReadOnly = false;
					triedReadOnly = true;
				}
			}

			if (!copyOnly)
			{
				fileLastWrite = new FileInfo(fileName).LastWriteTime;
				SetModifiedFlag(false);
				SetFileName(fileName);
			}
		}

		public void SetClipboardFile(string fileName, bool isCut = false) => SetClipboardFiles(new List<string> { fileName }, isCut);

		public void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false) => TabsParent.AddClipboardStrings(fileNames, isCut);

		public void SetClipboardString(string text) => SetClipboardStrings(new List<string> { text });

		public void SetClipboardStrings(IEnumerable<string> strs) => TabsParent.AddClipboardStrings(strs);

		public void SetFileName(string fileName)
		{
			if (FileName == fileName)
				return;

			FileName = fileName;
			ContentType = ParserExtensions.GetParserType(FileName);
			DisplayName = null;

			SetAutoRefresh();
		}

		public void SetAutoRefresh(bool? value = null)
		{
			ClearWatcher();

			if (value.HasValue)
				AutoRefresh = value.Value;
			if ((!AutoRefresh) || (!File.Exists(FileName)))
				return;

			watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(FileName),
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				Filter = Path.GetFileName(FileName),
			};
			watcher.Changed += (s1, e1) =>
			{
				watcherFileModified = true;
				Dispatcher.Invoke(() => TabsParent.QueueDoActivated());
			};
			watcher.EnableRaisingEvents = true;
		}

		public void Activated(AnswerResult answer)
		{
			if (!watcherFileModified)
				return;

			watcherFileModified = false;
			Command_File_Refresh(answer);
		}

		void SetModifiedFlag(bool? newValue = null)
		{
			if (newValue.HasValue)
			{
				if (newValue == false)
					modifiedChecksum.SetValue(Data.Data);
				else
					modifiedChecksum.Invalidate(); // Nothing will match, file will be perpetually modified
			}
			IsModified = !modifiedChecksum.Match(Data.Data);
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

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		public bool StringsAreFiles(List<string> strs)
		{
			if ((strs.Count == 0) || (strs.Count > 500))
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => (!str.StartsWith("\\\\")) && (!drives.Any(drive => str.StartsWith(drive, StringComparison.OrdinalIgnoreCase)))))
				return false;
			if (strs.Any(str => !Helpers.FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		public override string ToString() => FileName ?? DisplayName;

		public bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsParallel().All(data => Coder.CanEncode(data, codePage))) || (ConfirmContinueWhenCannotEncode());

		public bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanEncode(str, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool VerifyCanEncode(AnswerResult answer)
		{
			if (Data.CanEncode(CodePage))
				return true;

			if (!answer[nameof(VerifyCanEncode)].HasFlag(MessageOptions.All))
				answer[nameof(VerifyCanEncode)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "The current encoding cannot fully represent this data. Switch to UTF-8?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();
			if (answer[nameof(VerifyCanEncode)].HasFlag(MessageOptions.Yes))
			{
				CodePage = Coder.CodePage.UTF8;
				return true;
			}
			if (answer[nameof(VerifyCanEncode)].HasFlag(MessageOptions.No))
				return true;
			if (answer[nameof(VerifyCanEncode)].HasFlag(MessageOptions.Cancel))
				return false;
			throw new Exception("Invalid response");
		}

		void Command_Macro_RepeatLastAction()
		{
			if (previous == null)
				return;
			HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult, previous.MultiStatus, new AnswerResult(), null);
		}

		public void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			TabsParent.AddTextEditor(textEditor);
			textEditor.ContentType = contentType;
			textEditor.DisplayName = name;
		}

		string savedBitmapText;
		System.Drawing.Bitmap savedBitmap;

		public System.Drawing.Bitmap GetBitmap()
		{
			if (!Coder.IsImage(CodePage))
			{
				savedBitmapText = null;
				savedBitmap = null;
			}
			else if (Data.Data != savedBitmapText)
			{
				savedBitmapText = Data.Data;
				savedBitmap = Coder.StringToBitmap(AllText);
			}
			return savedBitmap;
		}

		string dbNameField;
		public string DBName { get => dbNameField; set { dbNameField = value; statusBarRenderTimer.Start(); } }

		public DbConnection dbConnection { get; set; }

		public CacheValue previousData { get; } = new CacheValue();
		public ParserType previousType { get; set; }
		public ParserNode previousRoot { get; set; }

		public List<InlineVariable> GetInlineVariables()
		{
			var inlineVars = new List<InlineVariable>();
			var regex = new Regex(@"\[(\w*):'(.*?)'=(.*?)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var found = new HashSet<string>();
			foreach (var tuple in Data.RegexMatches(regex, BeginOffset, EndOffset - BeginOffset, false, false, false))
			{
				var match = regex.Match(Data.GetString(tuple.Item1, tuple.Item2));
				var valueRange = Range.FromIndex(tuple.Item1 + match.Groups[3].Index, match.Groups[3].Length);
				var inlineVar = new InlineVariable(match.Groups[1].Value, match.Groups[2].Value, Range.FromIndex(tuple.Item1 + match.Groups[2].Index, match.Groups[2].Length), GetString(valueRange), valueRange);
				if (!string.IsNullOrEmpty(inlineVar.Name))
				{
					if (found.Contains(inlineVar.Name))
						throw new Exception($"Duplicate inline variable: {inlineVar.Name}");
					found.Add(inlineVar.Name);
				}
				inlineVars.Add(inlineVar);
			}
			return inlineVars;
		}

		public List<Range> GetEnclosingRegions(int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var useRegions = Regions[useRegion];
			var regions = new List<Range>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in Selections)
			{
				while ((currentRegion < useRegions.Count) && (useRegions[currentRegion].End <= selection.Start))
				{
					if ((useAllRegions) && (!used))
						throw new Exception("Extra regions found.");
					used = false;
					++currentRegion;
				}
				if ((currentRegion < useRegions.Count) && (selection.Start >= useRegions[currentRegion].Start) && (selection.End <= useRegions[currentRegion].End))
				{
					regions.Add(useRegions[currentRegion]);
					used = true;
				}
				else if (mustBeInRegion)
					throw new Exception("No region found. All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((Selections.Any()) && (useAllRegions) && (currentRegion != useRegions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		public void UpdateViewValue(byte[] value, int? size)
		{
			var sels = Selections.Select(range => Range.FromIndex(range.Start, size ?? range.Length)).ToList();
			var values = Enumerable.Repeat(Coder.BytesToString(value, CodePage), sels.Count).ToList();
			Replace(sels, values);
		}
	}
}
