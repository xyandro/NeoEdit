using System;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		public DateTime LastActivated { get; set; }

		void Execute_Internal_Activate()
		{
			LastActivated = DateTime.Now;
			foreach (var tab in AllTabs)
			{
				AddToTransaction(tab);
				tab.Activated();
			}
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
			var configuration = state.Configuration as Configuration_Internal_Scroll;
			var tab = configuration.Tab as Tab;
			AddToTransaction(tab);
			tab.StartColumn = configuration.Column;
			tab.StartRow = configuration.Row;
		}

		void Execute_Internal_Mouse()
		{
			var configuration = state.Configuration as Configuration_Internal_Mouse;
			var tab = configuration.Tab as Tab;

			if ((ActiveTabs.Count != 1) || (!IsActive(tab)))
			{
				ClearAllActive();
				SetActive(tab);
				return;
			}

			tab.Execute_Internal_Mouse(configuration.Line, configuration.Column, configuration.ClickCount, configuration.Selecting);
		}

		void Execute_Internal_SetupDiff()
		{
			AllTabs.ForEach(tab => AddToTransaction(tab));
			for (var ctr = 0; ctr + 1 < AllTabs.Count; ctr += 2)
			{
				AllTabs[ctr].DiffTarget = AllTabs[ctr + 1];
				if (AllTabs[ctr].ContentType == ParserType.None)
					AllTabs[ctr].ContentType = AllTabs[ctr + 1].ContentType;
				if (AllTabs[ctr + 1].ContentType == ParserType.None)
					AllTabs[ctr + 1].ContentType = AllTabs[ctr].ContentType;
			}
			SetLayout(new WindowLayout(maxColumns: 2));
		}

		void Execute_Internal_GotoTab()
		{
			var result = state.Configuration as Configuration_Internal_GotoTab;
			(result.Tab as Tab).Goto(result.Line, result.Column, result.Index);
		}
	}
}
