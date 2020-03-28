using System;
using System.Collections.Generic;

namespace NeoEdit.Common
{
	public interface ITabs
	{
		int AllTabsHash { get; }
		ITab FocusedITab { get; }
		IEnumerable<ITab> AllITabs { get; }
		int? Columns { get; }
		int? Rows { get; }
		int? MaxColumns { get; }
		int? MaxRows { get; }
		DateTime LastActivated { get; set; }
		IEnumerable<ITab> SortedActiveITabs { get; }
		IEnumerable<ITab> UnsortedActiveITabs { get; }
		bool MacroVisualize { get; }

		bool IsActive(ITab tab);
		bool HandleCommand(ExecuteState state, bool configure = true);
		List<string> GetStatusBar();
	}
}
