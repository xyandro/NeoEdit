using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
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
			return dataGrid.SelectedCells.Select(cell => GetCellLocation(cell)).ToList();
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

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				default: return true;
			}

			//return dialogResult != null;
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
			}
		}

		public override bool Empty()
		{
			return (FileName == null) && (!IsModified) && (!Table.Headers.Any());
		}

		CellLocation GetCellLocation(DataGridCellInfo cellInfo)
		{
			return new CellLocation(Table.GetRow(cellInfo.Item as ObservableCollection<object>), (cellInfo.Column as TableColumn).Column);
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

		void DataGridPreviewKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter: dataGrid.CommitEdit(); break;
				default: e.Handled = false; break;
			}
		}

		UndoRedoStep GetUndo(UndoRedoStep step)
		{
			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: return UndoRedoStep.CreateChangeCells(step.Cells, step.Cells.Select(cell => Table[cell]).ToList());
				default: throw new NotImplementedException();
			}
		}

		void Replace(UndoRedoStep step, ReplaceType replaceType = ReplaceType.Normal)
		{
			var undoStep = GetUndo(step);

			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(undoStep); break;
				case ReplaceType.Redo: undoRedo.AddRedone(undoStep); break;
				case ReplaceType.Normal: undoRedo.AddUndo(undoStep); break;
			}

			switch (step.Action)
			{
				case UndoRedoAction.ChangeCells: Table.ChangeCells(step.Cells, step.Values); break;
			}
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
	}
}
