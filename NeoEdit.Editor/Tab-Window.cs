using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		HashSet<Coder.CodePage> Configure_Window_ViewBinaryCodePages() => state.TabsWindow.RunCodePagesDialog(ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = state.Configuration as HashSet<Coder.CodePage>;
	}
}
