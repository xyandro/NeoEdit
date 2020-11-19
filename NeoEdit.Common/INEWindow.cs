using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public static class INEWindowStatic
	{
		public static Func<ModifierKeys, Key, bool> HandlesKey { get; set; }
	}

	public interface INEWindow
	{
		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool StopTasks();
		bool KillTasks();
	}
}
