namespace NeoEdit
{
	public static class ViewFunctions
	{
		static public void Command_View_TabIndex(ITextEditor te, bool activeOnly)
		{
			te.ReplaceSelections((te.TabsParent.GetIndex(te, activeOnly) + 1).ToString());
		}
	}
}
