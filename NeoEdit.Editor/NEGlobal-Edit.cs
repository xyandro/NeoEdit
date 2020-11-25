namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void Execute_Edit_Undo_Global()
		{
			if (undoGlobalData != null)
				SetData(undoGlobalData);
		}
	}
}
