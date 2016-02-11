using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextView
{
	public enum TextViewCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift)] File_NewWindow,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		File_OpenCopiedCutFiles,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		File_CopyPath,
		File_Split,
		File_Combine,
		File_Merge,
		File_Encoding,
		File_Exit,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		View_Full,
		View_Grid,
		View_CustomGrid,
	}
}
