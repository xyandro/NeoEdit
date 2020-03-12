namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Execute_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((state.TabsWindow.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_Window_ViewValues(bool? multiStatus) => ViewValues = multiStatus != true;
	}
}
