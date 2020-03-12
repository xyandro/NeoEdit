namespace NeoEdit.Program
{
	partial class TextEditor
	{
		void Command_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((TabsParent.GetTabIndex(this, activeOnly) + 1).ToString());
		}

		void Command_Window_ViewValues(bool? multiStatus) => ViewValues = multiStatus != true;
	}
}
