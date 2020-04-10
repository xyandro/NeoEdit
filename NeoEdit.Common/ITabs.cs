using System;
using System.Collections.Generic;
using System.Windows.Input;

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
		void HandleCommand(ExecuteState state);
		bool HandlesKey(ModifierKeys modifiers, Key key);
		List<string> GetStatusBar();
	}
}
