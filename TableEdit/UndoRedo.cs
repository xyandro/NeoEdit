﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Tables;

namespace NeoEdit.TableEdit
{
	enum ReplaceType
	{
		Normal,
		Undo,
		Redo,
	}

	internal enum UndoRedoAction
	{
		ChangeCells,
		InsertRows,
		DeleteRows,
		InsertColumns,
		DeleteColumns,
		RenameHeader,
		ChangeTable,
	}

	class UndoRedoStep
	{
		public List<Cell> Cells { get; private set; }
		public List<object> Values { get; private set; }
		public List<int> Positions { get; private set; }
		public List<List<object>> InsertData { get; private set; }
		public List<string> Headers { get; private set; }
		public UndoRedoAction Action { get; private set; }
		public Table Table { get; private set; }

		UndoRedoStep() { }

		static public UndoRedoStep CreateChangeCells(List<Cell> cells, List<object> values)
		{
			return new UndoRedoStep
			{
				Cells = cells.ToList(),
				Values = values,
				Action = UndoRedoAction.ChangeCells,
			};
		}

		static public UndoRedoStep CreateInsertRows(List<int> positions, List<List<object>> insertData)
		{
			return new UndoRedoStep
			{
				Positions = positions,
				InsertData = insertData,
				Action = UndoRedoAction.InsertRows,
			};
		}

		static public UndoRedoStep CreateInsertColumns(List<int> positions, List<string> headers, List<List<object>> insertData)
		{
			return new UndoRedoStep
			{
				Positions = positions,
				InsertData = insertData,
				Headers = headers,
				Action = UndoRedoAction.InsertColumns,
			};
		}

		static public UndoRedoStep CreateDeleteRows(List<int> positions)
		{
			return new UndoRedoStep
			{
				Positions = positions,
				Action = UndoRedoAction.DeleteRows,
			};
		}

		static public UndoRedoStep CreateDeleteColumns(List<int> positions)
		{
			return new UndoRedoStep
			{
				Positions = positions,
				Action = UndoRedoAction.DeleteColumns,
			};
		}
		static public UndoRedoStep CreateRenameHeader(int column, string newName)
		{
			return new UndoRedoStep
			{
				Positions = new List<int> { column },
				Headers = new List<string> { newName },
				Action = UndoRedoAction.RenameHeader,
			};
		}

		static public UndoRedoStep CreateChangeTable(Table table)
		{
			return new UndoRedoStep
			{
				Table = table,
				Action = UndoRedoAction.ChangeTable,
			};
		}
	}

	class UndoRedo
	{
		readonly List<UndoRedoStep> undo = new List<UndoRedoStep>();
		readonly List<UndoRedoStep> redo = new List<UndoRedoStep>();
		readonly Action<bool> setChanged;
		int _modifiedSteps = 0;
		int modifiedSteps
		{
			get { return _modifiedSteps; }
			set
			{
				if (value == _modifiedSteps)
					return;
				_modifiedSteps = value;
				setChanged(_modifiedSteps != 0);
			}
		}

		internal UndoRedo(Action<bool> _setChanged) { setChanged = _setChanged; }

		internal void SetModified(bool modified = true) => modifiedSteps = modified ? Int32.MinValue / 2 : 0;

		internal void Clear()
		{
			undo.Clear();
			redo.Clear();
			modifiedSteps = 0;
		}

		internal UndoRedoStep GetUndo()
		{
			if (undo.Count == 0)
				return null;

			var step = undo.Last();
			undo.Remove(step);
			return step;
		}

		internal UndoRedoStep GetRedo()
		{
			if (redo.Count == 0)
				return null;

			var step = redo.Last();
			redo.Remove(step);
			return step;
		}

		internal void AddUndone(UndoRedoStep current)
		{
			redo.Add(current);
			--modifiedSteps;
		}

		internal void AddRedone(UndoRedoStep current)
		{
			undo.Add(current);
			++modifiedSteps;
		}

		internal void AddUndo(UndoRedoStep current)
		{
			if (modifiedSteps < 0)
				modifiedSteps = Int32.MinValue / 2; // Never reach 0 again

			redo.Clear();

			undo.Add(current);
			++modifiedSteps;
		}
	}
}