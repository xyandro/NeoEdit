namespace NeoEdit
{
	partial class TextEditor
	{
		void Command_View_TabIndex(bool activeOnly)
		{
			ReplaceSelections((TabsParent.GetIndex(this, activeOnly) + 1).ToString());
		}
	}
}
