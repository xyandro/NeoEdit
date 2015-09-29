﻿using System;
using System.Collections.Generic;
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
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.Tables.Dialogs;

namespace NeoEdit.Tables
{
	public class TabsControl : TabsControl<TableEditor, TablesCommand> { }

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

		string AESKey = null;

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

			NEClipboard.ClipboardChanged += () => { };
		}

		public TableEditor(string fileName)
		{
			InitializeComponent();
			undoRedo = new UndoRedo(b => IsModified = b);
			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			OpenFile(fileName);

			Selections.Add(new CellRange(0, 0));
			Selections.CollectionChanged += (s, e) => { MakeActiveVisible(); canvasRenderTimer.Start(); };
			SetupTabLabel();
		}

		DateTime fileLastWrite;
		void OpenFile(string fileName)
		{
			FileName = fileName;
			var bytes = new byte[0];
			if (fileName != null)
				bytes = File.ReadAllBytes(fileName);

			string aesKey;
			FileEncryptor.HandleDecrypt(ref bytes, out aesKey);
			AESKey = aesKey;

			var text = Coder.BytesToString(bytes, Coder.CodePage.AutoByBOM, true);

			Table = String.IsNullOrEmpty(text) ? new Table() : Table = new Table(text);

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			undoRedo.SetModified(false);
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
						if (!controlDown)
							MoveSelections(0, 0, shiftDown, columnRel: false);
						else if (shiftDown)
							MoveSelections(0, 0, shiftDown, false, false);
						else
							Selections.Replace(new CellRange());
						break;
					case Key.End:
						if (!controlDown)
							MoveSelections(0, int.MaxValue, shiftDown, columnRel: false);
						else if (shiftDown)
							MoveSelections(int.MaxValue, int.MaxValue, shiftDown, false, false);
						else
							Selections.Replace(new CellRange(Table.NumRows - 1, Table.NumColumns - 1));
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
							result = false;
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

		bool IsInteger(Type type)
		{
			return (type == typeof(sbyte)) || (type == typeof(byte)) || (type == typeof(short)) || (type == typeof(ushort)) || (type == typeof(int)) || (type == typeof(uint)) || (type == typeof(long)) || (type == typeof(ulong));
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
					header.Width = Font.GetText(header.TypeName).Width + ColumnSpacing * 2;

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
				text[1] = Font.GetText(header.TypeName);
				alignment[0] = alignment[1] = HorizontalAlignment.Center;
				background[0] = background[1] = Misc.headersBrush;

				for (var row = startRow; row < endRow; ++row)
				{
					var value = Table[row, column];
					var display = value == null ? "<NULL>" : value.ToString();
					var item = text[row - startRow + HeaderRows] = Font.GetText(display);
					alignment[row - startRow + HeaderRows] = IsInteger(header.Type) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
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
			SetBinding(UIHelper<TabsControl<TableEditor, TablesCommand>>.GetProperty(a => a.TabLabel), multiBinding);
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
			var data = Table.ConvertToString("\r\n", GetFileTableType(fileName));
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

		void Command_File_Operations_Explore()
		{
			Process.Start("explorer.exe", "/select,\"" + FileName + "\"");
		}

		void Command_File_Operations_OpenDisk()
		{
			Launcher.Static.LaunchDisk(FileName);
		}

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

		void SetClipboardFiles(List<string> data, bool isCut)
		{
			NEClipboard.SetFiles(data, isCut, typeof(TableEditor));
		}

		void SetClipboard(object data)
		{
			NEClipboard.Set(data, data.ToString(), typeof(TableEditor));
		}

		void Command_File_Copy_Path()
		{
			SetClipboardFiles(new List<string> { FileName }, false);
		}

		void Command_File_Copy_Name()
		{
			SetClipboard(Path.GetFileName(FileName));
		}

		string Command_File_Encryption_Dialog()
		{
			return FileEncryptor.GetKey(WindowParent);
		}

		void Command_File_Encryption(string result)
		{
			if (result == null)
				return;
			AESKey = result == "" ? null : result;
		}

		List<CellLocation> GetSelectedCells(bool preserveOrder = false)
		{
			return Selections.EnumerateCells(Table.NumRows, Table.NumColumns, preserveOrder).ToList();
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
		}

		void Command_Edit_Copy_Copy(bool headers)
		{
			var values = GetSelectedCells().Select(cell => Table[cell]).ToList();
			var data = Table.GetTableData(Selections);
			NEClipboard.Set(values, data);
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

			values = values.Select((value, index) => Table.Headers[cells[index].Column].GetValue(value)).ToList();

			ReplaceCells(Selections, values);
		}

		void Command_Edit_Sort()
		{
			var columns = Selections.EnumerateColumns(Table.NumColumns, true).ToList();
			if (!columns.Any())
				return;
			var sortOrder = Table.GetSortOrder(columns);
			if (!sortOrder.Any())
				return;
			if (sortOrder.Select((val, index) => val == index).All(b => b))
				sortOrder.Reverse();

			Sort(sortOrder);

			Selections.Replace(columns.Select(column => new CellRange(0, column, allColumns: true)));
		}

		EditHeaderDialog.Result Command_Edit_Header_Dialog()
		{
			Selections.Replace(Selections.SimplifyToColumns());
			if ((Selections.Count != 1) || (Selections[0].NumColumns != 1))
				throw new Exception("Must have single column selected");
			return EditHeaderDialog.Run(WindowParent, Table.Headers[Selections[0].MinColumn], GetExpressionData(10));
		}

		void Command_Edit_Header(EditHeaderDialog.Result result)
		{
			Selections.Replace(Selections.SimplifyToColumns());
			if ((Selections.Count != 1) || (Selections[0].NumColumns != 1))
				throw new Exception("Must have single column selected");
			ReplaceHeader(Selections.Single(), result.Header, result.Expression);
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

		GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<bool>(result.Expression);
			Selections.Replace(GetSelectedCells().Where((str, num) => results[num]).ToList());
		}

		void Command_Select_All()
		{
			Selections.Replace(new CellRange(endRow: Table.NumRows - 1, endColumn: Table.NumColumns - 1));
		}

		void Command_Select_Cells()
		{
			Selections.Replace(GetSelectedCells());
		}

		void Command_Select_NullNotNull(bool isNull)
		{
			Selections.Replace(GetSelectedCells().Where(cell => (Table[cell] == null) == isNull));
		}

		void Command_Select_UniqueDuplicates(bool unique)
		{
			var found = new HashSet<object>();
			Selections.Replace(GetSelectedCells().Where(cell =>
			{
				var value = Table[cell];
				var contains = found.Contains(value);
				if (!contains)
					found.Add(value);
				return contains != unique;
			}));
		}

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TablesCommand.Expression_Expression: dialogResult = Command_Edit_Expression_Dialog(); break;
				case TablesCommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				case TablesCommand.File_Encryption: dialogResult = Command_File_Encryption_Dialog(); break;
				case TablesCommand.Edit_Header: dialogResult = Command_Edit_Header_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		bool timeNext = false;
		internal void HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			var start = DateTime.UtcNow;

			switch (command)
			{
				case TablesCommand.File_Save_Save: Command_File_Save_Save(); break;
				case TablesCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case TablesCommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case TablesCommand.File_Operations_Delete: Command_File_Operations_Delete(); break;
				case TablesCommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case TablesCommand.File_Operations_OpenDisk: Command_File_Operations_OpenDisk(); break;
				case TablesCommand.File_Close: if (CanClose()) TabsParent.Remove(this); break;
				case TablesCommand.File_Refresh: Command_File_Refresh(); break;
				case TablesCommand.File_Revert: Command_File_Revert(); break;
				case TablesCommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case TablesCommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case TablesCommand.File_Encryption: Command_File_Encryption(dialogResult as string); break;
				case TablesCommand.Edit_Undo: Command_Edit_UndoRedo(ReplaceType.Undo); break;
				case TablesCommand.Edit_Redo: Command_Edit_UndoRedo(ReplaceType.Redo); break;
				case TablesCommand.Edit_Copy_Copy: Command_Edit_Copy_Copy(false); break;
				case TablesCommand.Edit_Copy_CopyWithHeaders: Command_Edit_Copy_Copy(true); break;
				case TablesCommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(true); break;
				case TablesCommand.Edit_Paste_PasteWithoutHeaders: Command_Edit_Paste_Paste(false); break;
				case TablesCommand.Edit_Sort: Command_Edit_Sort(); break;
				case TablesCommand.Edit_Header: Command_Edit_Header(dialogResult as EditHeaderDialog.Result); break;
				case TablesCommand.Expression_Expression: Command_Edit_Expression(dialogResult as GetExpressionDialog.Result); break;
				case TablesCommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TablesCommand.Select_All: Command_Select_All(); break;
				case TablesCommand.Select_Cells: Command_Select_Cells(); break;
				case TablesCommand.Select_Null: Command_Select_NullNotNull(true); break;
				case TablesCommand.Select_NonNull: Command_Select_NullNotNull(false); break;
				case TablesCommand.Select_Unique: Command_Select_UniqueDuplicates(true); break;
				case TablesCommand.Select_Duplicates: Command_Select_UniqueDuplicates(false); break;
				case TablesCommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != TablesCommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message
				{
					Title = "Timer",
					Text = String.Format("Elapsed time: {0:n} ms", elapsed),
					Options = Message.OptionsEnum.Ok,
				}.Show();
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
		protected bool altDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Alt); } }

		List<int> GetListReverse(List<int> list)
		{
			var sortOrderDict = new Dictionary<int, int>();
			for (var ctr = 0; ctr < list.Count; ++ctr)
				sortOrderDict[list[ctr]] = ctr;
			var reverse = Enumerable.Range(0, list.Count).Select(index => sortOrderDict[index]).ToList();
			return reverse;
		}

		UndoRedoStep GetUndoStep(UndoRedoStep step)
		{
			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: return UndoRedoStep.CreateChangeCells(step.Ranges, step.Ranges.EnumerateCells(Table.NumRows, Table.NumColumns).Select(cell => Table[cell]).ToList());
				case UndoRedoAction.Sort: return UndoRedoStep.CreateSort(GetListReverse(step.Positions));
				case UndoRedoAction.DeleteRows: return UndoRedoStep.CreateInsertRows(step.Ranges.DeleteToInsertRows(), Table.GetRowData(step.Ranges));
				case UndoRedoAction.InsertRows: return UndoRedoStep.CreateDeleteRows(step.Ranges.InsertToDeleteRows());
				case UndoRedoAction.DeleteColumns: return UndoRedoStep.CreateInsertColumns(step.Ranges.DeleteToInsertColumns(), step.Ranges.GetDeleteColumns().Select(index => Table.Headers[index]).ToList(), Table.GetColumnData(step.Ranges));
				case UndoRedoAction.InsertColumns: return UndoRedoStep.CreateDeleteColumns(step.Ranges.InsertToDeleteColumns());
				case UndoRedoAction.ChangeHeader: return UndoRedoStep.CreateChangeHeader(step.Ranges.Single(), Table.Headers[step.Ranges.Single().MinColumn], step.Ranges.Single().EnumerateCells(Table.NumRows, Table.NumColumns).Select(cell => Table[cell]).ToList());
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
				case UndoRedoAction.DeleteRows: Table.DeleteRows(step.Ranges); break;
				case UndoRedoAction.InsertRows: Table.InsertRows(step.Ranges, step.InsertData, true); break;
				case UndoRedoAction.DeleteColumns: Table.DeleteColumns(step.Ranges); break;
				case UndoRedoAction.InsertColumns: Table.InsertColumns(step.Ranges, step.Headers, step.InsertData, true); break;
				case UndoRedoAction.ChangeHeader: Table.ChangeHeader(step.Ranges[0], step.Headers[0], step.Values); break;
			}

			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: Selections.Replace(step.Ranges); break;
				case UndoRedoAction.InsertRows: Selections.Replace(step.Ranges.ToOffsetRows()); break;
				case UndoRedoAction.InsertColumns: Selections.Replace(step.Ranges.ToOffsetColumns()); break;
				default: SetHome(); break;
			}

			canvasRenderTimer.Start();
		}

		void InsertRows(bool after)
		{
			if (!Selections.Any())
				return;

			var ranges = Selections.SimplifyToRows(after);
			var data = Enumerable.Repeat(Table.Headers.Select(header => header.GetDefault()).ToList(), ranges.TotalNumRows);
			Replace(UndoRedoStep.CreateInsertRows(ranges, data.ToList()));
		}

		void InsertColumns(bool after)
		{
			if (!Selections.Any())
				return;

			var ranges = Selections.SimplifyToColumns(after);
			var totalCount = ranges.TotalColumnCount;

			var headers = new List<Table.Header>();
			var headersUsed = new HashSet<string>(Table.Headers.Select(header => header.Name));
			var columnNum = 0;
			while (headers.Count < totalCount)
			{
				var header = String.Format("Column {0}", ++columnNum);
				if (headersUsed.Contains(header))
					continue;

				headersUsed.Add(header);
				headers.Add(new Table.Header { Name = header, Type = typeof(string), Nullable = true });
			}

			var data = headers.Select(header => Enumerable.Repeat(header.GetDefault(), Table.NumRows).ToList()).ToList();
			Replace(UndoRedoStep.CreateInsertColumns(ranges, headers, data));
		}

		void DeleteRows()
		{
			if (!Selections.Any())
				return;

			Replace(UndoRedoStep.CreateDeleteRows(Selections.SimplifyToRows()));
		}

		void DeleteColumns()
		{
			if (!Selections.Any())
				return;

			Replace(UndoRedoStep.CreateDeleteColumns(Selections.SimplifyToColumns()));
		}

		void ReplaceCells(CellRanges ranges, List<object> values = null, string defaultValue = null)
		{
			if (!ranges.Any())
				return;

			var cells = ranges.EnumerateCells(Table.NumRows, Table.NumColumns).ToList();
			if (values == null)
			{
				var columnValues = cells.Select(cell => cell.Column).Distinct().ToDictionary(column => column, column => defaultValue == null ? Table.Headers[column].GetDefault() : Table.Headers[column].GetValue(defaultValue));
				values = cells.Select(cell => columnValues[cell.Column]).ToList();
			}
			if (cells.Count != values.Count)
				throw new Exception("Invalid value count");

			if (Enumerable.Range(0, cells.Count).All(ctr => Object.Equals(Table[cells[ctr]], values[ctr])))
				return;

			Replace(UndoRedoStep.CreateChangeCells(ranges, values));
		}

		void Sort(List<int> sortOrder)
		{
			if (!sortOrder.Any())
				return;

			Replace(UndoRedoStep.CreateSort(sortOrder));
		}

		void ReplaceHeader(CellRange column, Table.Header header, string expression)
		{
			var values = column.EnumerateCells(Table.NumRows, Table.NumColumns).Select(cell => Table[cell]).ToList();
			if (expression != null)
				values = GetExpressionResults<object>(expression);
			values = values.Select(value => header.GetValue(value)).ToList();
			Replace(UndoRedoStep.CreateChangeHeader(column, header, values));
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
