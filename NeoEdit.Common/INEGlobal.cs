using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public interface INEGlobal
	{
		bool HandlesKey(ModifierKeys modifiers, Key key);
		void HandleCommand(INEWindow neWindow, INEWindowUI neWindowUI, ExecuteState executeState, Func<bool> skipDraw);
		bool StopTasks();
		bool KillTasks();

		IEnumerable<INEWindow> NEWindows { get; }
	}
}
