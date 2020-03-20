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

		bool Execute_Internal_Key()
		{
			if ((!state.ControlDown) || (state.AltDown))
				return false;

			switch (state.Key)
			{
				case Key.PageUp: MovePrevNext(-1, state.ShiftDown); break;
				case Key.PageDown: MovePrevNext(1, state.ShiftDown); break;
				case Key.Tab: MovePrevNext(1, state.ShiftDown, true); break;
				default: return false;
			}
			return true;
		}

		void Execute_Internal_Scroll()
		{
			(var tab, var newColumn, var newRow) = ((Tab, int, int))state.Configuration;
			tab.StartColumn = newColumn;
			tab.StartRow = newRow;
		}

		void Execute_Internal_Mouse()
		{
			//Tab?.Tabs.TabsWindow.HandleCommand(new ExecuteState(NECommand.Internal_Mouse) { Configuration = (Tab, position, e.ClickCount) });
			(var tab, var line, var column, var clickCount, var selecting) = ((Tab, int, int, int, bool?))state.Configuration;
			if ((UnsortedActiveTabsCount != 1) || (!IsActive(tab)))
			{
				ClearAllActive();
				SetActive(tab);
				return;
			}

			tab.Execute_Internal_Mouse(line, column, clickCount, selecting);
		}
	}
}
