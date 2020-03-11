using System.Collections.Generic;

namespace NeoEdit.Program
{
	partial class TextEditorData
	{
		void Command_Edit_Undo()
		{
			var step = UndoRedo2.GetUndo(ref newUndoRedo);
			if (step == null)
				return;

			Replace(step.ranges, step.text, ReplaceType.Undo);

			var offset = 0;
			var sels = new List<Range>();
			for (var ctr = 0; ctr < step.ranges.Count; ++ctr)
			{
				sels.Add(Range.FromIndex(step.ranges[ctr].Start + offset, step.text[ctr].Length));
				offset += step.text[ctr].Length - step.ranges[ctr].Length;
			}

			SetSelections(sels);
		}

		void Command_Edit_Redo()
		{
			var step = UndoRedo2.GetRedo(ref newUndoRedo);
			if (step == null)
				return;

			Replace(step.ranges, step.text, ReplaceType.Redo);

			var offset = 0;
			var sels = new List<Range>();
			for (var ctr = 0; ctr < step.ranges.Count; ++ctr)
			{
				sels.Add(Range.FromIndex(step.ranges[ctr].Start + offset, step.text[ctr].Length));
				offset += step.text[ctr].Length - step.ranges[ctr].Length;
			}

			SetSelections(sels);
		}
	}
}
