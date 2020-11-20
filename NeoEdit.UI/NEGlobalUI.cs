using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	public static class NEGlobalUI
	{
		public static Dispatcher Dispatcher { get; set; }
		public static INEGlobal neGlobal { get; set; }

		static readonly ActionRunner actionRunner = new ActionRunner();

		public static bool HandlesKey(ModifierKeys modifiers, Key key) => neGlobal.HandlesKey(modifiers, key);

		public static void HandleCommand(ExecuteState state) => HandleCommand(null, state);

		public static void HandleCommand(INEWindow neWindow, ExecuteState state)
		{
			actionRunner.Add(moreQueued =>
			{
				Dispatcher.Invoke(() => Clipboarder.GetSystem());
				neGlobal.HandleCommand(neWindow, state, moreQueued);
				Dispatcher.Invoke(() => Clipboarder.SetSystem());
			});
		}

		public static bool StopTasks()
		{
			var result = false;
			if (actionRunner.CancelActive())
				result = true;
			if (neGlobal.StopTasks())
				result = true;
			return result;
		}

		public static bool KillTasks()
		{
			actionRunner.CancelActive();
			neGlobal.KillTasks();
			return true;
		}
	}
}
