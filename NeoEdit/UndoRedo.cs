using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Program
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

		public static void Clear(ref UndoRedo undoRedo)
		{
			undoRedo = new UndoRedo(new List<UndoRedoStep>(), new List<UndoRedoStep>());
		}

		public static UndoRedoStep GetUndo(ref UndoRedo undoRedo)
		{
			if (undoRedo.undo.Count == 0)
				return null;

			var result = undoRedo.undo.Last();
			undoRedo = new UndoRedo(undoRedo.undo.Take(undoRedo.undo.Count - 1).ToList(), undoRedo.redo);
			return result;
		}

		public static UndoRedoStep GetRedo(ref UndoRedo undoRedo)
		{
			if (undoRedo.redo.Count == 0)
				return null;

			var step = undoRedo.redo.Last();
			undoRedo = new UndoRedo(undoRedo.undo, undoRedo.redo.Take(undoRedo.redo.Count - 1).ToList());
			return step;
		}

		public static void AddUndone(ref UndoRedo undoRedo, UndoRedoStep current)
		{
			undoRedo = new UndoRedo(undoRedo.undo, undoRedo.redo.Concat(current).ToList());
		}

		public static void AddRedone(ref UndoRedo undoRedo, UndoRedoStep current)
		{
			undoRedo = new UndoRedo(undoRedo.undo.Concat(current).ToList(), undoRedo.redo);
		}

		const int maxUndo = 1048576 * 10;
		public static void AddUndo(ref UndoRedo undoRedo, UndoRedoStep current, bool modified)
		{
			var undo = undoRedo.undo.ToList();

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
				undo.Add(current);

			// Limit undo buffer
			while (true)
			{
				var totalChars = undo.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
				if (totalChars <= maxUndo)
					break;
				undo.RemoveAt(0);
			}

			undoRedo = new UndoRedo(undo, new List<UndoRedoStep>());
		}
	}
}
