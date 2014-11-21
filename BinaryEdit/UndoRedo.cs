using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.BinaryEdit
{
	class UndoRedo
	{
		internal class UndoRedoStep
		{
			public long index, count;
			public byte[] bytes;

			public UndoRedoStep(long _position, long _length, byte[] _bytes)
			{
				index = _position;
				count = _length;
				bytes = _bytes;
			}
		}

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

		const int maxUndo = 1048576 * 10;
		internal void AddUndo(UndoRedoStep current)
		{
			if (modifiedSteps < 0)
				modifiedSteps = Int32.MinValue / 2; // Never reach 0 again

			redo.Clear();

			// See if we can add this one to the last one
			var done = false;
			if ((modifiedSteps != 0) && (undo.Count != 0))
			{
				var last = undo.Last();
				if (last.index + last.count == current.index)
				{
					last.count += current.count;
					var oldSize = last.bytes.LongLength;
					Array.Resize(ref last.bytes, (int)(last.bytes.LongLength + current.bytes.LongLength));
					Array.Copy(current.bytes, 0, last.bytes, oldSize, current.bytes.LongLength);
					done = true;
				}
			}

			if (!done)
			{
				undo.Add(current);
				++modifiedSteps;
			}

			// Limit undo buffer
			while (true)
			{
				var totalChars = undo.Sum(undoItem => undoItem.bytes.LongLength);
				if (totalChars <= maxUndo)
					break;
				undo.RemoveAt(0);
			}
		}
	}
}
