namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		void Command_View_TabIndex()
		{
			ReplaceSelections((TabsParent.GetIndex(this) + 1).ToString());
		}
	}
}
