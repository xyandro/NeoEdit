using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	enum DiskCommand
	{
		None,
		[Header("_New Tab")] [KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control)] File_New,
		[Header("_Close")] [KeyGesture(Key.F4, ModifierKeys.Control)] [KeyGesture(Key.W, ModifierKeys.Control)] File_Close,
		[Header("_Rename")] [KeyGesture(Key.F2)] File_Rename,
		[Header("_Identify")] [KeyGesture(Key.I, ModifierKeys.Control)] File_Identify,
		[Header("_MD5")] [KeyGesture(Key.M, ModifierKeys.Control)] File_MD5,
		[Header("_SHA1")] [KeyGesture(Key.S, ModifierKeys.Control)] File_SHA1,
		[Header("_Delete")] [KeyGesture(Key.Delete)] File_Delete,
		[Header("E_xit")] File_Exit,
		[Header("Cu_t")] [KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[Header("_Copy")] [KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[Header("_Paste")] [KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[Header("_Find")] [KeyGesture(Key.F3)] Edit_Find,
		[Header("_All")] [KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[Header("_None")] [KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)] Select_None,
		[Header("_Invert")] Select_Invert,
		[Header("_Directories")] Select_Directories,
		[Header("_Files")] Select_Files,
		[Header("_Expression")] [KeyGesture(Key.E, ModifierKeys.Control)] [KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift)] Select_Expression,
		[Header("_Remove")] [KeyGesture(Key.OemMinus, ModifierKeys.Alt)] [KeyGesture(Key.OemMinus, ModifierKeys.Control)] Select_Remove,
		[Header("_Refresh")] [KeyGesture(Key.F5)] View_Refresh,
		[Header("_Tiles")] View_Tiles,
	}
}
