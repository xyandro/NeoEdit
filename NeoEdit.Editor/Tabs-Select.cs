using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		void Execute_Select_Summarize(bool caseSensitive, bool showAllTabs)
		{
			var selectionsByTab = ActiveTabs.Select(tab => (DisplayName: tab.FileName, Selections: tab.GetSelectionStrings())).ToList();

			if (!showAllTabs)
				selectionsByTab = new List<(string DisplayName, IReadOnlyList<string> Selections)> { (DisplayName: "Summary", Selections: selectionsByTab.SelectMany(x => x.Selections).ToList()) };

			var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			var summaryByTab = selectionsByTab.Select(tuple => (tuple.DisplayName, selections: tuple.Selections.GroupBy(x => x, comparer).Select(group => (str: group.Key, count: group.Count())).OrderByDescending(x => x.count).ToList())).ToList();

			var tabs = new Tabs(false);
			tabs.BeginTransaction(state);
			foreach (var tab in summaryByTab)
				tabs.AddTab(Tab.CreateSummaryTab(tab.DisplayName, tab.selections));
			tabs.SetLayout(new WindowLayout(maxColumns: 4, maxRows: 4));
			tabs.Commit();
		}
	}
}
