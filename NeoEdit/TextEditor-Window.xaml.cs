﻿namespace NeoEdit
{
	partial class TextEditor
	{
		void Command_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((TabsParent.GetIndex(this, activeOnly) + 1).ToString());
		}
	}
}