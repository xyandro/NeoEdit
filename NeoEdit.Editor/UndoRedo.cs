using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class UndoRedo
	{
		public class UndoRedoStep
		{
			public List<Range> ranges { get; }
			public List<string> text { get; }
			public bool tryJoinLast { get; }

			public UndoRedoStep(List<Range> ranges, List<string> text, bool tryJoinLast)
			{
				this.ranges = ranges;
				this.text = text;
				this.tryJoinLast = tryJoinLast;
			}
		}

		readonly IReadOnlyList<UndoRedoStep> undo;
		readonly IReadOnlyList<UndoRedoStep> redo;

		public UndoRedo()
		{
			undo = new List<UndoRedoStep>();
			redo = new List<UndoRedoStep>();
		}

		UndoRedo(IReadOnlyList<UndoRedoStep> undo, IReadOnlyList<UndoRedoStep> redo)
		{
			this.undo = undo;
			this.redo = redo;
		}

		public static UndoRedo Create()
		{
			return new UndoRedo(new List<UndoRedoStep>(), new List<UndoRedoStep>());
		}

		public (UndoRedo, UndoRedoStep) GetUndo()
		{
			if (undo.Count == 0)
				return (this, null);

			var result = undo.Last();
			var undoRedo = new UndoRedo(undo.Take(undo.Count - 1).ToList(), redo);
			return (undoRedo, result);
		}

		public (UndoRedo, UndoRedoStep) GetRedo()
		{
			if (redo.Count == 0)
				return (this, null);

			var step = redo.Last();
			var undoRedo = new UndoRedo(undo, redo.Take(redo.Count - 1).ToList());
			return (undoRedo, step);
		}

		public UndoRedo AddUndone(UndoRedoStep current)
		{
			return new UndoRedo(undo, redo.Concat(current).ToList());
		}

		public UndoRedo AddRedone(UndoRedoStep current)
		{
			return new UndoRedo(undo.Concat(current).ToList(), redo);
		}

		const int maxUndo = 1048576 * 10;
		public UndoRedo AddUndo(UndoRedoStep current, bool modified)
		{
			var undoList = undo.ToList();

			// See if we can add this one to the last one
			var done = false;
			if ((current.tryJoinLast) && (modified) && (undoList.Count != 0))
			{
				var last = undoList.Last();
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
				undoList.Add(current);

			// Limit undo buffer
			while (true)
			{
				var totalChars = undoList.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
				if (totalChars <= maxUndo)
					break;
				undoList.RemoveAt(0);
			}

			return new UndoRedo(undoList, new List<UndoRedoStep>());
		}
	}
}
