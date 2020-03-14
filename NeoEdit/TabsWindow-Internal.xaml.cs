using System;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program
{
	partial class TabsWindow
	{
		public DateTime LastActivated { get; private set; }
		readonly RunOnceTimer activateTabsTimer;

		public void QueueActivateTabs() => Dispatcher.Invoke(() => activateTabsTimer.Start());

		void OnActivated(object sender, EventArgs e)
		{
			LastActivated = DateTime.Now;
			QueueActivateTabs();
		}

		void ActivateTabs()
		{
			if (!IsActive)
				return;

			HandleCommand(new ExecuteState(NECommand.Internal_Activate));
		}

		void Execute_Internal_Activate()
		{
			Activated -= OnActivated;
			try
			{
				foreach (var tab in Tabs)
					tab.Activated();
			}
			finally { Activated += OnActivated; }
		}

		void Execute_Internal_Key(ExecuteState state)
		{
			if ((state.ControlDown) && (!state.AltDown))
			{
				var oldHandled = state.Handled;
				state.Handled = true;
				switch (state.Key)
				{
					case Key.PageUp: MovePrev(); break;
					case Key.PageDown: MoveNext(); break;
					case Key.Tab: MoveTabOrder(); break;
					default: state.Handled = oldHandled; break;
				}
			}
		}

		void Execute_Internal_AddTab(Tab tab) => Tabs = Tabs.Concat(tab).ToList();
	}
}
