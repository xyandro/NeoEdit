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

		void Execute_Internal_PreviewKey(ExecuteState state)
		{
			if ((state.ControlDown) && (!state.AltDown))
			{
				var handled = true;
				switch ((Key)state.ConfigureExecuteData)
				{
					case Key.PageUp: MovePrev(); break;
					case Key.PageDown: MoveNext(); break;
					case Key.Tab: MoveTabOrder(); break;
					default: handled = false; break;
				}
				if (handled)
					state.Result = true;
			}
		}

		void Execute_Internal_AddTextEditor(TextEditor textEditor) => Tabs = Tabs.Concat(textEditor).ToList();
	}
}
