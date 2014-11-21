using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextView
{
	enum TextViewCommand
	{
		None,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		File_OpenCopiedCutFiles,
		File_CopyPath,
		File_Exit,
		View_Tiles,
	}
}
