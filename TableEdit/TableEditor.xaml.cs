using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Tables;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TableEdit.Dialogs;

namespace NeoEdit.TableEdit
{
	public class TabsControl : TabsControl<TableEditor, TableEditCommand> { }

	partial class TableEditor
	{
		[DepProp]
		public string FileName { get { return UIHelper<TableEditor>.GetPropValue<string>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TableEditor>.GetPropValue<bool>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public double xScrollValue { get { return UIHelper<TableEditor>.GetPropValue<double>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TableEditor>.GetPropValue<int>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? SelectedRow { get { return UIHelper<TableEditor>.GetPropValue<int?>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? SelectedColumn { get { return UIHelper<TableEditor>.GetPropValue<int?>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? SelectedCells { get { return UIHelper<TableEditor>.GetPropValue<int?>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? SelectedRows { get { return UIHelper<TableEditor>.GetPropValue<int?>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? SelectedColumns { get { return UIHelper<TableEditor>.GetPropValue<int?>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int ClipboardCount { get { return UIHelper<TableEditor>.GetPropValue<int>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }

		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize) - HeaderRows;
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize) - HeaderRows;

		Table table;

		class ColumnWidth
		{
			List<double> widths = new List<double>();
			public double this[int column]
			{
				get
				{
					if ((column < 0) || (column >= widths.Count))
						return 0;
					return widths[column];
				}
				set
				{
					while (widths.Count <= column)
						widths.Add(column);
					widths[column] = value;
				}
			}
		}
		ColumnWidth columnWidth = new ColumnWidth();

		string AESKey = null;

		readonly ObservableCollectionEx<CellRange> Selections = new ObservableCollectionEx<CellRange>();
		readonly ObservableCollectionEx<Cell> Searches = new ObservableCollectionEx<Cell>();

		readonly UndoRedo undoRedo;
		readonly RunOnceTimer canvasRenderTimer;

		static TableEditor()
		{
			UIHelper<TableEditor>.Register();
			UIHelper<TableEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TableEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TableEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.canvasRenderTimer.Start());
			UIHelper<TableEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.canvasRenderTimer.Start());
		}

		public TableEditor(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, DbDataReader reader = null, bool? modified = null)
		{
			InitializeComponent();
			NEClipboard.ClipboardChanged += () => ClipboardCount = NEClipboard.Objects.Count;
			undoRedo = new UndoRedo(b => IsModified = b);
			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			OpenFile(fileName, bytes, codePage, reader, modified);
			undoRedo.Clear();

			Selections.CollectionChanged += (s, e) =>
			{
				if (Selections.Any())
				{
					SelectedRow = Selections.Last().End.Row + 1;
					SelectedColumn = Selections.Last().End.Column + 1;
					SelectedCells = Selections.EnumerateCells().Distinct().Count();
					SelectedRows = Selections.EnumerateRows().Distinct().Count();
					SelectedColumns = Selections.EnumerateColumns().Distinct().Count();
				}
				else
					SelectedRow = SelectedColumn = SelectedCells = SelectedRows = SelectedColumns = null;

				MakeActiveVisible();
				canvasRenderTimer.Start();
			};
			Searches.CollectionChanged += (s, e) => canvasRenderTimer.Start();
			SetupTabLabel();

			SetHome();
		}

		DateTime fileLastWrite;
		void OpenFile(string fileName, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, DbDataReader reader = null, bool? modified = null)
		{
			FileName = fileName;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
				bytes = new byte[0];
			if ((bytes.Length == 0) && (fileName != null))
				bytes = File.ReadAllBytes(fileName);

			string aesKey;
			FileEncryptor.HandleDecrypt(ref bytes, out aesKey);
			AESKey = aesKey;

			var text = Coder.BytesToString(bytes, codePage, true);
			var table = !String.IsNullOrEmpty(text) ? new Table(text) : reader != null ? new Table(reader) : new Table();

			ReplaceTable(table);

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			undoRedo.SetModified(isModified);
		}

		CellRange MoveSelection(CellRange range, int row, int column, bool selecting, bool rowRel = true, bool columnRel = true)
		{
			if (rowRel)
				row += range.End.Row;
			if (columnRel)
				column += range.End.Column;
			row = Math.Max(0, Math.Min(row, table.NumRows - 1));
			column = Math.Max(0, Math.Min(column, table.NumColumns - 1));
			var location = new Cell(row, column);
			return new CellRange(range, selecting ? default(Cell?) : location, location);
		}

		void MoveSelections(int row, int column, bool selecting, bool disjoint, bool rowRel = true, bool columnRel = true)
		{
			if (!disjoint)
			{
				// Make inactive active
				if ((Selections.Count != 0) && (!Selections[Selections.Count - 1].Active))
					Selections[Selections.Count - 1] = new CellRange(Selections[Selections.Count - 1], active: true);

				Selections.Replace(selection => MoveSelection(selection, row, column, selecting, rowRel, columnRel));
				return;
			}

			if ((selecting) && (!Selections[Selections.Count - 1].Active))
				Selections[Selections.Count - 1] = new CellRange(Selections[Selections.Count - 1], active: true);
			if ((!selecting) && (Selections[Selections.Count - 1].Active))
				Selections.Add(new CellRange(Selections[Selections.Count - 1], active: false));

			Selections[Selections.Count - 1] = MoveSelection(Selections[Selections.Count - 1], row, column, selecting, rowRel, columnRel);
		}

		void MakeActiveVisible()
		{
			if (!Selections.Any())
				return;

			var cursor = Selections.Last().End;
			var column = Math.Min(cursor.Column, table.NumColumns - 1);
			var row = Math.Min(cursor.Row, table.NumRows - 1);

			if ((column < 0) || (row < 0))
				return;

			var xStart = Enumerable.Range(0, column).Sum(column2 => columnWidth[column2]);
			var xEnd = xStart + columnWidth[column];
			xScrollValue = Math.Min(xStart, Math.Max(xEnd - xScroll.ViewportSize, xScrollValue));
			yScrollValue = Math.Min(row, Math.Max(row - yScrollViewportFloor + 1, yScrollValue));
		}

		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			if ((e.Text.Length != 1) || (!Char.IsControl(e.Text[0])))
				StartEdit(true);
			base.OnPreviewTextInput(e);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = true;

			if (IsEditing)
			{
				switch (key)
				{
					case Key.Escape: EndEdit(false); break;
					case Key.Enter: EndEdit(true); break;
					default: result = false; break;
				}
			}
			else
			{
				switch (key)
				{
					case Key.Up:
						if (controlDown)
							MoveSelections(0, 0, shiftDown, altDown, rowRel: false);
						else
							MoveSelections(-1, 0, shiftDown, altDown);
						break;
					case Key.Down:
						if (controlDown)
							MoveSelections(int.MaxValue, 0, shiftDown, altDown, rowRel: false);
						else
							MoveSelections(1, 0, shiftDown, altDown);
						break;
					case Key.Left:
						if (controlDown)
							MoveSelections(0, 0, shiftDown, altDown, columnRel: false);
						else
							MoveSelections(0, -1, shiftDown, altDown);
						break;
					case Key.Right:
						if (controlDown)
							MoveSelections(0, int.MaxValue, shiftDown, altDown, columnRel: false);
						else
							MoveSelections(0, 1, shiftDown, altDown);
						break;
					case Key.Home:
						if (!controlDown)
							MoveSelections(0, 0, shiftDown, altDown, columnRel: false);
						else if ((shiftDown) || (altDown))
							MoveSelections(0, 0, shiftDown, altDown, false, false);
						else
							Selections.Replace(new CellRange());
						break;
					case Key.End:
						if (!controlDown)
							MoveSelections(0, int.MaxValue, shiftDown, altDown, columnRel: false);
						else if ((shiftDown) || (altDown))
							MoveSelections(int.MaxValue, int.MaxValue, shiftDown, altDown, false, false);
						else
							Selections.Replace(new CellRange(table.NumRows - 1, table.NumColumns - 1));
						break;
					case Key.PageUp: MoveSelections(1 - yScrollViewportFloor, 0, shiftDown, altDown); break;
					case Key.PageDown: MoveSelections(yScrollViewportFloor - 1, 0, shiftDown, altDown); break;
					case Key.Space:
						bool allRows = false, allColumns = false;
						if (shiftDown)
							allRows = true;
						if (controlDown)
							allColumns = true;
						if ((allColumns) || (allRows))
							Selections.Replace(range => new CellRange(allColumns ? 0 : range.StartRow, allRows ? 0 : range.StartColumn, allColumns ? table.NumRows - 1 : range.EndRow, allRows ? table.NumColumns - 1 : range.EndColumn));
						else if ((!altDown) || (Selections.Count == 0))
							result = false;
						else if (Selections.Last().Active)
							Selections[Selections.Count - 1] = new CellRange(Selections[Selections.Count - 1].End, active: false);
						else
						{
							var cursor = Selections[Selections.Count - 1].End;
							var cellRange = Selections.LastOrDefault(range => (range.Active) && (range.Contains(cursor)));
							if (cellRange != null)
								Selections.Remove(cellRange);
							else
								Selections[Selections.Count - 1] = new CellRange(Selections[Selections.Count - 1].End);
						}
						break;
					case Key.F2: StartEdit(false); break;
					case Key.Insert:
						if (shiftDown)
							InsertRows(altDown);
						else if (controlDown)
							InsertColumns(altDown);
						break;
					case Key.Delete:
						if (shiftDown)
							DeleteRows();
						else if (controlDown)
							DeleteColumns();
						else
							ReplaceCells(Selections, null);
						break;
					default: result = false; break;
				}
			}
			return result;
		}

		public bool IsEditing { get; private set; }
		public void StartEdit(bool empty)
		{
			if ((IsEditing) || (!Selections.Any()))
				return;

			IsEditing = true;
			var cell = Selections.Last().End;

			var x = Enumerable.Range(0, cell.Column).Sum(column => columnWidth[column]) - xScrollValue;
			var y = (cell.Row + HeaderRows - yScrollValue) * rowHeight;
			var tb = new TextBox
			{
				Text = empty ? "" : (table[cell] ?? "").ToString(),
				FontFamily = Font.FontFamily,
				FontSize = Font.Size,
				MinWidth = columnWidth[cell.Column],
				Height = rowHeight,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(ColumnSpacing, RowTopSpacing, ColumnSpacing, RowTopSpacing),
				Margin = new Thickness(0),
			};
			Canvas.SetLeft(tb, x);
			Canvas.SetTop(tb, y);
			canvas.Children.Add(tb);
			tb.Focus();
		}

		public string EndEdit(bool success)
		{
			if (!IsEditing)
				return null;

			var result = canvas.Children.Cast<TextBox>().First().Text;
			IsEditing = false;
			canvas.Children.Clear();
			Focus();

			if (!success)
				return null;

			return result;
		}

		public bool HandleText(string value)
		{
			ReplaceCells(Selections, defaultValue: value);
			return true;
		}

		const double RowTopSpacing = 1;
		const double RowBottomSpacing = 3;
		const double ColumnSpacing = 8;
		const int HeaderRows = 1;
		readonly static double rowHeight = Font.lineHeight + RowTopSpacing + RowBottomSpacing;
		void OnCanvasRender(object s, DrawingContext dc)
		{
			if ((table == null) || (canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			Selections.RemoveDups();
			Searches.RemoveDups();
			canvasRenderTimer.Stop();

			for (var ctr = 0; ctr < table.Headers.Count; ++ctr)
				if (columnWidth[ctr] == 0)
					columnWidth[ctr] = Font.GetText(table.Headers[ctr]).Width + ColumnSpacing * 2;

			xScroll.Minimum = 0;
			xScroll.ViewportSize = xScroll.LargeChange = canvas.ActualWidth;
			xScroll.SmallChange = xScroll.LargeChange / 4;

			yScroll.ViewportSize = canvas.ActualHeight / rowHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = table.NumRows - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);

			if ((xScroll.ViewportSize <= 0) || (yScrollViewportCeiling <= 0))
				return;

			var startRow = yScrollValue;
			var endRow = Math.Min(table.NumRows, startRow + yScrollViewportCeiling);
			var numRows = endRow - startRow + HeaderRows;
			var y = Enumerable.Range(0, numRows + 1).ToDictionary(row => row, row => row * rowHeight);
			var active = Selections.Select(range => range.End).ToList();

			double xOffset = -xScrollValue;
			for (var column = 0; column < table.NumColumns; ++column)
			{
				if (xOffset > canvas.ActualWidth)
					break;

				var header = table.Headers[column];
				var xStart = xOffset;
				xOffset += columnWidth[column];
				if (xOffset < 0)
					continue;

				var text = new FormattedText[numRows];
				var alignment = new HorizontalAlignment[numRows];
				var background = new Brush[numRows];
				var isActive = new bool[numRows];

				text[0] = Font.GetText(header);
				alignment[0] = HorizontalAlignment.Center;
				background[0] = Misc.headersBrush;

				for (var row = startRow; row < endRow; ++row)
				{
					var value = table[row, column];
					var display = value == null ? "<NULL>" : value.ToString();
					var item = text[row - startRow + HeaderRows] = Font.GetText(display);
					alignment[row - startRow + HeaderRows] = HorizontalAlignment.Left;
					if (value == null)
					{
						item.SetForegroundBrush(Misc.nullBrush);
						alignment[row - startRow + HeaderRows] = HorizontalAlignment.Center;
					}
					if (Selections.Any(range => range.Contains(row, column)))
						background[row - startRow + HeaderRows] = Misc.selectedBrush;
					isActive[row - startRow + HeaderRows] = active.Any(cell => cell.Equals(row, column));
				}

				columnWidth[column] = Math.Max(columnWidth[column], text.Max(formattedText => formattedText.Width) + ColumnSpacing * 2);
				xOffset = xStart + columnWidth[column];

				for (var row = 0; row < numRows; ++row)
				{
					var rect = new Rect(xStart, y[row], columnWidth[column], rowHeight);
					dc.DrawRectangle(background[row], Misc.linesPen, rect);
					if (isActive[row])
						dc.DrawRectangle(null, Misc.activePen, rect);

					double position;
					switch (alignment[row])
					{
						case HorizontalAlignment.Left: position = xStart + ColumnSpacing; break;
						case HorizontalAlignment.Center: position = (xStart + xOffset - text[row].Width) / 2; break;
						case HorizontalAlignment.Right: position = xOffset - ColumnSpacing - text[row].Width; break;
						default: throw new NotImplementedException();
					}
					dc.DrawText(text[row], new Point(position, y[row] + RowTopSpacing));
				}

			}

			xScroll.Maximum = Enumerable.Range(0, table.NumColumns).Select(column => columnWidth[column]).Sum() - xScroll.ViewportSize;
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] == null?""[Untitled]"":FileName([0]))+([1]?""*"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			SetBinding(UIHelper<TabsControl<TableEditor, TableEditCommand>>.GetProperty(a => a.TabLabel), multiBinding);
		}

		public override bool CanClose(ref Message.OptionsEnum answer)
		{
			if (!IsModified)
				return true;

			if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
				answer = new Message
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

		Table.TableTypeEnum GetFileTableType(string fileName)
		{
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".tsv": return Table.TableTypeEnum.TSV;
				case ".csv": return Table.TableTypeEnum.CSV;
				case ".txt": return Table.TableTypeEnum.Columns;
				default: return Table.TableTypeEnum.TSV;
			}
		}

		string GetSaveFileName()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "TSV files|*.tsv|CSV files|*.csv|Text files|*.txt",
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
				FilterIndex = (int)GetFileTableType(FileName),
			};
			if (dialog.ShowDialog() != true)
				return null;

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist");
			return dialog.FileName;
		}

		void Save(string fileName)
		{
			var data = table.ConvertToString("\r\n", GetFileTableType(fileName));
			var bytes = Coder.StringToBytes(data, Coder.CodePage.UTF8, true);
			bytes = FileEncryptor.Encrypt(bytes, AESKey);
			File.WriteAllBytes(fileName, bytes);
			fileLastWrite = new FileInfo(fileName).LastWriteTime;
			undoRedo.SetModified(false);
			FileName = fileName;
		}

		internal Dictionary<string, List<object>> GetExpressionData(int? count = null, NEExpression expression = null)
		{
			var sels = GetSelectedCells();

			var parallelDataActions = new Dictionary<HashSet<string>, Action<HashSet<string>, Action<string, List<object>>>>();
			parallelDataActions.Add(new HashSet<string> { "x" }, (items, addData) => addData("x", sels.Select(cell => table[cell]).ToList()));
			parallelDataActions.Add(new HashSet<string> { "y" }, (items, addData) => addData("y", sels.Select((cell, index) => (object)(index + 1)).ToList()));
			parallelDataActions.Add(new HashSet<string> { "z" }, (items, addData) => addData("z", sels.Select((cell, index) => (object)index).ToList()));
			parallelDataActions.Add(new HashSet<string> { "row" }, (items, addData) => addData("row", sels.Select(cell => (object)cell.Row).ToList()));
			parallelDataActions.Add(new HashSet<string> { "column" }, (items, addData) => addData("column", sels.Select(cell => (object)cell.Row).ToList()));
			for (var ctr = 0; ctr < table.Headers.Count; ++ctr)
			{
				var num = ctr; // If we don't copy this the threads get the wrong value
				var columnName = table.Headers[num];
				var columnNameLen = $"{columnName}l";
				var columnNum = $"c{ctr + 1}";
				var columnNumLen = $"{columnNum}l";
				parallelDataActions.Add(new HashSet<string> { columnName, columnNum }, (items, addData) =>
				{
					var columnData = sels.Select(cell => table[cell.Row, num]).ToList();
					addData(columnName, columnData);
					addData(columnNum, columnData);
				});
				parallelDataActions.Add(new HashSet<string> { columnNameLen, columnNumLen }, (items, addData) =>
				{
					var columnLens = sels.Select(cell => (object)(table[cell.Row, num] ?? "").ToString().Length).ToList();
					addData(columnNameLen, columnLens);
					addData(columnNumLen, columnLens);
				});
			}

			var used = expression != null ? expression.Variables : new HashSet<string>(parallelDataActions.SelectMany(action => action.Key));
			var data = new Dictionary<string, List<object>>();
			Parallel.ForEach(parallelDataActions, pair =>
			{
				if (pair.Key.Any(key => used.Contains(key)))
					pair.Value(used, (key, value) =>
					{
						lock (data)
							data[key] = value;
					});
			});

			return data;
		}

		CellRange AllCells() => new CellRange(endRow: table.NumRows - 1, endColumn: table.NumColumns - 1);

		void RunSearch(FindTextDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			var cells = result.SelectionOnly ? Selections.EnumerateCells() : AllCells().EnumerateCells();
			Searches.Replace(cells.Where(cell => result.Regex.IsMatch(table.GetString(cell))));
		}

		void FindNext(bool forward)
		{
			if (Searches.Count == 0)
			{
				Selections.Clear();
				return;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(cell => cell > Selections[ctr].End);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(cell => cell < Selections[ctr].Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				Selections[ctr] = new CellRange(Searches[index]);
			}
		}

		void Command_File_OpenWith_Disk() => Launcher.Static.LaunchDisk(FileName);

		void Command_File_OpenWith_HexEditor()
		{
			var data = table.ConvertToString("\r\n", Table.TableTypeEnum.TSV);
			var codePage = Coder.CodePage.UTF8;
			var bytes = Coder.StringToBytes(data, codePage, true);
			Launcher.Static.LaunchHexEditor(FileName, bytes, codePage, IsModified);
			WindowParent.Remove(this, true);
		}

		void Command_File_OpenWith_TextEditor()
		{
			var data = table.ConvertToString("\r\n", Table.TableTypeEnum.TSV);
			var codePage = Coder.CodePage.UTF8;
			var bytes = Coder.StringToBytes(data, codePage, true);
			Launcher.Static.LaunchTextEditor(FileName, bytes, codePage, IsModified);
			WindowParent.Remove(this, true);
		}

		void Command_File_Save_Save()
		{
			if (FileName == null)
				Command_File_Save_SaveAs();
			else
				Save(FileName);
		}

		void Command_File_Save_SaveAs()
		{
			var fileName = GetSaveFileName();
			if (fileName != null)
				Save(fileName);
		}

		void Command_File_Operations_Rename()
		{
			if (String.IsNullOrEmpty(FileName))
			{
				Command_File_Save_SaveAs();
				return;
			}

			var fileName = GetSaveFileName();
			if (fileName == null)
				return;

			File.Delete(fileName);
			File.Move(FileName, fileName);
			FileName = fileName;
		}

		void Command_File_Operations_Delete()
		{
			if (FileName == null)
				throw new Exception("No filename.");

			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete this file?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			File.Delete(FileName);
		}

		void Command_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Command_File_Refresh()
		{
			if (String.IsNullOrEmpty(FileName))
				return;
			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "This file has been updated on disk.  Reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() == Message.OptionsEnum.Yes)
					Command_File_Revert();
			}
		}

		void Command_File_Revert()
		{
			if (IsModified)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			OpenFile(FileName);
			canvasRenderTimer.Start();
		}

		void Command_File_Copy_Path() => NEClipboard.CopiedFile = FileName;

		void Command_File_Copy_Name() => NEClipboard.Text = Path.GetFileName(FileName);

		string Command_File_Encryption_Dialog() => FileEncryptor.GetKey(WindowParent);

		void Command_File_Encryption(string result)
		{
			if (result == null)
				return;
			AESKey = result == "" ? null : result;
		}

		List<Cell> GetSelectedCells(bool preserveOrder = false) => Selections.EnumerateCells(preserveOrder).ToList();

		void SetHome() => Selections.Replace(new CellRange(0, 0));

		void Command_Edit_UndoRedo(ReplaceType replaceType)
		{
			var undoRedoStep = replaceType == ReplaceType.Undo ? undoRedo.GetUndo() : undoRedo.GetRedo();
			if (undoRedoStep == null)
				return;

			Replace(undoRedoStep, replaceType);
		}

		void Command_Edit_Copy_CopyCut(bool cut, bool headers)
		{
			var cells = GetSelectedCells();
			var values = cells.Select(cell => table[cell]).ToList();
			var data = table.GetTableData(Selections);
			NEClipboard.SetObjects(values, data);
			if (cut)
				ReplaceCells(cells, null);

		}

		void Command_Edit_Paste_Paste(bool headers)
		{
			var cells = GetSelectedCells();
			var clipboardValues = NEClipboard.Objects;
			if (cells.Count % clipboardValues.Count != 0)
				throw new Exception("Cells and clipboard counts must be divisible");

			var values = new List<object>();
			while (values.Count < cells.Count)
				values.AddRange(clipboardValues);

			if (values.All(value => (value == null) || (value is string)))
				values = values.Select(value => Table.GetValue((string)value)).ToList();

			ReplaceCells(cells, values);
		}

		FindTextDialog.Result Command_Edit_Find_FindReplace_Dialog(bool isReplace)
		{
			string text = null;
			var selectionOnly = (Selections.Count > 1) || (Selections.Any(range => range.NumCells > 1));

			if ((Selections.Count == 1) && (Selections[0].NumCells == 1))
				text = table.GetString(Selections[0].EnumerateCells().First());

			return FindTextDialog.Run(WindowParent, isReplace ? FindTextDialog.FindTextType.Replace : FindTextDialog.FindTextType.Selections, text, selectionOnly);
		}

		void Command_Edit_Find_FindReplace(bool replace, FindTextDialog.Result result)
		{
			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				Selections.Replace(Selections.EnumerateCells().Where(cell => result.Regex.IsMatch(table.GetString(cell)) == result.KeepMatching));
				return;
			}

			RunSearch(result);

			if ((replace) || (result.ResultType == FindTextDialog.GetRegExResultType.All))
			{
				Selections.Replace(Searches);
				Searches.Clear();

				if (replace)
				{
					var cells = Selections.EnumerateCells().ToList();
					var values = cells.Select(cell => result.Regex.Replace(table.GetString(cell), result.Replace) as object).ToList();
					ReplaceCells(cells, values);
				}

				return;
			}

			FindNext(true);
		}

		void Command_Edit_Find_NextPrevious(bool next) => FindNext(next);

		void Command_Edit_Sort()
		{
			var columns = Selections.EnumerateColumns(true).ToList();
			if (!columns.Any())
				throw new ArgumentException("No columns selected.");
			var sortOrder = table.GetSortOrder(columns);
			if (!sortOrder.Any())
				return;

			Sort(sortOrder);

			Selections.Replace(columns.Select(column => new CellRange(0, column, table.NumRows - 1)));
		}

		GroupDialog.Result Command_Edit_Group_Dialog()
		{
			var columns = Selections.EnumerateColumns().ToList();
			if (!columns.Any())
				throw new ArgumentException("No columns selected.");

			return GroupDialog.Run(WindowParent, table, columns);
		}

		void Command_Edit_Group(GroupDialog.Result result)
		{
			var columns = Selections.EnumerateColumns().ToList();
			if (!columns.Any())
				throw new ArgumentException("No columns selected.");

			ReplaceTable(table.Aggregate(result.AggregateColumns, result.AggregateData));
		}

		static Table joinSourceTable = null;
		static List<int> joinSourceColumns = null;
		JoinDialog.Result Command_Edit_Join_Dialog()
		{
			if ((joinSourceTable == null) || (joinSourceColumns == null))
				throw new ArgumentException("Must set join source first");
			return JoinDialog.Run(WindowParent);
		}

		void Command_Edit_Join(JoinDialog.Result result)
		{
			if ((joinSourceTable == null) || (joinSourceColumns == null))
				throw new ArgumentException("Must set join source first");
			ReplaceTable(Table.Join(table, joinSourceTable, Selections.EnumerateColumns(true).ToList(), joinSourceColumns, result.Type));
		}

		void Command_Edit_SetJoinSource()
		{
			joinSourceTable = table;
			joinSourceColumns = Selections.EnumerateColumns(true).ToList();
		}

		EditHeaderDialog.Result Command_Edit_Header_Dialog()
		{
			if ((Selections.Count != 1) || (Selections[0].NumColumns != 1))
				throw new Exception("Must have single column selected");
			var column = Selections[0].MinColumn;
			return EditHeaderDialog.Run(WindowParent, table.Headers[column]);
		}

		void Command_Edit_Header(EditHeaderDialog.Result result)
		{
			if ((Selections.Count != 1) || (Selections[0].NumColumns != 1))
				throw new Exception("Must have single column selected");
			var column = Selections[0].MinColumn;
			RenameHeader(column, result.Name);
		}

		GetExpressionDialog.Result Command_Edit_Expression_Dialog() => GetExpressionDialog.Run(WindowParent, GetExpressionData(10));

		List<T> GetExpressionResults<T>(string expression, bool resizeToSelections = true, bool matchToSelections = true)
		{
			var selectedCells = GetSelectedCells();
			var neExpression = new NEExpression(expression);
			var results = neExpression.Evaluate<T>(GetExpressionData(expression: neExpression));
			if ((resizeToSelections) && (results.Count == 1))
				results = results.Expand(selectedCells.Count, results[0]).ToList();
			if ((matchToSelections) && (results.Count != selectedCells.Count))
				throw new Exception("Expression count doesn't match selection count");
			return results;
		}

		void Command_Edit_Expression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<object>(result.Expression);
			ReplaceCells(Selections, results);
		}

		GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog() => GetExpressionDialog.Run(WindowParent, GetExpressionData(10));

		void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<bool>(result.Expression);
			Selections.Replace(GetSelectedCells().Where((str, num) => results[num]));
		}

		void Command_Select_All() => Selections.Replace(AllCells());

		void Command_Select_Cells() => Selections.Replace(GetSelectedCells());

		void Command_Select_NullNotNull(bool isNull) => Selections.Replace(GetSelectedCells().Where(cell => (table[cell] == null) == isNull));

		void Command_Select_UniqueDuplicates(bool unique)
		{
			var found = new HashSet<ItemSet<object>>();
			Selections.Limit(range =>
			{
				var value = range.EnumerateCells().Select(cell => table[cell]).ToItemSet();
				var contains = found.Contains(value);
				if (!contains)
					found.Add(value);
				return contains != unique;
			});
		}

		internal bool GetDialogResult(TableEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TableEditCommand.File_Encryption: dialogResult = Command_File_Encryption_Dialog(); break;
				case TableEditCommand.Edit_Find_Find: dialogResult = Command_Edit_Find_FindReplace_Dialog(false); break;
				case TableEditCommand.Edit_Find_Replace: dialogResult = Command_Edit_Find_FindReplace_Dialog(true); break;
				case TableEditCommand.Edit_Group: dialogResult = Command_Edit_Group_Dialog(); break;
				case TableEditCommand.Edit_Join: dialogResult = Command_Edit_Join_Dialog(); break;
				case TableEditCommand.Edit_Header: dialogResult = Command_Edit_Header_Dialog(); break;
				case TableEditCommand.Expression_Expression: dialogResult = Command_Edit_Expression_Dialog(); break;
				case TableEditCommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool timeNext = false;
		internal void HandleCommand(TableEditCommand command, bool shiftDown, object dialogResult)
		{
			var start = DateTime.UtcNow;

			switch (command)
			{
				case TableEditCommand.File_OpenWith_Disk: Command_File_OpenWith_Disk(); break;
				case TableEditCommand.File_OpenWith_HexEditor: Command_File_OpenWith_HexEditor(); break;
				case TableEditCommand.File_OpenWith_TextEditor: Command_File_OpenWith_TextEditor(); break;
				case TableEditCommand.File_Save_Save: Command_File_Save_Save(); break;
				case TableEditCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case TableEditCommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case TableEditCommand.File_Operations_Delete: Command_File_Operations_Delete(); break;
				case TableEditCommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case TableEditCommand.File_Close: if (CanClose()) TabsParent.Remove(this); break;
				case TableEditCommand.File_Refresh: Command_File_Refresh(); break;
				case TableEditCommand.File_Revert: Command_File_Revert(); break;
				case TableEditCommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case TableEditCommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case TableEditCommand.File_Encryption: Command_File_Encryption(dialogResult as string); break;
				case TableEditCommand.Edit_Undo: Command_Edit_UndoRedo(ReplaceType.Undo); break;
				case TableEditCommand.Edit_Redo: Command_Edit_UndoRedo(ReplaceType.Redo); break;
				case TableEditCommand.Edit_Copy_Copy: Command_Edit_Copy_CopyCut(false, false); break;
				case TableEditCommand.Edit_Copy_CopyWithHeaders: Command_Edit_Copy_CopyCut(false, true); break;
				case TableEditCommand.Edit_Copy_Cut: Command_Edit_Copy_CopyCut(true, false); break;
				case TableEditCommand.Edit_Copy_CutWithHeaders: Command_Edit_Copy_CopyCut(true, true); break;
				case TableEditCommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(true); break;
				case TableEditCommand.Edit_Paste_PasteWithoutHeaders: Command_Edit_Paste_Paste(false); break;
				case TableEditCommand.Edit_Find_Find: Command_Edit_Find_FindReplace(false, dialogResult as FindTextDialog.Result); break;
				case TableEditCommand.Edit_Find_Next: Command_Edit_Find_NextPrevious(true); break;
				case TableEditCommand.Edit_Find_Previous: Command_Edit_Find_NextPrevious(false); break;
				case TableEditCommand.Edit_Find_Replace: Command_Edit_Find_FindReplace(true, dialogResult as FindTextDialog.Result); break;
				case TableEditCommand.Edit_Sort: Command_Edit_Sort(); break;
				case TableEditCommand.Edit_Group: Command_Edit_Group(dialogResult as GroupDialog.Result); break;
				case TableEditCommand.Edit_Join: Command_Edit_Join(dialogResult as JoinDialog.Result); break;
				case TableEditCommand.Edit_SetJoinSource: Command_Edit_SetJoinSource(); break;
				case TableEditCommand.Edit_Header: Command_Edit_Header(dialogResult as EditHeaderDialog.Result); break;
				case TableEditCommand.Expression_Expression: Command_Edit_Expression(dialogResult as GetExpressionDialog.Result); break;
				case TableEditCommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TableEditCommand.Select_All: Command_Select_All(); break;
				case TableEditCommand.Select_Cells: Command_Select_Cells(); break;
				case TableEditCommand.Select_Null: Command_Select_NullNotNull(true); break;
				case TableEditCommand.Select_NonNull: Command_Select_NullNotNull(false); break;
				case TableEditCommand.Select_Unique: Command_Select_UniqueDuplicates(true); break;
				case TableEditCommand.Select_Duplicates: Command_Select_UniqueDuplicates(false); break;
				case TableEditCommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != TableEditCommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message
				{
					Title = "Timer",
					Text = $"Elapsed time: {elapsed:n} ms",
					Options = Message.OptionsEnum.Ok,
				}.Show();
			}
		}

		public override bool Empty() => (FileName == null) && (!IsModified) && (!table.Headers.Any());

		List<object> GetValues(IEnumerable<Cell> cells) => cells.Select(cell => table[cell]).ToList();

		protected bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		protected bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		protected bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

		List<int> GetListReverse(List<int> list)
		{
			var sortOrderDict = new Dictionary<int, int>();
			for (var ctr = 0; ctr < list.Count; ++ctr)
				sortOrderDict[list[ctr]] = ctr;
			var reverse = Enumerable.Range(0, list.Count).Select(index => sortOrderDict[index]).ToList();
			return reverse;
		}

		List<int> DeleteToInsert(List<int> values) => values.Select((value, index) => value - index).ToList();
		List<int> InsertToDelete(List<int> values) => values.Select((value, index) => value + index).ToList();

		UndoRedoStep GetUndoStep(UndoRedoStep step)
		{
			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: return UndoRedoStep.CreateChangeCells(step.Cells, step.Cells.Select(cell => table[cell]).ToList());
				case UndoRedoAction.Sort: return UndoRedoStep.CreateSort(GetListReverse(step.Positions));
				case UndoRedoAction.DeleteRows: return UndoRedoStep.CreateInsertRows(DeleteToInsert(step.Positions), table.GetRowData(step.Positions));
				case UndoRedoAction.InsertRows: return UndoRedoStep.CreateDeleteRows(InsertToDelete(step.Positions));
				case UndoRedoAction.DeleteColumns: return UndoRedoStep.CreateInsertColumns(DeleteToInsert(step.Positions), step.Positions.Select(index => table.Headers[index]).ToList(), table.GetColumnData(step.Positions));
				case UndoRedoAction.InsertColumns: return UndoRedoStep.CreateDeleteColumns(InsertToDelete(step.Positions));
				case UndoRedoAction.RenameHeader: return UndoRedoStep.CreateRenameHeader(step.Positions.Single(), table.Headers[step.Positions.Single()]);
				case UndoRedoAction.ChangeTable: return UndoRedoStep.CreateChangeTable(table);
				default: throw new NotImplementedException();
			}
		}

		void Replace(UndoRedoStep step, ReplaceType replaceType = ReplaceType.Normal)
		{
			var undoStep = GetUndoStep(step);

			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(undoStep); break;
				case ReplaceType.Redo: undoRedo.AddRedone(undoStep); break;
				case ReplaceType.Normal: undoRedo.AddUndo(undoStep); break;
			}

			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: table.ChangeCells(step.Cells, step.Values); break;
				case UndoRedoAction.Sort: table.Sort(step.Positions); break;
				case UndoRedoAction.DeleteRows: table.DeleteRows(step.Positions); break;
				case UndoRedoAction.InsertRows: table.InsertRows(step.Positions, step.InsertData, true); break;
				case UndoRedoAction.DeleteColumns: table.DeleteColumns(step.Positions); break;
				case UndoRedoAction.InsertColumns: table.InsertColumns(step.Positions, step.Headers, step.InsertData, true); break;
				case UndoRedoAction.RenameHeader: table.RenameHeader(step.Positions[0], step.Headers[0] as string); break;
				case UndoRedoAction.ChangeTable: table = step.Table; break;
			}

			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: Selections.Replace(step.Cells); break;
				case UndoRedoAction.InsertRows: Selections.Replace(step.Positions.Select((row, index) => new CellRange(row + index, 0, endColumn: table.NumColumns - 1))); break;
				case UndoRedoAction.InsertColumns: Selections.Replace(step.Positions.Select((column, index) => new CellRange(0, column + index, endRow: table.NumRows - 1))); break;
				case UndoRedoAction.ChangeTable: SetHome(); break;
				default: SetHome(); break;
			}

			canvasRenderTimer.Start();
		}

		void InsertRows(bool after)
		{
			if (!Selections.Any())
				return;

			var rows = Selections.SelectMany(range => Enumerable.Repeat(after ? range.MaxRow + 1 : range.MinRow, range.NumRows)).ToList();
			var data = rows.Select(row => Enumerable.Repeat(default(object), table.NumColumns).ToList()).ToList();
			Replace(UndoRedoStep.CreateInsertRows(rows, data.ToList()));
		}

		void InsertColumns(bool after)
		{
			if (!Selections.Any())
				return;

			var columns = Selections.SelectMany(range => Enumerable.Repeat(after ? range.MaxColumn + 1 : range.MinColumn, range.NumColumns)).ToList();

			var headers = new List<string>();
			var headersUsed = new HashSet<string>(table.Headers);
			var columnNum = 0;
			while (headers.Count < columns.Count)
			{
				var header = $"Column {++columnNum}";
				if (headersUsed.Contains(header))
					continue;

				headersUsed.Add(header);
				headers.Add(header);
			}

			var data = headers.Select(header => Enumerable.Repeat(default(object), table.NumRows).ToList()).ToList();
			Replace(UndoRedoStep.CreateInsertColumns(columns, headers, data));
		}

		void DeleteRows()
		{
			if (!Selections.Any())
				return;

			Replace(UndoRedoStep.CreateDeleteRows(Selections.EnumerateRows().ToList()));
		}

		void DeleteColumns()
		{
			if (!Selections.Any())
				return;

			Replace(UndoRedoStep.CreateDeleteColumns(Selections.EnumerateColumns().ToList()));
		}

		void ReplaceCells(ObservableCollectionEx<CellRange> ranges, List<object> values = null, string defaultValue = null) => ReplaceCells(ranges.EnumerateCells().ToList(), values, defaultValue);

		void ReplaceCells(List<Cell> cells, List<object> values = null, string defaultValue = null)
		{
			if (!cells.Any())
				return;
			if (values == null)
				values = Enumerable.Repeat(Table.GetValue(defaultValue), cells.Count).ToList();
			if (cells.Count != values.Count)
				throw new Exception("Invalid value count");

			if (Enumerable.Range(0, cells.Count).All(ctr => Object.Equals(table[cells[ctr]], values[ctr])))
				return;

			Replace(UndoRedoStep.CreateChangeCells(cells, values));
		}

		void ReplaceTable(Table table) => Replace(UndoRedoStep.CreateChangeTable(table));

		void Sort(List<int> sortOrder)
		{
			if (!sortOrder.Any())
				return;

			Replace(UndoRedoStep.CreateSort(sortOrder));
		}

		void RenameHeader(int column, string newName) => Replace(UndoRedoStep.CreateRenameHeader(column, newName));

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			EndEdit(true);
			MouseHandler(e.GetPosition(canvas), shiftDown);
			canvas.CaptureMouse();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			MouseHandler(e.GetPosition(canvas), true);
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void MouseHandler(Point mousePos, bool selecting)
		{
			var row = Math.Min(table.NumRows - 1, (int)(mousePos.Y / rowHeight - HeaderRows) + yScrollValue);
			if ((row < 0) || (row > table.NumRows))
				return;

			var column = 0;
			var xPos = mousePos.X + xScrollValue;
			while (true)
			{
				if (column >= table.Headers.Count)
					return;

				var width = columnWidth[column];
				if (xPos < width)
					break;

				xPos -= width;
				++column;
			}

			if ((!controlDown) && (!selecting))
				Selections.Clear();

			if ((selecting) && (Selections.Any()))
				Selections[Selections.Count - 1] = MoveSelection(Selections.Last(), row, column, true, false, false);
			else
				Selections.Add(new CellRange(new Cell(row, column)));
		}
	}
}
