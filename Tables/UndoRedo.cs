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
	}

	class UndoRedoStep
	{
		public List<CellLocation> Cells { get; private set; }
		public List<object> Values { get; private set; }
		public List<int> SortOrder { get; private set; }
		public UndoRedoAction Action { get; private set; }

		UndoRedoStep() { }

		static public UndoRedoStep CreateChangeCells(List<CellLocation> cells, List<object> values)
		{
			return new UndoRedoStep
			{
				Cells = cells,
				Values = values,
				Action = UndoRedoAction.ChangeCells,
			};
		}

		static public UndoRedoStep CreateSort(List<int> sortOrder)
		{
			return new UndoRedoStep
			{
				SortOrder = sortOrder,
				Action = UndoRedoAction.Sort,
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
