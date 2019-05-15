namespace NeoEdit
{
	partial class TextEditor
	{
		void Command_View_TabIndex(ITextEditor te, bool activeOnly)
		{
			te.ReplaceSelections((te.TabsParent.GetIndex(this, activeOnly) + 1).ToString());
		}
	}
}
