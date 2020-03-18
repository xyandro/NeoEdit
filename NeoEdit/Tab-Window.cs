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

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		HashSet<Coder.CodePage> Configure_Window_ViewBinaryCodePages() => CodePagesDialog.Run(state.Window, ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = state.Configuration as HashSet<Coder.CodePage>;
	}
}
