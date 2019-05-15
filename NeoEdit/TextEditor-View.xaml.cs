namespace NeoEdit
{
	partial class TextEditor
	{
		static void Command_View_TabIndex(ITextEditor te, bool activeOnly)
		{
			te.ReplaceSelections((te.TabsParent.GetIndex(te as TextEditor, activeOnly) + 1).ToString());
		}
	}
}
