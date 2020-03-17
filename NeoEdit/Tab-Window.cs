using System.Collections.Generic;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tab
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewValues() => ViewValues = state.MultiStatus != true;

		HashSet<Coder.CodePage> Configure_Window_ViewValuesCodePages() => CodePagesDialog.Run(state.TabsWindow, ViewValuesCodePages);

		void Execute_Window_ViewValuesCodePages() => ViewValuesCodePages = state.Configuration as HashSet<Coder.CodePage>;
	}
}
