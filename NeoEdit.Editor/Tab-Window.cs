using NeoEdit.Common.Configuration;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		static Configuration_Window_CustomGrid Configure_Window_CustomGrid(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_CustomGrid(state.Tabs.WindowLayout);

		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((Tabs.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewBinary() => ViewBinary = state.MultiStatus != true;

		static Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_Window_ViewBinaryCodePages(state.Tabs.Focused.ViewBinaryCodePages);

		void Execute_Window_ViewBinaryCodePages() => ViewBinaryCodePages = (state.Configuration as Configuration_Window_ViewBinaryCodePages).CodePages;
	}
}
