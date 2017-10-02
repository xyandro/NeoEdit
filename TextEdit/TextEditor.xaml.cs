using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;
using NeoEdit.TextEdit.Highlighting;

namespace NeoEdit.TextEdit
{
	public class TabsControl : TabsControl<TextEditor, TextEditCommand> { }

	partial class TextEditor
	{
		class PreviousStruct
		{
			public TextEditCommand Command { get; set; }
			public bool ShiftDown { get; set; }
			public object DialogResult { get; set; }
			public bool? MultiStatus { get; set; }
		}

		enum DragType
		{
			None,
			CurrentFile,
			Selections,
		}

		enum FindMinMaxType { String, Numeric, Length }

		enum GetPathType
		{
			FileName,
			FileNameWoExtension,
			Directory,
			Extension,
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		[Flags]
		enum TimestampType
		{
			Write = 1,
			Access = 2,
			Create = 4,
			All = Write | Access | Create,
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}


		TextData _data = new TextData();
		TextData Data
		{
			get { return _data; }
			set
			{
				_data = value;
				Selections.Clear();
				Regions.Values.ForEach(region => region.Clear());
				Searches.Clear();
				Bookmarks.Clear();
				undoRedo.Clear();
				CalculateDiff();
				CalculateBoundaries();
			}
		}

		[DepProp]
		public string DisplayName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool AutoRefresh { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Parser.ParserType ContentType { get { return UIHelper<TextEditor>.GetPropValue<Parser.ParserType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<TextEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string AESKey { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int CurrentSelection { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSelections { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Dictionary<int, int> NumRegions { get { return UIHelper<TextEditor>.GetPropValue<Dictionary<int, int>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEnding { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Clipboard { get { return UIHelper<TextEditor>.GetPropValue<List<string>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
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
					diffTarget = value;
					value.diffTarget = this;

					var diffXScrollValue = diffTarget.xScrollValue;
					var diffYScrollValue = diffTarget.yScrollValue;
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.xScrollValue), new Binding(UIHelper<TextEditor>.GetProperty(a => a.xScrollValue).Name) { Source = value, Mode = BindingMode.TwoWay });
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue), new Binding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue).Name) { Source = value, Mode = BindingMode.TwoWay });
					IsDiff = diffTarget.IsDiff = true;
					CalculateDiff();
					xScrollValue = diffXScrollValue;
					yScrollValue = diffTarget.Data.GetNonDiffLine(diffYScrollValue);
				}
			}
		}

		int xScrollViewportFloor => (int)Math.Floor(xScroll.ViewportSize);
		int xScrollViewportCeiling => (int)Math.Ceiling(xScroll.ViewportSize);
		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize);
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize);

		public bool HasSelections => Selections.Any();

		static Dictionary<string, List<string>> variables { get; } = new Dictionary<string, List<string>>();

		static TextEditor()
		{
			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => { obj.canvasRenderTimer.Start(); obj.bookmarkRenderTimer.Start(); });
			UIHelper<TextEditor>.AddCallback(a => a.ContentType, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.CodePage, (obj, o, n) => obj.CalculateDiff());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
			SetupStaticKeys();
		}

		readonly RangeList Selections, Searches, Bookmarks;
		readonly Dictionary<int, RangeList> Regions;
		RunOnceTimer canvasRenderTimer, bookmarkRenderTimer;
		List<PropertyChangeNotifier> localCallbacks;
		readonly UndoRedo undoRedo;
		static ThreadSafeRandom random = new ThreadSafeRandom();
		DateTime fileLastWrite;
		int mouseClickCount = 0;
		DragType doDrag = DragType.None;
		CacheValue modifiedChecksum = new CacheValue();
		string diffIgnoreCharacters;
		PreviousStruct previous = null;
		FileSystemWatcher watcher = null;
		ShutdownData shutdownData;

		internal TextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, bool? modified = null, int? line = null, int? column = null, ShutdownData shutdownData = null)
		{
			fileName = fileName?.Trim('"');
			SetupLocalKeys();
			this.shutdownData = shutdownData ?? new ShutdownData(null, 1);

			InitializeComponent();
			AutoRefresh = KeepSelections = true;

			SetupTabLabel();

			AllowDrop = true;
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			Drop += (s, e) => { OnDrop(e.Data); e.Handled = true; };

			undoRedo = new UndoRedo();
			Selections = new RangeList(SelectionsInvalidated);
			Searches = new RangeList(SearchesInvalidated);
			Regions = Enumerable.Range(1, 9).ToDictionary(num => num, num => new RangeList(() => RegionsInvalidated(num)));
			Bookmarks = new RangeList(BookmarksInvalidated);

			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());
			canvasRenderTimer.AddDependency(new RunOnceTimer[] { Selections.Timer, Searches.Timer }.Concat(Regions.Values.Select(region => region.Timer)).ToArray());
			bookmarkRenderTimer = new RunOnceTimer(() => bookmarks.InvalidateVisual());

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column);

			localCallbacks = UIHelper<TextEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			bookmarks.Render += OnBookmarksRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				canvasRenderTimer.Start();
			};

			FontSizeChanged(Font.FontSize);
			Font.FontSizeChanged += FontSizeChanged;
		}

		void FontSizeChanged(double fontSize)
		{
			bookmarks.Width = fontSize;
			CalculateBoundaries();
		}

		int BeginOffset => Data.GetOffset(0, 0);
		int EndOffset => Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		Range BeginRange => new Range(BeginOffset);
		Range EndRange => new Range(EndOffset);
		Range FullRange => new Range(EndOffset, BeginOffset);
		string AllText => GetString(FullRange);

		void BlockSelDown()
		{
			foreach (var range in Selections.ToList())
			{
				var cursorLine = Data.GetOffsetLine(range.Cursor);
				var highlightLine = Data.GetOffsetLine(range.Anchor);
				var cursorIndex = Data.GetOffsetIndex(range.Cursor, cursorLine);
				var highlightIndex = Data.GetOffsetIndex(range.Anchor, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Data.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Data.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Data.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Data.GetLineLength(highlightLine)));

				Selections.Add(new Range(Data.GetOffset(cursorLine, cursorIndex), Data.GetOffset(highlightLine, highlightIndex)));
			}
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

			Selections.Replace(sels);
		}

		void BookmarksInvalidated()
		{
			Bookmarks.Replace(Bookmarks.Select(range => MoveCursor(range, 0, 0, false, lineRel: true, indexRel: false)).ToList());
			Bookmarks.DeOverlap();
			bookmarkRenderTimer.Start();
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.CharWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Data.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / Font.FontSize;
			yScroll.Minimum = 0;
			yScroll.Maximum = Data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			LineEnding = Data.OnlyEnding;

			canvasRenderTimer.Start();
		}

		void CalculateDiff()
		{
			if (diffTarget == null)
				return;

			diffTarget.DiffIgnoreWhitespace = DiffIgnoreWhitespace;
			diffTarget.DiffIgnoreCase = DiffIgnoreCase;
			diffTarget.DiffIgnoreNumbers = DiffIgnoreNumbers;
			diffTarget.DiffIgnoreLineEndings = DiffIgnoreLineEndings;
			diffTarget.diffIgnoreCharacters = diffIgnoreCharacters;
			DiffEncodingMismatch = diffTarget.DiffEncodingMismatch = CodePage != diffTarget.CodePage;

			var left = TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget) ? this : DiffTarget;
			var right = left == this ? DiffTarget : this;
			TextData.CalculateDiff(left.Data, right.Data, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, diffIgnoreCharacters);

			CalculateBoundaries();
			diffTarget.CalculateBoundaries();
		}

		public override bool CanClose(ref Message.OptionsEnum answer)
		{
			if (!IsModified)
				return true;

			if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
				answer = new Message(WindowParent)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show();

			switch (answer)
			{
				case Message.OptionsEnum.Cancel:
					return false;
				case Message.OptionsEnum.No:
				case Message.OptionsEnum.NoToAll:
					return true;
				case Message.OptionsEnum.Yes:
				case Message.OptionsEnum.YesToAll:
					Command_File_Save_Save();
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

		public override void Closed()
		{
			DiffTarget = null;
			globalKeysChanged -= SetupLocalOrGlobalKeys;
			Font.FontSizeChanged -= FontSizeChanged;
			ClearWatcher();
			shutdownData.OnShutdown();
			base.Closed();
		}

		bool ConfirmCheckCanFullyEncode()
		{
			return new Message(WindowParent)
			{
				Title = "Confirm",
				Text = "The specified encoding cannot fully represent the data.  Continue anyway?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes;
		}

		bool ConfirmVerifyCanFullyEncode()
		{
			switch (new Message(WindowParent)
			{
				Title = "Confirm",
				Text = "The current encoding cannot fully represent this data.  Switch to UTF-8?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show())
			{
				case Message.OptionsEnum.Yes: CodePage = Coder.CodePage.UTF8; return true;
				case Message.OptionsEnum.No: return true;
				case Message.OptionsEnum.Cancel: return false;
				default: throw new Exception("Invalid response");
			}
		}

		public override bool Empty() => (FileName == null) && (!IsModified) && (BeginOffset == EndOffset);

		void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
			{
				LineMin = LineMax = IndexMin = IndexMax = PositionMin = PositionMax = ColumnMin = ColumnMax = null;
				return;
			}

			var range = Selections[CurrentSelection];
			var lineMin = Data.GetOffsetLine(range.Start);
			var lineMax = Data.GetOffsetLine(range.End);
			var indexMin = Data.GetOffsetIndex(range.Start, lineMin);
			var indexMax = Data.GetOffsetIndex(range.End, lineMax);
			LineMin = Data.GetDiffLine(lineMin) + 1;
			LineMax = Data.GetDiffLine(lineMax) + 1;
			IndexMin = indexMin + 1;
			IndexMax = indexMax + 1;
			PositionMin = range.Start;
			PositionMax = range.End;
			ColumnMin = Data.GetColumnFromIndex(lineMin, indexMin) + 1;
			ColumnMax = Data.GetColumnFromIndex(lineMax, indexMax) + 1;

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
		}

		string Factor(BigInteger value)
		{
			var factors = new List<BigInteger>();
			if (value < 0)
			{
				factors.Add(-1);
				value = -value;
			}

			BigInteger factor = 2;
			while (value > 1)
			{
				if (value % factor == 0)
				{
					factors.Add(factor);
					value /= factor;
					continue;
				}

				++factor;
			}

			if (!factors.Any())
				factors.Add(value);

			factors.Reverse();

			return string.Join("*", factors);
		}

		static bool FileOrDirectoryExists(string name) => (Directory.Exists(name)) || (File.Exists(name));

		List<Range> FindMinMax(bool min, FindMinMaxType type)
		{
			switch (type)
			{
				case FindMinMaxType.String: return FindMinMax(min, range => GetString(range), Comparer<string>.Default);
				case FindMinMaxType.Numeric: return FindMinMax(min, range => GetString(range), Comparer<string>.Create((x, y) => x.SmartCompare(y)));
				case FindMinMaxType.Length: return FindMinMax(min, range => range.Length, Comparer<int>.Default);
				default: throw new Exception("Invalid type");
			}
		}

		List<Range> FindMinMax<Input>(bool min, Func<Range, Input> select, Comparer<Input> sort)
		{
			if (!Selections.Any())
				throw new Exception("No selections");
			var selections = Selections.AsParallel().GroupBy(range => select(range)).OrderBy(group => group.Key, sort);
			var result = min ? selections.First() : selections.Last();
			return result.ToList();
		}

		decimal Floor(decimal number, decimal interval) => Math.Truncate(number / interval) * interval;

		List<T> GetFixedExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateRows<T>(GetVariables(), Selections.Count());

		WordSkipType GetWordSkipType(int line, int index)
		{
			if ((index < 0) || (index >= Data.GetLineLength(line)))
				return WordSkipType.Space;

			var c = Data[line, index];
			if (char.IsWhiteSpace(c))
				return WordSkipType.Space;
			else if ((char.IsLetterOrDigit(c)) || (c == '_'))
				return WordSkipType.Char;
			else
				return WordSkipType.Symbol;
		}

		int GetNextWord(int offset)
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

		int GetPrevWord(int offset)
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

		string GetRandomData(string chars, int length) => new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());

		List<string> GetSelectionStrings() => Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();

		string GetString(Range range) => Data.GetString(range.Start, range.Length);

		List<T> GetVariableExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateRows<T>(GetVariables());

		public NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var fileName = FileName;
			var clipboard = Clipboard;
			var KeysAndValues = this.KeysAndValues;

			var results = new NEVariables();

			var strs = default(List<string>);
			var initializeStrs = new NEVariableListInitializer(() => strs = Selections.Select(range => GetString(range)).ToList());
			results.Add(NEVariable.List("x", "Selection", () => strs, initializeStrs));
			results.Add(NEVariable.Constant("xn", "Selection count", () => Selections.Count));
			results.Add(NEVariable.List("xl", "Selection length", () => Selections.Select(range => range.Length)));
			results.Add(NEVariable.Constant("xlmin", "Selection min length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xlmax", "Selection max length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xmin", "Selection numeric min", () => Selections.AsParallel().GroupBy(range => GetString(range)).Select(group => double.Parse(group.Key)).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xmax", "Selection numeric max", () => Selections.AsParallel().GroupBy(range => GetString(range)).Select(group => double.Parse(group.Key)).DefaultIfEmpty(0).Max()));

			foreach (var pair in Regions)
			{
				var regions = default(List<string>);
				var initializeRegions = new NEVariableListInitializer(() => regions = pair.Value.Select(range => GetString(range)).ToList());
				results.Add(NEVariable.List($"r{pair.Key}", $"Region {pair.Key}", () => regions, initializeRegions));
				results.Add(NEVariable.Constant($"r{pair.Key}n", $"Region {pair.Key} count", () => pair.Value.Count));
				results.Add(NEVariable.List($"r{pair.Key}l", $"Region {pair.Key} length", () => pair.Value.Select(range => range.Length)));
				results.Add(NEVariable.Constant($"r{pair.Key}lmin", $"Region {pair.Key} min length", () => pair.Value.Select(range => range.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"r{pair.Key}lmax", $"Region {pair.Key} max length", () => pair.Value.Select(range => range.Length).DefaultIfEmpty(0).Max()));
			}

			results.Add(NEVariable.Series("y", "One-based index", index => index + 1));
			results.Add(NEVariable.Series("z", "Zero-based index", index => index));

			if (clipboard.Count == 1)
			{
				results.Add(NEVariable.Constant("c", "Clipboard", () => clipboard[0]));
				results.Add(NEVariable.Constant("cl", "Clipboard length", () => clipboard[0].Length));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => clipboard[0].Length));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => clipboard[0].Length));
			}
			else
			{
				results.Add(NEVariable.List("c", "Clipboard", () => clipboard));
				results.Add(NEVariable.List("cl", "Clipboard length", () => clipboard.Select(str => str.Length)));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => clipboard.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => clipboard.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}
			results.Add(NEVariable.Constant("cn", "Clipboard count", () => clipboard.Count));

			results.Add(NEVariable.Constant("f", "Filename", () => fileName));

			var lineStarts = default(List<int>);
			var initializeLineStarts = new NEVariableListInitializer(() => lineStarts = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => lineStarts, initializeLineStarts));
			var lineEnds = default(List<int>);
			var initializeLineEnds = new NEVariableListInitializer(() => lineEnds = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => lineEnds, initializeLineEnds));

			var colStarts = default(List<int>);
			var initializeColStarts = new NEVariableListInitializer(() => colStarts = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.Start, lineStarts[index] - 1) + 1).ToList(), initializeLineStarts);
			results.Add(NEVariable.List("col", "Selection column start", () => colStarts, initializeColStarts));
			var colEnds = default(List<int>);
			var initializeColEnds = new NEVariableListInitializer(() => colEnds = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.End, lineEnds[index] - 1) + 1).ToList(), initializeLineEnds);
			results.Add(NEVariable.List("colend", "Selection column end", () => colEnds, initializeColEnds));

			var posStarts = default(List<int>);
			var initializePosStarts = new NEVariableListInitializer(() => posStarts = Selections.Select(range => range.Start).ToList());
			results.Add(NEVariable.List("pos", "Selection position start", () => posStarts, initializePosStarts));
			var posEnds = default(List<int>);
			var initializePosEnds = new NEVariableListInitializer(() => posEnds = Selections.Select(range => range.End).ToList());
			results.Add(NEVariable.List("posend", "Selection position end", () => posEnds, initializePosEnds));

			for (var ctr = 0; ctr < KeysAndValues.Count; ++ctr)
			{
				var name = ctr == 0 ? "k" : $"v{ctr}";
				var desc = ctr == 0 ? "Keys" : $"Values {ctr}";
				var values = KeysAndValues[ctr];
				results.Add(NEVariable.List(name, desc, () => values));
				results.Add(NEVariable.Constant($"{name}n", $"{desc} count", () => values.Count));
				results.Add(NEVariable.List($"{name}l", $"{desc} length", () => values.Select(str => str.Length)));
				results.Add(NEVariable.Constant($"{name}lmin", $"{desc} min length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"{name}lmax", $"{desc} max length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}

			// Add variables that aren't already set
			results.AddRange(variables.Where(pair => !results.Contains(pair.Key)).ForEach(pair => NEVariable.List(pair.Key, "User-defined", () => pair.Value)));

			return results;
		}

		void Goto(int? line, int? column)
		{
			var useLine = Math.Max(0, Math.Min(line ?? 1, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(useLine, Math.Max(0, (column ?? 1) - 1), true);
			Selections.Add(new Range(Data.GetOffset(useLine, index)));
		}

		public bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Save_SaveAsByExpression: dialogResult = Command_File_Save_SaveAsByExpression_Dialog(); break;
				case TextEditCommand.File_Save_CopyToByExpression: dialogResult = Command_File_Save_SaveAsByExpression_Dialog(); break;
				case TextEditCommand.File_Operations_RenameByExpression: dialogResult = Command_File_Operations_RenameByExpression_Dialog(); break;
				case TextEditCommand.File_Encoding_Encoding: dialogResult = Command_File_Encoding_Encoding_Dialog(); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: dialogResult = Command_File_Encoding_ReopenWithEncoding_Dialog(); break;
				case TextEditCommand.File_Encoding_LineEndings: dialogResult = Command_File_Encoding_LineEndings_Dialog(); break;
				case TextEditCommand.File_Encryption: dialogResult = Command_File_Encryption_Dialog(); break;
				case TextEditCommand.Edit_Find_Find: dialogResult = Command_Edit_Find_Find_Dialog(); break;
				case TextEditCommand.Edit_Find_MassFind: dialogResult = Command_Edit_Find_MassFind_Dialog(); break;
				case TextEditCommand.Edit_Find_Replace: dialogResult = Command_Edit_Find_Replace_Dialog(); break;
				case TextEditCommand.Edit_Rotate: dialogResult = Command_Edit_Rotate_Dialog(); break;
				case TextEditCommand.Edit_Repeat: dialogResult = Command_Edit_Repeat_Dialog(); break;
				case TextEditCommand.Edit_URL_Absolute: dialogResult = Command_Edit_URL_Absolute_Dialog(); break;
				case TextEditCommand.Edit_Color: dialogResult = Command_Edit_Color_Dialog(); break;
				case TextEditCommand.Edit_Data_Hash: dialogResult = Command_Edit_Data_Hash_Dialog(); break;
				case TextEditCommand.Edit_Data_Compress: dialogResult = Command_Edit_Data_Compress_Dialog(); break;
				case TextEditCommand.Edit_Data_Decompress: dialogResult = Command_Edit_Data_Decompress_Dialog(); break;
				case TextEditCommand.Edit_Data_Encrypt: dialogResult = Command_Edit_Data_Encrypt_Dialog(); break;
				case TextEditCommand.Edit_Data_Decrypt: dialogResult = Command_Edit_Data_Decrypt_Dialog(); break;
				case TextEditCommand.Edit_Data_Sign: dialogResult = Command_Edit_Data_Sign_Dialog(); break;
				case TextEditCommand.Edit_Sort: dialogResult = Command_Edit_Sort_Dialog(); break;
				case TextEditCommand.Edit_Convert: dialogResult = Command_Edit_Convert_Dialog(); break;
				case TextEditCommand.Diff_IgnoreCharacters: dialogResult = Command_Diff_IgnoreCharacters_Dialog(); break;
				case TextEditCommand.Diff_Fix_Whitespace: dialogResult = Command_Diff_Fix_Whitespace_Dialog(); break;
				case TextEditCommand.Files_Name_MakeAbsolute: dialogResult = Command_Files_Name_MakeAbsolute_Dialog(); break;
				case TextEditCommand.Files_Name_GetUnique: dialogResult = Command_Files_Name_GetUnique_Dialog(); break;
				case TextEditCommand.Files_Set_Size: dialogResult = Command_Files_Set_Size_Dialog(); break;
				case TextEditCommand.Files_Set_Time_Write: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Time_Access: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Time_Create: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Time_All: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Attributes: dialogResult = Command_Files_Set_Attributes_Dialog(); break;
				case TextEditCommand.Files_Find_Binary: dialogResult = Command_Files_Find_Binary_Dialog(); break;
				case TextEditCommand.Files_Find_Text: dialogResult = Command_Files_Find_Text_Dialog(); break;
				case TextEditCommand.Files_Insert: dialogResult = Command_Files_Insert_Dialog(); break;
				case TextEditCommand.Files_Create_FromExpressions: dialogResult = Command_Files_Create_FromExpressions_Dialog(); break;
				case TextEditCommand.Files_Hash: dialogResult = Command_Files_Hash_Dialog(); break;
				case TextEditCommand.Files_Sign: dialogResult = Command_Files_Sign_Dialog(); break;
				case TextEditCommand.Files_Operations_Copy: dialogResult = Command_Files_Operations_CopyMove_Dialog(false); break;
				case TextEditCommand.Files_Operations_Move: dialogResult = Command_Files_Operations_CopyMove_Dialog(true); break;
				case TextEditCommand.Expression_Expression: dialogResult = Command_Expression_Expression_Dialog(); break;
				case TextEditCommand.Expression_Copy: dialogResult = Command_Expression_Copy_Dialog(); break;
				case TextEditCommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				case TextEditCommand.Expression_SetVariables: dialogResult = Command_Expression_SetVariables_Dialog(); break;
				case TextEditCommand.Text_Select_Trim: dialogResult = Command_Text_Select_Trim_Dialog(); break;
				case TextEditCommand.Text_Select_ByWidth: dialogResult = Command_Text_Select_ByWidth_Dialog(); break;
				case TextEditCommand.Text_Select_WholeWord: dialogResult = Command_Text_Select_WholeBoundedWord_Dialog(true); break;
				case TextEditCommand.Text_Select_BoundedWord: dialogResult = Command_Text_Select_WholeBoundedWord_Dialog(false); break;
				case TextEditCommand.Text_Width: dialogResult = Command_Text_Width_Dialog(); break;
				case TextEditCommand.Text_Trim: dialogResult = Command_Text_Trim_Dialog(); break;
				case TextEditCommand.Text_RandomText: dialogResult = Command_Text_RandomText_Dialog(); break;
				case TextEditCommand.Text_ReverseRegEx: dialogResult = Command_Text_ReverseRegEx_Dialog(); break;
				case TextEditCommand.Text_FirstDistinct: dialogResult = Command_Text_FirstDistinct_Dialog(); break;
				case TextEditCommand.Numeric_ConvertBase: dialogResult = Command_Numeric_ConvertBase_Dialog(); break;
				case TextEditCommand.Numeric_Series_Linear: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(true); break;
				case TextEditCommand.Numeric_Series_Geometric: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(false); break;
				case TextEditCommand.Numeric_Scale: dialogResult = Command_Numeric_Scale_Dialog(); break;
				case TextEditCommand.Numeric_Floor: dialogResult = Command_Numeric_Floor_Dialog(); break;
				case TextEditCommand.Numeric_Ceiling: dialogResult = Command_Numeric_Ceiling_Dialog(); break;
				case TextEditCommand.Numeric_Round: dialogResult = Command_Numeric_Round_Dialog(); break;
				case TextEditCommand.Numeric_Limit: dialogResult = Command_Numeric_Limit_Dialog(); break;
				case TextEditCommand.Numeric_RandomNumber: dialogResult = Command_Numeric_RandomNumber_Dialog(); break;
				case TextEditCommand.Numeric_CombinationsPermutations: dialogResult = Command_Numeric_CombinationsPermutations_Dialog(); break;
				case TextEditCommand.Numeric_MinMaxValues: dialogResult = Command_Numeric_MinMaxValues_Dialog(); break;
				case TextEditCommand.DateTime_Convert: dialogResult = Command_DateTime_Convert_Dialog(); break;
				case TextEditCommand.Table_Convert: dialogResult = Command_Table_Convert_Dialog(); break;
				case TextEditCommand.Table_TextToTable: dialogResult = Command_Table_TextToTable_Dialog(); break;
				case TextEditCommand.Table_EditTable: dialogResult = Command_Table_EditTable_Dialog(); break;
				case TextEditCommand.Table_AddColumn: dialogResult = Command_Table_AddColumn_Dialog(); break;
				case TextEditCommand.Table_Select_RowsByExpression: dialogResult = Command_Table_Select_RowsByExpression_Dialog(); break;
				case TextEditCommand.Table_Join: dialogResult = Command_Table_Join_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateInserts: dialogResult = Command_Table_Database_GenerateInserts_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateUpdates: dialogResult = Command_Table_Database_GenerateUpdates_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateDeletes: dialogResult = Command_Table_Database_GenerateDeletes_Dialog(); break;
				case TextEditCommand.Position_Goto_Lines: dialogResult = Command_Position_Goto_Dialog(GotoType.Line); break;
				case TextEditCommand.Position_Goto_Columns: dialogResult = Command_Position_Goto_Dialog(GotoType.Column); break;
				case TextEditCommand.Position_Goto_Positions: dialogResult = Command_Position_Goto_Dialog(GotoType.Position); break;
				case TextEditCommand.Content_Ancestor: dialogResult = Command_Content_Ancestor_Dialog(); break;
				case TextEditCommand.Content_Attributes: dialogResult = Command_Content_Attributes_Dialog(); break;
				case TextEditCommand.Content_WithAttribute: dialogResult = Command_Content_WithAttribute_Dialog(); break;
				case TextEditCommand.Content_Children_WithAttribute: dialogResult = Command_Content_Children_WithAttribute_Dialog(); break;
				case TextEditCommand.Content_Descendants_WithAttribute: dialogResult = Command_Content_Descendants_WithAttribute_Dialog(); break;
				case TextEditCommand.Network_AbsoluteURL: dialogResult = Command_Network_AbsoluteURL_Dialog(); break;
				case TextEditCommand.Network_FetchFile: dialogResult = Command_Network_FetchFile_Dialog(); break;
				case TextEditCommand.Network_Ping: dialogResult = Command_Network_Ping_Dialog(); break;
				case TextEditCommand.Network_ScanPorts: dialogResult = Command_Network_ScanPorts_Dialog(); break;
				case TextEditCommand.Database_Connect: dialogResult = Command_Database_Connect_Dialog(); break;
				case TextEditCommand.Database_QueryBuilder: dialogResult = Command_Database_QueryBuilder_Dialog(); break;
				case TextEditCommand.Database_Examine: Command_Database_Examine_Dialog(); break;
				case TextEditCommand.Select_Limit: dialogResult = Command_Select_Limit_Dialog(); break;
				case TextEditCommand.Select_ByCount: dialogResult = Command_Select_ByCount_Dialog(); break;
				case TextEditCommand.Select_Split: dialogResult = Command_Select_Split_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool timeNext = false;
		public void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult, bool? multiStatus, ref Message.OptionsEnum answer)
		{
			doDrag = DragType.None;

			var start = DateTime.UtcNow;
			if (command != TextEditCommand.Macro_RepeatLastAction)
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
				case TextEditCommand.File_NewFromSelections: Command_File_NewFromSelections(); break;
				case TextEditCommand.File_Open_Selected: Command_File_Open_Selected(); break;
				case TextEditCommand.File_OpenWith_Disk: Command_File_OpenWith_Disk(); break;
				case TextEditCommand.File_OpenWith_HexEditor: Command_File_OpenWith_HexEditor(); break;
				case TextEditCommand.File_Save_Save: Command_File_Save_Save(); break;
				case TextEditCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case TextEditCommand.File_Save_SaveAsByExpression: Command_File_Save_SaveAsByExpression(dialogResult as GetExpressionDialog.Result, ref answer); break;
				case TextEditCommand.File_Save_CopyTo: Command_File_Save_SaveAs(true); break;
				case TextEditCommand.File_Save_CopyToByExpression: Command_File_Save_SaveAsByExpression(dialogResult as GetExpressionDialog.Result, ref answer, true); break;
				case TextEditCommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case TextEditCommand.File_Operations_RenameByExpression: Command_File_Operations_RenameByExpression(dialogResult as GetExpressionDialog.Result, ref answer); break;
				case TextEditCommand.File_Operations_Delete: Command_File_Operations_Delete(ref answer); break;
				case TextEditCommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case TextEditCommand.File_Operations_CommandPrompt: Command_File_Operations_CommandPrompt(); break;
				case TextEditCommand.File_Operations_DragDrop: Command_File_Operations_DragDrop(); break;
				case TextEditCommand.File_Close: if (CanClose(ref answer)) { TabsParent.Remove(this); } break;
				case TextEditCommand.File_Refresh: Command_File_Refresh(ref answer); break;
				case TextEditCommand.File_AutoRefresh: Command_File_AutoRefresh(multiStatus); break;
				case TextEditCommand.File_Revert: Command_File_Revert(ref answer); break;
				case TextEditCommand.File_Insert_Files: Command_File_Insert_Files(); break;
				case TextEditCommand.File_Insert_CopiedCut: Command_File_Insert_CopiedCut(); break;
				case TextEditCommand.File_Insert_Selected: Command_File_Insert_Selected(); break;
				case TextEditCommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case TextEditCommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case TextEditCommand.File_Copy_DisplayName: Command_File_Copy_DisplayName(); break;
				case TextEditCommand.File_Copy_Count: Command_File_Copy_Count(); break;
				case TextEditCommand.File_Encoding_Encoding: Command_File_Encoding_Encoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: Command_File_Encoding_ReopenWithEncoding(dialogResult as EncodingDialog.Result, ref answer); break;
				case TextEditCommand.File_Encoding_LineEndings: Command_File_Encoding_LineEndings(dialogResult as FileEncodingLineEndingsDialog.Result); break;
				case TextEditCommand.File_Encryption: Command_File_Encryption(dialogResult as string); break;
				case TextEditCommand.Edit_Undo: Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Copy_Copy: Command_Edit_Copy_CutCopy(false); break;
				case TextEditCommand.Edit_Copy_Cut: Command_Edit_Copy_CutCopy(true); break;
				case TextEditCommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(shiftDown, false); break;
				case TextEditCommand.Edit_Paste_RotatePaste: Command_Edit_Paste_Paste(shiftDown, true); break;
				case TextEditCommand.Edit_Find_Find: Command_Edit_Find_Find(shiftDown, dialogResult as EditFindFindDialog.Result); break;
				case TextEditCommand.Edit_Find_Next: Command_Edit_Find_NextPrevious(true, shiftDown); break;
				case TextEditCommand.Edit_Find_Previous: Command_Edit_Find_NextPrevious(false, shiftDown); break;
				case TextEditCommand.Edit_Find_Selected: Command_Edit_Find_Selected(shiftDown); break;
				case TextEditCommand.Edit_Find_MassFind: Command_Edit_Find_MassFind(dialogResult as EditFindMassFindDialog.Result); break;
				case TextEditCommand.Edit_Find_Replace: Command_Edit_Find_Replace(dialogResult as EditFindReplaceDialog.Result); break;
				case TextEditCommand.Edit_Find_ClearSearchResults: Command_Edit_Find_ClearSearchResults(); break;
				case TextEditCommand.Edit_CopyDown: Command_Edit_CopyDown(); break;
				case TextEditCommand.Edit_Rotate: Command_Edit_Rotate(dialogResult as EditRotateDialog.Result); break;
				case TextEditCommand.Edit_Repeat: Command_Edit_Repeat(dialogResult as EditRepeatDialog.Result); break;
				case TextEditCommand.Edit_Markup_Escape: Command_Edit_Markup_Escape(); break;
				case TextEditCommand.Edit_Markup_Unescape: Command_Edit_Markup_Unescape(); break;
				case TextEditCommand.Edit_RegEx_Escape: Command_Edit_RegEx_Escape(); break;
				case TextEditCommand.Edit_RegEx_Unescape: Command_Edit_RegEx_Unescape(); break;
				case TextEditCommand.Edit_URL_Escape: Command_Edit_URL_Escape(); break;
				case TextEditCommand.Edit_URL_Unescape: Command_Edit_URL_Unescape(); break;
				case TextEditCommand.Edit_URL_Absolute: Command_Edit_URL_Absolute(dialogResult as FilesNamesMakeAbsoluteDialog.Result); break;
				case TextEditCommand.Edit_Color: Command_Edit_Color(dialogResult as EditColorDialog.Result); break;
				case TextEditCommand.Edit_Data_Hash: Command_Edit_Data_Hash(dialogResult as EditDataHashDialog.Result); break;
				case TextEditCommand.Edit_Data_Compress: Command_Edit_Data_Compress(dialogResult as EditDataCompressDialog.Result); break;
				case TextEditCommand.Edit_Data_Decompress: Command_Edit_Data_Decompress(dialogResult as EditDataCompressDialog.Result); break;
				case TextEditCommand.Edit_Data_Encrypt: Command_Edit_Data_Encrypt(dialogResult as EditDataEncryptDialog.Result); break;
				case TextEditCommand.Edit_Data_Decrypt: Command_Edit_Data_Decrypt(dialogResult as EditDataEncryptDialog.Result); break;
				case TextEditCommand.Edit_Data_Sign: Command_Edit_Data_Sign(dialogResult as EditDataSignDialog.Result); break;
				case TextEditCommand.Edit_Sort: Command_Edit_Sort(dialogResult as EditSortDialog.Result); break;
				case TextEditCommand.Edit_Convert: Command_Edit_Convert(dialogResult as EditConvertDialog.Result); break;
				case TextEditCommand.Edit_Bookmarks_Toggle: Command_Edit_Bookmarks_Toggle(); break;
				case TextEditCommand.Edit_Bookmarks_Next: Command_Edit_Bookmarks_NextPreviousBookmark(true, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Previous: Command_Edit_Bookmarks_NextPreviousBookmark(false, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Clear: Command_Edit_Bookmarks_Clear(); break;
				case TextEditCommand.Edit_Navigate_WordLeft: Command_Edit_Navigate_WordLeft(shiftDown); break;
				case TextEditCommand.Edit_Navigate_WordRight: Command_Edit_Navigate_WordRight(shiftDown); break;
				case TextEditCommand.Edit_Navigate_AllLeft: Command_Edit_Navigate_AllLeft(shiftDown); break;
				case TextEditCommand.Edit_Navigate_AllRight: Command_Edit_Navigate_AllRight(shiftDown); break;
				case TextEditCommand.Diff_Selections: Command_Diff_Selections(); break;
				case TextEditCommand.Diff_SelectedFiles: Command_Diff_SelectedFiles(); break;
				case TextEditCommand.Diff_Regions_Region1: Command_Diff_Regions_Region(1); break;
				case TextEditCommand.Diff_Regions_Region2: Command_Diff_Regions_Region(2); break;
				case TextEditCommand.Diff_Regions_Region3: Command_Diff_Regions_Region(3); break;
				case TextEditCommand.Diff_Regions_Region4: Command_Diff_Regions_Region(4); break;
				case TextEditCommand.Diff_Regions_Region5: Command_Diff_Regions_Region(5); break;
				case TextEditCommand.Diff_Regions_Region6: Command_Diff_Regions_Region(6); break;
				case TextEditCommand.Diff_Regions_Region7: Command_Diff_Regions_Region(7); break;
				case TextEditCommand.Diff_Regions_Region8: Command_Diff_Regions_Region(8); break;
				case TextEditCommand.Diff_Regions_Region9: Command_Diff_Regions_Region(9); break;
				case TextEditCommand.Diff_Break: Command_Diff_Break(); break;
				case TextEditCommand.Diff_IgnoreWhitespace: Command_Diff_IgnoreWhitespace(multiStatus); break;
				case TextEditCommand.Diff_IgnoreCase: Command_Diff_IgnoreCase(multiStatus); break;
				case TextEditCommand.Diff_IgnoreNumbers: Command_Diff_IgnoreNumbers(multiStatus); break;
				case TextEditCommand.Diff_IgnoreLineEndings: Command_Diff_IgnoreLineEndings(multiStatus); break;
				case TextEditCommand.Diff_IgnoreCharacters: Command_Diff_IgnoreCharacters(dialogResult as DiffIgnoreCharactersDialog.Result); break;
				case TextEditCommand.Diff_Reset: Command_Diff_Reset(); break;
				case TextEditCommand.Diff_Next: Command_Diff_NextPrevious(true, shiftDown); break;
				case TextEditCommand.Diff_Previous: Command_Diff_NextPrevious(false, shiftDown); break;
				case TextEditCommand.Diff_CopyLeft: Command_Diff_CopyLeftRight(true); break;
				case TextEditCommand.Diff_CopyRight: Command_Diff_CopyLeftRight(false); break;
				case TextEditCommand.Diff_Fix_Whitespace: Command_Diff_Fix_Whitespace(dialogResult as DiffFixWhitespaceDialog.Result); break;
				case TextEditCommand.Diff_Fix_Case: Command_Diff_Fix_Case(); break;
				case TextEditCommand.Diff_Fix_Numbers: Command_Diff_Fix_Numbers(); break;
				case TextEditCommand.Diff_Fix_LineEndings: Command_Diff_Fix_LineEndings(); break;
				case TextEditCommand.Diff_Select_Match: Command_Diff_Select_MatchDiff(true); break;
				case TextEditCommand.Diff_Select_Diff: Command_Diff_Select_MatchDiff(false); break;
				case TextEditCommand.Files_Name_Simplify: Command_Files_Name_Simplify(); break;
				case TextEditCommand.Files_Name_MakeAbsolute: Command_Files_Name_MakeAbsolute(dialogResult as FilesNamesMakeAbsoluteDialog.Result); break;
				case TextEditCommand.Files_Name_GetUnique: Command_Files_Name_GetUnique(dialogResult as FilesNamesGetUniqueDialog.Result); break;
				case TextEditCommand.Files_Name_Sanitize: Command_Files_Name_Sanitize(); break;
				case TextEditCommand.Files_Get_Size: Command_Files_Get_Size(); break;
				case TextEditCommand.Files_Get_Time_Write: Command_Files_Get_Time(TimestampType.Write); break;
				case TextEditCommand.Files_Get_Time_Access: Command_Files_Get_Time(TimestampType.Access); break;
				case TextEditCommand.Files_Get_Time_Create: Command_Files_Get_Time(TimestampType.Create); break;
				case TextEditCommand.Files_Get_Attributes: Command_Files_Get_Attributes(); break;
				case TextEditCommand.Files_Get_Version_File: Command_Files_Get_Version_File(); break;
				case TextEditCommand.Files_Get_Version_Product: Command_Files_Get_Version_Product(); break;
				case TextEditCommand.Files_Get_Version_Assembly: Command_Files_Get_Version_Assembly(); break;
				case TextEditCommand.Files_Get_Children: Command_Files_Get_ChildrenDescendants(false); break;
				case TextEditCommand.Files_Get_Descendants: Command_Files_Get_ChildrenDescendants(true); break;
				case TextEditCommand.Files_Set_Size: Command_Files_Set_Size(dialogResult as FilesSetSizeDialog.Result); break;
				case TextEditCommand.Files_Set_Time_Write: Command_Files_Set_Time(TimestampType.Write, dialogResult as FilesSetTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Time_Access: Command_Files_Set_Time(TimestampType.Access, dialogResult as FilesSetTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Time_Create: Command_Files_Set_Time(TimestampType.Create, dialogResult as FilesSetTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Time_All: Command_Files_Set_Time(TimestampType.All, dialogResult as FilesSetTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Attributes: Command_Files_Set_Attributes(dialogResult as FilesSetAttributesDialog.Result); break;
				case TextEditCommand.Files_Find_Binary: Command_Files_Find_Binary(dialogResult as FindBinaryDialog.Result); break;
				case TextEditCommand.Files_Find_Text: Command_Files_Find_Text(dialogResult as FindTextDialog.Result); break;
				case TextEditCommand.Files_Insert: Command_Files_Insert(dialogResult as FilesInsertDialog.Result); break;
				case TextEditCommand.Files_Create_Files: Command_Files_Create_Files(); break;
				case TextEditCommand.Files_Create_Directories: Command_Files_Create_Directories(); break;
				case TextEditCommand.Files_Create_FromExpressions: Command_Files_Create_FromExpressions(dialogResult as FilesCreateFromExpressionsDialog.Result); break;
				case TextEditCommand.Files_Select_Name_Directory: Command_Files_Select_Name(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Select_Name_Name: Command_Files_Select_Name(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Select_Name_FileNamewoExtension: Command_Files_Select_Name(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Select_Name_Extension: Command_Files_Select_Name(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case TextEditCommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case TextEditCommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case TextEditCommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case TextEditCommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case TextEditCommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case TextEditCommand.Files_Hash: Command_Files_Hash(dialogResult as HashDialog.Result); break;
				case TextEditCommand.Files_Sign: Command_Files_Sign(dialogResult as FilesSignDialog.Result); break;
				case TextEditCommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(dialogResult as FilesOperationsCopyMoveDialog.Result, false); break;
				case TextEditCommand.Files_Operations_Move: Command_Files_Operations_CopyMove(dialogResult as FilesOperationsCopyMoveDialog.Result, true); break;
				case TextEditCommand.Files_Operations_Delete: Command_Files_Operations_Delete(); break;
				case TextEditCommand.Files_Operations_DragDrop: Command_Files_Operations_DragDrop(); break;
				case TextEditCommand.Files_Operations_OpenDisk: Command_Files_Operations_OpenDisk(); break;
				case TextEditCommand.Files_Operations_Explore: Command_Files_Operations_Explore(); break;
				case TextEditCommand.Files_Operations_CommandPrompt: Command_Files_Operations_CommandPrompt(); break;
				case TextEditCommand.Files_Operations_RunCommand_Parallel: Command_Files_Operations_RunCommand_Parallel(); break;
				case TextEditCommand.Files_Operations_RunCommand_Sequential: Command_Files_Operations_RunCommand_Sequential(); break;
				case TextEditCommand.Files_Operations_RunCommand_Shell: Command_Files_Operations_RunCommand_Shell(); break;
				case TextEditCommand.Expression_Expression: Command_Expression_Expression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_Copy: Command_Expression_Copy(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_EvaluateSelected: Command_Expression_EvaluateSelected(); break;
				case TextEditCommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_ClearVariables: Command_Expression_ClearVariables(); break;
				case TextEditCommand.Expression_SetVariables: Command_Expression_SetVariables(dialogResult as ExpressionSetVariablesDialog.Result); break;
				case TextEditCommand.Text_Select_Trim: Command_Text_Select_Trim(dialogResult as TextTrimDialog.Result); break;
				case TextEditCommand.Text_Select_ByWidth: Command_Text_Select_ByWidth(dialogResult as TextWidthDialog.Result); break;
				case TextEditCommand.Text_Select_WholeWord: Command_Text_Select_WholeBoundedWord(dialogResult as TextSelectWholeBoundedWordDialog.Result, true); break;
				case TextEditCommand.Text_Select_BoundedWord: Command_Text_Select_WholeBoundedWord(dialogResult as TextSelectWholeBoundedWordDialog.Result, false); break;
				case TextEditCommand.Text_Select_Min_Text: Command_Type_Select_MinMax(true, TextEditor.FindMinMaxType.String); break;
				case TextEditCommand.Text_Select_Min_Length: Command_Type_Select_MinMax(true, TextEditor.FindMinMaxType.Length); break;
				case TextEditCommand.Text_Select_Max_Text: Command_Type_Select_MinMax(false, TextEditor.FindMinMaxType.String); break;
				case TextEditCommand.Text_Select_Max_Length: Command_Type_Select_MinMax(false, TextEditor.FindMinMaxType.Length); break;
				case TextEditCommand.Text_Case_Upper: Command_Text_Case_Upper(); break;
				case TextEditCommand.Text_Case_Lower: Command_Text_Case_Lower(); break;
				case TextEditCommand.Text_Case_Proper: Command_Text_Case_Proper(); break;
				case TextEditCommand.Text_Case_Toggle: Command_Text_Case_Toggle(); break;
				case TextEditCommand.Text_Length: Command_Text_Length(); break;
				case TextEditCommand.Text_Width: Command_Text_Width(dialogResult as TextWidthDialog.Result); break;
				case TextEditCommand.Text_Trim: Command_Text_Trim(dialogResult as TextTrimDialog.Result); break;
				case TextEditCommand.Text_SingleLine: Command_Text_SingleLine(); break;
				case TextEditCommand.Text_GUID: Command_Text_GUID(); break;
				case TextEditCommand.Text_RandomText: Command_Text_RandomText(dialogResult as TextRandomTextDialog.Result); break;
				case TextEditCommand.Text_LoremIpsum: Command_Text_LoremIpsum(); break;
				case TextEditCommand.Text_ReverseRegEx: Command_Text_ReverseRegEx(dialogResult as TextReverseRegExDialog.Result); break;
				case TextEditCommand.Text_FirstDistinct: Command_Text_FirstDistinct(dialogResult as TextFirstDistinctDialog.Result); break;
				case TextEditCommand.Text_RepeatCount: Command_Text_RepeatCount(); break;
				case TextEditCommand.Text_RepeatIndex: Command_Text_RepeatIndex(); break;
				case TextEditCommand.Numeric_Select_Min: Command_Type_Select_MinMax(true, TextEditor.FindMinMaxType.Numeric); break;
				case TextEditCommand.Numeric_Select_Max: Command_Type_Select_MinMax(false, TextEditor.FindMinMaxType.Numeric); break;
				case TextEditCommand.Numeric_Select_Whole: Command_Numeric_Select_Whole(); break;
				case TextEditCommand.Numeric_Select_Fraction: Command_Numeric_Select_Fraction(); break;
				case TextEditCommand.Numeric_Select_Integer: Command_Numeric_Select_Integer(); break;
				case TextEditCommand.Numeric_Select_Float: Command_Numeric_Select_Float(); break;
				case TextEditCommand.Numeric_Select_Hex: Command_Numeric_Select_Hex(); break;
				case TextEditCommand.Numeric_Hex_ToHex: Command_Numeric_Hex_ToHex(); break;
				case TextEditCommand.Numeric_Hex_FromHex: Command_Numeric_Hex_FromHex(); break;
				case TextEditCommand.Numeric_ConvertBase: Command_Numeric_ConvertBase(dialogResult as NumericConvertBaseDialog.Result); break;
				case TextEditCommand.Numeric_Series_ZeroBased: Command_Numeric_Series_ZeroBased(); break;
				case TextEditCommand.Numeric_Series_OneBased: Command_Numeric_Series_OneBased(); break;
				case TextEditCommand.Numeric_Series_Linear: Command_Numeric_Series_Linear(dialogResult as NumericSeriesDialog.Result); break;
				case TextEditCommand.Numeric_Series_Geometric: Command_Numeric_Series_Geometric(dialogResult as NumericSeriesDialog.Result); break;
				case TextEditCommand.Numeric_Scale: Command_Numeric_Scale(dialogResult as NumericScaleDialog.Result); break;
				case TextEditCommand.Numeric_Add_Sum: Command_Numeric_Add_Sum(); break;
				case TextEditCommand.Numeric_Add_ForwardSum: Command_Numeric_Add_ForwardReverseSum(true); break;
				case TextEditCommand.Numeric_Add_ReverseSum: Command_Numeric_Add_ForwardReverseSum(false); break;
				case TextEditCommand.Numeric_Add_Increment: Command_Numeric_Add_IncrementDecrement(true); break;
				case TextEditCommand.Numeric_Add_Decrement: Command_Numeric_Add_IncrementDecrement(false); break;
				case TextEditCommand.Numeric_Add_AddClipboard: Command_Numeric_Add_AddSubtractClipboard(true); break;
				case TextEditCommand.Numeric_Add_SubtractClipboard: Command_Numeric_Add_AddSubtractClipboard(false); break;
				case TextEditCommand.Numeric_Whole: Command_Numeric_Whole(); break;
				case TextEditCommand.Numeric_Fraction: Command_Numeric_Fraction(); break;
				case TextEditCommand.Numeric_Absolute: Command_Numeric_Absolute(); break;
				case TextEditCommand.Numeric_Floor: Command_Numeric_Floor(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Ceiling: Command_Numeric_Ceiling(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Round: Command_Numeric_Round(dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Limit: Command_Numeric_Limit(dialogResult as NumericLimitDialog.Result); break;
				case TextEditCommand.Numeric_Trim: Command_Numeric_Trim(); break;
				case TextEditCommand.Numeric_Factor: Command_Numeric_Factor(); break;
				case TextEditCommand.Numeric_RandomNumber: Command_Numeric_RandomNumber(dialogResult as NumericRandomNumberDialog.Result); break;
				case TextEditCommand.Numeric_CombinationsPermutations: Command_Numeric_CombinationsPermutations(dialogResult as NumericCombinationsPermutationsDialog.Result); break;
				case TextEditCommand.Numeric_MinMaxValues: Command_Numeric_MinMaxValues(dialogResult as NumericMinMaxValuesDialog.Result); break;
				case TextEditCommand.DateTime_Now: Command_DateTime_Now(); break;
				case TextEditCommand.DateTime_UtcNow: Command_DateTime_UtcNow(); break;
				case TextEditCommand.DateTime_Convert: Command_DateTime_Convert(dialogResult as DateTimeConvertDialog.Result); break;
				case TextEditCommand.Table_DetectType: Command_Table_DetectType(); break;
				case TextEditCommand.Table_Convert: Command_Table_Convert(dialogResult as TableConvertDialog.Result); break;
				case TextEditCommand.Table_TextToTable: Command_Table_TextToTable(dialogResult as TableTextToTableDialog.Result); break;
				case TextEditCommand.Table_LineSelectionsToTable: Command_Table_LineSelectionsToTable(); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region1: Command_Table_RegionSelectionsToTable_Region(1); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region2: Command_Table_RegionSelectionsToTable_Region(2); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region3: Command_Table_RegionSelectionsToTable_Region(3); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region4: Command_Table_RegionSelectionsToTable_Region(4); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region5: Command_Table_RegionSelectionsToTable_Region(5); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region6: Command_Table_RegionSelectionsToTable_Region(6); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region7: Command_Table_RegionSelectionsToTable_Region(7); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region8: Command_Table_RegionSelectionsToTable_Region(8); break;
				case TextEditCommand.Table_RegionSelectionsToTable_Region9: Command_Table_RegionSelectionsToTable_Region(9); break;
				case TextEditCommand.Table_EditTable: Command_Table_EditTable(dialogResult as TableEditTableDialog.Result); break;
				case TextEditCommand.Table_AddHeaders: Command_Table_AddHeaders(); break;
				case TextEditCommand.Table_AddRow: Command_Table_AddRow(); break;
				case TextEditCommand.Table_AddColumn: Command_Table_AddColumn(dialogResult as TableAddColumnDialog.Result); break;
				case TextEditCommand.Table_Select_RowsByExpression: Command_Table_Select_RowsByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Table_SetJoinSource: Command_Table_SetJoinSource(); break;
				case TextEditCommand.Table_Join: Command_Table_Join(dialogResult as TableJoinDialog.Result); break;
				case TextEditCommand.Table_Transpose: Command_Table_Transpose(); break;
				case TextEditCommand.Table_SetVariables: Command_Table_SetVariables(); break;
				case TextEditCommand.Table_Database_GenerateInserts: Command_Table_Database_GenerateInserts(dialogResult as TableDatabaseGenerateInsertsDialog.Result); break;
				case TextEditCommand.Table_Database_GenerateUpdates: Command_Table_Database_GenerateUpdates(dialogResult as TableDatabaseGenerateUpdatesDialog.Result); break;
				case TextEditCommand.Table_Database_GenerateDeletes: Command_Table_Database_GenerateDeletes(dialogResult as TableDatabaseGenerateDeletesDialog.Result); break;
				case TextEditCommand.Position_Goto_Lines: Command_Position_Goto(GotoType.Line, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Columns: Command_Position_Goto(GotoType.Column, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Positions: Command_Position_Goto(GotoType.Position, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case TextEditCommand.Position_Goto_FilesLines: Command_Position_Goto_FilesLines(); break;
				case TextEditCommand.Position_Copy_Lines: Command_Position_Copy(GotoType.Line); break;
				case TextEditCommand.Position_Copy_Columns: Command_Position_Copy(GotoType.Column); break;
				case TextEditCommand.Position_Copy_Positions: Command_Position_Copy(GotoType.Position); break;
				case TextEditCommand.Content_Type_SetFromExtension: Command_Content_Type_SetFromExtension(); break;
				case TextEditCommand.Content_Type_None: Command_Content_Type(Parser.ParserType.None); break;
				case TextEditCommand.Content_Type_Balanced: Command_Content_Type(Parser.ParserType.Balanced); break;
				case TextEditCommand.Content_Type_Columns: Command_Content_Type(Parser.ParserType.Columns); break;
				case TextEditCommand.Content_Type_CPlusPlus: Command_Content_Type(Parser.ParserType.CPlusPlus); break;
				case TextEditCommand.Content_Type_CSharp: Command_Content_Type(Parser.ParserType.CSharp); break;
				case TextEditCommand.Content_Type_CSV: Command_Content_Type(Parser.ParserType.CSV); break;
				case TextEditCommand.Content_Type_ExactColumns: Command_Content_Type(Parser.ParserType.ExactColumns); break;
				case TextEditCommand.Content_Type_HTML: Command_Content_Type(Parser.ParserType.HTML); break;
				case TextEditCommand.Content_Type_JSON: Command_Content_Type(Parser.ParserType.JSON); break;
				case TextEditCommand.Content_Type_SQL: Command_Content_Type(Parser.ParserType.SQL); break;
				case TextEditCommand.Content_Type_TSV: Command_Content_Type(Parser.ParserType.TSV); break;
				case TextEditCommand.Content_Type_XML: Command_Content_Type(Parser.ParserType.XML); break;
				case TextEditCommand.Content_Reformat: Command_Content_Reformat(); break;
				case TextEditCommand.Content_Comment: Command_Content_Comment(); break;
				case TextEditCommand.Content_Uncomment: Command_Content_Uncomment(); break;
				case TextEditCommand.Content_TogglePosition: Command_Content_TogglePosition(shiftDown); break;
				case TextEditCommand.Content_Current: Command_Content_Current(); break;
				case TextEditCommand.Content_Parent: Command_Content_Parent(); break;
				case TextEditCommand.Content_Ancestor: Command_Content_Ancestor(dialogResult as ContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Attributes: Command_Content_Attributes(dialogResult as ContentAttributesDialog.Result); break;
				case TextEditCommand.Content_WithAttribute: Command_Content_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Children_Children: Command_Content_Children_Children(); break;
				case TextEditCommand.Content_Children_SelfAndChildren: Command_Content_Children_SelfAndChildren(); break;
				case TextEditCommand.Content_Children_First: Command_Content_Children_First(); break;
				case TextEditCommand.Content_Children_WithAttribute: Command_Content_Children_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Descendants_Descendants: Command_Content_Descendants_Descendants(); break;
				case TextEditCommand.Content_Descendants_SelfAndDescendants: Command_Content_Descendants_SelfAndDescendants(); break;
				case TextEditCommand.Content_Descendants_First: Command_Content_Descendants_First(); break;
				case TextEditCommand.Content_Descendants_WithAttribute: Command_Content_Descendants_WithAttribute(dialogResult as ContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Navigate_Up: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Up, shiftDown); break;
				case TextEditCommand.Content_Navigate_Down: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Down, shiftDown); break;
				case TextEditCommand.Content_Navigate_Left: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Left, shiftDown); break;
				case TextEditCommand.Content_Navigate_Right: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Right, shiftDown); break;
				case TextEditCommand.Content_Navigate_Home: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Home, shiftDown); break;
				case TextEditCommand.Content_Navigate_End: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.End, shiftDown); break;
				case TextEditCommand.Content_Navigate_PgUp: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgUp, shiftDown); break;
				case TextEditCommand.Content_Navigate_PgDn: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgDn, shiftDown); break;
				case TextEditCommand.Content_Navigate_Row: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case TextEditCommand.Content_Navigate_Column: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case TextEditCommand.Content_KeepSelections: Command_Content_KeepSelections(multiStatus); break;
				case TextEditCommand.Network_AbsoluteURL: Command_Network_AbsoluteURL(dialogResult as NetworkAbsoluteURLDialog.Result); break;
				case TextEditCommand.Network_Fetch: Command_Network_Fetch(); break;
				case TextEditCommand.Network_FetchHex: Command_Network_Fetch(Coder.CodePage.Hex); break;
				case TextEditCommand.Network_FetchFile: Command_Network_FetchFile(dialogResult as NetworkFetchFileDialog.Result); break;
				case TextEditCommand.Network_Lookup_IP: Command_Network_Lookup_IP(); break;
				case TextEditCommand.Network_Lookup_HostName: Command_Network_Lookup_HostName(); break;
				case TextEditCommand.Network_AdaptersInfo: Command_Network_AdaptersInfo(); break;
				case TextEditCommand.Network_Ping: Command_Network_Ping(dialogResult as NetworkPingDialog.Result); break;
				case TextEditCommand.Network_ScanPorts: Command_Network_ScanPorts(dialogResult as NetworkScanPortsDialog.Result); break;
				case TextEditCommand.Database_Connect: Command_Database_Connect(dialogResult as DatabaseConnectDialog.Result); break;
				case TextEditCommand.Database_ExecuteQuery: Command_Database_ExecuteQuery(); break;
				case TextEditCommand.Database_QueryBuilder: Command_Database_QueryBuilder(dialogResult as string); break;
				case TextEditCommand.Keys_Set_Keys: Command_Keys_Set(0); break;
				case TextEditCommand.Keys_Set_Values1: Command_Keys_Set(1); break;
				case TextEditCommand.Keys_Set_Values2: Command_Keys_Set(2); break;
				case TextEditCommand.Keys_Set_Values3: Command_Keys_Set(3); break;
				case TextEditCommand.Keys_Set_Values4: Command_Keys_Set(4); break;
				case TextEditCommand.Keys_Set_Values5: Command_Keys_Set(5); break;
				case TextEditCommand.Keys_Set_Values6: Command_Keys_Set(6); break;
				case TextEditCommand.Keys_Set_Values7: Command_Keys_Set(7); break;
				case TextEditCommand.Keys_Set_Values8: Command_Keys_Set(8); break;
				case TextEditCommand.Keys_Set_Values9: Command_Keys_Set(9); break;
				case TextEditCommand.Keys_Add_Keys: Command_Keys_Add(0); break;
				case TextEditCommand.Keys_Add_Values1: Command_Keys_Add(1); break;
				case TextEditCommand.Keys_Add_Values2: Command_Keys_Add(2); break;
				case TextEditCommand.Keys_Add_Values3: Command_Keys_Add(3); break;
				case TextEditCommand.Keys_Add_Values4: Command_Keys_Add(4); break;
				case TextEditCommand.Keys_Add_Values5: Command_Keys_Add(5); break;
				case TextEditCommand.Keys_Add_Values6: Command_Keys_Add(6); break;
				case TextEditCommand.Keys_Add_Values7: Command_Keys_Add(7); break;
				case TextEditCommand.Keys_Add_Values8: Command_Keys_Add(8); break;
				case TextEditCommand.Keys_Add_Values9: Command_Keys_Add(9); break;
				case TextEditCommand.Keys_Remove_Keys: Command_Keys_Remove(0); break;
				case TextEditCommand.Keys_Remove_Values1: Command_Keys_Remove(1); break;
				case TextEditCommand.Keys_Remove_Values2: Command_Keys_Remove(2); break;
				case TextEditCommand.Keys_Remove_Values3: Command_Keys_Remove(3); break;
				case TextEditCommand.Keys_Remove_Values4: Command_Keys_Remove(4); break;
				case TextEditCommand.Keys_Remove_Values5: Command_Keys_Remove(5); break;
				case TextEditCommand.Keys_Remove_Values6: Command_Keys_Remove(6); break;
				case TextEditCommand.Keys_Remove_Values7: Command_Keys_Remove(7); break;
				case TextEditCommand.Keys_Remove_Values8: Command_Keys_Remove(8); break;
				case TextEditCommand.Keys_Remove_Values9: Command_Keys_Remove(9); break;
				case TextEditCommand.Keys_Replace_Values1: Command_Keys_Replace(1); break;
				case TextEditCommand.Keys_Replace_Values2: Command_Keys_Replace(2); break;
				case TextEditCommand.Keys_Replace_Values3: Command_Keys_Replace(3); break;
				case TextEditCommand.Keys_Replace_Values4: Command_Keys_Replace(4); break;
				case TextEditCommand.Keys_Replace_Values5: Command_Keys_Replace(5); break;
				case TextEditCommand.Keys_Replace_Values6: Command_Keys_Replace(6); break;
				case TextEditCommand.Keys_Replace_Values7: Command_Keys_Replace(7); break;
				case TextEditCommand.Keys_Replace_Values8: Command_Keys_Replace(8); break;
				case TextEditCommand.Keys_Replace_Values9: Command_Keys_Replace(9); break;
				case TextEditCommand.Select_All: Command_Select_All(); break;
				case TextEditCommand.Select_Nothing: Command_Select_Nothing(); break;
				case TextEditCommand.Select_Limit: Command_Select_Limit(dialogResult as SelectLimitDialog.Result); break;
				case TextEditCommand.Select_Lines: Command_Select_Lines(false); break;
				case TextEditCommand.Select_WholeLines: Command_Select_Lines(true); break;
				case TextEditCommand.Select_Rectangle: Command_Select_Rectangle(); break;
				case TextEditCommand.Select_Invert: Command_Select_Invert(); break;
				case TextEditCommand.Select_Join: Command_Select_Join(); break;
				case TextEditCommand.Select_Empty: Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case TextEditCommand.Select_ToggleOpenClose: Command_Select_ToggleOpenClose(shiftDown); break;
				case TextEditCommand.Select_Unique: Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Command_Select_Duplicates(); break;
				case TextEditCommand.Select_MatchPrevious: Command_Select_MatchPrevious(); break;
				case TextEditCommand.Select_NonMatchPrevious: Command_Select_NonMatchPrevious(); break;
				case TextEditCommand.Select_RepeatedLines: Command_Select_RepeatedLines(); break;
				case TextEditCommand.Select_ByCount: Command_Select_ByCount(dialogResult as SelectByCountDialog.Result); break;
				case TextEditCommand.Select_Split: Command_Select_Split(dialogResult as SelectSplitDialog.Result); break;
				case TextEditCommand.Select_Selection_First: Command_Select_Selection_First(); break;
				case TextEditCommand.Select_Selection_CenterVertically: Command_Select_Selection_CenterVertically(); break;
				case TextEditCommand.Select_Selection_Center: Command_Select_Selection_Center(); break;
				case TextEditCommand.Select_Selection_ToggleAnchor: Command_Select_Selection_ToggleAnchor(); break;
				case TextEditCommand.Select_Selection_Next: Command_Select_Selection_NextPrevious(true); break;
				case TextEditCommand.Select_Selection_Previous: Command_Select_Selection_NextPrevious(false); break;
				case TextEditCommand.Select_Selection_Single: Command_Select_Selection_Single(); break;
				case TextEditCommand.Select_Selection_Remove: Command_Select_Selection_Remove(); break;
				case TextEditCommand.Select_Selection_RemoveBeforeCurrent: Command_Select_Selection_RemoveBeforeCurrent(); break;
				case TextEditCommand.Select_Selection_RemoveAfterCurrent: Command_Select_Selection_RemoveAfterCurrent(); break;
				case TextEditCommand.Region_SetSelections_Region1: Command_Region_SetSelections_Region(1); break;
				case TextEditCommand.Region_SetSelections_Region2: Command_Region_SetSelections_Region(2); break;
				case TextEditCommand.Region_SetSelections_Region3: Command_Region_SetSelections_Region(3); break;
				case TextEditCommand.Region_SetSelections_Region4: Command_Region_SetSelections_Region(4); break;
				case TextEditCommand.Region_SetSelections_Region5: Command_Region_SetSelections_Region(5); break;
				case TextEditCommand.Region_SetSelections_Region6: Command_Region_SetSelections_Region(6); break;
				case TextEditCommand.Region_SetSelections_Region7: Command_Region_SetSelections_Region(7); break;
				case TextEditCommand.Region_SetSelections_Region8: Command_Region_SetSelections_Region(8); break;
				case TextEditCommand.Region_SetSelections_Region9: Command_Region_SetSelections_Region(9); break;
				case TextEditCommand.Region_SetSelections_All: Command_Region_SetSelections_Region(); break;
				case TextEditCommand.Region_AddSelections_Region1: Command_Region_AddSelections_Region(1); break;
				case TextEditCommand.Region_AddSelections_Region2: Command_Region_AddSelections_Region(2); break;
				case TextEditCommand.Region_AddSelections_Region3: Command_Region_AddSelections_Region(3); break;
				case TextEditCommand.Region_AddSelections_Region4: Command_Region_AddSelections_Region(4); break;
				case TextEditCommand.Region_AddSelections_Region5: Command_Region_AddSelections_Region(5); break;
				case TextEditCommand.Region_AddSelections_Region6: Command_Region_AddSelections_Region(6); break;
				case TextEditCommand.Region_AddSelections_Region7: Command_Region_AddSelections_Region(7); break;
				case TextEditCommand.Region_AddSelections_Region8: Command_Region_AddSelections_Region(8); break;
				case TextEditCommand.Region_AddSelections_Region9: Command_Region_AddSelections_Region(9); break;
				case TextEditCommand.Region_AddSelections_All: Command_Region_AddSelections_Region(); break;
				case TextEditCommand.Region_RemoveSelections_Region1: Command_Region_RemoveSelections_Region(1); break;
				case TextEditCommand.Region_RemoveSelections_Region2: Command_Region_RemoveSelections_Region(2); break;
				case TextEditCommand.Region_RemoveSelections_Region3: Command_Region_RemoveSelections_Region(3); break;
				case TextEditCommand.Region_RemoveSelections_Region4: Command_Region_RemoveSelections_Region(4); break;
				case TextEditCommand.Region_RemoveSelections_Region5: Command_Region_RemoveSelections_Region(5); break;
				case TextEditCommand.Region_RemoveSelections_Region6: Command_Region_RemoveSelections_Region(6); break;
				case TextEditCommand.Region_RemoveSelections_Region7: Command_Region_RemoveSelections_Region(7); break;
				case TextEditCommand.Region_RemoveSelections_Region8: Command_Region_RemoveSelections_Region(8); break;
				case TextEditCommand.Region_RemoveSelections_Region9: Command_Region_RemoveSelections_Region(9); break;
				case TextEditCommand.Region_RemoveSelections_All: Command_Region_RemoveSelections_Region(); break;
				case TextEditCommand.Region_ReplaceSelections_Region1: Command_Region_ReplaceSelections_Region(1); break;
				case TextEditCommand.Region_ReplaceSelections_Region2: Command_Region_ReplaceSelections_Region(2); break;
				case TextEditCommand.Region_ReplaceSelections_Region3: Command_Region_ReplaceSelections_Region(3); break;
				case TextEditCommand.Region_ReplaceSelections_Region4: Command_Region_ReplaceSelections_Region(4); break;
				case TextEditCommand.Region_ReplaceSelections_Region5: Command_Region_ReplaceSelections_Region(5); break;
				case TextEditCommand.Region_ReplaceSelections_Region6: Command_Region_ReplaceSelections_Region(6); break;
				case TextEditCommand.Region_ReplaceSelections_Region7: Command_Region_ReplaceSelections_Region(7); break;
				case TextEditCommand.Region_ReplaceSelections_Region8: Command_Region_ReplaceSelections_Region(8); break;
				case TextEditCommand.Region_ReplaceSelections_Region9: Command_Region_ReplaceSelections_Region(9); break;
				case TextEditCommand.Region_ReplaceSelections_All: Command_Region_ReplaceSelections_Region(); break;
				case TextEditCommand.Region_LimitToSelections_Region1: Command_Region_LimitToSelections_Region(1); break;
				case TextEditCommand.Region_LimitToSelections_Region2: Command_Region_LimitToSelections_Region(2); break;
				case TextEditCommand.Region_LimitToSelections_Region3: Command_Region_LimitToSelections_Region(3); break;
				case TextEditCommand.Region_LimitToSelections_Region4: Command_Region_LimitToSelections_Region(4); break;
				case TextEditCommand.Region_LimitToSelections_Region5: Command_Region_LimitToSelections_Region(5); break;
				case TextEditCommand.Region_LimitToSelections_Region6: Command_Region_LimitToSelections_Region(6); break;
				case TextEditCommand.Region_LimitToSelections_Region7: Command_Region_LimitToSelections_Region(7); break;
				case TextEditCommand.Region_LimitToSelections_Region8: Command_Region_LimitToSelections_Region(8); break;
				case TextEditCommand.Region_LimitToSelections_Region9: Command_Region_LimitToSelections_Region(9); break;
				case TextEditCommand.Region_LimitToSelections_All: Command_Region_LimitToSelections_Region(); break;
				case TextEditCommand.Region_Clear_Region1: Command_Region_Clear_Region(1); break;
				case TextEditCommand.Region_Clear_Region2: Command_Region_Clear_Region(2); break;
				case TextEditCommand.Region_Clear_Region3: Command_Region_Clear_Region(3); break;
				case TextEditCommand.Region_Clear_Region4: Command_Region_Clear_Region(4); break;
				case TextEditCommand.Region_Clear_Region5: Command_Region_Clear_Region(5); break;
				case TextEditCommand.Region_Clear_Region6: Command_Region_Clear_Region(6); break;
				case TextEditCommand.Region_Clear_Region7: Command_Region_Clear_Region(7); break;
				case TextEditCommand.Region_Clear_Region8: Command_Region_Clear_Region(8); break;
				case TextEditCommand.Region_Clear_Region9: Command_Region_Clear_Region(9); break;
				case TextEditCommand.Region_Clear_All: Command_Region_Clear_Region(); break;
				case TextEditCommand.Region_RepeatBySelections_Region1: Command_Region_RepeatBySelections_Region(1); break;
				case TextEditCommand.Region_RepeatBySelections_Region2: Command_Region_RepeatBySelections_Region(2); break;
				case TextEditCommand.Region_RepeatBySelections_Region3: Command_Region_RepeatBySelections_Region(3); break;
				case TextEditCommand.Region_RepeatBySelections_Region4: Command_Region_RepeatBySelections_Region(4); break;
				case TextEditCommand.Region_RepeatBySelections_Region5: Command_Region_RepeatBySelections_Region(5); break;
				case TextEditCommand.Region_RepeatBySelections_Region6: Command_Region_RepeatBySelections_Region(6); break;
				case TextEditCommand.Region_RepeatBySelections_Region7: Command_Region_RepeatBySelections_Region(7); break;
				case TextEditCommand.Region_RepeatBySelections_Region8: Command_Region_RepeatBySelections_Region(8); break;
				case TextEditCommand.Region_RepeatBySelections_Region9: Command_Region_RepeatBySelections_Region(9); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region1: Command_Region_CopyEnclosingRegion_Region(1); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region2: Command_Region_CopyEnclosingRegion_Region(2); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region3: Command_Region_CopyEnclosingRegion_Region(3); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region4: Command_Region_CopyEnclosingRegion_Region(4); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region5: Command_Region_CopyEnclosingRegion_Region(5); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region6: Command_Region_CopyEnclosingRegion_Region(6); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region7: Command_Region_CopyEnclosingRegion_Region(7); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region8: Command_Region_CopyEnclosingRegion_Region(8); break;
				case TextEditCommand.Region_CopyEnclosingRegion_Region9: Command_Region_CopyEnclosingRegion_Region(9); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region1: Command_Region_CopyEnclosingRegionIndex_Region(1); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region2: Command_Region_CopyEnclosingRegionIndex_Region(2); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region3: Command_Region_CopyEnclosingRegionIndex_Region(3); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region4: Command_Region_CopyEnclosingRegionIndex_Region(4); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region5: Command_Region_CopyEnclosingRegionIndex_Region(5); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region6: Command_Region_CopyEnclosingRegionIndex_Region(6); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region7: Command_Region_CopyEnclosingRegionIndex_Region(7); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region8: Command_Region_CopyEnclosingRegionIndex_Region(8); break;
				case TextEditCommand.Region_CopyEnclosingRegionIndex_Region9: Command_Region_CopyEnclosingRegionIndex_Region(9); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region1: Command_Region_TransformSelections_Flatten_Region(1); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region2: Command_Region_TransformSelections_Flatten_Region(2); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region3: Command_Region_TransformSelections_Flatten_Region(3); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region4: Command_Region_TransformSelections_Flatten_Region(4); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region5: Command_Region_TransformSelections_Flatten_Region(5); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region6: Command_Region_TransformSelections_Flatten_Region(6); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region7: Command_Region_TransformSelections_Flatten_Region(7); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region8: Command_Region_TransformSelections_Flatten_Region(8); break;
				case TextEditCommand.Region_TransformSelections_Flatten_Region9: Command_Region_TransformSelections_Flatten_Region(9); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region1: Command_Region_TransformSelections_Transpose_Region(1); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region2: Command_Region_TransformSelections_Transpose_Region(2); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region3: Command_Region_TransformSelections_Transpose_Region(3); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region4: Command_Region_TransformSelections_Transpose_Region(4); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region5: Command_Region_TransformSelections_Transpose_Region(5); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region6: Command_Region_TransformSelections_Transpose_Region(6); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region7: Command_Region_TransformSelections_Transpose_Region(7); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region8: Command_Region_TransformSelections_Transpose_Region(8); break;
				case TextEditCommand.Region_TransformSelections_Transpose_Region9: Command_Region_TransformSelections_Transpose_Region(9); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region1: Command_Region_TransformSelections_RotateLeft_Region(1); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region2: Command_Region_TransformSelections_RotateLeft_Region(2); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region3: Command_Region_TransformSelections_RotateLeft_Region(3); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region4: Command_Region_TransformSelections_RotateLeft_Region(4); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region5: Command_Region_TransformSelections_RotateLeft_Region(5); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region6: Command_Region_TransformSelections_RotateLeft_Region(6); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region7: Command_Region_TransformSelections_RotateLeft_Region(7); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region8: Command_Region_TransformSelections_RotateLeft_Region(8); break;
				case TextEditCommand.Region_TransformSelections_RotateLeft_Region9: Command_Region_TransformSelections_RotateLeft_Region(9); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region1: Command_Region_TransformSelections_RotateRight_Region(1); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region2: Command_Region_TransformSelections_RotateRight_Region(2); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region3: Command_Region_TransformSelections_RotateRight_Region(3); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region4: Command_Region_TransformSelections_RotateRight_Region(4); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region5: Command_Region_TransformSelections_RotateRight_Region(5); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region6: Command_Region_TransformSelections_RotateRight_Region(6); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region7: Command_Region_TransformSelections_RotateRight_Region(7); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region8: Command_Region_TransformSelections_RotateRight_Region(8); break;
				case TextEditCommand.Region_TransformSelections_RotateRight_Region9: Command_Region_TransformSelections_RotateRight_Region(9); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region1: Command_Region_TransformSelections_Rotate180_Region(1); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region2: Command_Region_TransformSelections_Rotate180_Region(2); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region3: Command_Region_TransformSelections_Rotate180_Region(3); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region4: Command_Region_TransformSelections_Rotate180_Region(4); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region5: Command_Region_TransformSelections_Rotate180_Region(5); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region6: Command_Region_TransformSelections_Rotate180_Region(6); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region7: Command_Region_TransformSelections_Rotate180_Region(7); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region8: Command_Region_TransformSelections_Rotate180_Region(8); break;
				case TextEditCommand.Region_TransformSelections_Rotate180_Region9: Command_Region_TransformSelections_Rotate180_Region(9); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region1: Command_Region_TransformSelections_MirrorHorizontal_Region(1); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region2: Command_Region_TransformSelections_MirrorHorizontal_Region(2); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region3: Command_Region_TransformSelections_MirrorHorizontal_Region(3); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region4: Command_Region_TransformSelections_MirrorHorizontal_Region(4); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region5: Command_Region_TransformSelections_MirrorHorizontal_Region(5); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region6: Command_Region_TransformSelections_MirrorHorizontal_Region(6); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region7: Command_Region_TransformSelections_MirrorHorizontal_Region(7); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region8: Command_Region_TransformSelections_MirrorHorizontal_Region(8); break;
				case TextEditCommand.Region_TransformSelections_MirrorHorizontal_Region9: Command_Region_TransformSelections_MirrorHorizontal_Region(9); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region1: Command_Region_TransformSelections_MirrorVertical_Region(1); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region2: Command_Region_TransformSelections_MirrorVertical_Region(2); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region3: Command_Region_TransformSelections_MirrorVertical_Region(3); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region4: Command_Region_TransformSelections_MirrorVertical_Region(4); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region5: Command_Region_TransformSelections_MirrorVertical_Region(5); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region6: Command_Region_TransformSelections_MirrorVertical_Region(6); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region7: Command_Region_TransformSelections_MirrorVertical_Region(7); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region8: Command_Region_TransformSelections_MirrorVertical_Region(8); break;
				case TextEditCommand.Region_TransformSelections_MirrorVertical_Region9: Command_Region_TransformSelections_MirrorVertical_Region(9); break;
				case TextEditCommand.Region_Select_Regions_Region1: Command_Region_Select_Regions_Region(shiftDown, 1); break;
				case TextEditCommand.Region_Select_Regions_Region2: Command_Region_Select_Regions_Region(shiftDown, 2); break;
				case TextEditCommand.Region_Select_Regions_Region3: Command_Region_Select_Regions_Region(shiftDown, 3); break;
				case TextEditCommand.Region_Select_Regions_Region4: Command_Region_Select_Regions_Region(shiftDown, 4); break;
				case TextEditCommand.Region_Select_Regions_Region5: Command_Region_Select_Regions_Region(shiftDown, 5); break;
				case TextEditCommand.Region_Select_Regions_Region6: Command_Region_Select_Regions_Region(shiftDown, 6); break;
				case TextEditCommand.Region_Select_Regions_Region7: Command_Region_Select_Regions_Region(shiftDown, 7); break;
				case TextEditCommand.Region_Select_Regions_Region8: Command_Region_Select_Regions_Region(shiftDown, 8); break;
				case TextEditCommand.Region_Select_Regions_Region9: Command_Region_Select_Regions_Region(shiftDown, 9); break;
				case TextEditCommand.Region_Select_Regions_All: Command_Region_Select_Regions_Region(shiftDown); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region1: Command_Region_Select_EnclosingRegion_Region(1); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region2: Command_Region_Select_EnclosingRegion_Region(2); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region3: Command_Region_Select_EnclosingRegion_Region(3); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region4: Command_Region_Select_EnclosingRegion_Region(4); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region5: Command_Region_Select_EnclosingRegion_Region(5); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region6: Command_Region_Select_EnclosingRegion_Region(6); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region7: Command_Region_Select_EnclosingRegion_Region(7); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region8: Command_Region_Select_EnclosingRegion_Region(8); break;
				case TextEditCommand.Region_Select_EnclosingRegion_Region9: Command_Region_Select_EnclosingRegion_Region(9); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region1: Command_Region_Select_WithEnclosingRegion_Region(1); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region2: Command_Region_Select_WithEnclosingRegion_Region(2); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region3: Command_Region_Select_WithEnclosingRegion_Region(3); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region4: Command_Region_Select_WithEnclosingRegion_Region(4); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region5: Command_Region_Select_WithEnclosingRegion_Region(5); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region6: Command_Region_Select_WithEnclosingRegion_Region(6); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region7: Command_Region_Select_WithEnclosingRegion_Region(7); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region8: Command_Region_Select_WithEnclosingRegion_Region(8); break;
				case TextEditCommand.Region_Select_WithEnclosingRegion_Region9: Command_Region_Select_WithEnclosingRegion_Region(9); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region1: Command_Region_Select_WithoutEnclosingRegion_Region(1); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region2: Command_Region_Select_WithoutEnclosingRegion_Region(2); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region3: Command_Region_Select_WithoutEnclosingRegion_Region(3); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region4: Command_Region_Select_WithoutEnclosingRegion_Region(4); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region5: Command_Region_Select_WithoutEnclosingRegion_Region(5); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region6: Command_Region_Select_WithoutEnclosingRegion_Region(6); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region7: Command_Region_Select_WithoutEnclosingRegion_Region(7); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region8: Command_Region_Select_WithoutEnclosingRegion_Region(8); break;
				case TextEditCommand.Region_Select_WithoutEnclosingRegion_Region9: Command_Region_Select_WithoutEnclosingRegion_Region(9); break;
				case TextEditCommand.Macro_RepeatLastAction: Command_Macro_RepeatLastAction(); break;
				case TextEditCommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != TextEditCommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message(WindowParent)
				{
					Title = "Timer",
					Text = $"Elapsed time: {elapsed:n} ms",
					Options = Message.OptionsEnum.Ok,
				}.Show();
			}
		}

		public bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var ret = true;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if (Selections.Any(range => range.HasSelection))
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
					Searches.Clear();
					doDrag = DragType.None;
					if (NEWindow.EscapeClearsSelections)
					{
						Command_Select_Selection_Single();
						if (!Selections.Any())
						{
							var pos = Data.GetOffset(Math.Max(0, Math.Min(yScrollValue, Data.NumLines - 1)), 0);
							Selections.Replace(new Range(pos));
						}
					}
					break;
				case Key.Left:
					{
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if ((!shiftDown) && (hasSelection))
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
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if ((!shiftDown) && (hasSelection))
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
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList());
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
						Selections.Replace(sels);
					}
					else
					{
						bool changed = false;
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var line = Data.GetOffsetLine(Selections[ctr].Cursor);
							var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);

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
							Selections[ctr] = MoveCursor(Selections[ctr], 0, first, shiftDown, indexRel: false);
						}
						if (!changed)
						{
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, shiftDown, indexRel: false)).ToList());
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, EndOffset, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(EndRange);
						Selections.Replace(sels);
					}
					else
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, shiftDown, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList());
					}
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList());
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (Data.GetOffsetLine(range.Start) == Data.GetOffsetLine(range.End))))
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

			if (Selections.Changed)
				EnsureVisible();

			return ret;
		}

		public bool HandleText(string text)
		{
			if (text.Length == 0)
				return true;

			ReplaceSelections(text, false, tryJoinUndo: true);
			if (Selections.Changed)
				EnsureVisible();
			return true;
		}

		void MouseHandler(Point mousePos, int clickCount, bool selecting)
		{
			var line = Math.Max(0, Math.Min(Data.NumLines - 1, (int)(mousePos.Y / Font.FontSize) + yScrollValue));
			var column = Math.Max(0, Math.Min(Data.GetLineColumnsLength(line), (int)(mousePos.X / Font.CharWidth) + xScrollValue));
			var index = Data.GetIndexFromColumn(line, column, true);
			var offset = Data.GetOffset(line, index);
			var mouseRange = CurrentSelection < Selections.Count ? Selections[CurrentSelection] : null;

			if (selecting)
			{
				if (mouseRange != null)
				{
					Selections.Remove(mouseRange);
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

					Selections.Add(new Range(offset, anchor));
					CurrentSelection = Selections.Count - 1;
				}
				return;
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
				Selections.Clear();

			if (clickCount == 1)
				Selections.Add(new Range(offset));
			else
			{
				if (mouseRange != null)
					Selections.Remove(mouseRange);
				Selections.Add(new Range(GetNextWord(offset), GetPrevWord(Math.Min(offset + 1, EndOffset))));
			}
			CurrentSelection = Selections.Count - 1;
		}

		Range MoveCursor(Range range, int cursor, bool selecting)
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

		void OnBookmarksRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (!Bookmarks.Any()))
				return;
			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			for (var line = startLine; line < endLine; ++line)
			{
				var offset = Data.GetOffset(line, 0);
				if (!Bookmarks.Any(range => range.Start == offset))
					continue;

				var y = (line - startLine) * Font.FontSize;

				dc.DrawRoundedRectangle(Brushes.CadetBlue, new Pen(Brushes.Black, 1), new Rect(1, y + 1, Font.FontSize - 2, Font.FontSize - 2), 2, 2);
			}
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (doDrag != DragType.None)
			{
				List<string> strs;
				switch (doDrag)
				{
					case DragType.CurrentFile: strs = new List<string> { FileName }; break;
					case DragType.Selections: strs = RelativeSelectedFiles(); break;
					default: throw new Exception("Invalid drag type");
				}
				if (!StringsAreFiles(strs))
					throw new Exception("Selections must be files.");

				DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, strs.ToArray()), DragDropEffects.Copy);
				doDrag = DragType.None;
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

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0))
				return;

			var brushes = new List<Tuple<RangeList, Brush>>
			{
				Tuple.Create(Selections, Misc.selectionBrush),
				Tuple.Create(Searches, Misc.searchBrush),
			};
			brushes.AddRange(Regions.Select(pair => Tuple.Create(pair.Value, Misc.regionBrush[pair.Key])));

			NumSelections = Selections.Count;
			NumRegions = Regions.ToDictionary(pair => pair.Key, pair => pair.Value.Count);

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			var startColumn = xScrollValue;
			var endColumn = Math.Min(Data.MaxColumn + 1, startColumn + xScrollViewportCeiling);

			var lines = Enumerable.Range(startLine, endLine - startLine).ToList();
			var lineRanges = lines.ToDictionary(line => line, line => new Range(Data.GetOffset(line, 0), Data.GetOffset(line, Data.GetLineLength(line) + 1)));
			var screenStart = lineRanges.First().Value.Start;
			var screenEnd = lineRanges.Last().Value.End + 1;
			var startIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, startColumn, true));
			var endIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, endColumn, true));
			var y = lines.ToDictionary(line => line, line => (line - startLine) * Font.FontSize);
			var cursorLineDone = new HashSet<int>();
			var visibleCursor = (CurrentSelection >= 0) && (CurrentSelection < Selections.Count) ? Selections[CurrentSelection] : null;

			foreach (var entry in brushes)
			{
				var hasSelection = entry.Item1.Any(range => range.HasSelection);

				foreach (var range in entry.Item1)
				{
					if ((range.End < screenStart) || (range.Start > screenEnd))
						continue;

					var entryStartLine = Data.GetOffsetLine(range.Start);
					var entryEndLine = Data.GetOffsetLine(range.End);
					var cursorLine = range.Cursor == range.Start ? entryStartLine : entryEndLine;
					entryStartLine = Math.Max(startLine, entryStartLine);
					entryEndLine = Math.Min(endLine, entryEndLine + 1);

					if ((entry.Item1 == Selections) && (!hasSelection) && (cursorLine >= entryStartLine) && (cursorLine < entryEndLine))
					{
						if (range == visibleCursor)
							dc.DrawRectangle(Misc.visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.FontSize));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(Misc.cursorBrush, Misc.cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.FontSize));
							cursorLineDone.Add(cursorLine);
						}

						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * Font.CharWidth - 1, y[cursorLine], 2, Font.FontSize));
						}
					}

					for (var line = entryStartLine; line < entryEndLine; ++line)
					{
						var start = Math.Max(lineRanges[line].Start, range.Start);
						var end = Math.Min(lineRanges[line].End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);

						if ((start > endIndexes[line]) || (end < startIndexes[line]))
							continue;

						start = Data.GetColumnFromIndex(line, start);
						end = Data.GetColumnFromIndex(line, end);

						start = Math.Max(0, start - startColumn);
						end = Math.Max(0, Math.Min(endColumn, end) - startColumn);
						var width = Math.Max(0, end - start);

						var steps = range == visibleCursor ? 2 : 1;
						for (var ctr = 0; ctr < steps; ++ctr)
							dc.DrawRectangle(entry.Item2, null, new Rect(start * Font.CharWidth, y[line], width * Font.CharWidth + 1, Font.FontSize));
					}
				}
			}

			var highlightDictionary = Highlight.Get(ContentType)?.GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				if (!Data.GetLineDiffMatches(line))
				{
					dc.DrawRectangle(Misc.diffMinorBrush, null, new Rect(0, y[line], canvas.ActualWidth, Font.FontSize));

					var map = Data.GetLineColumnMap(line, true);
					foreach (var tuple in Data.GetLineColumnDiffs(line))
					{
						var start = tuple.Item1;
						if (start != int.MaxValue)
							start = map[start];
						start = Math.Max(0, start - startColumn);

						var end = tuple.Item2;
						if (end != int.MaxValue)
							end = map[end];
						end = Math.Max(0, end - startColumn);

						var startX = Math.Max(0, start * Font.CharWidth);
						var endX = Math.Min(ActualWidth, end * Font.CharWidth);
						if (endX > startX)
							dc.DrawRectangle(Misc.diffMajorBrush, null, new Rect(startX, y[line], endX - startX, Font.FontSize));
					}
				}

				var str = Data.GetLineColumns(line);
				if (str.Length <= startColumn)
					continue;

				var highlight = new List<Tuple<Brush, int, int>>();
				if (highlightDictionary != null)
				{
					foreach (var entry in highlightDictionary)
					{
						var start = Math.Max(startColumn - 500, 0);
						var count = Math.Min(endColumn - startColumn + 500, str.Length - start);
						var highlightStr = str.Substring(start, count);
						var matches = entry.Key.Matches(highlightStr);
						foreach (Match match in matches)
							highlight.Add(new Tuple<Brush, int, int>(entry.Value, match.Index + start, match.Length));
					}
				}

				str = str.Substring(startColumn, Math.Min(endColumn, str.Length) - startColumn);
				var text = Font.GetText(str);
				foreach (var entry in highlight)
				{
					var start = entry.Item2 - startColumn;
					var count = entry.Item3;
					if (start < 0)
					{
						count += start;
						start = 0;
					}
					count = Math.Min(count, str.Length - start);
					if (count <= 0)
						continue;
					text.SetForegroundBrush(entry.Item1, start, count);
				}
				dc.DrawText(text, new Point(0, y[line]));
			}
		}

		void OnDrop(IDataObject data)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var result = OnDropDialog.Run(WindowParent, data);
			if (result != null)
				ReplaceOneWithMany(result, true);
		}

		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, bool? modified = null, bool keepUndo = false)
		{
			SetFileName(fileName);
			if (ContentType == Parser.ParserType.None)
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

			string aesKey;
			FileEncryptor.HandleDecrypt(WindowParent, ref bytes, out aesKey);
			AESKey = aesKey;

			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			CodePage = codePage;

			var data = Coder.BytesToString(bytes, codePage, true);
			Replace(new List<Range> { FullRange }, new List<string> { data });

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanFullyEncode(bytes, CodePage);

			if (!keepUndo)
				undoRedo.Clear();
			SetModifiedFlag(isModified);
		}

		void RegionsInvalidated(int useRegion)
		{
			Regions[useRegion].DeOverlap();
			canvasRenderTimer.Start();
		}

		List<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsParallel().AsOrdered().Select(range => fileName.RelativeChild(GetString(range))).ToList();
		}

		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
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

			var translateMap = RangeList.GetTranslateMap(ranges, strs, new RangeList[] { Selections, Searches, Bookmarks }.Concat(Regions.Values).ToArray());
			Selections.Translate(translateMap);
			Regions.Values.ForEach(region => region.Translate(translateMap));
			var searchLens = Searches.Select(range => range.Length).ToList();
			Searches.Translate(translateMap);
			Searches.Replace(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());
			Bookmarks.Translate(translateMap);

			CalculateBoundaries();
		}

		void ReplaceOneWithMany(List<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			addNewLines = addNewLines ?? strs.Any(str => !str.EndsWith(Data.DefaultEnding));
			var ending = addNewLines.Value ? Data.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var offset = Selections.Single().Start;
			ReplaceSelections(string.Join("", strs));
			Selections.Clear();
			foreach (var str in strs)
			{
				Selections.Add(Range.FromIndex(offset, str.Length - ending.Length));
				offset += str.Length;
			}
		}

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType, tryJoinUndo);

		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		void Save(string fileName, bool copyOnly = false)
		{
			if (((Data.NumChars >> 20) < 50) && (!VerifyCanFullyEncode()))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = false;
					File.WriteAllBytes(fileName, FileEncryptor.Encrypt(Data.GetBytes(CodePage), AESKey));
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = true;
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (new Message(WindowParent)
					{
						Title = "Confirm",
						Text = "Save failed.  Remove read-only flag?",
						Options = Message.OptionsEnum.YesNo,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show() != Message.OptionsEnum.Yes)
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

		void SearchesInvalidated()
		{
			Searches.Replace(Searches.Where(range => range.HasSelection).ToList());
			Searches.DeOverlap();
			canvasRenderTimer.Start();
		}

		void SelectionsInvalidated()
		{
			Selections.DeOverlap();
			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void SetClipboardFile(string fileName, bool isCut = false) => SetClipboardFiles(new List<string> { fileName }, isCut);

		void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false) => (WindowParent as TextEditTabs).AddClipboardStrings(fileNames, isCut);

		void SetClipboardString(string text) => SetClipboardStrings(new List<string> { text });

		void SetClipboardStrings(IEnumerable<string> strs) => (WindowParent as TextEditTabs).AddClipboardStrings(strs);

		void SetFileName(string fileName)
		{
			if (FileName == fileName)
				return;

			FileName = fileName;
			ContentType = Parser.GetParserType(FileName);
			DisplayName = null;

			SetAutoRefresh();
		}

		void SetAutoRefresh(bool? value = null)
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
				Dispatcher.Invoke(() => (WindowParent as TextEditTabs).QueueDoActivated());
			};
			watcher.EnableRaisingEvents = true;
		}

		public void Activated(ref Message.OptionsEnum answer)
		{
			if (!watcherFileModified)
				return;

			watcherFileModified = false;
			Command_File_Refresh(ref answer);
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
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] ?? FileName([1]) ?? ""[Untitled]"")t+([2]?""*"":"""")t+([3]?"" (Diff""t+([4]?"" - Encoding mismatch"":"""")t+"")"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.DisplayName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.IsDiff).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.DiffEncodingMismatch).Name) { Source = this });
			SetBinding(UIHelper<TabsControl<TextEditor, TextEditCommand>>.GetProperty(a => a.TabLabel), multiBinding);
		}

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		bool StringsAreFiles(List<string> strs)
		{
			if (!strs.Any())
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => !drives.Any(drive => str.StartsWith(drive, StringComparison.OrdinalIgnoreCase))))
				return false;
			if (strs.Any(str => !FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		public override string ToString() => FileName;

		bool CheckCanFullyEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsParallel().All(data => Coder.CanFullyEncode(data, codePage))) || (ConfirmCheckCanFullyEncode());

		bool CheckCanFullyEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmCheckCanFullyEncode());

		bool VerifyCanFullyEncode() => (Data.CanFullyEncode(CodePage)) || (ConfirmVerifyCanFullyEncode());

		bool VerifyCanFullyEncode(List<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());

		void Command_Type_Select_MinMax(bool min, FindMinMaxType type) => Selections.Replace(FindMinMax(min, type));

		void Command_Macro_RepeatLastAction()
		{
			if (previous == null)
				return;
			var answer = Message.OptionsEnum.None;
			HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult, previous.MultiStatus, ref answer);
		}
	}
}
