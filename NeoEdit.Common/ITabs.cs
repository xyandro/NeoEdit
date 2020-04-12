using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public interface ITabs
	{
		int AllTabsHash { get; }
		ITab FocusedITab { get; }
		IReadOnlyList<ITab> AllITabs { get; }
		WindowLayout WindowLayout { get; }
		DateTime LastActivated { get; set; }
		IReadOnlyList<ITab> ActiveITabs { get; }
		bool MacroVisualize { get; }

		bool IsActive(ITab tab);
		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool HandlesKey(ModifierKeys modifiers, Key key);
		List<string> GetStatusBar();
	}
}
