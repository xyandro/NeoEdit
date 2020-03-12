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
		public bool IsActive => TabsParent.TabIsActive(this);

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
			Drop += OnDrop;

			undoRedo = new UndoRedo();

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column, index);

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

		public Range BeginRange => new Range(0);
		Range EndRange => new Range(Data.MaxPosition);
		public Range FullRange => new Range(Data.MaxPosition, 0);
		public string AllText => GetString(FullRange);

		void BlockSelDown()
		{
			var sels = new List<Range>();
			foreach (var range in Selections)
			{
				var cursorLine = Data.GetPositionLine(range.Cursor);
				var highlightLine = Data.GetPositionLine(range.Anchor);
				var cursorIndex = Data.GetPositionIndex(range.Cursor, cursorLine);
				var highlightIndex = Data.GetPositionIndex(range.Anchor, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Data.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Data.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Data.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Data.GetLineLength(highlightLine)));

				sels.Add(new Range(Data.GetPosition(cursorLine, cursorIndex), Data.GetPosition(highlightLine, highlightIndex)));
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
				var startLine = Data.GetPositionLine(range.Start);
				var endLine = Data.GetPositionLine(range.End);
				var startIndex = Data.GetPositionIndex(range.Start, startLine);
				var endIndex = Data.GetPositionIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Data.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Data.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Data.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Data.GetLineLength(endLine)));

				var prevLineRange = new Range(Data.GetPosition(startLine, startIndex), Data.GetPosition(endLine, endIndex));
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
			DiffTarget = null;
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

		public bool Empty() => (FileName == null) && (!IsModified) && (0 == Data.MaxPosition);

		public void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
				return;

			var range = Selections[CurrentSelection];
			var lineMin = Data.GetPositionLine(range.Start);
			var lineMax = Data.GetPositionLine(range.End);
			var indexMin = Data.GetPositionIndex(range.Start, lineMin);
			var indexMax = Data.GetPositionIndex(range.End, lineMax);

			if (centerVertically)
			{
				yScrollValue = (lineMin + lineMax - yScrollViewportFloor) / 2;
				if (centerHorizontally)
					xScrollValue = (Data.GetColumnFromIndex(lineMin, indexMin) + Data.GetColumnFromIndex(lineMax, indexMax) - xScrollViewportFloor) / 2;
				else
					xScrollValue = 0;
			}

			var line = Data.GetPositionLine(range.Cursor);
			var index = Data.GetPositionIndex(range.Cursor, line);
			var x = Data.GetColumnFromIndex(line, index);
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));

			statusBarRenderTimer.Start();
		}

		public WordSkipType GetWordSkipType(int position)
		{
			if ((position < 0) || (position >= Data.MaxPosition))
				return WordSkipType.Space;

			var c = Data[position];
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

		public int GetNextWord(int position)
		{
			WordSkipType moveType = WordSkipType.None;

			--position;
			while (true)
			{
				if (position >= Data.MaxPosition)
					return Data.MaxPosition;

				++position;
				var current = GetWordSkipType(position);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return position;
			}
		}

		public int GetPrevWord(int position)
		{
			WordSkipType moveType = WordSkipType.None;

			while (true)
			{
				if (position < 0)
					return 0;

				--position;
				var current = GetWordSkipType(position);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return position + 1;
			}
		}

		public List<string> GetSelectionStrings() => Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();

		public string GetString(Range range) => Data.GetString(range.Start, range.Length);

		public List<T> GetExpressionResults<T>(string expression, int? count = null) => new NEExpression(expression).EvaluateList<T>(GetVariables(), count);

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
			var initializeLineStarts = new NEVariableInitializer(() => lineStarts = Selections.AsParallel().AsOrdered().Select(range => Data.GetPositionLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => lineStarts, initializeLineStarts));
			var lineEnds = default(List<int>);
			var initializeLineEnds = new NEVariableInitializer(() => lineEnds = Selections.AsParallel().AsOrdered().Select(range => Data.GetPositionLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => lineEnds, initializeLineEnds));

			var colStarts = default(List<int>);
			var initializeColStarts = new NEVariableInitializer(() => colStarts = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetPositionIndex(range.Start, lineStarts[index] - 1) + 1).ToList(), initializeLineStarts);
			results.Add(NEVariable.List("col", "Selection column start", () => colStarts, initializeColStarts));
			var colEnds = default(List<int>);
			var initializeColEnds = new NEVariableInitializer(() => colEnds = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetPositionIndex(range.End, lineEnds[index] - 1) + 1).ToList(), initializeLineEnds);
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

			return results;
		}

		public void Goto(int? line, int? column, int? index)
		{
			var pos = 0;
			if (line.HasValue)
			{
				var useLine = Math.Max(0, Math.Min(line.Value, Data.NumLines) - 1);
				int useIndex;
				if (column.HasValue)
					useIndex = Data.GetIndexFromColumn(useLine, Math.Max(0, column.Value - 1), true);
				else if (index.HasValue)
					useIndex = Math.Max(0, Math.Min(index.Value - 1, Data.GetLineLength(useLine)));
				else
					useIndex = 0;

				pos = Data.GetPosition(useLine, useIndex);
			}
			SetSelections(new List<Range> { new Range(pos) });
		}

		bool timeNext = false;
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
							var position = range.Start;
							var anchor = range.Anchor;

							if (controlDown)
							{
								if (key == Key.Back)
									position = GetPrevWord(position);
								else
									position = GetNextWord(position);
							}
							else if ((shiftDown) && (key == Key.Delete))
							{
								var line = Data.GetPositionLine(position);
								position = Data.GetPosition(line, 0);
								anchor = position + Data.GetLineLength(line) + Data.GetEndingLength(line);
							}
							else
							{
								var line = Data.GetPositionLine(position);
								var index = Data.GetPositionIndex(position, line);

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

								position = Data.GetPosition(line, index);
							}

							return new Range(position, anchor);
						}).Where(range => range != null).ToList(), null);
					}
					break;
				case Key.Escape:
					DragFiles = null;
					if (Settings.EscapeClearsSelections)
					{
						HandleCommand(NECommand.Select_Selection_Single, false, null, null, null);
						if (!Selections.Any())
						{
							var pos = Data.GetPosition(Math.Max(0, Math.Min(yScrollValue, Data.NumLines - 1)), 0);
							SetSelections(new List<Range> { new Range(pos) });
						}
					}
					break;
				case Key.Left:
					{
						SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetPositionLine(range.Cursor);
							var index = Data.GetPositionIndex(range.Cursor, line);
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
							var line = Data.GetPositionLine(range.Cursor);
							var index = Data.GetPositionIndex(range.Cursor, line);
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
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, shiftDown)).ToList(); // Have to use MoveCursor for selection
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
							var line = Data.GetPositionLine(selection.Cursor);
							var index = Data.GetPositionIndex(selection.Cursor, line);

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
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, Data.MaxPosition, shiftDown)).ToList(); // Have to use MoveCursor for selection
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
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (Data.GetPositionLine(range.Start) == Data.GetPositionLine(Math.Max(range.Start, range.End - 1)))))
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

						var selLines = Selections.AsParallel().AsOrdered().Where(a => a.HasSelection).Select(range => new { start = Data.GetPositionLine(range.Start), end = Data.GetPositionLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy().ToDictionary(line => line, line => Data.GetPosition(line, 0));
						int length;
						string replace;
						if (shiftDown)
						{
							length = 1;
							replace = "";
							lines = lines.Where(entry => (Data.GetLineLength(entry.Key) != 0) && (Data[entry.Key, 0] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							length = 0;
							replace = "\t";
							lines = lines.Where(entry => Data.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, length)).ToList();
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
			var position = Data.GetPosition(line, index);
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
						if (position < anchor)
							position = GetPrevWord(position + 1);
						else
							position = GetNextWord(position);

						if ((mouseRange.Cursor <= anchor) != (position <= anchor))
						{
							if (position <= anchor)
								anchor = GetNextWord(anchor);
							else
								anchor = GetPrevWord(anchor);
						}
					}

					currentSelection = new Range(position, anchor);
				}
			}
			else
			{
				if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
					sels.Clear();

				if (clickCount == 1)
					currentSelection = new Range(position);
				else
				{
					if (mouseRange != null)
						sels.Remove(mouseRange);
					currentSelection = new Range(GetNextWord(position), GetPrevWord(Math.Min(position + 1, Data.MaxPosition)));
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
			cursor = Math.Max(0, Math.Min(cursor, Data.MaxPosition));
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
				var startLine = Data.GetPositionLine(range.Cursor);
				var startIndex = Data.GetPositionIndex(range.Cursor, startLine);

				if (lineRel)
					line = Data.SkipDiffGaps(line + startLine, line > 0 ? 1 : -1);
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data.GetLineLength(line)));
			return MoveCursor(range, Data.GetPosition(line, index), selecting);
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
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0) || (!canvas.IsVisible))
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
				var startLine = Data.GetPositionLine(range.Start);
				var endLine = Data.GetPositionLine(range.End);
				var indexMin = Data.GetPositionIndex(range.Start, startLine);
				var indexMax = Data.GetPositionIndex(range.End, endLine);
				var lineMin = Data.GetDiffLine(startLine);
				var lineMax = Data.GetDiffLine(endLine);
				var columnMin = Data.GetColumnFromIndex(startLine, indexMin);
				var columnMax = Data.GetColumnFromIndex(endLine, indexMax);
				var posMin = range.Start;
				var posMax = range.End;

				try
				{
					ViewValuesData = Coder.StringToBytes(Data.GetString(range.Start, Math.Min(range.HasSelection ? range.Length : 100, Data.MaxPosition - range.Start)), CodePage);
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

			var translateMap = RangeList.GetTranslateMap(ranges, strs, new RangeList[] { Selections }.Concat(Regions.Values).ToArray());
			SetSelections(Selections.Translate(translateMap));
			Regions.Keys.ToList().ForEach(key => SetRegions(key, Regions[key].Translate(translateMap)));

			CalculateBoundaries();
		}

		public void ReplaceOneWithMany(List<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var ending = addNewLines ?? strs.Any(str => !str.EndsWith(Data.DefaultEnding)) ? Data.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var position = Selections.Single().Start;
			ReplaceSelections(string.Join("", strs));

			var sels = new List<Range>();
			foreach (var str in strs)
			{
				sels.Add(Range.FromIndex(position, str.Length - ending.Length));
				position += str.Length;
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

		public void Save(string fileName, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Data.MaxPosition >> 20) < 50) && (!VerifyCanEncode()))
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

					if (!savedAnswers[nameof(Save)].HasFlag(MessageOptions.All))
						savedAnswers[nameof(Save)] = new Message(TabsParent)
						{
							Title = "Confirm",
							Text = "Save failed. Remove read-only flag?",
							Options = MessageOptions.YesNoAll,
							DefaultAccept = MessageOptions.Yes,
							DefaultCancel = MessageOptions.No,
						}.Show();
					if (!savedAnswers[nameof(Save)].HasFlag(MessageOptions.Yes))
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

		public void Activated()
		{
			if (!watcherFileModified)
				return;

			watcherFileModified = false;
			//TODO Command_File_Refresh();
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

		bool VerifyCanEncode()
		{
			if (Data.CanEncode(CodePage))
				return true;

			if (!savedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(VerifyCanEncode)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "The current encoding cannot fully represent this data. Switch to UTF-8?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();
			if (savedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.Yes))
			{
				CodePage = Coder.CodePage.UTF8;
				return true;
			}
			if (savedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.No))
				return true;
			throw new Exception("Invalid response");
		}

		void Command_Macro_RepeatLastAction()
		{
			if (previous == null)
				return;
			HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult, previous.MultiStatus, null);
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
