using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Tables
{
	public class TabsControl : TabsControl<TableEditor> { }

	partial class TableEditor
	{
		[DepProp]
		public Table Table
		{
			get { return UIHelper<TableEditor>.GetPropValue<Table>(this); }
			set
			{
				UIHelper<TableEditor>.SetPropValue(this, value);
				undoRedo.Clear();
			}
		}
		[DepProp]
		public string FileName { get { return UIHelper<TableEditor>.GetPropValue<string>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TableEditor>.GetPropValue<bool>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		readonly UndoRedo undoRedo;

		static TableEditor() { UIHelper<TableEditor>.Register(); }

		public TableEditor(string fileName)
		{
			InitializeComponent();
			FileName = fileName;
			undoRedo = new UndoRedo(b => IsModified = b);

			if (fileName != null)
			{
				var text = File.ReadAllText(fileName);
				Table = new Table(text);
			}

			Table = Table ?? new Table();

			SetupTabLabel();

			dataGrid.PreparingCellForEdit += (s, e) => isEditing = true;
			dataGrid.CellEditEnding += (s, e) => isEditing = false;
		}

		bool isEditing = false;

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

		List<CellLocation> GetSelectedCells()
		{
			return selectedCells.Select(cell => new CellLocation(Table.GetRowIndex(cell.Item as ObservableCollection<object>), (cell.Column as TableColumn).Column)).Distinct().ToList();
		}

		List<int> GetSelectedColumns()
		{
			return selectedCells.Select(cell => (cell.Column as TableColumn).Column).Distinct().ToList();
		}

		List<int> GetSelectedRows()
		{
			return selectedCells.Select(cell => Table.GetRowIndex(cell.Item as ObservableCollection<object>)).Distinct().ToList();
		}

		void SetSelectedCells(IEnumerable<CellLocation> cells)
		{
			dataGrid.SelectedCells.Clear();
			foreach (var cell in cells)
				dataGrid.SelectedCells.Add(new DataGridCellInfo(dataGrid.Items[cell.Row], dataGrid.Columns[cell.Column]));
			dataGrid.CurrentCell = dataGrid.SelectedCells.LastOrDefault();
		}

		void SetHome()
		{
			dataGrid.SelectedCells.Clear();
			if ((dataGrid.Items.Count != 0) && (dataGrid.Columns.Count != 0))
				dataGrid.SelectedCells.Add(new DataGridCellInfo(dataGrid.Items[0], dataGrid.Columns[0]));
			dataGrid.CurrentCell = dataGrid.SelectedCells.LastOrDefault();
		}

		void Command_Edit_UndoRedo(ReplaceType replaceType)
		{
			var undoRedoStep = replaceType == ReplaceType.Undo ? undoRedo.GetUndo() : undoRedo.GetRedo();
			if (undoRedoStep == null)
				return;

			Replace(undoRedoStep, replaceType);
			switch (undoRedoStep.Action)
			{
				case UndoRedoAction.ChangeCells: SetSelectedCells(undoRedoStep.Cells); break;
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

			SetSelectedCells(columns.Select(column => new CellLocation(0, column)));
		}

		GetExpressionDialog.Result Command_Edit_Expression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		List<T> GetExpressionResults<T>(string expression, bool resizeToSelections = true, bool matchToSelections = true)
		{
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
			ReplaceCells(GetSelectedCells(), results);
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

		void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			if (e.EditAction != DataGridEditAction.Commit)
				return;

			var newValue = ((TextBox)e.EditingElement).Text;
			if (String.IsNullOrEmpty(newValue))
				newValue = null;
			dataGrid.CancelEdit();

			var cellLocations = GetSelectedCells();
			var types = cellLocations.Select(cell => cell.Column).Distinct().Select(index => Table.Headers[index].Type).Distinct().ToList();
			var typeValues = types.ToDictionary(type => type, type => Convert.ChangeType(newValue, type));
			var values = cellLocations.Select(cell => typeValues[Table.Headers[cell.Column].Type]).ToList();

			ReplaceCells(cellLocations, values);
		}

		protected bool shiftDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); } }
		protected bool controlDown { get { return Keyboard.Modifiers.HasFlag(ModifierKeys.Control); } }

		void DataGridPreviewKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter:
					if (isEditing)
					{
						dataGrid.CommitEdit();
						e.Handled = true;
					}
					else
						e.Handled = false;
					break;
				case Key.Delete:
					if (shiftDown)
						DeleteRows();
					else if (controlDown)
						DeleteColumns();
					else
						ReplaceCells(GetSelectedCells(), null);
					break;
				default: e.Handled = false; break;
			}
		}

		List<DataGridCellInfo> selectedCells = new List<DataGridCellInfo>();
		void DataGridSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			var cells = new HashSet<DataGridCellInfo>(dataGrid.SelectedCells);
			selectedCells = selectedCells.Where(cell => cells.Contains(cell)).Concat(e.AddedCells).ToList();
		}

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
				case UndoRedoAction.ChangeCells: return UndoRedoStep.CreateChangeCells(step.Cells, step.Cells.Select(cell => Table[cell]).ToList());
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
				case UndoRedoAction.ChangeCells: Table.ChangeCells(step.Cells, step.Values); break;
				case UndoRedoAction.Sort: Table.Sort(step.Positions); break;
				case UndoRedoAction.DeleteRows: Table.DeleteRows(step.Positions); break;
				case UndoRedoAction.InsertRows: Table.InsertRows(step.Positions, step.InsertData); break;
				case UndoRedoAction.DeleteColumns: Table.DeleteColumns(step.Positions); break;
				case UndoRedoAction.InsertColumns: Table.InsertColumns(step.Positions, step.Headers, step.InsertData); break;
			}
		}

		void DeleteRows()
		{
			var rows = GetSelectedRows().OrderBy(row => row).ToList();
			if (!rows.Any())
				return;
			Replace(UndoRedoStep.CreateDeleteRows(rows));
		}

		void DeleteColumns()
		{
			var columns = GetSelectedColumns().OrderBy(row => row).ToList();
			if (!columns.Any())
				return;
			Replace(UndoRedoStep.CreateDeleteColumns(columns));
		}

		void ReplaceCells(List<CellLocation> cells, object value)
		{
			ReplaceCells(cells, Enumerable.Repeat(value, cells.Count).ToList());
		}

		void ReplaceCells(List<CellLocation> cells, List<object> values)
		{
			if (!cells.Any())
				return;
			if (values == null)
				values = Enumerable.Repeat(default(object), cells.Count).ToList();
			if (cells.Count != values.Count)
				throw new Exception("Invalid value count");

			if (Enumerable.Range(0, cells.Count).All(ctr => (Table[cells[ctr]] ?? "").Equals(values[ctr] ?? "")))
				return;

			Replace(UndoRedoStep.CreateChangeCells(cells, values));
		}

		void Sort(List<int> sortOrder)
		{
			if (!sortOrder.Any())
				return;

			Replace(UndoRedoStep.CreateSort(sortOrder));
		}
	}
}
