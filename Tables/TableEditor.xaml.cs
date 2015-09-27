using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.Tables
{
	public class TabsControl : TabsControl<TableEditor> { }

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

		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize) - HeaderRows; } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize) - HeaderRows; } }

		Table table;
		public Table Table
		{
			get { return table; }
			set
			{
				table = value;
				undoRedo.Clear();
			}
		}

		readonly CellRanges Selections = new CellRanges();

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

		public TableEditor(string fileName)
		{
			InitializeComponent();
			FileName = fileName;
			undoRedo = new UndoRedo(b => IsModified = b);
			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			Selections.Add(new CellRange(0, 0));
			Selections.CollectionChanged += (s, e) => { MakeActiveVisible(); canvasRenderTimer.Start(); };

			if (fileName != null)
			{
				var text = File.ReadAllText(fileName);
				Table = new Table(text);
			}

			Table = Table ?? new Table();

			SetupTabLabel();
		}

		CellRange MoveSelection(CellRange range, int row, int column, bool selecting, bool rowRel = true, bool columnRel = true)
		{
			if (rowRel)
				row += range.End.Row;
			if (columnRel)
				column += range.End.Column;
			row = Math.Max(0, Math.Min(row, Table.NumRows - 1));
			column = Math.Max(0, Math.Min(column, Table.NumColumns - 1));
			var location = new CellLocation(row, column);
			return new CellRange(range, selecting ? null : location, location, selecting ? default(bool?) : false, selecting ? default(bool?) : false);
		}

		void MoveSelections(int row, int column, bool selecting, bool rowRel = true, bool columnRel = true)
		{
			Selections.Replace(selection => MoveSelection(selection, row, column, selecting, rowRel, columnRel));
		}

		void MakeActiveVisible()
		{
			if (!Selections.Any())
				return;

			var cursor = Selections.First().End;
			var xStart = Enumerable.Range(0, cursor.Column).Sum(column => Table.Headers[column].Width);
			var xEnd = xStart + Table.Headers[cursor.Column].Width;
			xScrollValue = Math.Min(xStart, Math.Max(xEnd - xScroll.ViewportSize, xScrollValue));
			yScrollValue = Math.Min(cursor.Row, Math.Max(cursor.Row - yScrollViewportFloor + 1, yScrollValue));
		}

		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			if ((e.Text.Length != 1) || (!Char.IsControl(e.Text[0])))
				StartEdit(true);
			base.OnPreviewTextInput(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;

			if (isEditing)
			{
				switch (e.Key)
				{
					case Key.Escape: EndEdit(false); break;
					case Key.Enter: EndEdit(true); break;
					default: e.Handled = false; break;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Up:
						if (controlDown)
							MoveSelections(0, 0, shiftDown, rowRel: false);
						else
							MoveSelections(-1, 0, shiftDown);
						break;
					case Key.Down:
						if (controlDown)
							MoveSelections(int.MaxValue, 0, shiftDown, rowRel: false);
						else
							MoveSelections(1, 0, shiftDown);
						break;
					case Key.Left:
						if (controlDown)
							MoveSelections(0, 0, shiftDown, columnRel: false);
						else
							MoveSelections(0, -1, shiftDown);
						break;
					case Key.Right:
						if (controlDown)
							MoveSelections(0, int.MaxValue, shiftDown, columnRel: false);
						else
							MoveSelections(0, 1, shiftDown);
						break;
					case Key.Home:
						if (controlDown)
							MoveSelections(0, 0, shiftDown, false, false);
						else
							MoveSelections(0, 0, shiftDown, columnRel: false);
						break;
					case Key.End:
						if (controlDown)
							MoveSelections(int.MaxValue, int.MaxValue, shiftDown, false, false);
						else
							MoveSelections(0, int.MaxValue, shiftDown, columnRel: false);
						break;
					case Key.PageUp: MoveSelections(1 - yScrollViewportFloor, 0, shiftDown); break;
					case Key.PageDown: MoveSelections(yScrollViewportFloor - 1, 0, shiftDown); break;
					case Key.Space:
						bool? allRows = null, allColumns = null;
						if (shiftDown)
							allRows = !Selections.All(selection => selection.AllRows);
						if (controlDown)
							allColumns = !Selections.All(selection => selection.AllColumns);
						if ((allRows.HasValue) || (allColumns.HasValue))
							Selections.Replace(selection => new CellRange(selection, allRows: allRows, allColumns: allColumns));
						else
							e.Handled = false;
						break;
					case Key.F2: StartEdit(false); break;
					case Key.Insert:
						if (shiftDown)
							InsertRows();
						else if (controlDown)
							InsertColumns();
						break;
					case Key.Delete:
						if (shiftDown)
							DeleteRows();
						else if (controlDown)
							DeleteColumns();
						else
							ReplaceCells(Selections, null);
						break;
					default: e.Handled = false; break;
				}

				if (!e.Handled)
					base.OnKeyDown(e);
			}
		}

		bool isEditing = false;
		void StartEdit(bool empty)
		{
			if ((isEditing) || (!Selections.Any()))
				return;

			isEditing = true;
			var cell = Selections.Last().End;

			var x = Enumerable.Range(0, cell.Column).Sum(column => Table.Headers[column].Width) - xScrollValue;
			var y = (cell.Row + HeaderRows - yScrollValue) * rowHeight;
			var tb = new TextBox
			{
				Text = empty ? "" : (Table[cell] ?? "").ToString(),
				FontFamily = Font.FontFamily,
				FontSize = Font.Size,
				MinWidth = Table.Headers[cell.Column].Width,
				Height = rowHeight,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(ColumnSpacing, RowSpacing, ColumnSpacing, RowSpacing),
				Margin = new Thickness(0),
			};
			Canvas.SetLeft(tb, x);
			Canvas.SetTop(tb, y);
			canvas.Children.Add(tb);
			tb.Focus();
		}

		void EndEdit(bool success)
		{
			if (!isEditing)
				return;

			var result = canvas.Children.Cast<TextBox>().First().Text;
			isEditing = false;
			canvas.Children.Clear();
			Focus();

			if (!success)
				return;

			ReplaceCells(Selections, defaultValue: result);
		}

		readonly static double rowHeight = Font.lineHeight + RowSpacing * 2;
		const double RowSpacing = 2;
		const double ColumnSpacing = 8;
		const int HeaderRows = 2;
		void OnCanvasRender(object s, DrawingContext dc)
		{
			if ((Table == null) || (canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			Selections.RemoveDups();

			foreach (var header in Table.Headers)
				if (header.Width == 0)
					header.Width = Font.GetText(header.Type.Name).Width + ColumnSpacing * 2;

			xScroll.Minimum = 0;
			xScroll.ViewportSize = xScroll.LargeChange = canvas.ActualWidth;
			xScroll.SmallChange = xScroll.LargeChange / 4;

			yScroll.ViewportSize = canvas.ActualHeight / rowHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Table.NumRows - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);

			if ((xScroll.ViewportSize <= 0) || (yScrollViewportCeiling <= 0))
				return;

			var startRow = yScrollValue;
			var endRow = Math.Min(Table.NumRows, startRow + yScrollViewportCeiling);
			var numRows = endRow - startRow + HeaderRows;
			var y = Enumerable.Range(0, numRows + 1).ToDictionary(row => row, row => row * rowHeight);
			var active = Selections.Select(range => range.End).ToList();

			double xOffset = -xScrollValue;
			for (var column = 0; column < Table.NumColumns; ++column)
			{
				if (xOffset > canvas.ActualWidth)
					break;

				var header = Table.Headers[column];
				var xStart = xOffset;
				xOffset += header.Width;
				if (xOffset < 0)
					continue;

				var text = new FormattedText[numRows];
				var alignment = new HorizontalAlignment[numRows];
				var background = new Brush[numRows];
				var isActive = new bool[numRows];

				text[0] = Font.GetText(header.Name);
				text[1] = Font.GetText(header.Type.Name);
				alignment[0] = alignment[1] = HorizontalAlignment.Center;
				background[0] = background[1] = Misc.headersBrush;

				for (var row = startRow; row < endRow; ++row)
				{
					var value = Table[row, column];
					var display = value == null ? "<NULL>" : value.ToString();
					var item = text[row - startRow + HeaderRows] = Font.GetText(display);
					alignment[row - startRow + HeaderRows] = header.Type == typeof(long) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
					if (value == null)
					{
						item.SetForegroundBrush(Misc.nullBrush);
						alignment[row - startRow + HeaderRows] = HorizontalAlignment.Center;
					}
					if (Selections.Any(range => range.Contains(row, column)))
						background[row - startRow + HeaderRows] = Misc.selectedBrush;
					isActive[row - startRow + HeaderRows] = active.Any(cell => cell.Equals(row, column));
				}

				header.Width = Math.Max(header.Width, text.Max(formattedText => formattedText.Width) + ColumnSpacing * 2);
				xOffset = xStart + header.Width;

				for (var row = 0; row < numRows; ++row)
				{
					var rect = new Rect(xStart, y[row], header.Width, rowHeight);
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
					dc.DrawText(text[row], new Point(position, y[row] + RowSpacing));
				}

			}

			xScroll.Maximum = Table.Headers.Sum(header => header.Width) - xScroll.ViewportSize;
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] == null?""[Untitled]"":FileName([0]))+([1]?""*"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			SetBinding(UIHelper<TabsControl<TableEditor>>.GetProperty(a => a.TabLabel), multiBinding);
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
					Command_File_Save();
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
			var data = Table.ConvertToString("\r\n", GetFileTableType(fileName));
			File.WriteAllText(fileName, data, Encoding.UTF8);
			FileName = fileName;
			undoRedo.SetModified(false);
		}

		internal Dictionary<string, List<object>> GetExpressionData(int? count = null, NEExpression expression = null)
		{
			var sels = GetSelectedCells();

			// Can't access DependencyProperties from other threads; grab a copy:
			var Table = this.Table;

			var parallelDataActions = new Dictionary<HashSet<string>, Action<HashSet<string>, Action<string, List<object>>>>();
			parallelDataActions.Add(new HashSet<string> { "x" }, (items, addData) => addData("x", sels.Select(cell => Table[cell]).ToList()));
			parallelDataActions.Add(new HashSet<string> { "y" }, (items, addData) => addData("y", sels.Select((cell, index) => (object)(index + 1)).ToList()));
			parallelDataActions.Add(new HashSet<string> { "z" }, (items, addData) => addData("z", sels.Select((cell, index) => (object)index).ToList()));
			parallelDataActions.Add(new HashSet<string> { "row" }, (items, addData) => addData("row", sels.Select(cell => (object)cell.Row).ToList()));
			parallelDataActions.Add(new HashSet<string> { "column" }, (items, addData) => addData("column", sels.Select(cell => (object)cell.Row).ToList()));
			for (var ctr = 0; ctr < Table.Headers.Count; ++ctr)
			{
				var num = ctr; // If we don't copy this the threads get the wrong value
				var columnName = Table.Headers[num].Name;
				var columnNameLen = columnName + "l";
				var columnNum = String.Format("c{0}", ctr + 1);
				var columnNumLen = columnNum + "l";
				parallelDataActions.Add(new HashSet<string> { columnName, columnNum }, (items, addData) =>
				{
					var columnData = sels.Select(cell => Table[cell.Row, num]).ToList();
					addData(columnName, columnData);
					addData(columnNum, columnData);
				});
				parallelDataActions.Add(new HashSet<string> { columnNameLen, columnNumLen }, (items, addData) =>
				{
					var columnLens = sels.Select(cell => (object)(Table[cell.Row, num] ?? "").ToString().Length).ToList();
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

		void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
				Save(FileName);
		}

		void Command_File_SaveAs()
		{
			var fileName = GetSaveFileName();
			if (fileName != null)
				Save(fileName);
		}

		List<CellLocation> GetSelectedCells(bool preserveOrder = false)
		{
			return Selections.GetCells(Table.NumRows, Table.NumColumns, preserveOrder).ToList();
		}

		List<int> GetSelectedColumns(bool preserveOrder = false)
		{
			return Selections.GetColumns(Table.NumColumns, preserveOrder).ToList();
		}

		List<int> GetSelectedRows(bool preserveOrder = false)
		{
			return Selections.GetRows(Table.NumRows, preserveOrder).ToList();
		}

		void SetSelectedCells(IEnumerable<CellRange> ranges)
		{
			Selections.Replace(ranges);
		}

		void SetHome()
		{
			//dataGrid.SelectedCells.Clear();
			//if ((dataGrid.Items.Count != 0) && (dataGrid.Columns.Count != 0))
			//	dataGrid.SelectedCells.Add(new DataGridCellInfo(dataGrid.Items[0], dataGrid.Columns[0]));
			//dataGrid.CurrentCell = dataGrid.SelectedCells.LastOrDefault();
		}

		void Command_Edit_UndoRedo(ReplaceType replaceType)
		{
			var undoRedoStep = replaceType == ReplaceType.Undo ? undoRedo.GetUndo() : undoRedo.GetRedo();
			if (undoRedoStep == null)
				return;

			Replace(undoRedoStep, replaceType);
			switch (undoRedoStep.Action)
			{
				case UndoRedoAction.ChangeCells: SetSelectedCells(undoRedoStep.Ranges); break;
				case UndoRedoAction.InsertRows: SetSelectedCells(undoRedoStep.Positions.Select((row, index) => new CellRange(row + index, 0, allRows: true))); break;
				case UndoRedoAction.InsertColumns: SetSelectedCells(undoRedoStep.Positions.Select((column, index) => new CellRange(0, column + index, allColumns: true))); break;
				default: SetHome(); break;
			}
		}

		void Command_Edit_Sort()
		{
			var columns = GetSelectedColumns();
			if (!columns.Any())
				return;
			var sortOrder = Table.GetSortOrder(columns);
			if (!sortOrder.Any())
				return;
			if (sortOrder.Select((val, index) => val == index).All(b => b))
				sortOrder.Reverse();

			Sort(sortOrder);

			SetSelectedCells(columns.Select(column => new CellRange(0, column, allColumns: true)));
		}

		GetExpressionDialog.Result Command_Edit_Expression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

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

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TablesCommand.Expression_Expression: dialogResult = Command_Edit_Expression_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TablesCommand.File_Save: Command_File_Save(); break;
				case TablesCommand.File_SaveAs: Command_File_SaveAs(); break;
				case TablesCommand.File_Close: if (CanClose()) TabsParent.Remove(this); break;
				case TablesCommand.Edit_Undo: Command_Edit_UndoRedo(ReplaceType.Undo); break;
				case TablesCommand.Edit_Redo: Command_Edit_UndoRedo(ReplaceType.Redo); break;
				case TablesCommand.Edit_Sort: Command_Edit_Sort(); break;
				case TablesCommand.Expression_Expression: Command_Edit_Expression(dialogResult as GetExpressionDialog.Result); break;
			}
		}

		public override bool Empty()
		{
			return (FileName == null) && (!IsModified) && (!Table.Headers.Any());
		}

		List<object> GetValues(IEnumerable<CellLocation> cells)
		{
			return cells.Select(cell => Table[cell]).ToList();
		}

		protected bool shiftDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); } }
		protected bool controlDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Control); } }

		List<int> GetListReverse(List<int> list)
		{
			var sortOrderDict = new Dictionary<int, int>();
			for (var ctr = 0; ctr < list.Count; ++ctr)
				sortOrderDict[list[ctr]] = ctr;
			var reverse = Enumerable.Range(0, list.Count).Select(index => sortOrderDict[index]).ToList();
			return reverse;
		}

		List<int> GetInsertPositions(List<int> deleted)
		{
			return deleted.Select((index, offset) => index - offset).ToList();
		}

		List<int> GetDeletePositions(List<int> inserted)
		{
			return inserted.Select((index, offset) => index + offset).ToList();
		}

		UndoRedoStep GetUndoStep(UndoRedoStep step)
		{
			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: return UndoRedoStep.CreateChangeCells(step.Ranges, step.Ranges.GetCells(Table.NumRows, Table.NumColumns).Select(cell => Table[cell]).ToList());
				case UndoRedoAction.Sort: return UndoRedoStep.CreateSort(GetListReverse(step.Positions));
				case UndoRedoAction.DeleteRows: return UndoRedoStep.CreateInsertRows(GetInsertPositions(step.Positions), Table.GetRowData(step.Positions));
				case UndoRedoAction.InsertRows: return UndoRedoStep.CreateDeleteRows(GetDeletePositions(step.Positions));
				case UndoRedoAction.DeleteColumns: return UndoRedoStep.CreateInsertColumns(GetInsertPositions(step.Positions), step.Positions.Select(index => Table.Headers[index]).ToList(), Table.GetColumnData(step.Positions));
				case UndoRedoAction.InsertColumns: return UndoRedoStep.CreateDeleteColumns(GetDeletePositions(step.Positions));
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
				case UndoRedoAction.ChangeCells: Table.ChangeCells(step.Ranges, step.Values); break;
				case UndoRedoAction.Sort: Table.Sort(step.Positions); break;
				case UndoRedoAction.DeleteRows: Table.DeleteRows(step.Positions); break;
				case UndoRedoAction.InsertRows: Table.InsertRows(step.Positions, step.InsertData, true); break;
				case UndoRedoAction.DeleteColumns: Table.DeleteColumns(step.Positions); break;
				case UndoRedoAction.InsertColumns: Table.InsertColumns(step.Positions, step.Headers, step.InsertData, true); break;
			}

			Selections.Replace(selection => MoveSelection(selection, 0, 0, true));
			canvasRenderTimer.Start();
		}

		object DefaultFor(Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		void InsertRows()
		{
			var rows = GetSelectedRows();
			if (!rows.Any())
				return;
			var columns = Table.Headers.Select(header => DefaultFor(header.Type)).ToList();
			Replace(UndoRedoStep.CreateInsertRows(rows, Enumerable.Repeat(columns, rows.Count).ToList()));
			SetSelectedCells(rows.Select((row, index) => new CellRange(row + index, 0, allRows: true)));
		}

		void InsertColumns()
		{
			var columns = GetSelectedColumns();
			if (!columns.Any())
				return;

			var headers = new List<Table.Header>();
			var headersUsed = new HashSet<string>(Table.Headers.Select(header => header.Name));
			var columnNum = 0;
			while (headers.Count < columns.Count)
			{
				var header = String.Format("Column {0}", ++columnNum);
				if (headersUsed.Contains(header))
					continue;

				headersUsed.Add(header);
				headers.Add(new Table.Header { Name = header, Type = typeof(long) });
			}

			var emptyColumn = Enumerable.Repeat(default(object), Table.NumRows).ToList();
			var data = Enumerable.Repeat(emptyColumn, columns.Count).ToList();
			Replace(UndoRedoStep.CreateInsertColumns(columns, headers, data));
			SetSelectedCells(columns.Select((column, index) => new CellRange(0, column + index, allColumns: true)));
		}

		void DeleteRows()
		{
			var rows = GetSelectedRows();
			if (!rows.Any())
				return;
			Replace(UndoRedoStep.CreateDeleteRows(rows));
			SetHome();
		}

		void DeleteColumns()
		{
			var columns = GetSelectedColumns();
			if (!columns.Any())
				return;
			Replace(UndoRedoStep.CreateDeleteColumns(columns));
			SetHome();
		}

		void ReplaceCells(CellRanges ranges, List<object> values = null, string defaultValue = null)
		{
			if (!ranges.Any())
				return;

			var cells = ranges.GetCells(Table.NumRows, Table.NumColumns).ToList();
			if (values == null)
			{
				var typeValues = ranges.GetColumns(Table.NumColumns).ToDictionary(column => column, column => defaultValue == null ? DefaultFor(Table.Headers[column].Type) : Convert.ChangeType(defaultValue, Table.Headers[column].Type));
				values = cells.Select(cell => typeValues[cell.Column]).ToList();
			}
			if (cells.Count != values.Count)
				throw new Exception("Invalid value count");

			if (Enumerable.Range(0, cells.Count).All(ctr => (Table[cells[ctr]] ?? "").Equals(values[ctr] ?? "")))
				return;

			Replace(UndoRedoStep.CreateChangeCells(ranges, values));
		}

		void Sort(List<int> sortOrder)
		{
			if (!sortOrder.Any())
				return;

			Replace(UndoRedoStep.CreateSort(sortOrder));
		}

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
			var row = Math.Min(Table.NumRows - 1, (int)(mousePos.Y / rowHeight - HeaderRows) + yScrollValue);
			if ((row < 0) || (row > Table.NumRows))
				return;

			var column = 0;
			var xPos = mousePos.X + xScrollValue;
			while (true)
			{
				if (column >= Table.Headers.Count)
					return;

				var width = Table.Headers[column].Width;
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
				Selections.Add(new CellRange(new CellLocation(row, column)));
		}
	}
}
