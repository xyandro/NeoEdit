using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public interface ITabs
	{
		DateTime LastActivated { get; set; }
		bool MacroVisualize { get; }

		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool HandlesKey(ModifierKeys modifiers, Key key);
		List<string> GetStatusBar();
	}
}
