using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class Tabs
	{
		static public void Command_Diff_Diff(ITabs tabs, bool shiftDown)
		{
			var diffTargets = tabs.Items.Count == 2 ? tabs.Items.ToList() : tabs.Items.Where(data => data.Active).ToList();
			if (diffTargets.Any(item => item.DiffTarget != null))
			{
				diffTargets.ForEach(item => item.DiffTarget = null);
				return;
			}

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!tabs.Items.Except(diffTargets).Any())
					tabs.SetLayout(TabsLayout.Grid);
				else
				{
					diffTargets.ForEach(diffTarget => tabs.Items.Remove(diffTarget));

					var textEditTabs = ITabsCreator.CreateTabs();
					textEditTabs.SetLayout(TabsLayout.Grid);
					diffTargets.ForEach(diffTarget => textEditTabs.Add(diffTarget));
					textEditTabs.TopMost = diffTargets[0];
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		static public void Command_Diff_Select_LeftRightBothTabs(ITabs tabs, bool? left)
		{
			var topMost = tabs.TopMost;
			var active = tabs.Items.Where(item => (item.Active) && (item.DiffTarget != null)).SelectMany(item => new List<TextEditor> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((tabs.GetIndex(item) < tabs.GetIndex(item.DiffTarget)) == left)).ToList();
			tabs.Items.ForEach(item => item.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			tabs.TopMost = topMost;
			active.ForEach(item => item.Active = true);
		}
	}
}
