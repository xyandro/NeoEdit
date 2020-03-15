﻿namespace NeoEdit.Program
{
	partial class Tab
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewValues() => ViewValues = state.MultiStatus != true;
	}
}
