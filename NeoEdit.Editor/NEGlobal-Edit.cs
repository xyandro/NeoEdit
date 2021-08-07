namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void Execute__Edit_Undo_Global()
		{
			if (undoGlobalData != null)
				SetData(undoGlobalData);
		}
	}
}
