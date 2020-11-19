using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public interface INEGlobal
	{
		bool HandlesKey(ModifierKeys modifiers, Key key);
		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool StopTasks();
		bool KillTasks();
	}
}
