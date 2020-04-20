using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		void Execute_Diff_Diff(bool shiftDown)
		{
			var diffTargets = AllTabs.Count == 2 ? AllTabs.ToList() : ActiveTabs.ToList();
			diffTargets.ForEach(diffTarget => AddToTransaction(diffTarget));

			var inDiff = false;
			for (var ctr = 0; ctr < diffTargets.Count; ++ctr)
				if (diffTargets[ctr].DiffTarget != null)
				{
					inDiff = true;
					diffTargets[ctr].DiffTarget = null;
				}
			if (inDiff)
				return;

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!AllTabs.Except(diffTargets).Any())
					SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
				else
				{
					diffTargets.ForEach(diffTarget => RemoveTab(diffTarget));

					var tabs = new Tabs();
					tabs.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
					diffTargets.ForEach(diffTarget => tabs.AddTab(diffTarget));
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		void Execute_Diff_Select_LeftRightBothTabs(bool? left)
		{
			var active = new HashSet<Tab>(ActiveTabs.NonNull(item => item.DiffTarget).SelectMany(item => new List<Tab> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((GetTabIndex(item) < GetTabIndex(item.DiffTarget)) == left)));
			AllTabs.ForEach(tab => SetActive(tab, active.Contains(tab)));
		}
	}
}
