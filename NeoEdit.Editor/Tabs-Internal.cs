using System;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		public DateTime LastActivated { get; set; }

		void Execute_Internal_Activate()
		{
			LastActivated = DateTime.Now;
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

		void Execute_Internal_Key()
		{
			if ((!state.ControlDown) || (state.AltDown))
			{
				ExecuteActiveTabs();
				return;
			}

			switch (state.Key)
			{
				case Key.PageUp: MovePrevNext(-1, state.ShiftDown); break;
				case Key.PageDown: MovePrevNext(1, state.ShiftDown); break;
				case Key.Tab: MovePrevNext(1, state.ShiftDown, true); break;
				default: ExecuteActiveTabs(); break;
			}
		}

		void Execute_Internal_Scroll()
		{
			(var itab, var newColumn, var newRow) = ((ITab, int, int))state.Configuration;
			var tab = itab as Tab;
			tab.StartColumn = newColumn;
			tab.StartRow = newRow;
		}

		void Execute_Internal_Mouse()
		{
			(var itab, var line, var column, var clickCount, var selecting) = ((ITab, int, int, int, bool?))state.Configuration;
			var tab = itab as Tab;
			if ((ActiveTabs.Count != 1) || (!IsActive(tab)))
			{
				ClearAllActive();
				SetActive(tab);
				return;
			}

			tab.Execute_Internal_Mouse(line, column, clickCount, selecting);
		}
	}
}
