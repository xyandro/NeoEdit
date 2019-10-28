using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		void Command_Diff_Diff(bool shiftDown)
		{
			var diffTargets = Windows.Count == 2 ? Windows.ToList() : ActiveWindows.ToList();
			if (diffTargets.Any(item => item.DiffTarget != null))
			{
				diffTargets.ForEach(item => item.DiffTarget = null);
				return;
			}

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!Windows.Except(diffTargets).Any())
					SetLayout(maxColumns: 5, maxRows: 5);
				else
				{
					diffTargets.ForEach(diffTarget => RemoveTextEditor(diffTarget));

					var textEditTabs = new Tabs();
					textEditTabs.SetLayout(maxColumns: 5, maxRows: 5);
					diffTargets.ForEach(diffTarget => textEditTabs.AddTextEditor(diffTarget));
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		void Command_Diff_Select_LeftRightBothTabs(bool? left) => SetActive(ActiveWindows.Where(item => item.DiffTarget != null).SelectMany(item => new List<TextEditor> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((WindowIndex(item) < WindowIndex(item.DiffTarget)) == left)).ToList());
	}
}
