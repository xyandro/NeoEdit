using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	public static class NEGlobalUI
	{
		public static INEGlobal neGlobal { get; set; }

		static readonly ActionRunner actionRunner = new ActionRunner();

		public static bool HandlesKey(ModifierKeys modifiers, Key key) => neGlobal.HandlesKey(modifiers, key);

		public static void HandleCommand(ExecuteState state, Dispatcher dispatcher = null)
		{
			actionRunner.Add(moreQueued =>
			{
				if (Clipboarder.ShouldGetSystem())
				{
					if (dispatcher == null)
						Clipboarder.GetSystem();
					else
						dispatcher.Invoke(() => Clipboarder.GetSystem());
				}

				neGlobal.HandleCommand(state, moreQueued);

				if (Clipboarder.ShouldSetSystem())
				{
					if (dispatcher == null)
						Clipboarder.SetSystem();
					else
						dispatcher.Invoke(() => Clipboarder.SetSystem());
				}
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
