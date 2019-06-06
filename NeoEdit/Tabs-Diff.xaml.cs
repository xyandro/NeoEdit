using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit
{
	partial class Tabs
	{
		void Command_Diff_Diff(bool shiftDown)
		{
			var diffTargets = Items.Count == 2 ? Items.ToList() : Items.Where(data => data.Active).ToList();
			if (diffTargets.Any(item => item.DiffTarget != null))
			{
				diffTargets.ForEach(item => item.DiffTarget = null);
				return;
			}

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!Items.Except(diffTargets).Any())
					SetLayout(TabsLayout.Grid);
				else
				{
					diffTargets.ForEach(diffTarget => Items.Remove(diffTarget));

					var textEditTabs = new Tabs();
					textEditTabs.SetLayout(TabsLayout.Grid);
					diffTargets.ForEach(diffTarget => textEditTabs.Add(diffTarget));
					textEditTabs.TopMost = diffTargets[0];
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		void Command_Diff_Select_LeftRightBothTabs(bool? left)
		{
			var topMost = TopMost;
			var active = Items.Where(item => (item.Active) && (item.DiffTarget != null)).SelectMany(item => new List<TextEditor> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((GetIndex(item) < GetIndex(item.DiffTarget)) == left)).ToList();
			Items.ForEach(item => item.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(item => item.Active = true);
		}
	}
}
