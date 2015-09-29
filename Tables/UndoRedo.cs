using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Tables
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
		Sort,
		InsertRows,
		DeleteRows,
		InsertColumns,
		DeleteColumns,
		ChangeHeader,
	}

	class UndoRedoStep
	{
		public CellRanges Ranges { get; private set; }
		public List<object> Values { get; private set; }
		public List<int> Positions { get; private set; }
		public List<List<object>> InsertData { get; private set; }
		public List<Table.Header> Headers { get; private set; }
		public UndoRedoAction Action { get; private set; }

		UndoRedoStep() { }

		static public UndoRedoStep CreateChangeCells(CellRanges ranges, List<object> values)
		{
			return new UndoRedoStep
			{
				Ranges = ranges.Copy(),
				Values = values,
				Action = UndoRedoAction.ChangeCells,
			};
		}

		static public UndoRedoStep CreateSort(List<int> sortOrder)
		{
			return new UndoRedoStep
			{
				Positions = sortOrder,
				Action = UndoRedoAction.Sort,
			};
		}

		static public UndoRedoStep CreateInsertRows(CellRanges ranges, List<List<object>> insertData)
		{
			return new UndoRedoStep
			{
				Ranges = ranges,
				InsertData = insertData,
				Action = UndoRedoAction.InsertRows,
			};
		}

		static public UndoRedoStep CreateInsertColumns(CellRanges ranges, List<Table.Header> headers, List<List<object>> insertData)
		{
			return new UndoRedoStep
			{
				Ranges = ranges,
				InsertData = insertData,
				Headers = headers,
				Action = UndoRedoAction.InsertColumns,
			};
		}

		static public UndoRedoStep CreateDeleteRows(CellRanges ranges)
		{
			return new UndoRedoStep
			{
				Ranges = ranges,
				Action = UndoRedoAction.DeleteRows,
			};
		}

		static public UndoRedoStep CreateDeleteColumns(CellRanges ranges)
		{
			return new UndoRedoStep
			{
				Ranges = ranges,
				Action = UndoRedoAction.DeleteColumns,
			};
		}
		static public UndoRedoStep CreateChangeHeader(CellRange column, Table.Header header, List<object> values)
		{
			return new UndoRedoStep
			{
				Ranges = new CellRanges { column },
				Headers = new List<Table.Header> { header },
				Values = values,
				Action = UndoRedoAction.ChangeHeader,
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

		internal UndoRedo(Action<bool> _setChanged)
		{
			setChanged = _setChanged;
		}

		internal void SetModified(bool modified = true)
		{
			modifiedSteps = modified ? Int32.MinValue / 2 : 0;
		}

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
