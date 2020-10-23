using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		string GetSummaryName(Tab tab, int index)
		{
			if (!string.IsNullOrWhiteSpace(tab.DisplayName))
				return tab.DisplayName;
			if (!string.IsNullOrWhiteSpace(tab.FileName))
				return $"Summary for {Path.GetFileName(tab.FileName)}";
			return $"Summary {index + 1}";
		}

		void Execute_Select_Summarize(bool caseSensitive, bool showAllTabs)
		{
			var selectionsByTab = ActiveTabs.Select((tab, index) => (DisplayName: GetSummaryName(tab, index), Selections: tab.GetSelectionStrings())).ToList();

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
