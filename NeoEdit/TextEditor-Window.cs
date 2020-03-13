namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((tabsWindow.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewValues() => ViewValues = state.MultiStatus != true;
	}
}
