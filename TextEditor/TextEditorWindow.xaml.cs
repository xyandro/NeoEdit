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
	public partial class TextEditorWindow : Window
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
		public static RoutedCommand Command_Data_Sort_String = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Reverse = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Randomize = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Length = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Lines_String = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Lines_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Lines_Keys = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Regions_String = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Regions_Numeric = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort_Regions_Keys = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetKeys = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues1 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues2 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues3 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues4 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues5 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues6 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues7 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues8 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_SetValues9 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues1 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues2 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues3 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues4 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues5 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues6 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues7 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues8 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_KeysToValues9 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyKeys = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues1 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues2 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues3 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues4 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues5 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues6 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues7 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues8 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_CopyValues9 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsKeys = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues1 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues2 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues3 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues4 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues5 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues6 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues7 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues8 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_HitsValues9 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesKeys = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues1 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues2 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues3 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues4 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues5 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues6 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues7 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues8 = new RoutedCommand();
		public static RoutedCommand Command_Data_Keys_MissesValues9 = new RoutedCommand();
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

		[DepProp]
		string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
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
		public int xScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorWindow() { UIHelper<TextEditorWindow>.Register(); }

		readonly UIHelper<TextEditorWindow> uiHelper;
		public TextEditorWindow(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int? line = null, int? column = null)
		{
			uiHelper = new UIHelper<TextEditorWindow>(this);
			InitializeComponent();

			canvas.Initialize(OnCanvasRender);

			OpenFile(filename, bytes, encoding);

			GotoPos(line.HasValue ? line.Value : 1, column.HasValue ? column.Value : 1);
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

			uiHelper.AddCallback(a => a.Data, (o, n) =>
			{
				foreach (var entry in ranges)
					ranges[entry.Key].Clear();
				InvalidateVisual();
				undo.Clear();
				redo.Clear();
			});
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
				var result = ranges[RangeType.Selection].Select(range => GetString(range)).ToArray();
				if (result.Length != 0)
					ClipboardWindow.Set(result);
				if (command == Command_Edit_Cut)
					Replace(ranges[RangeType.Selection], null, false);
			}
			else if (command == Command_Edit_Paste)
			{
				var result = ClipboardWindow.GetStrings().ToList();
				if ((result == null) || (result.Count == 0))
					return;

				var sels = ranges[RangeType.Selection];
				var separator = sels.Count == 1 ? Data.DefaultEnding : " ";
				while (result.Count > sels.Count)
				{
					result[result.Count - 2] += separator + result[result.Count - 1];
					result.RemoveAt(result.Count - 1);
				}
				while (result.Count < sels.Count)
					result.Add(result.Last());

				Replace(sels, result, false);
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
				var selectionOnly = ranges[RangeType.Selection].Any(range => range.HasSelection());

				if (ranges[RangeType.Selection].Count == 1)
				{
					var sel = ranges[RangeType.Selection].First();
					if ((sel.HasSelection()) && (Data.GetOffsetLine(sel.Pos1) == Data.GetOffsetLine(sel.Pos2)))
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
					if (ranges[RangeType.Search].Count != 0)
						ranges[RangeType.Selection] = ranges[RangeType.Search];
					ranges[RangeType.Search] = new List<Range>();
				}

				FindNext(true);
			}
			else if ((command == Command_Edit_FindNext) || (command == Command_Edit_FindPrev))
				FindNext(command == Command_Edit_FindNext);
			else if (command == Command_Edit_GotoLine)
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
			else if (command == Command_Edit_GotoIndex)
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
			else if (command == Command_Edit_BOM)
			{
				if (Data.BOM)
					Replace(new List<Range> { new Range { Pos1 = 0, Pos2 = 1 } }, new List<string> { "" }, true);
				else
					Replace(new List<Range> { new Range { Pos1 = 0, Pos2 = 0 } }, new List<string> { "\ufeff" }, true);
			}
			else if (command == Command_Data_Char_Upper)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_Lower)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_Proper)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => ProperCase(GetString(range))).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_Toggle)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => ToggleCase(GetString(range))).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Hex_ToHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Hex_FromHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_ToChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => ((char)UInt16.Parse(GetString(range), NumberStyles.HexNumber)).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Char_FromChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.Length == 1).ToList();
				var strs = selections.Select(range => ((UInt16)GetString(range)[0]).ToString("x2")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Sort_String)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).OrderBy(str => str).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Sort_Numeric)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).OrderBy(str => SortStr(str)).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Sort_Reverse)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).Reverse().ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Sort_Randomize)
			{
				var rng = new Random();
				var strs = ranges[RangeType.Selection].Select(range => GetString(range)).OrderBy(range => rng.Next()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == Command_Data_Sort_Length)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).OrderBy(str => str.Length).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Sort_Lines_String)
			{
				var regions = ranges[RangeType.Selection].Select(range => Data.GetOffsetLine(range.Start)).Select(line => new { index = Data.GetOffset(line, 0), length = Data[line].Length }).Select(entry => new Range { Pos1 = entry.index, Pos2 = entry.index + entry.length }).ToList();
				var ordering = ranges[RangeType.Selection].Select((range, index) => new { str = GetString(range), index = index }).OrderBy(entry => entry.str).Select(entry => entry.index).ToList();
				SortRegions(regions, ordering);
			}
			else if (command == Command_Data_Sort_Lines_Numeric)
			{
				var regions = ranges[RangeType.Selection].Select(range => Data.GetOffsetLine(range.Start)).Select(line => new { index = Data.GetOffset(line, 0), length = Data[line].Length }).Select(entry => new Range { Pos1 = entry.index, Pos2 = entry.index + entry.length }).ToList();
				var ordering = ranges[RangeType.Selection].Select((range, index) => new { str = GetString(range), index = index }).OrderBy(entry => SortStr(entry.str)).Select(entry => entry.index).ToList();
				SortRegions(regions, ordering);
			}
			else if (command == Command_Data_Sort_Lines_Keys)
			{
				var regions = ranges[RangeType.Selection].Select(range => Data.GetOffsetLine(range.Start)).Select(line => new { index = Data.GetOffset(line, 0), length = Data[line].Length }).Select(entry => new Range { Pos1 = entry.index, Pos2 = entry.index + entry.length }).ToList();

				var sort = keysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
				var ordering = ranges[RangeType.Selection].Select((range, index) => new { key = GetString(range), index = index }).OrderBy(key => key.key, (key1, key2) => (sort.ContainsKey(key1) ? sort[key1] : int.MaxValue).CompareTo(sort.ContainsKey(key2) ? sort[key2] : int.MaxValue)).Select(obj => obj.index).ToList();

				SortRegions(regions, ordering);
			}
			else if (command == Command_Data_Sort_Regions_String)
			{
				var regions = new List<Range>();
				foreach (var selection in ranges[RangeType.Selection])
				{
					var region = ranges[RangeType.Mark].Where(mark => (selection.Start >= mark.Start) && (selection.End <= mark.End)).ToList();
					if (region.Count == 0)
						throw new Exception("No region found.  All selection must be inside a marked region.");
					if (region.Count != 1)
						throw new Exception("Multiple regions found.  All selection must be inside a single marked region.");
					regions.Add(region.Single());
				}

				if (ranges[RangeType.Mark].Count != regions.Count)
					throw new Exception("Extra regions found.");

				var ordering = ranges[RangeType.Selection].Select((range, index) => new { str = GetString(range), index = index }).OrderBy(entry => entry.str).Select(entry => entry.index).ToList();
				ranges[RangeType.Mark].Clear();
				SortRegions(regions, ordering, true);
			}
			else if (command == Command_Data_Sort_Regions_Numeric)
			{
				var regions = new List<Range>();
				foreach (var selection in ranges[RangeType.Selection])
				{
					var region = ranges[RangeType.Mark].Where(mark => (selection.Start >= mark.Start) && (selection.End <= mark.End)).ToList();
					if (region.Count == 0)
						throw new Exception("No region found.  All selection must be inside a marked region.");
					if (region.Count != 1)
						throw new Exception("Multiple regions found.  All selection must be inside a single marked region.");
					regions.Add(region.Single());
				}

				if (ranges[RangeType.Mark].Count != regions.Count)
					throw new Exception("Extra regions found.");

				var ordering = ranges[RangeType.Selection].Select((range, index) => new { str = GetString(range), index = index }).OrderBy(entry => SortStr(entry.str)).Select(entry => entry.index).ToList();
				ranges[RangeType.Mark].Clear();
				SortRegions(regions, ordering, true);
			}
			else if (command == Command_Data_Sort_Regions_Keys)
			{
				var regions = new List<Range>();
				foreach (var selection in ranges[RangeType.Selection])
				{
					var region = ranges[RangeType.Mark].Where(mark => (selection.Start >= mark.Start) && (selection.End <= mark.End)).ToList();
					if (region.Count == 0)
						throw new Exception("No region found.  All selection must be inside a marked region.");
					if (region.Count != 1)
						throw new Exception("Multiple regions found.  All selection must be inside a single marked region.");
					regions.Add(region.Single());
				}

				if (ranges[RangeType.Mark].Count != regions.Count)
					throw new Exception("Extra regions found.");

				var sort = keysAndValues[0].Select((key, index) => new { key = key, index = index }).ToDictionary(entry => entry.key, entry => entry.index);
				var ordering = ranges[RangeType.Selection].Select((range, index) => new { key = GetString(range), index = index }).OrderBy(key => key.key, (key1, key2) => (sort.ContainsKey(key1) ? sort[key1] : int.MaxValue).CompareTo(sort.ContainsKey(key2) ? sort[key2] : int.MaxValue)).Select(obj => obj.index).ToList();
				ranges[RangeType.Mark].Clear();
				SortRegions(regions, ordering, true);
			}
			else if (GetKeysValuesCommand(command) == Command_Data_Keys_SetValues1)
			{
				// Handles keys as well as values
				var index = GetKeysValuesIndex(command);
				var values = ranges[RangeType.Selection].Select(range => GetString(range)).ToList();
				if ((index == 0) && (values.Distinct().Count() != values.Count))
					throw new ArgumentException("Cannot have duplicate keys.");
				keysAndValues[index] = values;
			}
			else if (GetKeysValuesCommand(command) == Command_Data_Keys_KeysToValues1)
			{
				var index = GetKeysValuesIndex(command);
				if (keysAndValues[0].Count != keysAndValues[index].Count)
					throw new Exception("Keys and values count must match.");

				var strs = new List<string>();
				foreach (var range in ranges[RangeType.Selection])
				{
					var str = GetString(range);
					var found = keysAndValues[0].IndexOf(str);
					if (found == -1)
						strs.Add(str);
					else
						strs.Add(keysAndValues[index][found]);
				}
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (GetKeysValuesCommand(command) == Command_Data_Keys_CopyValues1)
				ClipboardWindow.Set(keysAndValues[GetKeysValuesIndex(command)].ToArray());
			else if (GetKeysValuesCommand(command) == Command_Data_Keys_HitsValues1)
			{
				var index = GetKeysValuesIndex(command);
				ranges[RangeType.Selection] = ranges[RangeType.Selection].Where(range => keysAndValues[index].Contains(GetString(range))).ToList();
			}
			else if (GetKeysValuesCommand(command) == Command_Data_Keys_MissesValues1)
			{
				var index = GetKeysValuesIndex(command);
				ranges[RangeType.Selection] = ranges[RangeType.Selection].Where(range => !keysAndValues[index].Contains(GetString(range))).ToList();
			}
			else if (command == Command_Data_Length)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Length.ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Width)
			{
				var selections = ranges[RangeType.Selection];
				var minWidth = selections.Select(range => range.Length).Max();
				var text = String.Join("", selections.Select(range => GetString(range)));
				var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
				var widthDialog = new WidthDialog { MinWidthNum = minWidth, PadChar = numeric ? '0' : ' ', Before = numeric };
				if (widthDialog.ShowDialog() == true)
					Replace(selections, selections.Select(range => SetWidth(GetString(range), widthDialog.WidthNum, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
			}
			else if (command == Command_Data_Trim)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Evaluate)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Select(expr => new NeoEdit.Common.Expression(expr).Evaluate().ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == Command_Data_Series)
			{
				var strs = Enumerable.Range(1, ranges[RangeType.Selection].Count).Select(num => num.ToString()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == Command_Data_Repeat)
			{
				var repeatDialog = new RepeatDialog();
				if (repeatDialog.ShowDialog() != true)
					return;

				var strs = ranges[RangeType.Selection].Select(range => RepeatString(GetString(range), repeatDialog.RepeatCount)).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == Command_Data_Unique)
			{
				var selections = ranges[RangeType.Selection];
				var dups = selections.GroupBy(range => GetString(range)).Select(list => list.First()).ToList();
				ranges[RangeType.Selection] = dups;
			}
			else if (command == Command_Data_Duplicates)
			{
				var selections = ranges[RangeType.Selection];
				var dups = selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList();
				if (dups.Count != 0)
					ranges[RangeType.Selection] = dups;
			}
			else if (command == Command_Data_MD5)
			{
				var strs = ranges[RangeType.Selection].Select(range => Checksum.Get(Checksum.Type.MD5, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == Command_Data_SHA1)
			{
				var strs = ranges[RangeType.Selection].Select(range => Checksum.Get(Checksum.Type.SHA1, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == Command_SelectMark_Toggle)
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
			else if (command == Command_Select_All)
			{
				foreach (var selection in ranges[RangeType.Selection])
				{
					SetPos1(selection, Int32.MaxValue, Int32.MaxValue, false, false);
					SetPos2(selection, 0, 0, false, false);
				}
			}
			else if (command == Command_Select_Unselect)
			{
				ranges[RangeType.Selection].ForEach(range => range.Pos1 = range.Pos2 = range.Start);
				InvalidateVisual();
			}
			else if (command == Command_Select_Single)
				ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].First() };
			else if (command == Command_Select_Lines)
			{
				var selectLinesDialog = new SelectLinesDialog();
				if (selectLinesDialog.ShowDialog() != true)
					return;

				var lines = ranges[RangeType.Selection].SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
				var sels = lines.Select(line => new Range { Pos1 = Data.GetOffset(line, Data[line].Length), Pos2 = Data.GetOffset(line, 0) }).ToList();
				if (selectLinesDialog.IgnoreBlankLines)
					sels = sels.Where(sel => sel.Pos1 != sel.Pos2).ToList();
				if (selectLinesDialog.LineMult > 1)
					sels = sels.Where((sel, index) => index % selectLinesDialog.LineMult == 0).ToList();
				ranges[RangeType.Selection] = sels;
				InvalidateVisual();
			}
			else if (command == Command_Select_Marks)
			{
				if (ranges[RangeType.Mark].Count == 0)
					return;

				ranges[RangeType.Selection] = ranges[RangeType.Mark];
				ranges[RangeType.Mark] = new List<Range>();
			}
			else if (command == Command_Select_Find)
			{
				ranges[RangeType.Selection] = ranges[RangeType.Search];
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == Command_Select_RemoveEmpty)
				ranges[RangeType.Selection] = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
			else if (command == Command_Mark_Selection)
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection].Select(range => range.Copy()));
			else if (command == Command_Mark_Find)
			{
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Search]);
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == Command_Mark_Clear)
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
			else if (command == Command_Mark_LimitToSelection)
				ranges[RangeType.Mark] = ranges[RangeType.Mark].Where(mark => ranges[RangeType.Selection].Any(selection => (mark.Start >= selection.Start) && (mark.End <= selection.End))).ToList();
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

		const int tabStop = 4;
		readonly double charWidth;
		readonly double lineHeight;

		int numLines { get { return (int)(canvas.ActualHeight / lineHeight); } }
		int numColumns { get { return (int)(canvas.ActualWidth / charWidth); } }

		enum RangeType
		{
			Search,
			Mark,
			Selection,
		}

		class Range
		{
			public Range Copy()
			{
				return new Range { Pos1 = Pos1, Pos2 = Pos2 };
			}

			void CalcParams()
			{
				Start = Math.Min(Pos1, Pos2);
				End = Math.Max(Pos1, Pos2);
				Length = Math.Abs(Pos1 - Pos2);
			}

			int pos1, pos2;
			public int Pos1 { get { return pos1; } set { pos1 = value; CalcParams(); } }
			public int Pos2 { get { return pos2; } set { pos2 = value; CalcParams(); } }
			public int Start { get; private set; }
			public int End { get; private set; }
			public int Length { get; private set; }

			public bool HasSelection()
			{
				return Pos1 != Pos2;
			}

			public override string ToString()
			{
				return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
			}
		}

		Dictionary<RangeType, List<Range>> ranges = Helpers.GetValues<RangeType>().ToDictionary(rangeType => rangeType, rangeType => new List<Range>());
		static List<string>[] keysAndValues = new List<string>[10] { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };

		readonly Typeface typeface;
		readonly double fontSize;

		class TextCanvasUndoRedo
		{
			public List<Range> ranges { get; private set; }
			public List<string> text { get; private set; }

			public TextCanvasUndoRedo(List<Range> _ranges, List<string> _text)
			{
				ranges = _ranges;
				text = _text;
			}
		}

		List<TextCanvasUndoRedo> undo = new List<TextCanvasUndoRedo>();
		List<TextCanvasUndoRedo> redo = new List<TextCanvasUndoRedo>();

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

		Range visibleSelection;
		void EnsureVisible(Range selection)
		{
			if ((visibleSelection != null) && (visibleSelection.Pos1 == selection.Pos1) && (visibleSelection.Pos2 == selection.Pos2))
				return;

			visibleSelection = selection.Copy();
			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
			yScrollValue = Math.Min(line, Math.Max(line - numLines + 1, yScrollValue));
			var x = GetXFromLineIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x - numColumns + 1, xScrollValue));
		}

		int GetXFromLineIndex(int line, int index)
		{
			return GetColumnFromIndex(line, index);
		}

		int GetColumnFromIndex(int line, int index)
		{
			return GetColumnFromIndex(Data[line], index);
		}

		int GetColumnFromIndex(string lineStr, int findIndex)
		{
			if (findIndex < 0)
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
					find = findIndex;
				else
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
			while (column < findColumn)
			{
				var find = lineStr.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}
				if (find == -1)
					find = findColumn - column + index;
				else
					find = Math.Min(find, findColumn - column + index);

				column += find - index;
				index = find;
			}
			return index;
		}

		DispatcherTimer drawTimer = null;
		new void InvalidateVisual()
		{
			if (drawTimer != null)
				return;

			drawTimer = new DispatcherTimer();
			drawTimer.Tick += (s, e) =>
			{
				drawTimer.Stop();
				drawTimer = null;

				canvas.InvalidateVisual();
			};
			drawTimer.Start();
		}

		static Dictionary<RangeType, Brush> brushes = new Dictionary<RangeType, Brush>
		{
			{ RangeType.Selection, new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)) }, //9cc7e6
			{ RangeType.Search, new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)) }, //e2e6d6
			{ RangeType.Mark, new SolidColorBrush(Color.FromArgb(178, 242, 155, 0)) }, //f6b94d
		};
		void OnCanvasRender(DrawingContext dc)
		{
			if (Data == null)
				return;

			HasBOM = Data.BOM;
			var columns = Enumerable.Range(0, Data.NumLines).Select(lineNum => Data[lineNum]).Select(line => GetColumnFromIndex(line, line.Length)).Max();

			xScroll.ViewportSize = numColumns;
			xScroll.Maximum = columns - numColumns;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = numColumns - 1;

			yScroll.ViewportSize = numLines;
			yScroll.Maximum = Data.NumLines - numLines;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = numLines - 1;

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

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + numLines + 1);

			var startColumn = xScrollValue;
			var endColumn = Math.Min(columns + 1, startColumn + numColumns + 1);

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var lineStr = Data[line];
				var lineRange = new Range { Pos1 = Data.GetOffset(line, 0), Pos2 = Data.GetOffset(line, lineStr.Length) };
				var y = (line - startLine) * lineHeight;
				var startIndex = GetIndexFromColumn(lineStr, startColumn);
				var endIndex = GetIndexFromColumn(lineStr, endColumn);

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

						if ((start >= endIndex) || (end < startIndex))
							continue;

						start = GetColumnFromIndex(lineStr, start);
						end = GetColumnFromIndex(lineStr, end);
						if (range.End > lineRange.End)
							end++;

						start = Math.Max(0, start - startColumn);
						end = Math.Min(endColumn, end) - startColumn;
						var width = end - start;

						dc.DrawRectangle(brushes[entry.Key], null, new Rect(start * charWidth, y, width * charWidth + 1, lineHeight));
					}
				}

				foreach (var selection in ranges[RangeType.Selection])
				{
					if ((selection.Pos1 < lineRange.Start) || (selection.Pos1 > lineRange.End))
						continue;

					var selIndex = Data.GetOffsetIndex(selection.Pos1, line);
					if ((selIndex < startIndex) || (selIndex > endIndex))
						continue;
					var column = GetColumnFromIndex(lineStr, selIndex);
					dc.DrawRectangle(Brushes.Black, null, new Rect((column - startColumn) * charWidth, y, 1, lineHeight));
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
				dc.DrawText(text, new Point(0, y));
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
							yScrollValue += mult;
						else
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, mult, 0);
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
					if (controlDown)
						yScrollValue -= numLines / 2;
					else
						foreach (var selection in ranges[RangeType.Selection])
							SetPos1(selection, 1 - numLines, 0);
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += numLines / 2;
					else
						foreach (var selection in ranges[RangeType.Selection])
							SetPos1(selection, numLines - 1, 0);
					break;
				case Key.Tab:
					{
						if (!ranges[RangeType.Selection].Any(range => range.HasSelection()))
						{
							AddCanvasText("\t");
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
					AddCanvasText(Data.DefaultEnding);
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
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / lineHeight) + yScrollValue);
			var index = GetIndexFromColumn(line, (int)(mousePos.X / charWidth) + xScrollValue);

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
									last.ranges[num] = new Range { Pos1 = last.ranges[num].Start + change, Pos2 = last.ranges[num].End + current.ranges[num].Length + change };
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

		void Replace(List<Range> replaceRanges, List<string> strs, bool leaveHighlighted, ReplaceType replaceType = ReplaceType.Normal)
		{
			if (strs == null)
				strs = replaceRanges.Select(range => "").ToList();

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < replaceRanges.Count; ++ctr)
			{
				var undoRange = new Range { Pos1 = replaceRanges[ctr].Start + change, Pos2 = replaceRanges[ctr].Start + strs[ctr].Length + change };
				undoRanges.Add(undoRange);
				undoText.Add(GetString(replaceRanges[ctr]));
				change = undoRange.Pos2 - replaceRanges[ctr].End;
			}

			AddUndoRedo(new TextCanvasUndoRedo(undoRanges, undoText), replaceType);

			Data.Replace(replaceRanges.Select(range => range.Start).ToList(), replaceRanges.Select(range => range.Length).ToList(), strs);

			ranges[RangeType.Search].Clear();

			var numsToMap = ranges.SelectMany(rangePair => rangePair.Value).SelectMany(range => new int[] { range.Start, range.End }).Distinct().OrderBy(num => num).ToList();
			var oldToNewMap = new Dictionary<int, int>();
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < numsToMap.Count)
			{
				int start = Int32.MaxValue, end = Int32.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (numsToMap[current] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = numsToMap[current];
				if ((value > start) && (value < end))
					value = start + length;

				oldToNewMap[numsToMap[current]] = value + offset;
				++current;
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

		void SortRegions(List<Range> regions, List<int> ordering, bool updateRegions = false)
		{
			var selections = ranges[RangeType.Selection].Select(range => range.Copy()).ToList();
			var newRegions = regions.Select(range => range.Copy()).ToList();
			if ((selections.Count != regions.Count) || (regions.Count != ordering.Count))
				throw new Exception("Selections, regions, and ordering must match");

			var orderedRegions = regions.OrderBy(range => range.Start).ToList();
			var pos = 0;
			foreach (var range in orderedRegions)
			{
				if (range.Start < pos)
					throw new Exception("Regions cannot overlap");
				pos = range.End;
			}

			for (var ctr = 0; ctr < selections.Count; ++ctr)
			{
				if ((selections[ctr].Start < regions[ctr].Start) || (selections[ctr].End > regions[ctr].End))
					throw new Exception("Selection must be in region");
			}

			orderedRegions = ordering.Select(index => regions[index]).ToList();
			var replaceStrs = orderedRegions.Select(range => GetString(range)).ToList();

			var add = 0;
			for (var ctr = 0; ctr < selections.Count; ++ctr)
			{
				var orderCtr = ordering[ctr];
				selections[orderCtr].Pos1 = selections[orderCtr].Pos1 - regions[orderCtr].Start + regions[ctr].Start + add;
				selections[orderCtr].Pos2 = selections[orderCtr].Pos2 - regions[orderCtr].Start + regions[ctr].Start + add;

				newRegions[orderCtr].Pos1 = newRegions[orderCtr].Pos1 - regions[orderCtr].Start + regions[ctr].Start + add;
				newRegions[orderCtr].Pos2 = newRegions[orderCtr].Pos2 - regions[orderCtr].Start + regions[ctr].Start + add;

				add += replaceStrs[ctr].Length - regions[ctr].Length;
			}
			selections = ordering.Select(num => selections[num]).ToList();

			Replace(regions, replaceStrs, false);
			ranges[RangeType.Selection] = selections;
			if (updateRegions)
				ranges[RangeType.Mark] = newRegions;
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

		ICommand GetKeysValuesCommand(ICommand command)
		{
			if ((command == Command_Data_Keys_SetKeys) || (command == Command_Data_Keys_SetValues1) || (command == Command_Data_Keys_SetValues2) || (command == Command_Data_Keys_SetValues3) || (command == Command_Data_Keys_SetValues4) || (command == Command_Data_Keys_SetValues5) || (command == Command_Data_Keys_SetValues6) || (command == Command_Data_Keys_SetValues7) || (command == Command_Data_Keys_SetValues8) || (command == Command_Data_Keys_SetValues9))
				return Command_Data_Keys_SetValues1;
			if ((command == Command_Data_Keys_KeysToValues1) || (command == Command_Data_Keys_KeysToValues2) || (command == Command_Data_Keys_KeysToValues3) || (command == Command_Data_Keys_KeysToValues4) || (command == Command_Data_Keys_KeysToValues5) || (command == Command_Data_Keys_KeysToValues6) || (command == Command_Data_Keys_KeysToValues7) || (command == Command_Data_Keys_KeysToValues8) || (command == Command_Data_Keys_KeysToValues9))
				return Command_Data_Keys_KeysToValues1;
			if ((command == Command_Data_Keys_CopyKeys) || (command == Command_Data_Keys_CopyValues1) || (command == Command_Data_Keys_CopyValues2) || (command == Command_Data_Keys_CopyValues3) || (command == Command_Data_Keys_CopyValues4) || (command == Command_Data_Keys_CopyValues5) || (command == Command_Data_Keys_CopyValues6) || (command == Command_Data_Keys_CopyValues7) || (command == Command_Data_Keys_CopyValues8) || (command == Command_Data_Keys_CopyValues9))
				return Command_Data_Keys_CopyValues1;
			if ((command == Command_Data_Keys_HitsKeys) || (command == Command_Data_Keys_HitsValues1) || (command == Command_Data_Keys_HitsValues2) || (command == Command_Data_Keys_HitsValues3) || (command == Command_Data_Keys_HitsValues4) || (command == Command_Data_Keys_HitsValues5) || (command == Command_Data_Keys_HitsValues6) || (command == Command_Data_Keys_HitsValues7) || (command == Command_Data_Keys_HitsValues8) || (command == Command_Data_Keys_HitsValues9))
				return Command_Data_Keys_HitsValues1;
			if ((command == Command_Data_Keys_MissesKeys) || (command == Command_Data_Keys_MissesValues1) || (command == Command_Data_Keys_MissesValues2) || (command == Command_Data_Keys_MissesValues3) || (command == Command_Data_Keys_MissesValues4) || (command == Command_Data_Keys_MissesValues5) || (command == Command_Data_Keys_MissesValues6) || (command == Command_Data_Keys_MissesValues7) || (command == Command_Data_Keys_MissesValues8) || (command == Command_Data_Keys_MissesValues9))
				return Command_Data_Keys_MissesValues1;

			return null;
		}

		int GetKeysValuesIndex(ICommand command)
		{
			if ((command == Command_Data_Keys_SetKeys) || (command == Command_Data_Keys_CopyKeys) || (command == Command_Data_Keys_HitsKeys) || (command == Command_Data_Keys_MissesKeys))
				return 0;
			if ((command == Command_Data_Keys_SetValues1) || (command == Command_Data_Keys_KeysToValues1) || (command == Command_Data_Keys_CopyValues1) || (command == Command_Data_Keys_HitsValues1) || (command == Command_Data_Keys_MissesValues1))
				return 1;
			if ((command == Command_Data_Keys_SetValues2) || (command == Command_Data_Keys_KeysToValues2) || (command == Command_Data_Keys_CopyValues2) || (command == Command_Data_Keys_HitsValues2) || (command == Command_Data_Keys_MissesValues2))
				return 2;
			if ((command == Command_Data_Keys_SetValues3) || (command == Command_Data_Keys_KeysToValues3) || (command == Command_Data_Keys_CopyValues3) || (command == Command_Data_Keys_HitsValues3) || (command == Command_Data_Keys_MissesValues3))
				return 3;
			if ((command == Command_Data_Keys_SetValues4) || (command == Command_Data_Keys_KeysToValues4) || (command == Command_Data_Keys_CopyValues4) || (command == Command_Data_Keys_HitsValues4) || (command == Command_Data_Keys_MissesValues4))
				return 4;
			if ((command == Command_Data_Keys_SetValues5) || (command == Command_Data_Keys_KeysToValues5) || (command == Command_Data_Keys_CopyValues5) || (command == Command_Data_Keys_HitsValues5) || (command == Command_Data_Keys_MissesValues5))
				return 5;
			if ((command == Command_Data_Keys_SetValues6) || (command == Command_Data_Keys_KeysToValues6) || (command == Command_Data_Keys_CopyValues6) || (command == Command_Data_Keys_HitsValues6) || (command == Command_Data_Keys_MissesValues6))
				return 6;
			if ((command == Command_Data_Keys_SetValues7) || (command == Command_Data_Keys_KeysToValues7) || (command == Command_Data_Keys_CopyValues7) || (command == Command_Data_Keys_HitsValues7) || (command == Command_Data_Keys_MissesValues7))
				return 7;
			if ((command == Command_Data_Keys_SetValues8) || (command == Command_Data_Keys_KeysToValues8) || (command == Command_Data_Keys_CopyValues8) || (command == Command_Data_Keys_HitsValues8) || (command == Command_Data_Keys_MissesValues8))
				return 8;
			if ((command == Command_Data_Keys_SetValues9) || (command == Command_Data_Keys_KeysToValues9) || (command == Command_Data_Keys_CopyValues9) || (command == Command_Data_Keys_HitsValues9) || (command == Command_Data_Keys_MissesValues9))
				return 9;
			throw new Exception("Invalid keys/values command");
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

			Replace(ranges[RangeType.Selection], ranges[RangeType.Selection].Select(range => text).ToList(), false);
		}
	}
}
