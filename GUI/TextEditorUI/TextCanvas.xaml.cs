using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.TextEditorUI.Dialogs;

namespace NeoEdit.GUI.TextEditorUI
{
	class Range
	{
		public Range Copy()
		{
			return new Range { Pos1 = Pos1, Pos2 = Pos2 };
		}

		public int Pos1 { get; set; }
		public int Pos2 { get; set; }
		public int Start { get { return Math.Min(Pos1, Pos2); } }
		public int End { get { return Math.Max(Pos1, Pos2); } }

		public bool HasSelection()
		{
			return Pos1 != Pos2;
		}

		public override string ToString()
		{
			return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
		}
	};

	class TextCanvasUndo
	{
		public List<int> offsets, lengths;
		public List<string> text;

		public TextCanvasUndo(List<int> _offsets, List<int> _lengths, List<string> _text)
		{
			offsets = _offsets;
			lengths = _lengths;
			text = _text;
		}
	}

	public partial class TextCanvas : Canvas
	{
		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Column { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSelections { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }

		const int tabStop = 4;
		readonly double charWidth;
		readonly double lineHeight;

		enum RangeType
		{
			Search,
			Mark,
			Selection,
		}
		Dictionary<RangeType, List<Range>> ranges = Helpers.GetValues<RangeType>().ToDictionary(rangeType => rangeType, rangeType => new List<Range>());
		static Dictionary<string, string> keysToValues = new Dictionary<string, string>();

		readonly Typeface typeface;
		readonly double fontSize;

		List<TextCanvasUndo> undo = new List<TextCanvasUndo>();

		static TextCanvas() { UIHelper<TextCanvas>.Register(); }

		readonly UIHelper<TextCanvas> uiHelper;
		public TextCanvas()
		{
			uiHelper = new UIHelper<TextCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) =>
			{
				foreach (var entry in ranges)
					ranges[entry.Key].Clear();
				InvalidateVisual();
				undo = new List<TextCanvasUndo>();
				Data.Undo += Data_Undo;
			});
			uiHelper.AddCallback(a => a.ChangeCount, (o, n) => { ranges[RangeType.Search].Clear(); InvalidateVisual(); });
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

		public void GotoPos(int line, int column)
		{
			try
			{
				var index = GetIndexFromColumn(line - 1, column - 1);
				var range = new Range();
				ranges[RangeType.Selection].Clear();
				ranges[RangeType.Selection].Add(range);
				SetPos1(range, line - 1, index, false, false);
			}
			catch { }
		}

		bool saveUndo = true;
		void Data_Undo(List<int> offsets, List<int> lengths, List<string> text)
		{
			if (!saveUndo)
				return;

			undo.Add(new TextCanvasUndo(offsets, lengths, text));
		}

		internal void EnsureVisible(Range selection)
		{
			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
			var y = GetYFromLine(line);
			yScrollValue = Math.Min(y, Math.Max(y + lineHeight - ActualHeight, yScrollValue));
			var x = GetXFromLineIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x + charWidth - ActualWidth, xScrollValue));
		}

		double GetXFromLineIndex(int line, int index)
		{
			var column = GetColumnFromIndex(line, index);
			return column * charWidth;
		}

		int GetColumnFromX(double x)
		{
			return (int)(x / charWidth);
		}

		double GetXFromColumn(int column)
		{
			return column * charWidth;
		}

		int GetLineFromY(double y)
		{
			return (int)(y / lineHeight);
		}

		double GetYFromLine(int line)
		{
			return line * lineHeight;
		}

		int GetColumnFromIndex(int line, int index)
		{
			return GetColumnFromIndex(Data[line], index);
		}

		int GetColumnFromIndex(string lineStr, int findIndex)
		{
			if ((findIndex < 0) || (findIndex > lineStr.Length))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < findIndex)
			{
				var find = lineStr.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = lineStr.Length;
				find = Math.Min(find, findIndex);

				column += find - index;
				index = find;
			}
			return column;
		}

		int GetIndexFromColumn(int line, int column)
		{
			return GetIndexFromColumn(Data[line], column);
		}

		int GetIndexFromColumn(string lineStr, int findColumn)
		{
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < lineStr.Length)
			{
				if (column >= findColumn)
					break;

				var find = lineStr.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = lineStr.Length;
				find = Math.Min(find, findColumn - column + index);

				column += find - index;
				index = find;
			}
			return index;
		}

		static Dictionary<RangeType, Brush> brushes = new Dictionary<RangeType, Brush>
		{
			{ RangeType.Selection, new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)) }, //9cc7e6
			{ RangeType.Search, new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)) }, //e2e6d6
			{ RangeType.Mark, new SolidColorBrush(Color.FromArgb(178, 242, 155, 0)) }, //f6b94d
		};
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			if (Data == null)
				return;

			HasBOM = Data.BOM;
			var columns = Enumerable.Range(0, Data.NumLines).Select(lineNum => Data[lineNum]).Select(line => GetColumnFromIndex(line, line.Length)).Max();

			xScrollMaximum = columns * charWidth - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = Data.NumLines * lineHeight - ActualHeight;
			yScrollSmallChange = lineHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;

			if (ranges[RangeType.Selection].Count == 0)
			{
				var range = new Range();
				ranges[RangeType.Selection].Add(range);
				SetPos1(range, 0, 0, false, false);
			}
			ranges[RangeType.Search] = ranges[RangeType.Search].Where(range => range.HasSelection()).ToList();

			EnsureVisible(ranges[RangeType.Selection].First());

			var keys = ranges.Keys.ToList();
			foreach (var key in keys)
			{
				ranges[key] = ranges[key].GroupBy(range => range.ToString()).Select(rangeGroup => rangeGroup.First()).OrderBy(range => range.Start).ToList();
				// Make sure ranges don't overlap
				for (var ctr = 0; ctr < ranges[key].Count - 1; ctr++)
				{
					ranges[key][ctr].Pos1 = Math.Min(ranges[key][ctr].Pos1, ranges[key][ctr + 1].Start);
					ranges[key][ctr].Pos2 = Math.Min(ranges[key][ctr].Pos2, ranges[key][ctr + 1].Start);
				}
			}

			var pos = ranges[RangeType.Selection].First().Pos1;
			Line = Data.GetOffsetLine(pos) + 1;
			Index = Data.GetOffsetIndex(pos, Line - 1) + 1;
			Column = GetColumnFromIndex(Line - 1, Index - 1) + 1;
			NumSelections = ranges[RangeType.Selection].Count;

			var startLine = Math.Max(0, GetLineFromY(yScrollValue));
			var endLine = Math.Min(Data.NumLines, GetLineFromY(ActualHeight + lineHeight + yScrollValue));

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var lineStr = Data[line];
				var lineRange = new Range { Pos1 = Data.GetOffset(line, 0), Pos2 = Data.GetOffset(line, lineStr.Length) };
				var y = line * lineHeight - yScrollValue;

				foreach (var entry in ranges)
				{
					foreach (var range in entry.Value)
					{
						if ((range.End < lineRange.Start) || (range.Start > lineRange.End))
							continue;

						var start = Math.Max(lineRange.Start, range.Start);
						var end = Math.Min(lineRange.End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);
						start = GetColumnFromIndex(lineStr, start);
						end = GetColumnFromIndex(lineStr, end);
						if (range.End > lineRange.End)
							end++;

						dc.DrawRectangle(brushes[entry.Key], null, new Rect(GetXFromColumn(start) - xScrollValue, y, (end - start) * charWidth + 1, lineHeight));
					}
				}

				foreach (var selection in ranges[RangeType.Selection])
				{
					if ((selection.Pos1 < lineRange.Start) || (selection.Pos1 > lineRange.End))
						continue;

					var column = GetColumnFromIndex(lineStr, Data.GetOffsetIndex(selection.Pos1, line));
					dc.DrawRectangle(Brushes.Black, null, new Rect(GetXFromColumn(column) - xScrollValue, y, 1, lineHeight));
				}

				var index = 0;
				var sb = new StringBuilder();
				while (index < lineStr.Length)
				{
					var find = lineStr.IndexOf('\t', index);
					if (find == index)
					{
						sb.Append(' ', (sb.Length / tabStop + 1) * tabStop - sb.Length);
						++index;
						continue;
					}

					if (find == -1)
						find = lineStr.Length;
					sb.Append(lineStr, index, find - index);
					index = find;
				}

				var str = sb.ToString();
				var text = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						text.SetForegroundBrush(entry.Value, match.Index, match.Length);
				}
				dc.DrawText(text, new Point(-xScrollValue, y));
			}
		}

		bool mouseDown;
		bool? shiftOverride;
		bool shiftDown { get { return shiftOverride.HasValue ? shiftOverride.Value : (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		internal bool selecting { get { return (mouseDown) || (shiftDown); } }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Back:
				case Key.Delete:
					foreach (var selection in ranges[RangeType.Selection])
					{
						if (selection.HasSelection())
							continue;

						var line = Data.GetOffsetLine(selection.Start);
						var index = Data.GetOffsetIndex(selection.Start, line);

						if (e.Key == Key.Back)
							--index;
						else
							++index;

						if (index < 0)
						{
							--line;
							if (line < 0)
								continue;
							index = Data[line].Length;
						}
						if (index > Data[line].Length)
						{
							++line;
							if (line >= Data.NumLines)
								continue;
							index = 0;
						}

						selection.Pos1 = Data.GetOffset(line, index);
					}

					Replace(ranges[RangeType.Selection], null, false);
					break;
				case Key.Escape:
					ranges[RangeType.Search].Clear();
					InvalidateVisual();
					break;
				case Key.Left:
					foreach (var selection in ranges[RangeType.Selection])
					{
						var line = Data.GetOffsetLine(selection.Pos1);
						var index = Data.GetOffsetIndex(selection.Pos1, line);
						if (controlDown)
							MovePrevWord(selection);
						else if ((index == 0) && (line != 0))
							SetPos1(selection, -1, Int32.MaxValue, indexRel: false);
						else
							SetPos1(selection, 0, -1);
					}
					break;
				case Key.Right:
					foreach (var selection in ranges[RangeType.Selection])
					{
						var line = Data.GetOffsetLine(selection.Pos1);
						var index = Data.GetOffsetIndex(selection.Pos1, line);
						if (controlDown)
							MoveNextWord(selection);
						else if ((index == Data[line].Length) && (line != Data.NumLines - 1))
							SetPos1(selection, 1, 0, indexRel: false);
						else
							SetPos1(selection, 0, 1);
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = e.Key == Key.Up ? -1 : 1;
						if (controlDown)
						{
							yScrollValue += lineHeight * mult;
							if (ranges[RangeType.Selection].Count == 1)
							{
								var line = Data.GetOffsetLine(ranges[RangeType.Selection][0].Pos1);
								var adj = Math.Min(0, line - GetLineFromY(yScrollValue + lineHeight - 1)) + Math.Max(0, line - GetLineFromY(yScrollValue + ActualHeight - lineHeight));
								SetPos1(ranges[RangeType.Selection][0], -adj, 0);

							}
						}
						else
						{
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, mult, 0);
						}
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
							SetPos1(selection, 0, 0, false, false);
					}
					else
					{
						bool changed = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							var line = Data.GetOffsetLine(selection.Pos1);
							var index = Data.GetOffsetIndex(selection.Pos1, line);
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
							SetPos1(selection, 0, tmpIndex, indexRel: false);
						}
						if (!changed)
						{
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, 0, 0, indexRel: false);
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, Data.NumLines - 1, Int32.MaxValue, false, false);
						else
							SetPos1(selection, 0, Int32.MaxValue, indexRel: false);
					break;
				case Key.PageUp:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, GetLineFromY(yScrollValue + lineHeight - 1), 0, lineRel: false);
						else
							SetPos1(selection, -(int)(ActualHeight / lineHeight - 1), 0);
					break;
				case Key.PageDown:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, GetLineFromY(ActualHeight + yScrollValue - lineHeight), 0, lineRel: false);
						else
							SetPos1(selection, (int)(ActualHeight / lineHeight - 1), 0);
					break;
				case Key.Tab:
					{
						if (!ranges[RangeType.Selection].Any(range => range.HasSelection()))
						{
							AddText("\t");
							break;
						}

						var lines = ranges[RangeType.Selection].Where(a => a.HasSelection()).ToDictionary(a => Data.GetOffsetLine(a.Start), a => Data.GetOffsetLine(a.End - 1));
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

						var sels = lines.Select(line => new Range { Pos1 = line.Value + offset, Pos2 = line.Value }).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert, false);
					}
					break;
				case Key.Enter:
					AddText(Data.DefaultEnding);
					break;
				case Key.OemCloseBrackets:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
						{
							var newPos = Data.GetOppositeBracket(selection.Pos1);
							if (newPos != -1)
							{
								var line = Data.GetOffsetLine(newPos);
								var index = Data.GetOffsetIndex(newPos, line);
								SetPos1(selection, line, index, false, false);
							}
						}
						InvalidateVisual();
					}
					else
						e.Handled = false;
					break;
				case Key.System:
					switch (e.SystemKey)
					{
						case Key.Up:
						case Key.Down:
						case Key.Left:
						case Key.Right:
							var lineMult = 0;
							var indexMult = 0;
							switch (e.SystemKey)
							{
								case Key.Up: lineMult = -1; break;
								case Key.Down: lineMult = 1; break;
								case Key.Left: indexMult = -1; break;
								case Key.Right: indexMult = 1; break;
							}

							if (altOnly)
							{
								for (var offset = 0; offset < ranges[RangeType.Selection].Count; ++offset)
									SetPos1(ranges[RangeType.Selection][offset], offset * lineMult, offset * indexMult);
							}
							break;
						default: e.Handled = false; break;
					}
					break;
				default: e.Handled = false; break;
			}
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		void MoveNextWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line) - 1;
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
					{
						SetPos1(selection, Data.NumLines - 1, Int32.MaxValue, false, false);
						return;
					}
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
				{
					SetPos1(selection, line, index, false, false);
					return;
				}
			}
		}

		void MovePrevWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
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
					{
						SetPos1(selection, 0, 0, false, false);
						return;
					}
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
				{
					SetPos1(selection, lastLine, lastIndex, false, false);
					return;
				}
			}
		}

		void SetPos1(Range selection, int line, int index, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(selection.Pos1);
				var startIndex = Data.GetOffsetIndex(selection.Pos1, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data[line].Length));
			selection.Pos1 = Data.GetOffset(line, index);

			if (!selecting)
				SetPos2(selection, line, index, false, false);

			InvalidateVisual();
		}

		void SetPos2(Range selection, int line, int index, bool lineRel = false, bool indexRel = false)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(selection.Pos2);
				var startIndex = Data.GetOffsetIndex(selection.Pos2, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data[line].Length));
			selection.Pos2 = Data.GetOffset(line, index);
			InvalidateVisual();
		}

		void MouseHandler(Point mousePos)
		{
			var line = Math.Min(Data.NumLines - 1, GetLineFromY(mousePos.Y + yScrollValue));
			var index = GetIndexFromColumn(line, GetColumnFromX(mousePos.X + xScrollValue));

			Range selection;
			if (selecting)
				selection = ranges[RangeType.Selection].Last();
			else
			{
				if (!controlDown)
					ranges[RangeType.Selection].Clear();

				selection = new Range();
				ranges[RangeType.Selection].Add(selection);
			}
			SetPos1(selection, line, index, false, false);
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			MouseHandler(e.GetPosition(this));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				CaptureMouse();
			else
				ReleaseMouseCapture();
			e.Handled = true;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(this));
			e.Handled = true;
		}

		void RunSearch(Regex regex, bool selectionOnly)
		{
			if (regex == null)
				return;

			ranges[RangeType.Search].Clear();

			for (var line = 0; line < Data.NumLines; line++)
			{
				var lineStr = Data[line];
				var matches = regex.Matches(lineStr);
				foreach (Match match in matches)
				{
					var searchResult = new Range { Pos1 = Data.GetOffset(line, match.Index + match.Length), Pos2 = Data.GetOffset(line, match.Index) };

					if (selectionOnly)
					{
						var foundMatch = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							if ((searchResult.Start < selection.Start) || (searchResult.End > selection.End))
								continue;

							foundMatch = true;
							break;
						}
						if (!foundMatch)
							continue;
					}

					ranges[RangeType.Search].Add(searchResult);
				}
			}
			InvalidateVisual();
		}

		string GetString(Range range)
		{
			return Data.GetString(range.Start, range.End);
		}

		void Replace(List<Range> replaceRanges, List<string> strs, bool leaveHighlighted)
		{
			if (strs == null)
				strs = replaceRanges.Select(range => "").ToList();

			Data.Replace(replaceRanges.Select(range => range.Start).ToList(), replaceRanges.Select(range => range.End - range.Start).ToList(), strs);

			var numsToMap = ranges.SelectMany(rangePair => rangePair.Value).SelectMany(range => new int[] { range.Start, range.End }).Distinct().OrderBy(num => num).ToList();
			var oldToNewMap = new Dictionary<int, int>();
			var replaceRange = 0;
			var offset = 0;
			while (numsToMap.Count != 0)
			{
				int start = Int32.MaxValue, end = Int32.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (numsToMap[0] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = numsToMap[0];
				if ((value > start) && (value < end))
					value = start + length;

				oldToNewMap[numsToMap[0]] = value + offset;
				numsToMap.RemoveAt(0);
			}

			foreach (var range in ranges.SelectMany(rangePair => rangePair.Value))
			{
				range.Pos1 = oldToNewMap[range.Pos1];
				range.Pos2 = oldToNewMap[range.Pos2];
			}

			if (!leaveHighlighted)
				replaceRanges.ForEach(range => range.Pos1 = range.Pos2 = range.End);

			InvalidateVisual();
		}

		void FindNext(bool forward)
		{
			if (ranges[RangeType.Search].Count == 0)
				return;

			foreach (var selection in ranges[RangeType.Selection])
			{
				int index;
				if (forward)
				{
					index = ranges[RangeType.Search].FindIndex(range => range.Start > selection.Start);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = ranges[RangeType.Search].FindLastIndex(range => range.Start < selection.Start);
					if (index == -1)
						index = ranges[RangeType.Search].Count - 1;
				}

				selection.Pos1 = ranges[RangeType.Search][index].End;
				selection.Pos2 = ranges[RangeType.Search][index].Start;
			}
			InvalidateVisual();
		}

		string SetWidth(string str, int length, char padChar, bool before)
		{
			var pad = new string(padChar, length - str.Length);
			if (before)
				return pad + str;
			return str + pad;
		}

		string SortStr(string str)
		{
			return Regex.Replace(str, @"\d+", match => new string('0', Math.Max(0, 20 - match.Value.Length)) + match.Value);
		}

		public void RunCommand(ICommand command)
		{
			InvalidateVisual();

			if (command == TextEditor.Command_Edit_Undo)
			{
				if (undo.Count == 0)
					return;

				var undoStep = undo.Last();
				undo.Remove(undoStep);
				var rangeList = Enumerable.Range(0, undoStep.offsets.Count).Select(num => new Range { Pos1 = undoStep.offsets[num], Pos2 = undoStep.offsets[num] + undoStep.lengths[num] }).ToList();
				saveUndo = false;
				Replace(rangeList, undoStep.text, true);
				saveUndo = true;
			}
			else if ((command == TextEditor.Command_Edit_Cut) || (command == TextEditor.Command_Edit_Copy))
			{
				var result = ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)).ToArray();
				if (result.Length != 0)
					Clipboard.Set(result);
				if (command == TextEditor.Command_Edit_Cut)
					Replace(ranges[RangeType.Selection], null, false);
			}
			else if (command == TextEditor.Command_Edit_Paste)
			{
				var result = Clipboard.GetStrings().ToList();
				if ((result == null) || (result.Count == 0))
					return;

				var sels = ranges[RangeType.Selection];
				while (result.Count > sels.Count)
				{
					result[result.Count - 2] += " " + result[result.Count - 1];
					result.RemoveAt(result.Count - 1);
				}
				while (result.Count < sels.Count)
					result.Add(result.Last());

				Replace(sels, result, false);
			}
			else if (command == TextEditor.Command_Edit_Find)
			{
				var selectionOnly = ranges[RangeType.Selection].Any(range => range.HasSelection());
				var findDialog = new FindDialog { SelectionOnly = selectionOnly };
				if (findDialog.ShowDialog() != true)
					return;

				RunSearch(findDialog.Regex, findDialog.SelectionOnly);
				if (findDialog.SelectAll)
				{
					if (ranges[RangeType.Search].Count != 0)
						ranges[RangeType.Selection] = ranges[RangeType.Search];
					ranges[RangeType.Search] = new List<Range>();
				}

				FindNext(true);
			}
			else if ((command == TextEditor.Command_Edit_FindNext) || (command == TextEditor.Command_Edit_FindPrev))
				FindNext(command == TextEditor.Command_Edit_FindNext);
			else if (command == TextEditor.Command_Edit_GotoLine)
			{
				var shift = shiftDown;
				var line = Data.GetOffsetLine(ranges[RangeType.Selection].First().Start);
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
					foreach (var selection in ranges[RangeType.Selection])
						SetPos1(selection, (int)getNumDialog.Value - 1, 0, false, true);
					shiftOverride = null;
				}
			}
			else if (command == TextEditor.Command_Edit_GotoIndex)
			{
				var offset = ranges[RangeType.Selection].First().Start;
				var line = Data.GetOffsetLine(offset);
				var index = Data.GetOffsetIndex(offset, line);
				var getNumDialog = new GetNumDialog
				{
					Title = "Go to column",
					Text = String.Format("Go to column: (1 - {0})", Data[line].Length + 1),
					MinValue = 1,
					MaxValue = Data[line].Length + 1,
					Value = index,
				};
				if (getNumDialog.ShowDialog() == true)
				{
					foreach (var selection in ranges[RangeType.Selection])
						SetPos1(selection, 0, (int)getNumDialog.Value - 1, true, false);
				}
			}
			else if (command == TextEditor.Command_Edit_BOM)
			{
				Data.SetBOM(!Data.BOM);
				var offset = Data.BOM ? 1 : -1;
				foreach (var rangeEntry in ranges)
					foreach (var range in rangeEntry.Value)
					{
						range.Pos1 += offset;
						range.Pos2 += offset;
					}
			}
			else if (command == TextEditor.Command_Data_ToUpper)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_ToLower)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_ToHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_FromHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_ToChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => ((char)UInt16.Parse(GetString(range), NumberStyles.HexNumber)).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_FromChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.End - range.Start == 1).ToList();
				var strs = selections.Select(range => ((UInt16)GetString(range)[0]).ToString("x2")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_Width)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var minWidth = selections.Select(range => range.End - range.Start).Max();
				var text = String.Join("", selections.Select(range => GetString(range)));
				var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
				var widthDialog = new WidthDialog { MinWidthNum = minWidth, PadChar = numeric ? '0' : ' ', Before = numeric };
				if (widthDialog.ShowDialog() == true)
					Replace(selections, selections.Select(range => SetWidth(GetString(range), widthDialog.WidthNum, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
			}
			else if (command == TextEditor.Command_Data_Trim)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_SetKeys)
			{
				var keys = ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)).ToList();
				if (keys.Distinct().Count() != keys.Count)
					throw new ArgumentException("Cannot have duplicate keys.");
				keysToValues = keys.ToDictionary(key => key, key => key);
			}
			else if (command == TextEditor.Command_Data_SetValues)
			{
				var keys = keysToValues.Keys.ToList();
				var values = ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)).ToList();
				if (values.Count() != keys.Count)
					throw new ArgumentException("Key count must match value count.");
				keysToValues = Enumerable.Range(0, keys.Count).ToDictionary(num => keys[num], num => values[num]);
			}
			else if (command == TextEditor.Command_Data_KeysToValues)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Select(sel => keysToValues.ContainsKey(sel) ? keysToValues[sel] : sel).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_Reverse)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Reverse().ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_Sort)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).OrderBy(str => SortStr(str)).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_Evaluate)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Select(expr => new NeoEdit.GUI.Common.Expression(expr).Evaluate().ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditor.Command_Data_Duplicates)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var dups = selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList();
				if (dups.Count != 0)
					ranges[RangeType.Selection] = dups;
			}
			else if (command == TextEditor.Command_Data_Randomize)
			{
				var rng = new Random();
				var strs = ranges[RangeType.Selection].Select(range => GetString(range)).OrderBy(range => rng.Next()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditor.Command_Data_Series)
			{
				var strs = Enumerable.Range(1, ranges[RangeType.Selection].Count).Select(num => num.ToString()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditor.Command_SelectMark_Toggle)
			{
				if (ranges[RangeType.Selection].Count > 1)
				{
					ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection].Select(range => range.Copy()));
					ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].First() };
				}
				else if (ranges[RangeType.Mark].Count != 0)
				{
					ranges[RangeType.Selection] = ranges[RangeType.Mark];
					ranges[RangeType.Mark] = new List<Range>();
				}
			}
			else if (command == TextEditor.Command_Select_All)
			{
				foreach (var selection in ranges[RangeType.Selection])
				{
					SetPos1(selection, Int32.MaxValue, Int32.MaxValue, false, false);
					SetPos2(selection, 0, 0, false, false);
				}
			}
			else if (command == TextEditor.Command_Select_Unselect)
			{
				ranges[RangeType.Selection].ForEach(range => range.Pos1 = range.Pos2 = range.Start);
				InvalidateVisual();
			}
			else if (command == TextEditor.Command_Select_Single)
				ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].First() };
			else if (command == TextEditor.Command_Select_Lines)
			{
				var lines = ranges[RangeType.Selection].SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
				var lengths = lines.ToDictionary(line => line, line => Data[line].Length);
				ranges[RangeType.Selection] = lengths.Select(line => new Range { Pos1 = Data.GetOffset(line.Key, line.Value), Pos2 = Data.GetOffset(line.Key, 0) }).ToList();
			}
			else if (command == TextEditor.Command_Select_Find)
			{
				ranges[RangeType.Selection] = ranges[RangeType.Search];
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == TextEditor.Command_Select_Marks)
			{
				if (ranges[RangeType.Mark].Count == 0)
					return;

				ranges[RangeType.Selection] = ranges[RangeType.Mark];
				ranges[RangeType.Mark] = new List<Range>();
			}
			else if (command == TextEditor.Command_Mark_Find)
			{
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Search]);
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == TextEditor.Command_Mark_Selection)
			{
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection].Select(range => range.Copy()));
			}
			else if (command == TextEditor.Command_Mark_LimitToSelection)
			{
				ranges[RangeType.Mark] = ranges[RangeType.Mark].Where(mark => ranges[RangeType.Selection].Any(selection => (mark.Start >= selection.Start) && (mark.End <= selection.End))).ToList();
			}
			else if (command == TextEditor.Command_Mark_Clear)
			{
				var hasSelection = ranges[RangeType.Selection].Any(range => range.HasSelection());
				if (!hasSelection)
					ranges[RangeType.Mark].Clear();
				else
				{
					foreach (var selection in ranges[RangeType.Selection])
					{
						var toRemove = ranges[RangeType.Mark].Where(mark => (mark.Start >= selection.Start) && (mark.End <= selection.End)).ToList();
						toRemove.ForEach(mark => ranges[RangeType.Mark].Remove(mark));
					}
				}
			}
		}

		void AddText(string text)
		{
			if (text.Length == 0)
				return;

			Replace(ranges[RangeType.Selection], ranges[RangeType.Selection].Select(range => text).ToList(), false);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			AddText(e.Text);
			e.Handled = true;
		}
	}
}
