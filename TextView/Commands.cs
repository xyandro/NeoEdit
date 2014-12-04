using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextView
{
	enum TextViewCommand
	{
		None,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		File_OpenCopiedCutFiles,
		[KeyGesture(Key.F4, ModifierKeys.Control)] [KeyGesture(Key.W, ModifierKeys.Control)] File_Close,
		File_CopyPath,
		File_Split,
		File_Combine,
		File_Merge,
		File_Encoding,
		File_Exit,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		View_Tiles,
	}
}
