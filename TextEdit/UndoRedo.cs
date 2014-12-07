using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit
{
	class UndoRedo
	{
		internal class UndoRedoStep
		{
			internal List<Range> ranges { get; private set; }
			internal List<string> text { get; private set; }
			internal bool tryJoinLast { get; private set; }

			internal UndoRedoStep(List<Range> _ranges, List<string> _text, bool _tryJoinLast)
			{
				ranges = _ranges;
				text = _text;
				tryJoinLast = _tryJoinLast;
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
			if ((current.tryJoinLast) && (modifiedSteps != 0) && (undo.Count != 0))
			{
				var last = undo.Last();
				if ((last.tryJoinLast) && (last.ranges.Count == current.ranges.Count))
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
							last.ranges[num] = new Range(last.ranges[num].Start + change, last.ranges[num].End + current.ranges[num].Length + change);
							last.text[num] += current.text[num];
							change += current.ranges[num].Length - current.text[num].Length;
						}
					}
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
				var totalChars = undo.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
				if (totalChars <= maxUndo)
					break;
				undo.RemoveAt(0);
			}
		}
	}
}
