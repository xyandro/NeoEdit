using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	enum DiskCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control)] File_New,
		[KeyGesture(Key.F4, ModifierKeys.Control)] [KeyGesture(Key.W, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F2)] File_Rename,
		[KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift)] File_Identify,
		[KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Shift)] File_MD5,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] File_SHA1,
		[KeyGesture(Key.Delete)] File_Delete,
		File_Exit,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[KeyGesture(Key.F3)] Edit_Find,
		[KeyGesture(Key.F, ModifierKeys.Control)] Edit_FindInFiles,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] Edit_TextEdit,
		[KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Alt)] Edit_BinaryEdit,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)] Select_None,
		[KeyGesture(Key.I, ModifierKeys.Control)] Select_Invert,
		Select_Directories,
		Select_Files,
		[KeyGesture(Key.E, ModifierKeys.Control)] [KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift)] Select_Expression,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] [KeyGesture(Key.OemMinus, ModifierKeys.Control)] Select_Remove,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt | ModifierKeys.Shift)] [KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift)] Select_RemoveWithChildren,
		[KeyGesture(Key.F5)] View_Refresh,
		View_Tiles,
	}
}
