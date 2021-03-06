﻿using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	public class UndoRedo
	{
		public class UndoRedoStep
		{
			public List<Range> ranges { get; }
			public List<string> text { get; }
			internal bool tryJoinLast { get; }

			public UndoRedoStep(List<Range> _ranges, List<string> _text, bool _tryJoinLast)
			{
				ranges = _ranges;
				text = _text;
				tryJoinLast = _tryJoinLast;
			}
		}

		readonly List<UndoRedoStep> undo = new List<UndoRedoStep>();
		readonly List<UndoRedoStep> redo = new List<UndoRedoStep>();

		public UndoRedo() { }

		public void Clear()
		{
			undo.Clear();
			redo.Clear();
		}

		public UndoRedoStep GetUndo()
		{
			if (undo.Count == 0)
				return null;

			var step = undo.Last();
			undo.Remove(step);
			return step;
		}

		public UndoRedoStep GetRedo()
		{
			if (redo.Count == 0)
				return null;

			var step = redo.Last();
			redo.Remove(step);
			return step;
		}

		public void AddUndone(UndoRedoStep current)
		{
			redo.Add(current);
		}

		public void AddRedone(UndoRedoStep current)
		{
			undo.Add(current);
		}

		const int maxUndo = 1048576 * 10;
		public void AddUndo(UndoRedoStep current, bool modified)
		{
			redo.Clear();

			// See if we can add this one to the last one
			var done = false;
			if ((current.tryJoinLast) && (modified) && (undo.Count != 0))
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
