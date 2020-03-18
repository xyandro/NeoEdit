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
			state.KeyHandled = false;

			if ((state.ControlDown) && (!state.AltDown))
			{
				state.KeyHandled = true;
				switch (state.Key)
				{
					case Key.PageUp: MovePrevNext(-1, state.ShiftDown); break;
					case Key.PageDown: MovePrevNext(1, state.ShiftDown); break;
					case Key.Tab: MovePrevNext(1, state.ShiftDown, true); break;
					default: state.KeyHandled = false; break;
				}
			}
		}

		void Execute_Internal_Scroll()
		{
			(Tab tab, int newColumn, int newRow) = ((Tab, int, int))state.Configuration;
			tab.StartColumn = newColumn;
			tab.StartRow = newRow;
		}

		void Execute_Internal_AddTab(Tab tab) => AddTab(tab);

		void Execute_Internal_MouseActivate(Tab tab)
		{
			if (!state.ShiftDown)
				ClearAllActive();
			SetActive(tab);
			Focused = tab;
		}

		void Execute_Internal_CloseTab(Tab tab)
		{
			tab.VerifyCanClose();
			RemoveTab(tab);
		}
	}
}
