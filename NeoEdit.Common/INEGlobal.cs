using System;
using System.Collections.Generic;

namespace NeoEdit.Common
{
	public interface INEGlobal
	{
		bool HandlesKey(Modifiers modifiers, string key);
		void HandleCommand(INEWindow neWindow, ExecuteState executeState, Func<bool> skipDraw);
		bool StopTasks();
		bool KillTasks();

		IEnumerable<INEWindow> NEWindows { get; }
	}
}
