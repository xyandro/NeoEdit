using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEditor.Dialogs;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorWindow
	{
		public static RoutedCommand Command_File_New = new RoutedCommand();
		public static RoutedCommand Command_File_Open = new RoutedCommand();
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_File_SaveAs = new RoutedCommand();
		public static RoutedCommand Command_File_Exit = new RoutedCommand();
		public static RoutedCommand Command_Edit_Undo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Redo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Cut = new RoutedCommand();
		public static RoutedCommand Command_Edit_Copy = new RoutedCommand();
		public static RoutedCommand Command_Edit_Paste = new RoutedCommand();
		public static RoutedCommand Command_Edit_ShowClipboard = new RoutedCommand();
		public static RoutedCommand Command_Edit_CopyName = new RoutedCommand();
		public static RoutedCommand Command_Edit_CopyPath = new RoutedCommand();
		public static RoutedCommand Command_Edit_Find = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindNext = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindPrev = new RoutedCommand();
		public static RoutedCommand Command_Edit_GotoLine = new RoutedCommand();
		public static RoutedCommand Command_Edit_GotoIndex = new RoutedCommand();
		public static RoutedCommand Command_Edit_BOM = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_Upper = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_Lower = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_Proper = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_Toggle = new RoutedCommand();
		public static RoutedCommand Command_Data_Hex_ToHex = new RoutedCommand();
		public static RoutedCommand Command_Data_Hex_FromHex = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_ToChar = new RoutedCommand();
		public static RoutedCommand Command_Data_Char_FromChar = new RoutedCommand();
		public static RoutedCommand Command_Data_Length = new RoutedCommand();
		public static RoutedCommand Command_Data_Width = new RoutedCommand();
		public static RoutedCommand Command_Data_Trim = new RoutedCommand();
		public static RoutedCommand Command_Data_Evaluate = new RoutedCommand();
		public static RoutedCommand Command_Data_Series = new RoutedCommand();
		public static RoutedCommand Command_Data_Repeat = new RoutedCommand();
		public static RoutedCommand Command_Data_Unique = new RoutedCommand();
		public static RoutedCommand Command_Data_Duplicates = new RoutedCommand();
		public static RoutedCommand Command_Data_MD5 = new RoutedCommand();
		public static RoutedCommand Command_Data_SHA1 = new RoutedCommand();
		public static RoutedCommand Command_SelectMark_Toggle = new RoutedCommand();
		public static RoutedCommand Command_Select_All = new RoutedCommand();
		public static RoutedCommand Command_Select_Unselect = new RoutedCommand();
		public static RoutedCommand Command_Select_Single = new RoutedCommand();
		public static RoutedCommand Command_Select_Lines = new RoutedCommand();
		public static RoutedCommand Command_Select_Marks = new RoutedCommand();
		public static RoutedCommand Command_Select_Find = new RoutedCommand();
		public static RoutedCommand Command_Select_RemoveEmpty = new RoutedCommand();
		public static RoutedCommand Command_Mark_Selection = new RoutedCommand();
		public static RoutedCommand Command_Mark_Find = new RoutedCommand();
		public static RoutedCommand Command_Mark_Clear = new RoutedCommand();
		public static RoutedCommand Command_Mark_LimitToSelection = new RoutedCommand();

		TextData _data = new TextData();
		TextData Data
		{
			get { return _data; }
			set
			{
				_data = value;
				Selections.Clear();
				Marks.Clear();
				Searches.Clear();
				undo.Clear();
				redo.Clear();
				InvalidateVisual();
			}
		}

		[DepProp]
		string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Column { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSelections { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollValue { get { return uiHelper.GetPropValue<int>(); } set { value = (int)Math.Max(xScroll.Minimum, Math.Min(value, xScroll.Maximum)); uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { value = (int)Math.Max(yScroll.Minimum, Math.Min(value, yScroll.Maximum)); uiHelper.SetPropValue(value); } }

		readonly RangeList Selections = new RangeList();
		readonly RangeList Searches = new RangeList();
		readonly RangeList Marks = new RangeList();

		static TextEditorWindow() { UIHelper<TextEditorWindow>.Register(); }

		readonly UIHelper<TextEditorWindow> uiHelper;
		public TextEditorWindow(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int? line = null, int? column = null)
		{
			uiHelper = new UIHelper<TextEditorWindow>(this);
			InitializeComponent();

			canvas.Initialize(OnCanvasRender);
			Selections.CollectionChanged += () => InvalidateSelections();
			Searches.CollectionChanged += () => InvalidateSearches();
			Marks.CollectionChanged += () => InvalidateMarks();

			OpenFile(filename, bytes, encoding);

			if (!line.HasValue)
				line = column = 1;
			if (!column.HasValue)
				column = 1;
			Selections.Add(new Range(Data.GetOffset(line.Value - 1, column.Value - 1)));
			CoderUsed = Data.CoderUsed;

			KeyDown += (s, e) => uiHelper.RaiseEvent(canvas, e);
			MouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);
			yScroll.MouseWheel += (s, e) => (s as ScrollBar).Value -= e.Delta;

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.HighlightType, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => InvalidateVisual());

			Loaded += (s, e) =>
			{
				Line = 0;
				InvalidateVisual();
			};
		}

		void OpenFile(string filename, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			FileName = filename;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}
			Data = new TextData(bytes, encoding);
			HighlightType = Highlighting.Get(FileName);
		}

		protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			uiHelper.RaiseEvent(canvas, e);
		}

		void OnCanvasTextInput(object sender, TextCompositionEventArgs e)
		{
			AddCanvasText(e.Text);
			e.Handled = true;
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			RunCommand(e.Command);
		}

		int BeginOffset()
		{
			return Data.GetOffset(0, 0);
		}

		int EndOffset()
		{
			return Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		}

		void RunCommand(ICommand command)
		{
			InvalidateVisual();

			if (command == Command_File_New)
			{
				FileName = null;
				Data = new TextData();
			}
			else if (command == Command_File_Open)
			{
				var dialog = new OpenFileDialog();
				if (dialog.ShowDialog() == true)
					OpenFile(dialog.FileName);
			}
			else if (command == Command_File_Save)
			{
				if (FileName == null)
					RunCommand(Command_File_SaveAs);
				else
					File.WriteAllBytes(FileName, Data.GetBytes(Data.CoderUsed));
			}
			else if (command == Command_File_SaveAs)
			{
				var dialog = new SaveFileDialog();
				if (dialog.ShowDialog() == true)
				{
					if (Directory.Exists(dialog.FileName))
						throw new Exception("A directory by that name already exists.");
					if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
						throw new Exception("Directory doesn't exist.");
					FileName = dialog.FileName;
					RunCommand(Command_File_Save);
				}
			}
			else if (command == Command_File_Exit)
				Close();
			else if (command == Command_Edit_Undo)
			{
				if (undo.Count == 0)
					return;

				var undoStep = undo.Last();
				undo.Remove(undoStep);
				Replace(undoStep.ranges, undoStep.text, true, ReplaceType.Undo);
			}
			else if (command == Command_Edit_Redo)
			{
				if (redo.Count == 0)
					return;

				var redoStep = redo.Last();
				redo.Remove(redoStep);
				Replace(redoStep.ranges, redoStep.text, true, ReplaceType.Redo);
			}
			else if ((command == Command_Edit_Cut) || (command == Command_Edit_Copy))
			{
				var result = Selections.Select(range => GetString(range)).ToArray();
				if (result.Length != 0)
					ClipboardWindow.Set(result);
				if (command == Command_Edit_Cut)
					Replace(Selections, null, false);
			}
			else if (command == Command_Edit_Paste)
			{
				var result = ClipboardWindow.GetStrings().ToList();
				if ((result == null) || (result.Count == 0))
					return;

				var separator = Selections.Count == 1 ? Data.DefaultEnding : " ";
				while (result.Count > Selections.Count)
				{
					result[result.Count - 2] += separator + result[result.Count - 1];
					result.RemoveAt(result.Count - 1);
				}
				while (result.Count < Selections.Count)
					result.Add(result.Last());

				Replace(Selections, result, false);
			}
			else if (command == Command_Edit_ShowClipboard)
				ClipboardWindow.Show();
			else if (command == Command_Edit_CopyName)
				Clipboard.SetText(Path.GetFileName(FileName));
			else if (command == Command_Edit_CopyPath)
				Clipboard.SetText(FileName);
			else if (command == Command_Edit_Find)
			{
				string text = null;
				var selectionOnly = Selections.Any(range => range.HasSelection());

				if (Selections.Count == 1)
				{
					var sel = Selections.First();
					if ((sel.HasSelection()) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)))
					{
						selectionOnly = false;
						text = GetString(sel);
					}
				}

				var findDialog = new FindDialog { Text = text, SelectionOnly = selectionOnly };
				if (findDialog.ShowDialog() != true)
					return;

				RunSearch(findDialog.Regex, findDialog.SelectionOnly);
				if (findDialog.SelectAll)
				{
					if (Searches.Count != 0)
						Selections.Replace(Searches);
					Searches.Clear();
				}

				FindNext(true);
			}
			else if ((command == Command_Edit_FindNext) || (command == Command_Edit_FindPrev))
				FindNext(command == Command_Edit_FindNext);
			else if (command == Command_Edit_GotoLine)
			{
				var shift = shiftDown;
				var line = Data.GetOffsetLine(Selections.First().Start);
				var getNumDialog = new GetNumDialog
				{
					Title = "Go to line",
					Text = String.Format("Go to line: (1 - {0})", Data.NumLines),
					MinValue = 1,
					MaxValue = Data.NumLines,
					Value = line,
				};
				if (getNumDialog.ShowDialog() == true)
				{
					shiftOverride = shift;
					Selections.Replace(Selections.Select(range => MoveCursor(range, (int)getNumDialog.Value - 1, 0, false, true)).ToList());
					shiftOverride = null;
				}
			}
			else if (command == Command_Edit_GotoIndex)
			{
				var shift = shiftDown;
				var offset = Selections.First().Start;
				var line = Data.GetOffsetLine(offset);
				var index = Data.GetOffsetIndex(offset, line);
				var getNumDialog = new GetNumDialog
				{
					Title = "Go to column",
					Text = String.Format("Go to column: (1 - {0})", Data.GetLineLength(line) + 1),
					MinValue = 1,
					MaxValue = Data.GetLineLength(line) + 1,
					Value = index,
				};
				if (getNumDialog.ShowDialog() == true)
				{
					shiftOverride = shift;
					Selections.Replace(Selections.Select(range => MoveCursor(range, 0, (int)getNumDialog.Value - 1, true, false)).ToList());
					shiftOverride = null;
				}
			}
			else if (command == Command_Edit_BOM)
			{
				if (Data.BOM)
					Replace(new RangeList { new Range(0, 1) }, new List<string> { "" }, true);
				else
					Replace(new RangeList { new Range(0, 0) }, new List<string> { "\ufeff" }, true);
			}
			else if (command == Command_Data_Char_Upper)
			{
				var strs = Selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Char_Lower)
			{
				var strs = Selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Char_Proper)
			{
				var strs = Selections.Select(range => ProperCase(GetString(range))).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Char_Toggle)
			{
				var strs = Selections.Select(range => ToggleCase(GetString(range))).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Hex_ToHex)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Hex_FromHex)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_ToChar)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => ((char)UInt16.Parse(GetString(range), NumberStyles.HexNumber)).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_FromChar)
			{
				var selections = Selections.Where(range => range.Length == 1).ToList();
				var strs = selections.Select(range => ((UInt16)GetString(range)[0]).ToString("x2")).ToList();
				Replace(selections, strs, true);
			}
			else if (RunSortCommand(command))
			{ }
			else if (RunKeysCommand(command))
			{ }
			else if (command == Command_Data_Length)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Length.ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Width)
			{
				var minWidth = Selections.Select(range => range.Length).Max();
				var text = String.Join("", Selections.Select(range => GetString(range)));
				var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
				var widthDialog = new WidthDialog { MinWidthNum = minWidth, PadChar = numeric ? '0' : ' ', Before = numeric };
				if (widthDialog.ShowDialog() == true)
					Replace(Selections, Selections.Select(range => SetWidth(GetString(range), widthDialog.WidthNum, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
			}
			else if (command == Command_Data_Trim)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Evaluate)
			{
				var selections = Selections.Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Select(expr => new NeoEdit.Common.Expression(expr).Evaluate().ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Series)
			{
				var strs = Enumerable.Range(1, Selections.Count).Select(num => num.ToString()).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Repeat)
			{
				var repeatDialog = new RepeatDialog();
				if (repeatDialog.ShowDialog() != true)
					return;

				var strs = Selections.Select(range => RepeatString(GetString(range), repeatDialog.RepeatCount + 1)).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_Unique)
				Selections.Replace(Selections.GroupBy(range => GetString(range)).Select(list => list.First()).ToList());
			else if (command == Command_Data_Duplicates)
				Selections.Replace(Selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());
			else if (command == Command_Data_MD5)
			{
				var strs = Selections.Select(range => Checksum.Get(Checksum.Type.MD5, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_Data_SHA1)
			{
				var strs = Selections.Select(range => Checksum.Get(Checksum.Type.SHA1, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(Selections, strs, true);
			}
			else if (command == Command_SelectMark_Toggle)
			{
				if (Selections.Count > 1)
				{
					Marks.AddRange(Selections);
					Selections.Replace(Selections.First());
				}
				else if (Marks.Count != 0)
				{
					Selections.Replace(Marks);
					Marks.Clear();
				}
			}
			else if (command == Command_Select_All)
				Selections.Replace(new Range(EndOffset(), BeginOffset()));
			else if (command == Command_Select_Unselect)
				Selections.Replace(Selections.Select(range => new Range(range.Start)).ToList());
			else if (command == Command_Select_Single)
				Selections.Replace(Selections.First());
			else if (command == Command_Select_Lines)
			{
				var selectLinesDialog = new SelectLinesDialog();
				if (selectLinesDialog.ShowDialog() != true)
					return;

				var lines = Selections.SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
				var sels = lines.Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList();
				if (selectLinesDialog.IgnoreBlankLines)
					sels = sels.Where(sel => sel.Cursor != sel.Highlight).ToList();
				if (selectLinesDialog.LineMult > 1)
					sels = sels.Where((sel, index) => index % selectLinesDialog.LineMult == 0).ToList();
				Selections.Replace(sels);
			}
			else if (command == Command_Select_Marks)
			{
				if (Marks.Count == 0)
					return;

				Selections.Replace(Marks);
				Marks.Clear();
			}
			else if (command == Command_Select_Find)
			{
				Selections.Replace(Searches);
				Searches.Clear();
			}
			else if (command == Command_Select_RemoveEmpty)
				Selections.Replace(Selections.Where(range => range.HasSelection()).ToList());
			else if (command == Command_Mark_Selection)
				Marks.AddRange(Selections);
			else if (command == Command_Mark_Find)
			{
				Marks.AddRange(Searches);
				Searches.Clear();
			}
			else if (command == Command_Mark_Clear)
			{
				var hasSelection = Selections.Any(range => range.HasSelection());
				if (!hasSelection)
					Marks.Clear();
				else
				{
					foreach (var selection in Selections)
					{
						var toRemove = Marks.Where(mark => (mark.Start >= selection.Start) && (mark.End <= selection.End)).ToList();
						toRemove.ForEach(mark => Marks.Remove(mark));
					}
				}
			}
			else if (command == Command_Mark_LimitToSelection)
				Marks.Replace(Marks.Where(mark => Selections.Any(selection => (mark.Start >= selection.Start) && (mark.End <= selection.End))).ToList());
		}

		void HighlightingClicked(object sender, RoutedEventArgs e)
		{
			var header = (sender as MenuItem).Header.ToString();
			HighlightType = Helpers.ParseEnum<Highlighting.HighlightingType>(header);
		}

		void EncodeClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			Coder.Type encoding;
			if (header == "Current")
				encoding = Data.CoderUsed;
			else
				encoding = Helpers.ParseEnum<Coder.Type>(header);
			Launcher.Static.LaunchBinaryEditor(FileName, Data.GetBytes(encoding));
			this.Close();
		}

		readonly double charWidth;
		readonly double lineHeight;

		int numLines { get { return (int)(canvas.ActualHeight / lineHeight); } }
		int numColumns { get { return (int)(canvas.ActualWidth / charWidth); } }

		readonly Typeface typeface;
		readonly double fontSize;

		class TextCanvasUndoRedo
		{
			public RangeList ranges { get; private set; }
			public List<string> text { get; private set; }

			public TextCanvasUndoRedo(RangeList _ranges, List<string> _text)
			{
				ranges = _ranges;
				text = _text;
			}
		}

		List<TextCanvasUndoRedo> undo = new List<TextCanvasUndoRedo>();
		List<TextCanvasUndoRedo> redo = new List<TextCanvasUndoRedo>();

		Range visibleRange;
		void EnsureVisible(Range range)
		{
			if ((visibleRange != null) && (visibleRange.Start == range.Start) && (visibleRange.End == range.End))
				return;

			visibleRange = range;
			var line = Data.GetOffsetLine(range.Cursor);
			var index = Data.GetOffsetIndex(range.Cursor, line);
			yScrollValue = Math.Min(line, Math.Max(line - numLines + 1, yScrollValue));
			var x = Data.GetColumnFromIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x - numColumns + 1, xScrollValue));
		}

		DispatcherTimer visualTimer = null;
		new void InvalidateVisual()
		{
			if (visualTimer != null)
				return;

			visualTimer = new DispatcherTimer();
			visualTimer.Tick += (s, e) =>
			{
				visualTimer.Stop();
				visualTimer = null;

				canvas.InvalidateVisual();
			};
			visualTimer.Start();
		}

		DispatcherTimer selectionsTimer = null;
		void InvalidateSelections()
		{
			if (selectionsTimer != null)
				return;

			selectionsTimer = new DispatcherTimer();
			selectionsTimer.Tick += (s, e) =>
			{
				selectionsTimer.Stop();
				if (Selections.Count == 0)
					Selections.Add(new Range(BeginOffset()));
				Selections.DeOverlap();
				EnsureVisible(Selections.First());

				selectionsTimer = null;
				InvalidateVisual();
			};
			selectionsTimer.Start();
		}

		DispatcherTimer searchesTimer = null;
		void InvalidateSearches()
		{
			if (searchesTimer != null)
				return;

			searchesTimer = new DispatcherTimer();
			searchesTimer.Tick += (s, e) =>
			{
				searchesTimer.Stop();
				Searches.Replace(Searches.Where(range => range.HasSelection()).ToList());
				Searches.DeOverlap();

				searchesTimer = null;
				InvalidateVisual();
			};
			searchesTimer.Start();
		}

		DispatcherTimer marksTimer = null;
		void InvalidateMarks()
		{
			if (marksTimer != null)
				return;

			marksTimer = new DispatcherTimer();
			marksTimer.Tick += (s, e) =>
			{
				marksTimer.Stop();
				Marks.DeOverlap();

				marksTimer = null;
				InvalidateVisual();
			};
			marksTimer.Start();
		}

		void OnCanvasRender(DrawingContext dc)
		{
			if (Data == null)
				return;

			var brushes = new Dictionary<Brush, RangeList>
			{
				{ new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)), Selections}, //9cc7e6
				{ new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)), Searches }, //e2e6d6
				{ new SolidColorBrush(Color.FromArgb(178, 242, 155, 0)), Marks }, //f6b94d
			};

			HasBOM = Data.BOM;

			xScroll.ViewportSize = numColumns;
			xScroll.Maximum = Data.MaxColumn - numColumns;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = numColumns - 1;

			yScroll.ViewportSize = numLines;
			yScroll.Maximum = Data.NumLines - numLines;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = numLines - 1;

			var pos = Selections.First().Cursor;
			Line = Data.GetOffsetLine(pos) + 1;
			Index = Data.GetOffsetIndex(pos, Line - 1) + 1;
			Column = Data.GetColumnFromIndex(Line - 1, Index - 1) + 1;
			NumSelections = Selections.Count;

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + numLines + 1);
			var startColumn = xScrollValue;
			var endColumn = Math.Min(Data.MaxColumn + 1, startColumn + numColumns + 1);

			var lines = Enumerable.Range(startLine, endLine - startLine).ToList();
			var lineRanges = lines.ToDictionary(line => line, line => new Range(Data.GetOffset(line, 0), Data.GetOffset(line, Data.GetLineLength(line))));
			var screenStart = lineRanges.First().Value.Start;
			var screenEnd = lineRanges.Last().Value.End + 1;
			var startIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, startColumn));
			var endIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, endColumn));
			var y = lines.ToDictionary(line => line, line => (line - startLine) * lineHeight);

			foreach (var entry in brushes)
			{
				foreach (var range in entry.Value)
				{
					if ((range.End < screenStart) || (range.Start > screenEnd))
						continue;

					var entryStartLine = Data.GetOffsetLine(range.Start);
					var entryEndLine = Data.GetOffsetLine(range.End);
					var cursorLine = range.Cursor == range.Start ? entryStartLine : entryEndLine;
					entryStartLine = Math.Max(startLine, entryStartLine);
					entryEndLine = Math.Min(endLine, entryEndLine + 1);

					if ((entry.Value == Selections) && (cursorLine >= entryStartLine) && (cursorLine < entryEndLine))
					{
						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * charWidth, y[cursorLine], 1, lineHeight));
						}
					}

					for (var line = entryStartLine; line < entryEndLine; ++line)
					{
						var start = Math.Max(lineRanges[line].Start, range.Start);
						var end = Math.Min(lineRanges[line].End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);

						if ((start >= endIndexes[line]) || (end < startIndexes[line]))
							continue;

						start = Data.GetColumnFromIndex(line, start);
						end = Data.GetColumnFromIndex(line, end);
						if (range.End > lineRanges[line].End)
							end++;

						start = Math.Max(0, start - startColumn);
						end = Math.Min(endColumn, end) - startColumn;
						var width = end - start;

						dc.DrawRectangle(entry.Key, null, new Rect(start * charWidth, y[line], width * charWidth + 1, lineHeight));
					}
				}
			}

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var str = Data.GetColumnsLine(line);
				if (str.Length <= startColumn)
					continue;

				var highlight = new List<Tuple<Brush, int, int>>();
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						highlight.Add(new Tuple<Brush, int, int>(entry.Value, match.Index, match.Length));
				}

				str = str.Substring(startColumn, Math.Min(endColumn, str.Length) - startColumn);
				var text = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
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

		bool mouseDown;
		bool? shiftOverride;
		bool shiftDown { get { return shiftOverride.HasValue ? shiftOverride.Value : (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool selecting { get { return (mouseDown) || (shiftDown); } }

		void OnCanvasKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			var controlDown = this.controlDown;
			shiftOverride = shiftDown;
			switch (e.Key)
			{
				case Key.Back:
				case Key.Delete:
					{
						var selections = new RangeList();
						foreach (var range in Selections)
						{
							if (range.HasSelection())
							{
								selections.Add(range);
								continue;
							}

							var line = Data.GetOffsetLine(range.Start);
							var index = Data.GetOffsetIndex(range.Start, line);

							if (e.Key == Key.Back)
								--index;
							else
								++index;

							if (index < 0)
							{
								--line;
								if (line < 0)
									continue;
								index = Data.GetLineLength(line);
							}
							if (index > Data.GetLineLength(line))
							{
								++line;
								if (line >= Data.NumLines)
									continue;
								index = 0;
							}

							selections.Add(new Range(Data.GetOffset(line, index), range.Highlight));
						}

						Replace(selections, null, false);
					}
					break;
				case Key.Escape:
					Searches.Clear();
					break;
				case Key.Left:
					{
						var newSelections = new RangeList();
						foreach (var selection in Selections)
						{
							var line = Data.GetOffsetLine(selection.Cursor);
							var index = Data.GetOffsetIndex(selection.Cursor, line);
							if (controlDown)
								newSelections.Add(MovePrevWord(selection));
							else if ((index == 0) && (line != 0))
								newSelections.Add(MoveCursor(selection, -1, Int32.MaxValue, indexRel: false));
							else
								newSelections.Add(MoveCursor(selection, 0, -1));
						}
						Selections.Replace(newSelections);
					}
					break;
				case Key.Right:
					{
						var newSelections = new RangeList();
						foreach (var selection in Selections)
						{
							var line = Data.GetOffsetLine(selection.Cursor);
							var index = Data.GetOffsetIndex(selection.Cursor, line);
							if (controlDown)
								newSelections.Add(MoveNextWord(selection));
							else if ((index == Data.GetLineLength(line)) && (line != Data.NumLines - 1))
								newSelections.Add(MoveCursor(selection, 1, 0, indexRel: false));
							else
								newSelections.Add(MoveCursor(selection, 0, 1));
						}
						Selections.Replace(newSelections);
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = e.Key == Key.Up ? -1 : 1;
						if (controlDown)
							yScrollValue += mult;
						else
							Selections.Replace(Selections.Select(range => MoveCursor(range, mult, 0)).ToList());
					}
					break;
				case Key.Home:
					if (controlDown)
						Selections.Replace(Selections.Select(range => MoveCursor(range, 0, 0, false, false)).ToList());
					else
					{
						var newSelections = new RangeList();
						bool changed = false;
						foreach (var selection in Selections)
						{
							var line = Data.GetOffsetLine(selection.Cursor);
							var index = Data.GetOffsetIndex(selection.Cursor, line);
							int tmpIndex;
							var lineStr = Data[line];
							for (tmpIndex = 0; tmpIndex < lineStr.Length; ++tmpIndex)
							{
								if ((lineStr[tmpIndex] != ' ') && (lineStr[tmpIndex] != '\t'))
									break;
							}
							if (tmpIndex == lineStr.Length)
								tmpIndex = 0;
							if (tmpIndex != index)
								changed = true;
							newSelections.Add(MoveCursor(selection, 0, tmpIndex, indexRel: false));
						}
						if (!changed)
						{
							newSelections.Replace(newSelections.Select(range => MoveCursor(range, 0, 0, indexRel: false)).ToList());
							xScrollValue = 0;
						}
						Selections.Replace(newSelections);
					}
					break;
				case Key.End:
					if (controlDown)
						Selections.Replace(Selections.Select(range => MoveCursor(range, Int32.MaxValue, Int32.MaxValue, false, false)).ToList());
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, 0, Int32.MaxValue, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= numLines / 2;
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, 1 - numLines, 0)).ToList());
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += numLines / 2;
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, numLines - 1, 0)).ToList());
					break;
				case Key.Tab:
					{
						if (!Selections.Any(range => range.HasSelection()))
						{
							AddCanvasText("\t");
							break;
						}

						var lines = Selections.Where(a => a.HasSelection()).ToDictionary(a => Data.GetOffsetLine(a.Start), a => Data.GetOffsetLine(a.End - 1));
						lines = lines.SelectMany(selection => Enumerable.Range(selection.Key, selection.Value - selection.Key + 1)).Distinct().OrderBy(lineNum => lineNum).ToDictionary(line => line, line => Data.GetOffset(line, 0));
						int offset;
						string replace;
						if (shiftDown)
						{
							offset = 1;
							replace = "";
							lines = lines.Where(entry => Data[entry.Key].StartsWith("\t")).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							offset = 0;
							replace = "\t";
							lines = lines.Where(entry => !String.IsNullOrWhiteSpace(Data[entry.Key])).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => new Range(line.Value + offset, line.Value)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert, false);
					}
					break;
				case Key.Enter:
					AddCanvasText(Data.DefaultEnding);
					break;
				case Key.OemCloseBrackets:
					if (controlDown)
					{
						var newSelections = new RangeList();
						foreach (var selection in Selections)
						{
							var newPos = Data.GetOppositeBracket(selection.Cursor);
							if (newPos != -1)
							{
								var line = Data.GetOffsetLine(newPos);
								var index = Data.GetOffsetIndex(newPos, line);
								newSelections.Add(MoveCursor(selection, line, index, false, false));
							}
						}
						Selections.Replace(newSelections);
					}
					else
						e.Handled = false;
					break;
				default: e.Handled = false; break;
			}
			shiftOverride = null;
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		Range MoveNextWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Cursor);
			var index = Data.GetOffsetIndex(selection.Cursor, line) - 1;
			string lineStr = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != line)
				{
					lineStr = Data[line];
					lineIndex = line;
				}

				if (index >= lineStr.Length)
				{
					++line;
					if (line >= Data.NumLines)
						return MoveCursor(selection, Data.NumLines - 1, Int32.MaxValue, false, false);
					index = -1;
					continue;
				}

				++index;
				WordSkipType current;
				if ((index >= lineStr.Length) || (lineStr[index] == ' ') || (lineStr[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(lineStr[index])) || (lineStr[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return MoveCursor(selection, line, index, false, false);
			}
		}

		Range MovePrevWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Cursor);
			var index = Data.GetOffsetIndex(selection.Cursor, line);
			int lastLine = -1, lastIndex = -1;
			string lineStr = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != line)
				{
					lineStr = Data[line];
					lineIndex = line;
					if (index < 0)
						index = lineStr.Length;
				}

				if (index < 0)
				{
					--line;
					if (line < 0)
						return MoveCursor(selection, 0, 0, false, false);
					continue;
				}

				lastLine = line;
				lastIndex = index;

				--index;
				WordSkipType current;
				if ((index < 0) || (lineStr[index] == ' ') || (lineStr[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(lineStr[index])) || (lineStr[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return MoveCursor(selection, lastLine, lastIndex, false, false);
			}
		}

		Range MoveCursor(Range range, int line, int index, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(range.Cursor);
				var startIndex = Data.GetOffsetIndex(range.Cursor, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data.GetLineLength(line)));
			var cursor = Data.GetOffset(line, index);
			var highlight = selecting ? range.Highlight : cursor;
			range = new Range(cursor, highlight);

			return range;
		}

		void MouseHandler(Point mousePos)
		{
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / lineHeight) + yScrollValue);
			var index = Data.GetIndexFromColumn(line, (int)(mousePos.X / charWidth) + xScrollValue);

			Range selection;
			if (selecting)
			{
				selection = Selections.Last();
				Selections.Remove(selection);
			}
			else
			{
				if (!controlDown)
					Selections.Clear();

				selection = new Range();
			}
			selection = MoveCursor(selection, line, index, false, false);
			Selections.Add(selection);
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			MouseHandler(e.GetPosition(canvas));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				canvas.CaptureMouse();
			else
				canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			OnCanvasMouseLeftButtonDown(sender, e);
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(canvas));
			e.Handled = true;
		}

		void RunSearch(Regex regex, bool selectionOnly)
		{
			if (regex == null)
				return;

			var searches = new RangeList();

			var regions = selectionOnly ? Selections : new RangeList { new Range(EndOffset(), BeginOffset()) };
			foreach (var region in regions)
				searches.AddRange(Data.RegexMatches(regex, region.Start, region.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

			Searches.Replace(searches);
		}

		string GetString(Range range)
		{
			return Data.GetString(range.Start, range.End);
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		const int maxUndoChars = 1048576 * 10;
		void AddUndoRedo(TextCanvasUndoRedo current, ReplaceType replaceType)
		{
			switch (replaceType)
			{
				case ReplaceType.Undo:
					redo.Add(current);
					break;
				case ReplaceType.Redo:
					undo.Add(current);
					break;
				case ReplaceType.Normal:
					redo.Clear();

					// See if we can add this one to the last one
					bool done = false;
					if (undo.Count != 0)
					{
						var last = undo.Last();
						if (last.ranges.Count == current.ranges.Count)
						{
							var change = 0;
							done = true;
							for (var num = 0; num < last.ranges.Count; ++num)
							{
								if (last.ranges[num].End + change != current.ranges[num].Start)
								{
									done = false;
									break;
								}
								change += current.ranges[num].Length - current.text[num].Length;
							}

							if (done)
							{
								change = 0;
								for (var num = 0; num < last.ranges.Count; ++num)
								{
									last.ranges[num] = new Range(last.ranges[num].Start + change, last.ranges[num].End + current.ranges[num].Length + change);
									last.text[num] += current.text[num];
									change += current.ranges[num].Length - current.text[num].Length;
								}
							}
						}
					}

					if (!done)
						undo.Add(current);

					// Limit undo buffer
					while (true)
					{
						var totalChars = undo.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
						if (totalChars <= maxUndoChars)
							break;
						undo.RemoveAt(0);
					}
					break;
			}
		}

		void Replace(RangeList ranges, List<string> strs, bool leaveHighlighted, ReplaceType replaceType = ReplaceType.Normal)
		{
			if (strs == null)
				strs = ranges.Select(range => "").ToList();

			var undoRanges = new RangeList();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Highlight - ranges[ctr].End;
			}

			AddUndoRedo(new TextCanvasUndoRedo(undoRanges, undoText), replaceType);

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);

			Searches.Clear();

			var translateNums = RangeExtensions.GetTranslateNums(Selections, Marks, Searches);
			var translateMap = RangeExtensions.GetTranslateMap(translateNums, ranges, strs);
			Selections.Translate(translateMap);
			Marks.Translate(translateMap);
			Searches.Translate(translateMap);

			if (!leaveHighlighted)
				Selections.Replace(Selections.Select(range => new Range(range.End)).ToList());

			InvalidateVisual();
		}

		void FindNext(bool forward)
		{
			if (Searches.Count == 0)
				return;

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(range => range.Start > Selections[ctr].Start);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(range => range.Start < Selections[ctr].Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				Selections[ctr] = new Range(Searches[index].End, Searches[index].Start);
			}
		}

		string SetWidth(string str, int length, char padChar, bool before)
		{
			var pad = new string(padChar, length - str.Length);
			if (before)
				return pad + str;
			return str + pad;
		}

		string ProperCase(string input)
		{
			var sb = new StringBuilder(input.Length);
			for (var ctr = 0; ctr < input.Length; ++ctr)
			{
				if (!Char.IsLetter(input[ctr]))
					sb.Append(input[ctr]);
				else if ((ctr == 0) || (!Char.IsLetterOrDigit(input[ctr - 1])))
					sb.Append(Char.ToUpperInvariant(input[ctr]));
				else
					sb.Append(Char.ToLowerInvariant(input[ctr]));
			}
			return sb.ToString();
		}

		string ToggleCase(string input)
		{
			var sb = new StringBuilder(input.Length);
			for (var ctr = 0; ctr < input.Length; ++ctr)
			{
				if (!Char.IsLetter(input[ctr]))
					sb.Append(input[ctr]);
				else if (Char.IsLower(input[ctr]))
					sb.Append(Char.ToUpperInvariant(input[ctr]));
				else
					sb.Append(Char.ToLowerInvariant(input[ctr]));
			}
			return sb.ToString();
		}

		public string RepeatString(string input, int count)
		{
			var builder = new StringBuilder(input.Length * count);
			for (int ctr = 0; ctr < count; ++ctr)
				builder.Append(input);
			return builder.ToString();
		}

		void AddCanvasText(string text)
		{
			if (text.Length == 0)
				return;

			Replace(Selections, Selections.Select(range => text).ToList(), false);
		}
	}
}
