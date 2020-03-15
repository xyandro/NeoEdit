using System;
using System.Windows.Input;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		public DateTime LastActivated { get; set; }

		void Execute_Internal_Activate()
		{
			foreach (var tab in AllTabs)
				tab.Activated();
		}

		void Execute_Internal_Key()
		{
			if ((state.ControlDown) && (!state.AltDown))
			{
				var oldHandled = state.Handled;
				state.Handled = true;
				switch (state.Key)
				{
					case Key.PageUp: MovePrevNext(-1, state.ShiftDown); break;
					case Key.PageDown: MovePrevNext(1, state.ShiftDown); break;
					case Key.Tab: MovePrevNext(1, state.ShiftDown, true); break;
					default: state.Handled = oldHandled; break;
				}
			}
		}

		void Execute_Internal_AddTab(Tab tab) => AddTab(tab);

		void Execute_Internal_MouseActivate(Tab tab)
		{
			ClearAllActive();
			SetActive(tab);
		}
	}
}
