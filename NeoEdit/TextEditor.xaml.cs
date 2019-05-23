﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Converters;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Highlighting;
using NeoEdit.MenuContent;
using NeoEdit.MenuContent.Dialogs;
using NeoEdit.MenuDatabase;
using NeoEdit.MenuDatabase.Dialogs;
using NeoEdit.MenuDateTime;
using NeoEdit.MenuDateTime.Dialogs;
using NeoEdit.MenuDiff;
using NeoEdit.MenuDiff.Dialogs;
using NeoEdit.MenuEdit;
using NeoEdit.MenuEdit.Dialogs;
using NeoEdit.MenuExpression;
using NeoEdit.MenuExpression.Dialogs;
using NeoEdit.MenuFile;
using NeoEdit.MenuFile.Dialogs;
using NeoEdit.MenuFiles;
using NeoEdit.MenuFiles.Dialogs;
using NeoEdit.MenuImage;
using NeoEdit.MenuImage.Dialogs;
using NeoEdit.MenuKeys;
using NeoEdit.MenuNetwork;
using NeoEdit.MenuNetwork.Dialogs;
using NeoEdit.MenuNumeric;
using NeoEdit.MenuNumeric.Dialogs;
using NeoEdit.MenuPosition;
using NeoEdit.MenuPosition.Dialogs;
using NeoEdit.MenuRegion;
using NeoEdit.MenuRegion.Dialogs;
using NeoEdit.MenuSelect;
using NeoEdit.MenuSelect.Dialogs;
using NeoEdit.MenuTable;
using NeoEdit.MenuTable.Dialogs;
using NeoEdit.MenuText;
using NeoEdit.MenuText.Dialogs;
using NeoEdit.MenuWindow;
using NeoEdit.Misc;

namespace NeoEdit
{
	partial class TextEditor : ITextEditor
	{
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
		public int ItemOrder { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool Active { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
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

		public ITabs TabsParent { get; set; }
		public Window WindowParent => TabsParent as Window;

		public bool CanClose() => CanClose(new AnswerResult());

		int currentSelectionField;
		public int CurrentSelection { get => currentSelectionField; set { currentSelectionField = value; canvasRenderTimer.Start(); statusBarRenderTimer.Start(); } }
		public int NumSelections => Selections.Count;
		public List<string> Clipboard => TabsParent.GetClipboard(this);
		JumpByType jumpBy;

		bool watcherFileModified = false;

		TextEditor diffTarget;
		public ITextEditor DiffTarget
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

		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static internal readonly Brush searchBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
		static internal readonly Dictionary<int, Brush> regionBrush = new Dictionary<int, Brush>
		{
			[1] = new SolidColorBrush(Color.FromArgb(64, 0, 64, 0)),
			[2] = new SolidColorBrush(Color.FromArgb(64, 64, 0, 0)),
			[3] = new SolidColorBrush(Color.FromArgb(64, 0, 0, 64)),
			[4] = new SolidColorBrush(Color.FromArgb(64, 64, 64, 0)),
			[5] = new SolidColorBrush(Color.FromArgb(64, 64, 0, 64)),
			[6] = new SolidColorBrush(Color.FromArgb(64, 0, 64, 64)),
			[7] = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0)),
			[8] = new SolidColorBrush(Color.FromArgb(64, 128, 0, 0)),
			[9] = new SolidColorBrush(Color.FromArgb(64, 0, 0, 128)),
		};
		static internal readonly Brush visibleCursorBrush = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
		static internal readonly Brush diffMajorBrush = new SolidColorBrush(Color.FromArgb(192, 239, 203, 5));
		static internal readonly Brush diffMinorBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Brush cursorBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
		static internal readonly Pen cursorPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), 1);

		int xScrollViewportFloor => (int)Math.Floor(xScroll.ViewportSize);
		int xScrollViewportCeiling => (int)Math.Ceiling(xScroll.ViewportSize);
		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize);
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize);

		public bool HasSelections => Selections.Any();

		static TextEditor()
		{
			selectionBrush.Freeze();
			searchBrush.Freeze();
			regionBrush.Values.ForEach(brush => brush.Freeze());
			visibleCursorBrush.Freeze();
			diffMajorBrush.Freeze();
			diffMinorBrush.Freeze();
			cursorBrush.Freeze();
			cursorPen.Freeze();

			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.Active, (obj, o, n) => obj.TabsParent?.NotifyActiveChanged());
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => { obj.canvasRenderTimer.Start(); obj.bookmarkRenderTimer.Start(); });
			UIHelper<TextEditor>.AddCallback(a => a.ContentType, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.CodePage, (obj, o, n) => obj.CalculateDiff());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.HighlightSyntax, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
			SetupStaticKeys();
		}

		RangeList selectionsList = new RangeList(new List<Range>());
		public RangeList Selections => selectionsList;
		public void SetSelections(List<Range> selections, bool deOverlap = true)
		{
			selectionsList = new RangeList(selections, deOverlap);
			EnsureVisible();
			canvasRenderTimer.Start();
			TabsParent?.QueueUpdateCounts();
		}

		RangeList searchesList = new RangeList(new List<Range>());
		public RangeList Searches => searchesList;
		public void SetSearches(List<Range> searches)
		{
			searchesList = new RangeList(searches.Where(range => range.HasSelection).ToList());
			canvasRenderTimer.Start();
		}

		RangeList bookmarksList = new RangeList(new List<Range>());
		public RangeList Bookmarks => bookmarksList;
		public void SetBookmarks(List<Range> bookmarks)
		{
			bookmarksList = new RangeList(bookmarks.Select(range => MoveCursor(range, 0, 0, false, lineRel: true, indexRel: false)).ToList());
			bookmarkRenderTimer.Start();
		}

		readonly Dictionary<int, RangeList> regionsList = Enumerable.Range(1, 9).ToDictionary(num => num, num => new RangeList(new List<Range>()));
		public IReadOnlyDictionary<int, RangeList> Regions => regionsList;
		public void SetRegions(int region, List<Range> regions)
		{
			regionsList[region] = new RangeList(regions);
			canvasRenderTimer.Start();
			TabsParent?.QueueUpdateCounts();
		}

		RunOnceTimer canvasRenderTimer, statusBarRenderTimer, bookmarkRenderTimer;
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
			fileName = fileName?.Trim('"');
			SetupLocalKeys();
			this.shutdownData = shutdownData;

			InitializeComponent();
			canvasRenderTimer = new RunOnceTimer(() => { canvas.InvalidateVisual(); statusBar.InvalidateVisual(); });
			statusBarRenderTimer = new RunOnceTimer(() => statusBar.InvalidateVisual());
			bookmarkRenderTimer = new RunOnceTimer(() => bookmarks.InvalidateVisual());
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

		public void InvalidateCanvas()
		{
			canvas.InvalidateVisual();
			statusBar.InvalidateVisual();
		}

		void FontSizeChanged(double fontSize)
		{
			bookmarks.Width = fontSize;
			CalculateBoundaries();
		}

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

			var left = TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget) ? this : DiffTarget;
			var right = left == this ? DiffTarget : this;
			TextData.CalculateDiff(left.Data, right.Data, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);

			CalculateBoundaries();
			diffTarget.CalculateBoundaries();
		}

		public bool CanClose(AnswerResult answer)
		{
			if (!IsModified)
				return true;

			if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
				answer.Answer = new Message(WindowParent)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = MessageOptions.YesNoYesAllNoAllCancel,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();

			switch (answer.Answer)
			{
				case MessageOptions.Cancel:
					return false;
				case MessageOptions.No:
				case MessageOptions.NoToAll:
					return true;
				case MessageOptions.Yes:
				case MessageOptions.YesToAll:
					FileFunctions.Command_File_Save_Save(this);
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
			globalKeysChanged -= SetupLocalOrGlobalKeys;
			Font.FontSizeChanged -= FontSizeChanged;
			ClearWatcher();
			RemoveKeysAndValuesCallback();
			shutdownData?.OnShutdown();
		}

		bool ConfirmContinueWhenCannotEncode()
		{
			return new Message(WindowParent)
			{
				Title = "Confirm",
				Text = "The specified encoding cannot fully represent the data. Continue anyway?",
				Options = MessageOptions.YesNo,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.No,
			}.Show() == MessageOptions.Yes;
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

			switch (command.GetArea())
			{
				case "File": return GetDialogResultFile(command, ref dialogResult, multiStatus);
				case "Edit": return GetDialogResultEdit(command, ref dialogResult, multiStatus);
				case "Diff": return GetDialogResultDiff(command, ref dialogResult, multiStatus);
				case "Files": return GetDialogResultFiles(command, ref dialogResult, multiStatus);
				case "Expression": return GetDialogResultExpression(command, ref dialogResult, multiStatus);
				case "Text": return GetDialogResultText(command, ref dialogResult, multiStatus);
				case "Numeric": return GetDialogResultNumeric(command, ref dialogResult, multiStatus);
				case "DateTime": return GetDialogResultDateTime(command, ref dialogResult, multiStatus);
				case "Image": return GetDialogResultImage(command, ref dialogResult, multiStatus);
				case "Table": return GetDialogResultTable(command, ref dialogResult, multiStatus);
				case "Position": return GetDialogResultPosition(command, ref dialogResult, multiStatus);
				case "Content": return GetDialogResultContent(command, ref dialogResult, multiStatus);
				case "Network": return GetDialogResultNetwork(command, ref dialogResult, multiStatus);
				case "Database": return GetDialogResultDatabase(command, ref dialogResult, multiStatus);
				case "Select": return GetDialogResultSelect(command, ref dialogResult, multiStatus);
				case "Region": return GetDialogResultRegion(command, ref dialogResult, multiStatus);
				default: return true;
			}
		}

		bool GetDialogResultFile(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.File_Save_SaveAsByExpression: dialogResult = FileFunctions.Command_File_Save_SaveAsByExpression_Dialog(this); break;
				case NECommand.File_Save_CopyToByExpression: dialogResult = FileFunctions.Command_File_Save_SaveAsByExpression_Dialog(this); break;
				case NECommand.File_Save_SetDisplayName: dialogResult = FileFunctions.Command_File_Save_SaveAsByExpression_Dialog(this); break;
				case NECommand.File_Operations_RenameByExpression: dialogResult = FileFunctions.Command_File_Operations_RenameByExpression_Dialog(this); break;
				case NECommand.File_Encoding_Encoding: dialogResult = FileFunctions.Command_File_Encoding_Encoding_Dialog(this); break;
				case NECommand.File_Encoding_ReopenWithEncoding: dialogResult = FileFunctions.Command_File_Encoding_ReopenWithEncoding_Dialog(this); break;
				case NECommand.File_Encoding_LineEndings: dialogResult = FileFunctions.Command_File_Encoding_LineEndings_Dialog(this); break;
				case NECommand.File_Encrypt: dialogResult = FileFunctions.Command_File_Encrypt_Dialog(this, multiStatus); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultEdit(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Edit_Find_Find: dialogResult = EditFunctions.Command_Edit_Find_Find_Dialog(this); break;
				case NECommand.Edit_Find_MassFind: dialogResult = EditFunctions.Command_Edit_Find_MassFind_Dialog(this); break;
				case NECommand.Edit_Find_Replace: dialogResult = EditFunctions.Command_Edit_Find_Replace_Dialog(this); break;
				case NECommand.Edit_Rotate: dialogResult = EditFunctions.Command_Edit_Rotate_Dialog(this); break;
				case NECommand.Edit_Repeat: dialogResult = EditFunctions.Command_Edit_Repeat_Dialog(this); break;
				case NECommand.Edit_URL_Absolute: dialogResult = EditFunctions.Command_Edit_URL_Absolute_Dialog(this); break;
				case NECommand.Edit_Data_Hash: dialogResult = EditFunctions.Command_Edit_Data_Hash_Dialog(this); break;
				case NECommand.Edit_Data_Compress: dialogResult = EditFunctions.Command_Edit_Data_Compress_Dialog(this); break;
				case NECommand.Edit_Data_Decompress: dialogResult = EditFunctions.Command_Edit_Data_Decompress_Dialog(this); break;
				case NECommand.Edit_Data_Encrypt: dialogResult = EditFunctions.Command_Edit_Data_Encrypt_Dialog(this); break;
				case NECommand.Edit_Data_Decrypt: dialogResult = EditFunctions.Command_Edit_Data_Decrypt_Dialog(this); break;
				case NECommand.Edit_Data_Sign: dialogResult = EditFunctions.Command_Edit_Data_Sign_Dialog(this); break;
				case NECommand.Edit_Sort: dialogResult = EditFunctions.Command_Edit_Sort_Dialog(this); break;
				case NECommand.Edit_Convert: dialogResult = EditFunctions.Command_Edit_Convert_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultDiff(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Diff_IgnoreCharacters: dialogResult = DiffFunctions.Command_Diff_IgnoreCharacters_Dialog(this); break;
				case NECommand.Diff_Fix_Whitespace: dialogResult = DiffFunctions.Command_Diff_Fix_Whitespace_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultFiles(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Files_Name_MakeAbsolute: dialogResult = FilesFunctions.Command_Files_Name_MakeAbsolute_Dialog(this); break;
				case NECommand.Files_Name_MakeRelative: dialogResult = FilesFunctions.Command_Files_Name_MakeRelative_Dialog(this); break;
				case NECommand.Files_Name_GetUnique: dialogResult = FilesFunctions.Command_Files_Name_GetUnique_Dialog(this); break;
				case NECommand.Files_Set_Size: dialogResult = FilesFunctions.Command_Files_Set_Size_Dialog(this); break;
				case NECommand.Files_Set_Time_Write: dialogResult = FilesFunctions.Command_Files_Set_Time_Dialog(this); break;
				case NECommand.Files_Set_Time_Access: dialogResult = FilesFunctions.Command_Files_Set_Time_Dialog(this); break;
				case NECommand.Files_Set_Time_Create: dialogResult = FilesFunctions.Command_Files_Set_Time_Dialog(this); break;
				case NECommand.Files_Set_Time_All: dialogResult = FilesFunctions.Command_Files_Set_Time_Dialog(this); break;
				case NECommand.Files_Set_Attributes: dialogResult = FilesFunctions.Command_Files_Set_Attributes_Dialog(this); break;
				case NECommand.Files_Find_Binary: dialogResult = FilesFunctions.Command_Files_Find_Binary_Dialog(this); break;
				case NECommand.Files_Find_Text: dialogResult = FilesFunctions.Command_Files_Find_Text_Dialog(this); break;
				case NECommand.Files_Find_MassFind: dialogResult = FilesFunctions.Command_Files_Find_MassFind_Dialog(this); break;
				case NECommand.Files_Insert: dialogResult = FilesFunctions.Command_Files_Insert_Dialog(this); break;
				case NECommand.Files_Create_FromExpressions: dialogResult = FilesFunctions.Command_Files_Create_FromExpressions_Dialog(this); break;
				case NECommand.Files_Select_ByVersionControlStatus: dialogResult = FilesFunctions.Command_Files_Select_ByVersionControlStatus_Dialog(this); break;
				case NECommand.Files_Hash: dialogResult = FilesFunctions.Command_Files_Hash_Dialog(this); break;
				case NECommand.Files_Sign: dialogResult = FilesFunctions.Command_Files_Sign_Dialog(this); break;
				case NECommand.Files_Operations_Copy: dialogResult = FilesFunctions.Command_Files_Operations_CopyMove_Dialog(this, false); break;
				case NECommand.Files_Operations_Move: dialogResult = FilesFunctions.Command_Files_Operations_CopyMove_Dialog(this, true); break;
				case NECommand.Files_Operations_Encoding: dialogResult = FilesFunctions.Command_Files_Operations_Encoding_Dialog(this); break;
				case NECommand.Files_Operations_SplitFile: dialogResult = FilesFunctions.Command_Files_Operations_SplitFile_Dialog(this); break;
				case NECommand.Files_Operations_CombineFiles: dialogResult = FilesFunctions.Command_Files_Operations_CombineFiles_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultExpression(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Expression_Expression: dialogResult = ExpressionFunctions.Command_Expression_Expression_Dialog(this); break;
				case NECommand.Expression_Copy: dialogResult = ExpressionFunctions.Command_Expression_Copy_Dialog(this); break;
				case NECommand.Expression_SelectByExpression: dialogResult = ExpressionFunctions.Command_Expression_SelectByExpression_Dialog(this); break;
				case NECommand.Expression_InlineVariables_Solve: dialogResult = ExpressionFunctions.Command_Expression_InlineVariables_Solve_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultText(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Text_Select_Trim: dialogResult = TextFunctions.Command_Text_Select_Trim_Dialog(this); break;
				case NECommand.Text_Select_ByWidth: dialogResult = TextFunctions.Command_Text_Select_ByWidth_Dialog(this); break;
				case NECommand.Text_Select_WholeWord: dialogResult = TextFunctions.Command_Text_Select_WholeBoundedWord_Dialog(this, true); break;
				case NECommand.Text_Select_BoundedWord: dialogResult = TextFunctions.Command_Text_Select_WholeBoundedWord_Dialog(this, false); break;
				case NECommand.Text_Width: dialogResult = TextFunctions.Command_Text_Width_Dialog(this); break;
				case NECommand.Text_Trim: dialogResult = TextFunctions.Command_Text_Trim_Dialog(this); break;
				case NECommand.Text_Unicode: dialogResult = TextFunctions.Command_Text_Unicode_Dialog(this); break;
				case NECommand.Text_RandomText: dialogResult = TextFunctions.Command_Text_RandomText_Dialog(this); break;
				case NECommand.Text_ReverseRegEx: dialogResult = TextFunctions.Command_Text_ReverseRegEx_Dialog(this); break;
				case NECommand.Text_FirstDistinct: dialogResult = TextFunctions.Command_Text_FirstDistinct_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultNumeric(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Numeric_ConvertBase: dialogResult = NumericFunctions.Command_Numeric_ConvertBase_Dialog(this); break;
				case NECommand.Numeric_Series_Linear: dialogResult = NumericFunctions.Command_Numeric_Series_LinearGeometric_Dialog(this, true); break;
				case NECommand.Numeric_Series_Geometric: dialogResult = NumericFunctions.Command_Numeric_Series_LinearGeometric_Dialog(this, false); break;
				case NECommand.Numeric_Scale: dialogResult = NumericFunctions.Command_Numeric_Scale_Dialog(this); break;
				case NECommand.Numeric_Floor: dialogResult = NumericFunctions.Command_Numeric_Floor_Dialog(this); break;
				case NECommand.Numeric_Ceiling: dialogResult = NumericFunctions.Command_Numeric_Ceiling_Dialog(this); break;
				case NECommand.Numeric_Round: dialogResult = NumericFunctions.Command_Numeric_Round_Dialog(this); break;
				case NECommand.Numeric_Limit: dialogResult = NumericFunctions.Command_Numeric_Limit_Dialog(this); break;
				case NECommand.Numeric_Cycle: dialogResult = NumericFunctions.Command_Numeric_Cycle_Dialog(this); break;
				case NECommand.Numeric_RandomNumber: dialogResult = NumericFunctions.Command_Numeric_RandomNumber_Dialog(this); break;
				case NECommand.Numeric_CombinationsPermutations: dialogResult = NumericFunctions.Command_Numeric_CombinationsPermutations_Dialog(this); break;
				case NECommand.Numeric_MinMaxValues: dialogResult = NumericFunctions.Command_Numeric_MinMaxValues_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultDateTime(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.DateTime_Convert: dialogResult = DateTimeFunctions.Command_DateTime_Convert_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultImage(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Image_GrabColor: dialogResult = ImageFunctions.Command_Image_GrabColor_Dialog(this); break;
				case NECommand.Image_GrabImage: dialogResult = ImageFunctions.Command_Image_GrabImage_Dialog(this); break;
				case NECommand.Image_AdjustColor: dialogResult = ImageFunctions.Command_Image_AdjustColor_Dialog(this); break;
				case NECommand.Image_AddColor: dialogResult = ImageFunctions.Command_Image_AddOverlayColor_Dialog(this, true); break;
				case NECommand.Image_OverlayColor: dialogResult = ImageFunctions.Command_Image_AddOverlayColor_Dialog(this, false); break;
				case NECommand.Image_Size: dialogResult = ImageFunctions.Command_Image_Size_Dialog(this); break;
				case NECommand.Image_Crop: dialogResult = ImageFunctions.Command_Image_Crop_Dialog(this); break;
				case NECommand.Image_Rotate: dialogResult = ImageFunctions.Command_Image_Rotate_Dialog(this); break;
				case NECommand.Image_GIF_Animate: dialogResult = ImageFunctions.Command_Image_GIF_Animate_Dialog(this); break;
				case NECommand.Image_GIF_Split: dialogResult = ImageFunctions.Command_Image_GIF_Split_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultTable(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Table_Convert: dialogResult = TableFunctions.Command_Table_Convert_Dialog(this); break;
				case NECommand.Table_TextToTable: dialogResult = TableFunctions.Command_Table_TextToTable_Dialog(this); break;
				case NECommand.Table_EditTable: dialogResult = TableFunctions.Command_Table_EditTable_Dialog(this); break;
				case NECommand.Table_AddColumn: dialogResult = TableFunctions.Command_Table_AddColumn_Dialog(this); break;
				case NECommand.Table_Select_RowsByExpression: dialogResult = TableFunctions.Command_Table_Select_RowsByExpression_Dialog(this); break;
				case NECommand.Table_Join: dialogResult = TableFunctions.Command_Table_Join_Dialog(this); break;
				case NECommand.Table_Database_GenerateInserts: dialogResult = TableFunctions.Command_Table_Database_GenerateInserts_Dialog(this); break;
				case NECommand.Table_Database_GenerateUpdates: dialogResult = TableFunctions.Command_Table_Database_GenerateUpdates_Dialog(this); break;
				case NECommand.Table_Database_GenerateDeletes: dialogResult = TableFunctions.Command_Table_Database_GenerateDeletes_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultPosition(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Position_Goto_Lines: dialogResult = PositionFunctions.Command_Position_Goto_Dialog(this, GotoType.Line); break;
				case NECommand.Position_Goto_Columns: dialogResult = PositionFunctions.Command_Position_Goto_Dialog(this, GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: dialogResult = PositionFunctions.Command_Position_Goto_Dialog(this, GotoType.Index); break;
				case NECommand.Position_Goto_Positions: dialogResult = PositionFunctions.Command_Position_Goto_Dialog(this, GotoType.Position); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultContent(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Content_Ancestor: dialogResult = ContentFunctions.Command_Content_Ancestor_Dialog(this); break;
				case NECommand.Content_Attributes: dialogResult = ContentFunctions.Command_Content_Attributes_Dialog(this); break;
				case NECommand.Content_WithAttribute: dialogResult = ContentFunctions.Command_Content_WithAttribute_Dialog(this); break;
				case NECommand.Content_Children_WithAttribute: dialogResult = ContentFunctions.Command_Content_Children_WithAttribute_Dialog(this); break;
				case NECommand.Content_Descendants_WithAttribute: dialogResult = ContentFunctions.Command_Content_Descendants_WithAttribute_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultNetwork(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Network_AbsoluteURL: dialogResult = NetworkFunctions.Command_Network_AbsoluteURL_Dialog(this); break;
				case NECommand.Network_FetchFile: dialogResult = NetworkFunctions.Command_Network_FetchFile_Dialog(this); break;
				case NECommand.Network_FetchStream: dialogResult = NetworkFunctions.Command_Network_FetchStream_Dialog(this); break;
				case NECommand.Network_FetchPlaylist: dialogResult = NetworkFunctions.Command_Network_FetchPlaylist_Dialog(this); break;
				case NECommand.Network_Ping: dialogResult = NetworkFunctions.Command_Network_Ping_Dialog(this); break;
				case NECommand.Network_ScanPorts: dialogResult = NetworkFunctions.Command_Network_ScanPorts_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultDatabase(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Database_Connect: dialogResult = DatabaseFunctions.Command_Database_Connect_Dialog(this); break;
				case NECommand.Database_Examine: DatabaseFunctions.Command_Database_Examine_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultSelect(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Select_Limit: dialogResult = SelectFunctions.Command_Select_Limit_Dialog(this); break;
				case NECommand.Select_Repeats_ByCount: dialogResult = SelectFunctions.Command_Select_Repeats_ByCount_Dialog(this); break;
				case NECommand.Select_Split: dialogResult = SelectFunctions.Command_Select_Split_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool GetDialogResultRegion(NECommand command, ref object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.Region_ModifyRegions: dialogResult = RegionFunctions.Command_Region_ModifyRegions_Dialog(this); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool timeNext = false;
		public void HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
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

			switch (command.GetArea())
			{
				case "File": HandleCommandFile(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Edit": HandleCommandEdit(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Diff": HandleCommandDiff(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Files": HandleCommandFiles(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Expression": HandleCommandExpression(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Text": HandleCommandText(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Numeric": HandleCommandNumeric(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "DateTime": HandleCommandDateTime(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Image": HandleCommandImage(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Table": HandleCommandTable(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Position": HandleCommandPosition(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Content": HandleCommandContent(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Network": HandleCommandNetwork(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Database": HandleCommandDatabase(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Keys": HandleCommandKeys(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Select": HandleCommandSelect(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Region": HandleCommandRegion(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Macro": HandleCommandMacro(command, shiftDown, dialogResult, multiStatus, answer); break;
				case "Window": HandleCommandWindow(command, shiftDown, dialogResult, multiStatus, answer); break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != NECommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message(WindowParent)
				{
					Title = "Timer",
					Text = $"Elapsed time: {elapsed:n} ms",
					Options = MessageOptions.Ok,
				}.Show();
			}
		}

		void HandleCommandFile(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.File_New_FromSelections: FileFunctions.Command_File_New_FromSelections(this); break;
				case NECommand.File_Open_Selected: FileFunctions.Command_File_Open_Selected(this); break;
				case NECommand.File_Save_Save: FileFunctions.Command_File_Save_Save(this); break;
				case NECommand.File_Save_SaveAs: FileFunctions.Command_File_Save_SaveAs(this); break;
				case NECommand.File_Save_SaveAsByExpression: FileFunctions.Command_File_Save_SaveAsByExpression(this, dialogResult as GetExpressionDialog.Result, answer); break;
				case NECommand.File_Save_CopyTo: FileFunctions.Command_File_Save_SaveAs(this, true); break;
				case NECommand.File_Save_CopyToByExpression: FileFunctions.Command_File_Save_SaveAsByExpression(this, dialogResult as GetExpressionDialog.Result, answer, true); break;
				case NECommand.File_Save_SetDisplayName: FileFunctions.Command_File_Save_SetDisplayName(this, dialogResult as GetExpressionDialog.Result); break;
				case NECommand.File_Operations_Rename: FileFunctions.Command_File_Operations_Rename(this); break;
				case NECommand.File_Operations_RenameByExpression: FileFunctions.Command_File_Operations_RenameByExpression(this, dialogResult as GetExpressionDialog.Result, answer); break;
				case NECommand.File_Operations_Delete: FileFunctions.Command_File_Operations_Delete(this, answer); break;
				case NECommand.File_Operations_Explore: FileFunctions.Command_File_Operations_Explore(this); break;
				case NECommand.File_Operations_CommandPrompt: FileFunctions.Command_File_Operations_CommandPrompt(this); break;
				case NECommand.File_Operations_VCSDiff: FileFunctions.Command_File_Operations_VCSDiff(this); break;
				case NECommand.File_Close: FileFunctions.Command_File_Close(this, answer); break;
				case NECommand.File_Refresh: FileFunctions.Command_File_Refresh(this, answer); break;
				case NECommand.File_AutoRefresh: FileFunctions.Command_File_AutoRefresh(this, multiStatus); break;
				case NECommand.File_Revert: FileFunctions.Command_File_Revert(this, answer); break;
				case NECommand.File_Insert_Files: FileFunctions.Command_File_Insert_Files(this); break;
				case NECommand.File_Insert_CopiedCut: FileFunctions.Command_File_Insert_CopiedCut(this); break;
				case NECommand.File_Insert_Selected: FileFunctions.Command_File_Insert_Selected(this); break;
				case NECommand.File_Copy_Path: FileFunctions.Command_File_Copy_Path(this); break;
				case NECommand.File_Copy_Name: FileFunctions.Command_File_Copy_Name(this); break;
				case NECommand.File_Copy_DisplayName: FileFunctions.Command_File_Copy_DisplayName(this); break;
				case NECommand.File_Encoding_Encoding: FileFunctions.Command_File_Encoding_Encoding(this, dialogResult as EncodingDialog.Result); break;
				case NECommand.File_Encoding_ReopenWithEncoding: FileFunctions.Command_File_Encoding_ReopenWithEncoding(this, dialogResult as EncodingDialog.Result, answer); break;
				case NECommand.File_Encoding_LineEndings: FileFunctions.Command_File_Encoding_LineEndings(this, dialogResult as FileEncodingLineEndingsDialog.Result); break;
				case NECommand.File_Encrypt: FileFunctions.Command_File_Encrypt(this, dialogResult as string); break;
				case NECommand.File_Compress: FileFunctions.Command_File_Compress(this, multiStatus); break;
			}
		}

		void HandleCommandEdit(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Edit_Undo: EditFunctions.Command_Edit_Undo(this); break;
				case NECommand.Edit_Redo: EditFunctions.Command_Edit_Redo(this); break;
				case NECommand.Edit_Copy_Copy: EditFunctions.Command_Edit_Copy_CutCopy(this, false); break;
				case NECommand.Edit_Copy_Cut: EditFunctions.Command_Edit_Copy_CutCopy(this, true); break;
				case NECommand.Edit_Paste_Paste: EditFunctions.Command_Edit_Paste_Paste(this, shiftDown, false); break;
				case NECommand.Edit_Paste_RotatePaste: EditFunctions.Command_Edit_Paste_Paste(this, true, true); break;
				case NECommand.Edit_Find_Find: EditFunctions.Command_Edit_Find_Find(this, shiftDown, dialogResult as EditFindFindDialog.Result); break;
				case NECommand.Edit_Find_Next: EditFunctions.Command_Edit_Find_NextPrevious(this, true, shiftDown); break;
				case NECommand.Edit_Find_Previous: EditFunctions.Command_Edit_Find_NextPrevious(this, false, shiftDown); break;
				case NECommand.Edit_Find_Selected: EditFunctions.Command_Edit_Find_Selected(this, shiftDown); break;
				case NECommand.Edit_Find_MassFind: EditFunctions.Command_Edit_Find_MassFind(this, dialogResult as EditFindMassFindDialog.Result); break;
				case NECommand.Edit_Find_Replace: EditFunctions.Command_Edit_Find_Replace(this, dialogResult as EditFindReplaceDialog.Result); break;
				case NECommand.Edit_Find_ClearSearchResults: EditFunctions.Command_Edit_Find_ClearSearchResults(this); break;
				case NECommand.Edit_CopyDown: EditFunctions.Command_Edit_CopyDown(this); break;
				case NECommand.Edit_Rotate: EditFunctions.Command_Edit_Rotate(this, dialogResult as EditRotateDialog.Result); break;
				case NECommand.Edit_Repeat: EditFunctions.Command_Edit_Repeat(this, dialogResult as EditRepeatDialog.Result); break;
				case NECommand.Edit_Markup_Escape: EditFunctions.Command_Edit_Markup_Escape(this); break;
				case NECommand.Edit_Markup_Unescape: EditFunctions.Command_Edit_Markup_Unescape(this); break;
				case NECommand.Edit_RegEx_Escape: EditFunctions.Command_Edit_RegEx_Escape(this); break;
				case NECommand.Edit_RegEx_Unescape: EditFunctions.Command_Edit_RegEx_Unescape(this); break;
				case NECommand.Edit_URL_Escape: EditFunctions.Command_Edit_URL_Escape(this); break;
				case NECommand.Edit_URL_Unescape: EditFunctions.Command_Edit_URL_Unescape(this); break;
				case NECommand.Edit_URL_Absolute: EditFunctions.Command_Edit_URL_Absolute(this, dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Edit_Data_Hash: EditFunctions.Command_Edit_Data_Hash(this, dialogResult as EditDataHashDialog.Result); break;
				case NECommand.Edit_Data_Compress: EditFunctions.Command_Edit_Data_Compress(this, dialogResult as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Decompress: EditFunctions.Command_Edit_Data_Decompress(this, dialogResult as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Encrypt: EditFunctions.Command_Edit_Data_Encrypt(this, dialogResult as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Decrypt: EditFunctions.Command_Edit_Data_Decrypt(this, dialogResult as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Sign: EditFunctions.Command_Edit_Data_Sign(this, dialogResult as EditDataSignDialog.Result); break;
				case NECommand.Edit_Sort: EditFunctions.Command_Edit_Sort(this, dialogResult as EditSortDialog.Result); break;
				case NECommand.Edit_Convert: EditFunctions.Command_Edit_Convert(this, dialogResult as EditConvertDialog.Result); break;
				case NECommand.Edit_Bookmarks_Toggle: EditFunctions.Command_Edit_Bookmarks_Toggle(this); break;
				case NECommand.Edit_Bookmarks_Next: EditFunctions.Command_Edit_Bookmarks_NextPreviousBookmark(this, true, shiftDown); break;
				case NECommand.Edit_Bookmarks_Previous: EditFunctions.Command_Edit_Bookmarks_NextPreviousBookmark(this, false, shiftDown); break;
				case NECommand.Edit_Bookmarks_Clear: EditFunctions.Command_Edit_Bookmarks_Clear(this); break;
				case NECommand.Edit_Navigate_WordLeft: EditFunctions.Command_Edit_Navigate_WordLeftRight(this, false, shiftDown); break;
				case NECommand.Edit_Navigate_WordRight: EditFunctions.Command_Edit_Navigate_WordLeftRight(this, true, shiftDown); break;
				case NECommand.Edit_Navigate_AllLeft: EditFunctions.Command_Edit_Navigate_AllLeft(this, shiftDown); break;
				case NECommand.Edit_Navigate_AllRight: EditFunctions.Command_Edit_Navigate_AllRight(this, shiftDown); break;
				case NECommand.Edit_Navigate_JumpBy_Words: EditFunctions.Command_Edit_Navigate_JumpBy(this, JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: EditFunctions.Command_Edit_Navigate_JumpBy(this, JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: EditFunctions.Command_Edit_Navigate_JumpBy(this, JumpByType.Paths); break;
			}
		}

		void HandleCommandDiff(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Diff_Selections: DiffFunctions.Command_Diff_Selections(this); break;
				case NECommand.Diff_SelectedFiles: DiffFunctions.Command_Diff_SelectedFiles(this); break;
				case NECommand.Diff_VCSNormalFiles: DiffFunctions.Command_Diff_Diff_VCSNormalFiles(this); break;
				case NECommand.Diff_Regions_Region1: DiffFunctions.Command_Diff_Regions_Region(this, 1); break;
				case NECommand.Diff_Regions_Region2: DiffFunctions.Command_Diff_Regions_Region(this, 2); break;
				case NECommand.Diff_Regions_Region3: DiffFunctions.Command_Diff_Regions_Region(this, 3); break;
				case NECommand.Diff_Regions_Region4: DiffFunctions.Command_Diff_Regions_Region(this, 4); break;
				case NECommand.Diff_Regions_Region5: DiffFunctions.Command_Diff_Regions_Region(this, 5); break;
				case NECommand.Diff_Regions_Region6: DiffFunctions.Command_Diff_Regions_Region(this, 6); break;
				case NECommand.Diff_Regions_Region7: DiffFunctions.Command_Diff_Regions_Region(this, 7); break;
				case NECommand.Diff_Regions_Region8: DiffFunctions.Command_Diff_Regions_Region(this, 8); break;
				case NECommand.Diff_Regions_Region9: DiffFunctions.Command_Diff_Regions_Region(this, 9); break;
				case NECommand.Diff_Break: DiffFunctions.Command_Diff_Break(this); break;
				case NECommand.Diff_IgnoreWhitespace: DiffFunctions.Command_Diff_IgnoreWhitespace(this, multiStatus); break;
				case NECommand.Diff_IgnoreCase: DiffFunctions.Command_Diff_IgnoreCase(this, multiStatus); break;
				case NECommand.Diff_IgnoreNumbers: DiffFunctions.Command_Diff_IgnoreNumbers(this, multiStatus); break;
				case NECommand.Diff_IgnoreLineEndings: DiffFunctions.Command_Diff_IgnoreLineEndings(this, multiStatus); break;
				case NECommand.Diff_IgnoreCharacters: DiffFunctions.Command_Diff_IgnoreCharacters(this, dialogResult as DiffIgnoreCharactersDialog.Result); break;
				case NECommand.Diff_Reset: DiffFunctions.Command_Diff_Reset(this); break;
				case NECommand.Diff_Next: DiffFunctions.Command_Diff_NextPrevious(this, true, shiftDown); break;
				case NECommand.Diff_Previous: DiffFunctions.Command_Diff_NextPrevious(this, false, shiftDown); break;
				case NECommand.Diff_CopyLeft: DiffFunctions.Command_Diff_CopyLeftRight(this, true); break;
				case NECommand.Diff_CopyRight: DiffFunctions.Command_Diff_CopyLeftRight(this, false); break;
				case NECommand.Diff_Fix_Whitespace: DiffFunctions.Command_Diff_Fix_Whitespace(this, dialogResult as DiffFixWhitespaceDialog.Result); break;
				case NECommand.Diff_Fix_Case: DiffFunctions.Command_Diff_Fix_Case(this); break;
				case NECommand.Diff_Fix_Numbers: DiffFunctions.Command_Diff_Fix_Numbers(this); break;
				case NECommand.Diff_Fix_LineEndings: DiffFunctions.Command_Diff_Fix_LineEndings(this); break;
				case NECommand.Diff_Fix_Encoding: DiffFunctions.Command_Diff_Fix_Encoding(this); break;
				case NECommand.Diff_Select_Match: DiffFunctions.Command_Diff_Select_MatchDiff(this, true); break;
				case NECommand.Diff_Select_Diff: DiffFunctions.Command_Diff_Select_MatchDiff(this, false); break;
			}
		}

		void HandleCommandFiles(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Files_Name_Simplify: FilesFunctions.Command_Files_Name_Simplify(this); break;
				case NECommand.Files_Name_MakeAbsolute: FilesFunctions.Command_Files_Name_MakeAbsolute(this, dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_MakeRelative: FilesFunctions.Command_Files_Name_MakeRelative(this, dialogResult as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_GetUnique: FilesFunctions.Command_Files_Name_GetUnique(this, dialogResult as FilesNamesGetUniqueDialog.Result); break;
				case NECommand.Files_Name_Sanitize: FilesFunctions.Command_Files_Name_Sanitize(this); break;
				case NECommand.Files_Get_Size: FilesFunctions.Command_Files_Get_Size(this); break;
				case NECommand.Files_Get_Time_Write: FilesFunctions.Command_Files_Get_Time(this, TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: FilesFunctions.Command_Files_Get_Time(this, TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: FilesFunctions.Command_Files_Get_Time(this, TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: FilesFunctions.Command_Files_Get_Attributes(this); break;
				case NECommand.Files_Get_Version_File: FilesFunctions.Command_Files_Get_Version_File(this); break;
				case NECommand.Files_Get_Version_Product: FilesFunctions.Command_Files_Get_Version_Product(this); break;
				case NECommand.Files_Get_Version_Assembly: FilesFunctions.Command_Files_Get_Version_Assembly(this); break;
				case NECommand.Files_Get_Children: FilesFunctions.Command_Files_Get_ChildrenDescendants(this, false); break;
				case NECommand.Files_Get_Descendants: FilesFunctions.Command_Files_Get_ChildrenDescendants(this, true); break;
				case NECommand.Files_Get_VersionControlStatus: FilesFunctions.Command_Files_Get_VersionControlStatus(this); break;
				case NECommand.Files_Set_Size: FilesFunctions.Command_Files_Set_Size(this, dialogResult as FilesSetSizeDialog.Result); break;
				case NECommand.Files_Set_Time_Write: FilesFunctions.Command_Files_Set_Time(this, TimestampType.Write, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Access: FilesFunctions.Command_Files_Set_Time(this, TimestampType.Access, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Create: FilesFunctions.Command_Files_Set_Time(this, TimestampType.Create, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_All: FilesFunctions.Command_Files_Set_Time(this, TimestampType.All, dialogResult as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Attributes: FilesFunctions.Command_Files_Set_Attributes(this, dialogResult as FilesSetAttributesDialog.Result); break;
				case NECommand.Files_Find_Binary: FilesFunctions.Command_Files_Find_Binary(this, dialogResult as FilesFindBinaryDialog.Result, answer); break;
				case NECommand.Files_Find_Text: FilesFunctions.Command_Files_Find_Text(this, dialogResult as FilesFindTextDialog.Result, answer); break;
				case NECommand.Files_Find_MassFind: FilesFunctions.Command_Files_Find_MassFind(this, dialogResult as FilesFindMassFindDialog.Result, answer); break;
				case NECommand.Files_Insert: FilesFunctions.Command_Files_Insert(this, dialogResult as FilesInsertDialog.Result); break;
				case NECommand.Files_Create_Files: FilesFunctions.Command_Files_Create_Files(this); break;
				case NECommand.Files_Create_Directories: FilesFunctions.Command_Files_Create_Directories(this); break;
				case NECommand.Files_Create_FromExpressions: FilesFunctions.Command_Files_Create_FromExpressions(this, dialogResult as FilesCreateFromExpressionsDialog.Result); break;
				case NECommand.Files_Select_Name_Directory: FilesFunctions.Command_Files_Select_Name(this, GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: FilesFunctions.Command_Files_Select_Name(this, GetPathType.FileName); break;
				case NECommand.Files_Select_Name_FileNamewoExtension: FilesFunctions.Command_Files_Select_Name(this, GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: FilesFunctions.Command_Files_Select_Name(this, GetPathType.Extension); break;
				case NECommand.Files_Select_Files: FilesFunctions.Command_Files_Select_Files(this); break;
				case NECommand.Files_Select_Directories: FilesFunctions.Command_Files_Select_Directories(this); break;
				case NECommand.Files_Select_Existing: FilesFunctions.Command_Files_Select_Existing(this, true); break;
				case NECommand.Files_Select_NonExisting: FilesFunctions.Command_Files_Select_Existing(this, false); break;
				case NECommand.Files_Select_Roots: FilesFunctions.Command_Files_Select_Roots(this, true); break;
				case NECommand.Files_Select_NonRoots: FilesFunctions.Command_Files_Select_Roots(this, false); break;
				case NECommand.Files_Select_MatchDepth: FilesFunctions.Command_Files_Select_MatchDepth(this); break;
				case NECommand.Files_Select_CommonAncestor: FilesFunctions.Command_Files_Select_CommonAncestor(this); break;
				case NECommand.Files_Select_ByVersionControlStatus: FilesFunctions.Command_Files_Select_ByVersionControlStatus(this, dialogResult as FilesSelectByVersionControlStatusDialog.Result); break;
				case NECommand.Files_Hash: FilesFunctions.Command_Files_Hash(this, dialogResult as FilesHashDialog.Result); break;
				case NECommand.Files_Sign: FilesFunctions.Command_Files_Sign(this, dialogResult as FilesSignDialog.Result); break;
				case NECommand.Files_Operations_Copy: FilesFunctions.Command_Files_Operations_CopyMove(this, dialogResult as FilesOperationsCopyMoveDialog.Result, false); break;
				case NECommand.Files_Operations_Move: FilesFunctions.Command_Files_Operations_CopyMove(this, dialogResult as FilesOperationsCopyMoveDialog.Result, true); break;
				case NECommand.Files_Operations_Delete: FilesFunctions.Command_Files_Operations_Delete(this); break;
				case NECommand.Files_Operations_DragDrop: FilesFunctions.Command_Files_Operations_DragDrop(this); break;
				case NECommand.Files_Operations_Explore: FilesFunctions.Command_Files_Operations_Explore(this); break;
				case NECommand.Files_Operations_CommandPrompt: FilesFunctions.Command_Files_Operations_CommandPrompt(this); break;
				case NECommand.Files_Operations_RunCommand_Parallel: FilesFunctions.Command_Files_Operations_RunCommand_Parallel(this); break;
				case NECommand.Files_Operations_RunCommand_Sequential: FilesFunctions.Command_Files_Operations_RunCommand_Sequential(this); break;
				case NECommand.Files_Operations_RunCommand_Shell: FilesFunctions.Command_Files_Operations_RunCommand_Shell(this); break;
				case NECommand.Files_Operations_Encoding: FilesFunctions.Command_Files_Operations_Encoding(this, dialogResult as FilesOperationsEncodingDialog.Result); break;
				case NECommand.Files_Operations_SplitFile: FilesFunctions.Command_Files_Operations_SplitFile(this, dialogResult as FilesOperationsSplitFileDialog.Result); break;
				case NECommand.Files_Operations_CombineFiles: FilesFunctions.Command_Files_Operations_CombineFiles(this, dialogResult as FilesOperationsCombineFilesDialog.Result); break;
			}
		}

		void HandleCommandExpression(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Expression_Expression: ExpressionFunctions.Command_Expression_Expression(this, dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_Copy: ExpressionFunctions.Command_Expression_Copy(this, dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_EvaluateSelected: ExpressionFunctions.Command_Expression_EvaluateSelected(this); break;
				case NECommand.Expression_SelectByExpression: ExpressionFunctions.Command_Expression_SelectByExpression(this, dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Expression_InlineVariables_Add: ExpressionFunctions.Command_Expression_InlineVariables_Add(this); break;
				case NECommand.Expression_InlineVariables_Calculate: ExpressionFunctions.Command_Expression_InlineVariables_Calculate(this); break;
				case NECommand.Expression_InlineVariables_Solve: ExpressionFunctions.Command_Expression_InlineVariables_Solve(this, dialogResult as ExpressionSolveDialog.Result, answer); break;
				case NECommand.Expression_InlineVariables_IncludeInExpressions: ExpressionFunctions.Command_Expression_InlineVariables_IncludeInExpressions(this, multiStatus); break;
			}
		}

		void HandleCommandText(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Text_Select_Trim: TextFunctions.Command_Text_Select_Trim(this, dialogResult as TextTrimDialog.Result); break;
				case NECommand.Text_Select_ByWidth: TextFunctions.Command_Text_Select_ByWidth(this, dialogResult as TextWidthDialog.Result); break;
				case NECommand.Text_Select_WholeWord: TextFunctions.Command_Text_Select_WholeBoundedWord(this, dialogResult as TextSelectWholeBoundedWordDialog.Result, true); break;
				case NECommand.Text_Select_BoundedWord: TextFunctions.Command_Text_Select_WholeBoundedWord(this, dialogResult as TextSelectWholeBoundedWordDialog.Result, false); break;
				case NECommand.Text_Select_Min_Text: TextFunctions.Command_Text_Select_MinMax_Text(this, false); break;
				case NECommand.Text_Select_Min_Length: TextFunctions.Command_Text_Select_MinMax_Length(this, false); break;
				case NECommand.Text_Select_Max_Text: TextFunctions.Command_Text_Select_MinMax_Text(this, true); break;
				case NECommand.Text_Select_Max_Length: TextFunctions.Command_Text_Select_MinMax_Length(this, true); break;
				case NECommand.Text_Case_Upper: TextFunctions.Command_Text_Case_Upper(this); break;
				case NECommand.Text_Case_Lower: TextFunctions.Command_Text_Case_Lower(this); break;
				case NECommand.Text_Case_Proper: TextFunctions.Command_Text_Case_Proper(this); break;
				case NECommand.Text_Case_Toggle: TextFunctions.Command_Text_Case_Toggle(this); break;
				case NECommand.Text_Length: TextFunctions.Command_Text_Length(this); break;
				case NECommand.Text_Width: TextFunctions.Command_Text_Width(this, dialogResult as TextWidthDialog.Result); break;
				case NECommand.Text_Trim: TextFunctions.Command_Text_Trim(this, dialogResult as TextTrimDialog.Result); break;
				case NECommand.Text_SingleLine: TextFunctions.Command_Text_SingleLine(this); break;
				case NECommand.Text_Unicode: TextFunctions.Command_Text_Unicode(this, dialogResult as TextUnicodeDialog.Result); break;
				case NECommand.Text_GUID: TextFunctions.Command_Text_GUID(this); break;
				case NECommand.Text_RandomText: TextFunctions.Command_Text_RandomText(this, dialogResult as TextRandomTextDialog.Result); break;
				case NECommand.Text_LoremIpsum: TextFunctions.Command_Text_LoremIpsum(this); break;
				case NECommand.Text_ReverseRegEx: TextFunctions.Command_Text_ReverseRegEx(this, dialogResult as TextReverseRegExDialog.Result); break;
				case NECommand.Text_FirstDistinct: TextFunctions.Command_Text_FirstDistinct(this, dialogResult as TextFirstDistinctDialog.Result); break;
				case NECommand.Text_RepeatCount: TextFunctions.Command_Text_RepeatCount(this); break;
				case NECommand.Text_RepeatIndex: TextFunctions.Command_Text_RepeatIndex(this); break;
			}
		}

		void HandleCommandNumeric(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Numeric_Select_Min: NumericFunctions.Command_Numeric_Select_MinMax(this, false); break;
				case NECommand.Numeric_Select_Max: NumericFunctions.Command_Numeric_Select_MinMax(this, true); break;
				case NECommand.Numeric_Select_Fraction_Whole: NumericFunctions.Command_Numeric_Select_Fraction_Whole(this); break;
				case NECommand.Numeric_Select_Fraction_Fraction: NumericFunctions.Command_Numeric_Select_Fraction_Fraction(this); break;
				case NECommand.Numeric_Hex_ToHex: NumericFunctions.Command_Numeric_Hex_ToHex(this); break;
				case NECommand.Numeric_Hex_FromHex: NumericFunctions.Command_Numeric_Hex_FromHex(this); break;
				case NECommand.Numeric_ConvertBase: NumericFunctions.Command_Numeric_ConvertBase(this, dialogResult as NumericConvertBaseDialog.Result); break;
				case NECommand.Numeric_Series_ZeroBased: NumericFunctions.Command_Numeric_Series_ZeroBased(this); break;
				case NECommand.Numeric_Series_OneBased: NumericFunctions.Command_Numeric_Series_OneBased(this); break;
				case NECommand.Numeric_Series_Linear: NumericFunctions.Command_Numeric_Series_LinearGeometric(this, dialogResult as NumericSeriesDialog.Result, true); break;
				case NECommand.Numeric_Series_Geometric: NumericFunctions.Command_Numeric_Series_LinearGeometric(this, dialogResult as NumericSeriesDialog.Result, false); break;
				case NECommand.Numeric_Scale: NumericFunctions.Command_Numeric_Scale(this, dialogResult as NumericScaleDialog.Result); break;
				case NECommand.Numeric_Add_Sum: NumericFunctions.Command_Numeric_Add_Sum(this); break;
				case NECommand.Numeric_Add_ForwardSum: NumericFunctions.Command_Numeric_Add_ForwardReverseSum(this, true, false); break;
				case NECommand.Numeric_Add_ReverseSum: NumericFunctions.Command_Numeric_Add_ForwardReverseSum(this, false, false); break;
				case NECommand.Numeric_Add_UndoForwardSum: NumericFunctions.Command_Numeric_Add_ForwardReverseSum(this, true, true); break;
				case NECommand.Numeric_Add_UndoReverseSum: NumericFunctions.Command_Numeric_Add_ForwardReverseSum(this, false, true); break;
				case NECommand.Numeric_Add_Increment: NumericFunctions.Command_Numeric_Add_IncrementDecrement(this, true); break;
				case NECommand.Numeric_Add_Decrement: NumericFunctions.Command_Numeric_Add_IncrementDecrement(this, false); break;
				case NECommand.Numeric_Add_AddClipboard: NumericFunctions.Command_Numeric_Add_AddSubtractClipboard(this, true); break;
				case NECommand.Numeric_Add_SubtractClipboard: NumericFunctions.Command_Numeric_Add_AddSubtractClipboard(this, false); break;
				case NECommand.Numeric_Fraction_Whole: NumericFunctions.Command_Numeric_Fraction_Whole(this); break;
				case NECommand.Numeric_Fraction_Fraction: NumericFunctions.Command_Numeric_Fraction_Fraction(this); break;
				case NECommand.Numeric_Fraction_Simplify: NumericFunctions.Command_Numeric_Fraction_Simplify(this); break;
				case NECommand.Numeric_Absolute: NumericFunctions.Command_Numeric_Absolute(this); break;
				case NECommand.Numeric_Floor: NumericFunctions.Command_Numeric_Floor(this, dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Ceiling: NumericFunctions.Command_Numeric_Ceiling(this, dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Round: NumericFunctions.Command_Numeric_Round(this, dialogResult as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Limit: NumericFunctions.Command_Numeric_Limit(this, dialogResult as NumericLimitDialog.Result); break;
				case NECommand.Numeric_Cycle: NumericFunctions.Command_Numeric_Cycle(this, dialogResult as NumericCycleDialog.Result); break;
				case NECommand.Numeric_Trim: NumericFunctions.Command_Numeric_Trim(this); break;
				case NECommand.Numeric_Factor: NumericFunctions.Command_Numeric_Factor(this); break;
				case NECommand.Numeric_RandomNumber: NumericFunctions.Command_Numeric_RandomNumber(this, dialogResult as NumericRandomNumberDialog.Result); break;
				case NECommand.Numeric_CombinationsPermutations: NumericFunctions.Command_Numeric_CombinationsPermutations(this, dialogResult as NumericCombinationsPermutationsDialog.Result); break;
				case NECommand.Numeric_MinMaxValues: NumericFunctions.Command_Numeric_MinMaxValues(this, dialogResult as NumericMinMaxValuesDialog.Result); break;
			}
		}

		void HandleCommandDateTime(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.DateTime_Now: DateTimeFunctions.Command_DateTime_Now(this); break;
				case NECommand.DateTime_UtcNow: DateTimeFunctions.Command_DateTime_UtcNow(this); break;
				case NECommand.DateTime_Convert: DateTimeFunctions.Command_DateTime_Convert(this, dialogResult as DateTimeConvertDialog.Result); break;
			}
		}

		void HandleCommandImage(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Image_GrabColor: ImageFunctions.Command_Image_GrabColor(this, dialogResult as ImageGrabColorDialog.Result); break;
				case NECommand.Image_GrabImage: ImageFunctions.Command_Image_GrabImage(this, dialogResult as ImageGrabImageDialog.Result); break;
				case NECommand.Image_AdjustColor: ImageFunctions.Command_Image_AdjustColor(this, dialogResult as ImageAdjustColorDialog.Result); break;
				case NECommand.Image_AddColor: ImageFunctions.Command_Image_AddColor(this, dialogResult as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_OverlayColor: ImageFunctions.Command_Image_OverlayColor(this, dialogResult as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_Size: ImageFunctions.Command_Image_Size(this, dialogResult as ImageSizeDialog.Result); break;
				case NECommand.Image_Crop: ImageFunctions.Command_Image_Crop(this, dialogResult as ImageCropDialog.Result); break;
				case NECommand.Image_FlipHorizontal: ImageFunctions.Command_Image_FlipHorizontal(this); break;
				case NECommand.Image_FlipVertical: ImageFunctions.Command_Image_FlipVertical(this); break;
				case NECommand.Image_Rotate: ImageFunctions.Command_Image_Rotate(this, dialogResult as ImageRotateDialog.Result); break;
				case NECommand.Image_GIF_Animate: ImageFunctions.Command_Image_GIF_Animate(this, dialogResult as ImageGIFAnimateDialog.Result); break;
				case NECommand.Image_GIF_Split: ImageFunctions.Command_Image_GIF_Split(this, dialogResult as ImageGIFSplitDialog.Result); break;
			}
		}

		void HandleCommandTable(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Table_DetectType: TableFunctions.Command_Table_DetectType(this); break;
				case NECommand.Table_Convert: TableFunctions.Command_Table_Convert(this, dialogResult as TableConvertDialog.Result); break;
				case NECommand.Table_TextToTable: TableFunctions.Command_Table_TextToTable(this, dialogResult as TableTextToTableDialog.Result); break;
				case NECommand.Table_LineSelectionsToTable: TableFunctions.Command_Table_LineSelectionsToTable(this); break;
				case NECommand.Table_RegionSelectionsToTable_Region1: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 1); break;
				case NECommand.Table_RegionSelectionsToTable_Region2: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 2); break;
				case NECommand.Table_RegionSelectionsToTable_Region3: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 3); break;
				case NECommand.Table_RegionSelectionsToTable_Region4: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 4); break;
				case NECommand.Table_RegionSelectionsToTable_Region5: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 5); break;
				case NECommand.Table_RegionSelectionsToTable_Region6: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 6); break;
				case NECommand.Table_RegionSelectionsToTable_Region7: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 7); break;
				case NECommand.Table_RegionSelectionsToTable_Region8: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 8); break;
				case NECommand.Table_RegionSelectionsToTable_Region9: TableFunctions.Command_Table_RegionSelectionsToTable_Region(this, 9); break;
				case NECommand.Table_EditTable: TableFunctions.Command_Table_EditTable(this, dialogResult as TableEditTableDialog.Result); break;
				case NECommand.Table_AddHeaders: TableFunctions.Command_Table_AddHeaders(this); break;
				case NECommand.Table_AddRow: TableFunctions.Command_Table_AddRow(this); break;
				case NECommand.Table_AddColumn: TableFunctions.Command_Table_AddColumn(this, dialogResult as TableAddColumnDialog.Result); break;
				case NECommand.Table_Select_RowsByExpression: TableFunctions.Command_Table_Select_RowsByExpression(this, dialogResult as GetExpressionDialog.Result); break;
				case NECommand.Table_SetJoinSource: TableFunctions.Command_Table_SetJoinSource(this); break;
				case NECommand.Table_Join: TableFunctions.Command_Table_Join(this, dialogResult as TableJoinDialog.Result); break;
				case NECommand.Table_Transpose: TableFunctions.Command_Table_Transpose(this); break;
				case NECommand.Table_Database_GenerateInserts: TableFunctions.Command_Table_Database_GenerateInserts(this, dialogResult as TableDatabaseGenerateInsertsDialog.Result); break;
				case NECommand.Table_Database_GenerateUpdates: TableFunctions.Command_Table_Database_GenerateUpdates(this, dialogResult as TableDatabaseGenerateUpdatesDialog.Result); break;
				case NECommand.Table_Database_GenerateDeletes: TableFunctions.Command_Table_Database_GenerateDeletes(this, dialogResult as TableDatabaseGenerateDeletesDialog.Result); break;
			}
		}

		void HandleCommandPosition(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Position_Goto_Lines: PositionFunctions.Command_Position_Goto(this, GotoType.Line, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Columns: PositionFunctions.Command_Position_Goto(this, GotoType.Column, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Indexes: PositionFunctions.Command_Position_Goto(this, GotoType.Index, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Positions: PositionFunctions.Command_Position_Goto(this, GotoType.Position, shiftDown, dialogResult as PositionGotoDialog.Result); break;
				case NECommand.Position_Copy_Lines: PositionFunctions.Command_Position_Copy(this, GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: PositionFunctions.Command_Position_Copy(this, GotoType.Column, !shiftDown); break;
				case NECommand.Position_Copy_Indexes: PositionFunctions.Command_Position_Copy(this, GotoType.Index, !shiftDown); break;
				case NECommand.Position_Copy_Positions: PositionFunctions.Command_Position_Copy(this, GotoType.Position, false); break;
			}
		}

		void HandleCommandContent(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Content_Type_SetFromExtension: ContentFunctions.Command_Content_Type_SetFromExtension(this); break;
				case NECommand.Content_Type_None: ContentFunctions.Command_Content_Type(this, ParserType.None); break;
				case NECommand.Content_Type_Balanced: ContentFunctions.Command_Content_Type(this, ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: ContentFunctions.Command_Content_Type(this, ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: ContentFunctions.Command_Content_Type(this, ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: ContentFunctions.Command_Content_Type(this, ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: ContentFunctions.Command_Content_Type(this, ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: ContentFunctions.Command_Content_Type(this, ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: ContentFunctions.Command_Content_Type(this, ParserType.HTML); break;
				case NECommand.Content_Type_JSON: ContentFunctions.Command_Content_Type(this, ParserType.JSON); break;
				case NECommand.Content_Type_SQL: ContentFunctions.Command_Content_Type(this, ParserType.SQL); break;
				case NECommand.Content_Type_TSV: ContentFunctions.Command_Content_Type(this, ParserType.TSV); break;
				case NECommand.Content_Type_XML: ContentFunctions.Command_Content_Type(this, ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: ContentFunctions.Command_Content_HighlightSyntax(this, multiStatus); break;
				case NECommand.Content_StrictParsing: ContentFunctions.Command_Content_StrictParsing(this, multiStatus); break;
				case NECommand.Content_Reformat: ContentFunctions.Command_Content_Reformat(this); break;
				case NECommand.Content_Comment: ContentFunctions.Command_Content_Comment(this); break;
				case NECommand.Content_Uncomment: ContentFunctions.Command_Content_Uncomment(this); break;
				case NECommand.Content_TogglePosition: ContentFunctions.Command_Content_TogglePosition(this, shiftDown); break;
				case NECommand.Content_Current: ContentFunctions.Command_Content_Current(this); break;
				case NECommand.Content_Parent: ContentFunctions.Command_Content_Parent(this); break;
				case NECommand.Content_Ancestor: ContentFunctions.Command_Content_Ancestor(this, dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Attributes: ContentFunctions.Command_Content_Attributes(this, dialogResult as ContentAttributesDialog.Result); break;
				case NECommand.Content_WithAttribute: ContentFunctions.Command_Content_WithAttribute(this, dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Children_Children: ContentFunctions.Command_Content_Children_Children(this); break;
				case NECommand.Content_Children_SelfAndChildren: ContentFunctions.Command_Content_Children_SelfAndChildren(this); break;
				case NECommand.Content_Children_First: ContentFunctions.Command_Content_Children_First(this); break;
				case NECommand.Content_Children_WithAttribute: ContentFunctions.Command_Content_Children_WithAttribute(this, dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Descendants_Descendants: ContentFunctions.Command_Content_Descendants_Descendants(this); break;
				case NECommand.Content_Descendants_SelfAndDescendants: ContentFunctions.Command_Content_Descendants_SelfAndDescendants(this); break;
				case NECommand.Content_Descendants_First: ContentFunctions.Command_Content_Descendants_First(this); break;
				case NECommand.Content_Descendants_WithAttribute: ContentFunctions.Command_Content_Descendants_WithAttribute(this, dialogResult as ContentAttributeDialog.Result); break;
				case NECommand.Content_Navigate_Up: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Up, shiftDown); break;
				case NECommand.Content_Navigate_Down: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Down, shiftDown); break;
				case NECommand.Content_Navigate_Left: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Left, shiftDown); break;
				case NECommand.Content_Navigate_Right: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Right, shiftDown); break;
				case NECommand.Content_Navigate_Home: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Home, shiftDown); break;
				case NECommand.Content_Navigate_End: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.End, shiftDown); break;
				case NECommand.Content_Navigate_PgUp: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.PgUp, shiftDown); break;
				case NECommand.Content_Navigate_PgDn: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.PgDn, shiftDown); break;
				case NECommand.Content_Navigate_Row: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: ContentFunctions.Command_Content_Navigate(this, ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: ContentFunctions.Command_Content_KeepSelections(this, multiStatus); break;
			}
		}

		void HandleCommandNetwork(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Network_AbsoluteURL: NetworkFunctions.Command_Network_AbsoluteURL(this, dialogResult as NetworkAbsoluteURLDialog.Result); break;
				case NECommand.Network_Fetch: NetworkFunctions.Command_Network_Fetch(this); break;
				case NECommand.Network_FetchHex: NetworkFunctions.Command_Network_Fetch(this, Coder.CodePage.Hex); break;
				case NECommand.Network_FetchFile: NetworkFunctions.Command_Network_FetchFile(this, dialogResult as NetworkFetchFileDialog.Result); break;
				case NECommand.Network_FetchStream: NetworkFunctions.Command_Network_FetchStream(this, dialogResult as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_FetchPlaylist: NetworkFunctions.Command_Network_FetchPlaylist(this, dialogResult as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_Lookup_IP: NetworkFunctions.Command_Network_Lookup_IP(this); break;
				case NECommand.Network_Lookup_HostName: NetworkFunctions.Command_Network_Lookup_HostName(this); break;
				case NECommand.Network_AdaptersInfo: NetworkFunctions.Command_Network_AdaptersInfo(this); break;
				case NECommand.Network_Ping: NetworkFunctions.Command_Network_Ping(this, dialogResult as NetworkPingDialog.Result); break;
				case NECommand.Network_ScanPorts: NetworkFunctions.Command_Network_ScanPorts(this, dialogResult as NetworkScanPortsDialog.Result); break;
			}
		}

		void HandleCommandDatabase(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Database_Connect: DatabaseFunctions.Command_Database_Connect(this, dialogResult as DatabaseConnectDialog.Result); break;
				case NECommand.Database_ExecuteQuery: DatabaseFunctions.Command_Database_ExecuteQuery(this); break;
				case NECommand.Database_GetSproc: DatabaseFunctions.Command_Database_GetSproc(this); break;
			}
		}

		void HandleCommandKeys(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Keys_Set_KeysCaseSensitive: KeysFunctions.Command_Keys_Set(this, 0, true); break;
				case NECommand.Keys_Set_KeysCaseInsensitive: KeysFunctions.Command_Keys_Set(this, 0, false); break;
				case NECommand.Keys_Set_Values1: KeysFunctions.Command_Keys_Set(this, 1); break;
				case NECommand.Keys_Set_Values2: KeysFunctions.Command_Keys_Set(this, 2); break;
				case NECommand.Keys_Set_Values3: KeysFunctions.Command_Keys_Set(this, 3); break;
				case NECommand.Keys_Set_Values4: KeysFunctions.Command_Keys_Set(this, 4); break;
				case NECommand.Keys_Set_Values5: KeysFunctions.Command_Keys_Set(this, 5); break;
				case NECommand.Keys_Set_Values6: KeysFunctions.Command_Keys_Set(this, 6); break;
				case NECommand.Keys_Set_Values7: KeysFunctions.Command_Keys_Set(this, 7); break;
				case NECommand.Keys_Set_Values8: KeysFunctions.Command_Keys_Set(this, 8); break;
				case NECommand.Keys_Set_Values9: KeysFunctions.Command_Keys_Set(this, 9); break;
				case NECommand.Keys_Add_Keys: KeysFunctions.Command_Keys_Add(this, 0); break;
				case NECommand.Keys_Add_Values1: KeysFunctions.Command_Keys_Add(this, 1); break;
				case NECommand.Keys_Add_Values2: KeysFunctions.Command_Keys_Add(this, 2); break;
				case NECommand.Keys_Add_Values3: KeysFunctions.Command_Keys_Add(this, 3); break;
				case NECommand.Keys_Add_Values4: KeysFunctions.Command_Keys_Add(this, 4); break;
				case NECommand.Keys_Add_Values5: KeysFunctions.Command_Keys_Add(this, 5); break;
				case NECommand.Keys_Add_Values6: KeysFunctions.Command_Keys_Add(this, 6); break;
				case NECommand.Keys_Add_Values7: KeysFunctions.Command_Keys_Add(this, 7); break;
				case NECommand.Keys_Add_Values8: KeysFunctions.Command_Keys_Add(this, 8); break;
				case NECommand.Keys_Add_Values9: KeysFunctions.Command_Keys_Add(this, 9); break;
				case NECommand.Keys_Remove_Keys: KeysFunctions.Command_Keys_Remove(this, 0); break;
				case NECommand.Keys_Remove_Values1: KeysFunctions.Command_Keys_Remove(this, 1); break;
				case NECommand.Keys_Remove_Values2: KeysFunctions.Command_Keys_Remove(this, 2); break;
				case NECommand.Keys_Remove_Values3: KeysFunctions.Command_Keys_Remove(this, 3); break;
				case NECommand.Keys_Remove_Values4: KeysFunctions.Command_Keys_Remove(this, 4); break;
				case NECommand.Keys_Remove_Values5: KeysFunctions.Command_Keys_Remove(this, 5); break;
				case NECommand.Keys_Remove_Values6: KeysFunctions.Command_Keys_Remove(this, 6); break;
				case NECommand.Keys_Remove_Values7: KeysFunctions.Command_Keys_Remove(this, 7); break;
				case NECommand.Keys_Remove_Values8: KeysFunctions.Command_Keys_Remove(this, 8); break;
				case NECommand.Keys_Remove_Values9: KeysFunctions.Command_Keys_Remove(this, 9); break;
				case NECommand.Keys_Replace_Values1: KeysFunctions.Command_Keys_Replace(this, 1); break;
				case NECommand.Keys_Replace_Values2: KeysFunctions.Command_Keys_Replace(this, 2); break;
				case NECommand.Keys_Replace_Values3: KeysFunctions.Command_Keys_Replace(this, 3); break;
				case NECommand.Keys_Replace_Values4: KeysFunctions.Command_Keys_Replace(this, 4); break;
				case NECommand.Keys_Replace_Values5: KeysFunctions.Command_Keys_Replace(this, 5); break;
				case NECommand.Keys_Replace_Values6: KeysFunctions.Command_Keys_Replace(this, 6); break;
				case NECommand.Keys_Replace_Values7: KeysFunctions.Command_Keys_Replace(this, 7); break;
				case NECommand.Keys_Replace_Values8: KeysFunctions.Command_Keys_Replace(this, 8); break;
				case NECommand.Keys_Replace_Values9: KeysFunctions.Command_Keys_Replace(this, 9); break;
			}
		}

		void HandleCommandSelect(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Select_All: SelectFunctions.Command_Select_All(this); break;
				case NECommand.Select_Nothing: SelectFunctions.Command_Select_Nothing(this); break;
				case NECommand.Select_Limit: SelectFunctions.Command_Select_Limit(this, dialogResult as SelectLimitDialog.Result); break;
				case NECommand.Select_Lines: SelectFunctions.Command_Select_Lines(this, false); break;
				case NECommand.Select_WholeLines: SelectFunctions.Command_Select_Lines(this, true); break;
				case NECommand.Select_Rectangle: SelectFunctions.Command_Select_Rectangle(this); break;
				case NECommand.Select_Invert: SelectFunctions.Command_Select_Invert(this); break;
				case NECommand.Select_Join: SelectFunctions.Command_Select_Join(this); break;
				case NECommand.Select_Empty: SelectFunctions.Command_Select_Empty(this, true); break;
				case NECommand.Select_NonEmpty: SelectFunctions.Command_Select_Empty(this, false); break;
				case NECommand.Select_ToggleOpenClose: SelectFunctions.Command_Select_ToggleOpenClose(this, shiftDown); break;
				case NECommand.Select_Repeats_Unique: SelectFunctions.Command_Select_Repeats_Unique(this, true); break;
				case NECommand.Select_Repeats_Duplicates: SelectFunctions.Command_Select_Repeats_Duplicates(this, true); break;
				case NECommand.Select_Repeats_MatchPrevious: SelectFunctions.Command_Select_Repeats_MatchPrevious(this, true); break;
				case NECommand.Select_Repeats_NonMatchPrevious: SelectFunctions.Command_Select_Repeats_NonMatchPrevious(this, true); break;
				case NECommand.Select_Repeats_RepeatedLines: SelectFunctions.Command_Select_Repeats_RepeatedLines(this); break;
				case NECommand.Select_Repeats_ByCount: SelectFunctions.Command_Select_Repeats_ByCount(this, dialogResult as SelectByCountDialog.Result); break;
				case NECommand.Select_Repeats_CaseInsensitive_Unique: SelectFunctions.Command_Select_Repeats_Unique(this, false); break;
				case NECommand.Select_Repeats_CaseInsensitive_Duplicates: SelectFunctions.Command_Select_Repeats_Duplicates(this, false); break;
				case NECommand.Select_Repeats_CaseInsensitive_MatchPrevious: SelectFunctions.Command_Select_Repeats_MatchPrevious(this, false); break;
				case NECommand.Select_Repeats_CaseInsensitive_NonMatchPrevious: SelectFunctions.Command_Select_Repeats_NonMatchPrevious(this, false); break;
				case NECommand.Select_Split: SelectFunctions.Command_Select_Split(this, dialogResult as SelectSplitDialog.Result); break;
				case NECommand.Select_Selection_First: SelectFunctions.Command_Select_Selection_First(this); break;
				case NECommand.Select_Selection_CenterVertically: SelectFunctions.Command_Select_Selection_CenterVertically(this); break;
				case NECommand.Select_Selection_Center: SelectFunctions.Command_Select_Selection_Center(this); break;
				case NECommand.Select_Selection_ToggleAnchor: SelectFunctions.Command_Select_Selection_ToggleAnchor(this); break;
				case NECommand.Select_Selection_Next: SelectFunctions.Command_Select_Selection_NextPrevious(this, true); break;
				case NECommand.Select_Selection_Previous: SelectFunctions.Command_Select_Selection_NextPrevious(this, false); break;
				case NECommand.Select_Selection_Single: SelectFunctions.Command_Select_Selection_Single(this); break;
				case NECommand.Select_Selection_Remove: SelectFunctions.Command_Select_Selection_Remove(this); break;
				case NECommand.Select_Selection_RemoveBeforeCurrent: SelectFunctions.Command_Select_Selection_RemoveBeforeCurrent(this); break;
				case NECommand.Select_Selection_RemoveAfterCurrent: SelectFunctions.Command_Select_Selection_RemoveAfterCurrent(this); break;
			}
		}

		void HandleCommandRegion(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Region_ModifyRegions: RegionFunctions.Command_Region_ModifyRegions(this, dialogResult as RegionModifyRegionsDialog.Result); break;
				case NECommand.Region_SetSelections_Region1: RegionFunctions.Command_Region_SetSelections_Region(this, 1); break;
				case NECommand.Region_SetSelections_Region2: RegionFunctions.Command_Region_SetSelections_Region(this, 2); break;
				case NECommand.Region_SetSelections_Region3: RegionFunctions.Command_Region_SetSelections_Region(this, 3); break;
				case NECommand.Region_SetSelections_Region4: RegionFunctions.Command_Region_SetSelections_Region(this, 4); break;
				case NECommand.Region_SetSelections_Region5: RegionFunctions.Command_Region_SetSelections_Region(this, 5); break;
				case NECommand.Region_SetSelections_Region6: RegionFunctions.Command_Region_SetSelections_Region(this, 6); break;
				case NECommand.Region_SetSelections_Region7: RegionFunctions.Command_Region_SetSelections_Region(this, 7); break;
				case NECommand.Region_SetSelections_Region8: RegionFunctions.Command_Region_SetSelections_Region(this, 8); break;
				case NECommand.Region_SetSelections_Region9: RegionFunctions.Command_Region_SetSelections_Region(this, 9); break;
				case NECommand.Region_SetSelections_All: RegionFunctions.Command_Region_SetSelections_Region(this); break;
				case NECommand.Region_AddSelections_Region1: RegionFunctions.Command_Region_AddSelections_Region(this, 1); break;
				case NECommand.Region_AddSelections_Region2: RegionFunctions.Command_Region_AddSelections_Region(this, 2); break;
				case NECommand.Region_AddSelections_Region3: RegionFunctions.Command_Region_AddSelections_Region(this, 3); break;
				case NECommand.Region_AddSelections_Region4: RegionFunctions.Command_Region_AddSelections_Region(this, 4); break;
				case NECommand.Region_AddSelections_Region5: RegionFunctions.Command_Region_AddSelections_Region(this, 5); break;
				case NECommand.Region_AddSelections_Region6: RegionFunctions.Command_Region_AddSelections_Region(this, 6); break;
				case NECommand.Region_AddSelections_Region7: RegionFunctions.Command_Region_AddSelections_Region(this, 7); break;
				case NECommand.Region_AddSelections_Region8: RegionFunctions.Command_Region_AddSelections_Region(this, 8); break;
				case NECommand.Region_AddSelections_Region9: RegionFunctions.Command_Region_AddSelections_Region(this, 9); break;
				case NECommand.Region_AddSelections_All: RegionFunctions.Command_Region_AddSelections_Region(this); break;
				case NECommand.Region_RemoveSelections_Region1: RegionFunctions.Command_Region_RemoveSelections_Region(this, 1); break;
				case NECommand.Region_RemoveSelections_Region2: RegionFunctions.Command_Region_RemoveSelections_Region(this, 2); break;
				case NECommand.Region_RemoveSelections_Region3: RegionFunctions.Command_Region_RemoveSelections_Region(this, 3); break;
				case NECommand.Region_RemoveSelections_Region4: RegionFunctions.Command_Region_RemoveSelections_Region(this, 4); break;
				case NECommand.Region_RemoveSelections_Region5: RegionFunctions.Command_Region_RemoveSelections_Region(this, 5); break;
				case NECommand.Region_RemoveSelections_Region6: RegionFunctions.Command_Region_RemoveSelections_Region(this, 6); break;
				case NECommand.Region_RemoveSelections_Region7: RegionFunctions.Command_Region_RemoveSelections_Region(this, 7); break;
				case NECommand.Region_RemoveSelections_Region8: RegionFunctions.Command_Region_RemoveSelections_Region(this, 8); break;
				case NECommand.Region_RemoveSelections_Region9: RegionFunctions.Command_Region_RemoveSelections_Region(this, 9); break;
				case NECommand.Region_RemoveSelections_All: RegionFunctions.Command_Region_RemoveSelections_Region(this); break;
				case NECommand.Region_ReplaceSelections_Region1: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 1); break;
				case NECommand.Region_ReplaceSelections_Region2: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 2); break;
				case NECommand.Region_ReplaceSelections_Region3: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 3); break;
				case NECommand.Region_ReplaceSelections_Region4: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 4); break;
				case NECommand.Region_ReplaceSelections_Region5: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 5); break;
				case NECommand.Region_ReplaceSelections_Region6: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 6); break;
				case NECommand.Region_ReplaceSelections_Region7: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 7); break;
				case NECommand.Region_ReplaceSelections_Region8: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 8); break;
				case NECommand.Region_ReplaceSelections_Region9: RegionFunctions.Command_Region_ReplaceSelections_Region(this, 9); break;
				case NECommand.Region_ReplaceSelections_All: RegionFunctions.Command_Region_ReplaceSelections_Region(this); break;
				case NECommand.Region_LimitToSelections_Region1: RegionFunctions.Command_Region_LimitToSelections_Region(this, 1); break;
				case NECommand.Region_LimitToSelections_Region2: RegionFunctions.Command_Region_LimitToSelections_Region(this, 2); break;
				case NECommand.Region_LimitToSelections_Region3: RegionFunctions.Command_Region_LimitToSelections_Region(this, 3); break;
				case NECommand.Region_LimitToSelections_Region4: RegionFunctions.Command_Region_LimitToSelections_Region(this, 4); break;
				case NECommand.Region_LimitToSelections_Region5: RegionFunctions.Command_Region_LimitToSelections_Region(this, 5); break;
				case NECommand.Region_LimitToSelections_Region6: RegionFunctions.Command_Region_LimitToSelections_Region(this, 6); break;
				case NECommand.Region_LimitToSelections_Region7: RegionFunctions.Command_Region_LimitToSelections_Region(this, 7); break;
				case NECommand.Region_LimitToSelections_Region8: RegionFunctions.Command_Region_LimitToSelections_Region(this, 8); break;
				case NECommand.Region_LimitToSelections_Region9: RegionFunctions.Command_Region_LimitToSelections_Region(this, 9); break;
				case NECommand.Region_LimitToSelections_All: RegionFunctions.Command_Region_LimitToSelections_Region(this); break;
				case NECommand.Region_Clear_Region1: RegionFunctions.Command_Region_Clear_Region(this, 1); break;
				case NECommand.Region_Clear_Region2: RegionFunctions.Command_Region_Clear_Region(this, 2); break;
				case NECommand.Region_Clear_Region3: RegionFunctions.Command_Region_Clear_Region(this, 3); break;
				case NECommand.Region_Clear_Region4: RegionFunctions.Command_Region_Clear_Region(this, 4); break;
				case NECommand.Region_Clear_Region5: RegionFunctions.Command_Region_Clear_Region(this, 5); break;
				case NECommand.Region_Clear_Region6: RegionFunctions.Command_Region_Clear_Region(this, 6); break;
				case NECommand.Region_Clear_Region7: RegionFunctions.Command_Region_Clear_Region(this, 7); break;
				case NECommand.Region_Clear_Region8: RegionFunctions.Command_Region_Clear_Region(this, 8); break;
				case NECommand.Region_Clear_Region9: RegionFunctions.Command_Region_Clear_Region(this, 9); break;
				case NECommand.Region_Clear_All: RegionFunctions.Command_Region_Clear_Region(this); break;
				case NECommand.Region_RepeatBySelections_Region1: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 1); break;
				case NECommand.Region_RepeatBySelections_Region2: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 2); break;
				case NECommand.Region_RepeatBySelections_Region3: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 3); break;
				case NECommand.Region_RepeatBySelections_Region4: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 4); break;
				case NECommand.Region_RepeatBySelections_Region5: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 5); break;
				case NECommand.Region_RepeatBySelections_Region6: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 6); break;
				case NECommand.Region_RepeatBySelections_Region7: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 7); break;
				case NECommand.Region_RepeatBySelections_Region8: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 8); break;
				case NECommand.Region_RepeatBySelections_Region9: RegionFunctions.Command_Region_RepeatBySelections_Region(this, 9); break;
				case NECommand.Region_CopyEnclosingRegion_Region1: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 1); break;
				case NECommand.Region_CopyEnclosingRegion_Region2: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 2); break;
				case NECommand.Region_CopyEnclosingRegion_Region3: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 3); break;
				case NECommand.Region_CopyEnclosingRegion_Region4: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 4); break;
				case NECommand.Region_CopyEnclosingRegion_Region5: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 5); break;
				case NECommand.Region_CopyEnclosingRegion_Region6: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 6); break;
				case NECommand.Region_CopyEnclosingRegion_Region7: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 7); break;
				case NECommand.Region_CopyEnclosingRegion_Region8: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 8); break;
				case NECommand.Region_CopyEnclosingRegion_Region9: RegionFunctions.Command_Region_CopyEnclosingRegion_Region(this, 9); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region1: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 1); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region2: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 2); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region3: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 3); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region4: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 4); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region5: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 5); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region6: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 6); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region7: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 7); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region8: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 8); break;
				case NECommand.Region_CopyEnclosingRegionIndex_Region9: RegionFunctions.Command_Region_CopyEnclosingRegionIndex_Region(this, 9); break;
				case NECommand.Region_TransformSelections_Flatten_Region1: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 1); break;
				case NECommand.Region_TransformSelections_Flatten_Region2: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 2); break;
				case NECommand.Region_TransformSelections_Flatten_Region3: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 3); break;
				case NECommand.Region_TransformSelections_Flatten_Region4: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 4); break;
				case NECommand.Region_TransformSelections_Flatten_Region5: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 5); break;
				case NECommand.Region_TransformSelections_Flatten_Region6: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 6); break;
				case NECommand.Region_TransformSelections_Flatten_Region7: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 7); break;
				case NECommand.Region_TransformSelections_Flatten_Region8: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 8); break;
				case NECommand.Region_TransformSelections_Flatten_Region9: RegionFunctions.Command_Region_TransformSelections_Flatten_Region(this, 9); break;
				case NECommand.Region_TransformSelections_Transpose_Region1: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 1); break;
				case NECommand.Region_TransformSelections_Transpose_Region2: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 2); break;
				case NECommand.Region_TransformSelections_Transpose_Region3: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 3); break;
				case NECommand.Region_TransformSelections_Transpose_Region4: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 4); break;
				case NECommand.Region_TransformSelections_Transpose_Region5: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 5); break;
				case NECommand.Region_TransformSelections_Transpose_Region6: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 6); break;
				case NECommand.Region_TransformSelections_Transpose_Region7: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 7); break;
				case NECommand.Region_TransformSelections_Transpose_Region8: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 8); break;
				case NECommand.Region_TransformSelections_Transpose_Region9: RegionFunctions.Command_Region_TransformSelections_Transpose_Region(this, 9); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region1: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 1); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region2: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 2); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region3: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 3); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region4: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 4); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region5: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 5); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region6: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 6); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region7: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 7); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region8: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 8); break;
				case NECommand.Region_TransformSelections_RotateLeft_Region9: RegionFunctions.Command_Region_TransformSelections_RotateLeft_Region(this, 9); break;
				case NECommand.Region_TransformSelections_RotateRight_Region1: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 1); break;
				case NECommand.Region_TransformSelections_RotateRight_Region2: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 2); break;
				case NECommand.Region_TransformSelections_RotateRight_Region3: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 3); break;
				case NECommand.Region_TransformSelections_RotateRight_Region4: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 4); break;
				case NECommand.Region_TransformSelections_RotateRight_Region5: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 5); break;
				case NECommand.Region_TransformSelections_RotateRight_Region6: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 6); break;
				case NECommand.Region_TransformSelections_RotateRight_Region7: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 7); break;
				case NECommand.Region_TransformSelections_RotateRight_Region8: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 8); break;
				case NECommand.Region_TransformSelections_RotateRight_Region9: RegionFunctions.Command_Region_TransformSelections_RotateRight_Region(this, 9); break;
				case NECommand.Region_TransformSelections_Rotate180_Region1: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 1); break;
				case NECommand.Region_TransformSelections_Rotate180_Region2: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 2); break;
				case NECommand.Region_TransformSelections_Rotate180_Region3: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 3); break;
				case NECommand.Region_TransformSelections_Rotate180_Region4: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 4); break;
				case NECommand.Region_TransformSelections_Rotate180_Region5: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 5); break;
				case NECommand.Region_TransformSelections_Rotate180_Region6: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 6); break;
				case NECommand.Region_TransformSelections_Rotate180_Region7: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 7); break;
				case NECommand.Region_TransformSelections_Rotate180_Region8: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 8); break;
				case NECommand.Region_TransformSelections_Rotate180_Region9: RegionFunctions.Command_Region_TransformSelections_Rotate180_Region(this, 9); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region1: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 1); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region2: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 2); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region3: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 3); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region4: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 4); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region5: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 5); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region6: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 6); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region7: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 7); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region8: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 8); break;
				case NECommand.Region_TransformSelections_MirrorHorizontal_Region9: RegionFunctions.Command_Region_TransformSelections_MirrorHorizontal_Region(this, 9); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region1: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 1); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region2: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 2); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region3: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 3); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region4: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 4); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region5: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 5); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region6: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 6); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region7: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 7); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region8: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 8); break;
				case NECommand.Region_TransformSelections_MirrorVertical_Region9: RegionFunctions.Command_Region_TransformSelections_MirrorVertical_Region(this, 9); break;
				case NECommand.Region_Select_Regions_Region1: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 1); break;
				case NECommand.Region_Select_Regions_Region2: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 2); break;
				case NECommand.Region_Select_Regions_Region3: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 3); break;
				case NECommand.Region_Select_Regions_Region4: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 4); break;
				case NECommand.Region_Select_Regions_Region5: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 5); break;
				case NECommand.Region_Select_Regions_Region6: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 6); break;
				case NECommand.Region_Select_Regions_Region7: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 7); break;
				case NECommand.Region_Select_Regions_Region8: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 8); break;
				case NECommand.Region_Select_Regions_Region9: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown, 9); break;
				case NECommand.Region_Select_Regions_All: RegionFunctions.Command_Region_Select_Regions_Region(this, shiftDown); break;
				case NECommand.Region_Select_EnclosingRegion_Region1: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 1); break;
				case NECommand.Region_Select_EnclosingRegion_Region2: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 2); break;
				case NECommand.Region_Select_EnclosingRegion_Region3: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 3); break;
				case NECommand.Region_Select_EnclosingRegion_Region4: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 4); break;
				case NECommand.Region_Select_EnclosingRegion_Region5: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 5); break;
				case NECommand.Region_Select_EnclosingRegion_Region6: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 6); break;
				case NECommand.Region_Select_EnclosingRegion_Region7: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 7); break;
				case NECommand.Region_Select_EnclosingRegion_Region8: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 8); break;
				case NECommand.Region_Select_EnclosingRegion_Region9: RegionFunctions.Command_Region_Select_EnclosingRegion_Region(this, 9); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region1: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 1); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region2: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 2); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region3: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 3); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region4: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 4); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region5: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 5); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region6: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 6); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region7: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 7); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region8: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 8); break;
				case NECommand.Region_Select_WithEnclosingRegion_Region9: RegionFunctions.Command_Region_Select_WithEnclosingRegion_Region(this, 9); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region1: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 1); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region2: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 2); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region3: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 3); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region4: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 4); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region5: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 5); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region6: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 6); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region7: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 7); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region8: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 8); break;
				case NECommand.Region_Select_WithoutEnclosingRegion_Region9: RegionFunctions.Command_Region_Select_WithoutEnclosingRegion_Region(this, 9); break;
			}
		}

		void HandleCommandMacro(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Macro_RepeatLastAction: Command_Macro_RepeatLastAction(); break;
				case NECommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}
		}

		void HandleCommandWindow(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus, AnswerResult answer)
		{
			switch (command)
			{
				case NECommand.Window_TabIndex: WindowFunctions.Command_Window_TabIndex(this, false); break;
				case NECommand.Window_ActiveTabIndex: WindowFunctions.Command_Window_TabIndex(this, true); break;
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
					if (Settings.EscapeClearsSelections)
					{
						HandleCommand(NECommand.Select_Selection_Single, false, null, null, null);
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
			var line = Math.Max(0, Math.Min(Data.NumLines - 1, (int)(mousePos.Y / Font.FontSize) + yScrollValue));
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

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0) || (!canvas.IsVisible))
				return;

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

			var brushes = new List<Tuple<RangeList, Brush>>
			{
				Tuple.Create(Selections, selectionBrush),
				Tuple.Create(Searches, searchBrush),
			};
			brushes.AddRange(Regions.Select(pair => Tuple.Create(pair.Value, regionBrush[pair.Key])));
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
							dc.DrawRectangle(visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.FontSize));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(cursorBrush, cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.FontSize));
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

			var highlightDictionary = HighlightSyntax ? Highlight.Get(ContentType)?.GetDictionary() : null;

			for (var line = startLine; line < endLine; ++line)
			{
				if (!Data.GetLineDiffMatches(line))
				{
					dc.DrawRectangle(diffMinorBrush, null, new Rect(0, y[line], canvas.ActualWidth, Font.FontSize));

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
							dc.DrawRectangle(diffMajorBrush, null, new Rect(startX, y[line], endX - startX, Font.FontSize));
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

		void OnStatusBarRender(object sender, DrawingContext dc)
		{
			var sb = new List<string>();

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

				sb.Add($"Selection {CurrentSelection + 1:n0}/{NumSelections:n0}");
				sb.Add($"Col {lineMin + 1:n0}:{columnMin + 1:n0}{((lineMin == lineMax) && (columnMin == columnMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{columnMax + 1:n0}")}");
				sb.Add($"In {lineMin + 1:n0}:{indexMin + 1:n0}{((lineMin == lineMax) && (indexMin == indexMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{indexMax + 1:n0}")}");
				sb.Add($"Pos {posMin:n0}{(posMin == posMax ? "" : $"-{posMax:n0}")}");
			}

			sb.Add($"Regions {string.Join(" / ", Regions.ToDictionary(pair => pair.Key, pair => pair.Value.Count).OrderBy(pair => pair.Key).Select(pair => $"{pair.Value:n0}"))}");
			sb.Add($"Keys/Values {string.Join(" / ", KeysAndValues.Select(l => $"{l.Count:n0}"))}");
			sb.Add($"Database {DBName}");

			var tf = SystemFonts.MessageFontFamily.GetTypefaces().Where(x => (x.Weight == FontWeights.Normal) && (x.Style == FontStyles.Normal)).First();
			dc.DrawText(new FormattedText(string.Join(" │ ", sb), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, tf, SystemFonts.MessageFontSize, Brushes.Black), new Point(2, 2));
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

			FileSaver.HandleDecrypt(WindowParent, ref bytes, out var aesKey);
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

			var translateMap = RangeList.GetTranslateMap(ranges, strs, new RangeList[] { Selections, Searches, Bookmarks }.Concat(Regions.Values).ToArray());
			SetSelections(Selections.Translate(translateMap));
			Regions.Keys.ToList().ForEach(key => SetRegions(key, Regions[key].Translate(translateMap)));
			var searchLens = Searches.Select(range => range.Length).ToList();
			SetSearches(Searches.Translate(translateMap));
			SetSearches(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());
			SetBookmarks(Bookmarks.Translate(translateMap));

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

		public void Save(string fileName, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Data.NumChars >> 20) < 50) && (!VerifyCanEncode()))
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

					if (new Message(WindowParent)
					{
						Title = "Confirm",
						Text = "Save failed. Remove read-only flag?",
						Options = MessageOptions.YesNo,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show() != MessageOptions.Yes)
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
			FileFunctions.Command_File_Refresh(this, answer);
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
			if (!strs.Any())
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => !drives.Any(drive => str.StartsWith(drive, StringComparison.OrdinalIgnoreCase))))
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

			switch (new Message(WindowParent)
			{
				Title = "Confirm",
				Text = "The current encoding cannot fully represent this data. Switch to UTF-8?",
				Options = MessageOptions.YesNoCancel,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.Cancel,
			}.Show())
			{
				case MessageOptions.Yes: CodePage = Coder.CodePage.UTF8; return true;
				case MessageOptions.No: return true;
				case MessageOptions.Cancel: return false;
				default: throw new Exception("Invalid response");
			}
		}

		void Command_Macro_RepeatLastAction()
		{
			if (previous == null)
				return;
			HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult, previous.MultiStatus, new AnswerResult());
		}

		public void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			var textEditor = TabsParent.Add(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			textEditor.ContentType = contentType;
			textEditor.DisplayName = name;
		}

		delegate void GlobalKeysChangedDelegate();

		static GlobalKeysChangedDelegate globalKeysChanged;
		static bool globalKeys = true;
		public bool GlobalKeys { get { return globalKeys; } set { globalKeys = value; globalKeysChanged?.Invoke(); } }

		ObservableCollection<ObservableCollection<string>> _keysAndValues;
		public ObservableCollection<ObservableCollection<string>> KeysAndValues
		{
			get => _keysAndValues;
			set
			{
				RemoveKeysAndValuesCallback();
				_keysAndValues = value;
				SetKeysAndValuesCallback();
			}
		}

		void SetKeysAndValuesCallback()
		{
			if (KeysAndValues == null)
				return;
			KeysAndValues.CollectionChanged += KeysAndValuesChanged;
			foreach (var coll in KeysAndValues)
				coll.CollectionChanged += KeysAndValuesChanged;
		}

		void RemoveKeysAndValuesCallback()
		{
			if (KeysAndValues == null)
				return;
			foreach (var coll in KeysAndValues)
				coll.CollectionChanged -= KeysAndValuesChanged;
			KeysAndValues.CollectionChanged -= KeysAndValuesChanged;
		}

		void KeysAndValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RemoveKeysAndValuesCallback();
			SetKeysAndValuesCallback();
			statusBarRenderTimer.Start();
		}

		static ObservableCollection<ObservableCollection<string>> staticKeysAndValues { get; set; }
		ObservableCollection<ObservableCollection<string>> localKeysAndValues { get; set; }
		public Dictionary<string, int> keysHash => GlobalKeys ? staticKeysHash : localKeysHash;
		static Dictionary<string, int> staticKeysHash = new Dictionary<string, int>();
		Dictionary<string, int> localKeysHash = new Dictionary<string, int>();

		void SetupLocalKeys()
		{
			localKeysAndValues = new ObservableCollection<ObservableCollection<string>>(Enumerable.Repeat(default(ObservableCollection<string>), 10));
			for (var ctr = 0; ctr < localKeysAndValues.Count; ++ctr)
				localKeysAndValues[ctr] = new ObservableCollection<string>();

			SetupLocalOrGlobalKeys();
			globalKeysChanged += SetupLocalOrGlobalKeys;
		}

		void SetupLocalOrGlobalKeys() => KeysAndValues = GlobalKeys ? staticKeysAndValues : localKeysAndValues;

		static void SetupStaticKeys()
		{
			staticKeysAndValues = new ObservableCollection<ObservableCollection<string>>(Enumerable.Repeat(default(ObservableCollection<string>), 10));
			for (var ctr = 0; ctr < staticKeysAndValues.Count; ++ctr)
				staticKeysAndValues[ctr] = new ObservableCollection<string>();
		}

		public void CalculateKeysHash(bool caseSensitive)
		{
			var hash = new Dictionary<string, int>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			for (var pos = 0; pos < KeysAndValues[0].Count; ++pos)
				hash[KeysAndValues[0][pos]] = pos;
			if (GlobalKeys)
				staticKeysHash = hash;
			else
				localKeysHash = hash;
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
	}
}
