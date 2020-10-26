using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public static class INEFilesStatic
	{
		public static Func<ModifierKeys, Key, bool> HandlesKey { get; set; }
	}

	public interface INEFiles
	{
		void HandleCommand(ExecuteState state, Func<bool> skipDraw = null);
		bool StopTasks();
		bool KillTasks();
	}
}
