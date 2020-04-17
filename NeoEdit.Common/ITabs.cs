using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public static class ITabsStatic
	{
		public static Func<ModifierKeys, Key, bool> HandlesKey { get; set; }
	}

	public interface ITabs
	{
		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool StopTasks();
		bool KillTasks();
	}
}
