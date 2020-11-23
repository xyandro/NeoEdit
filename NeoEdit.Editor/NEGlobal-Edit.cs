namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void Execute_Edit_Undo_Global()
		{
			if (undoGlobalData != null)
			{
				redoGlobalData = Data;
				SetData(undoGlobalData);
				undoGlobalData = null;
			}
		}

		void Execute_Edit_Redo_Global()
		{
			if (redoGlobalData != null)
			{
				undoGlobalData = Data;
				SetData(redoGlobalData);
				redoGlobalData = null;
			}
		}
	}
}
