using NeoEdit.Common.Configuration;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		static Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_ViewBinaryCodePages(state.Tabs.Focused.ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_ViewBinaryCodePages).CodePages;
	}
}
