using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Disk
{
	enum DiskCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, false)] File_NewTab,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F2)] File_Rename,
		[KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift)] File_Identify,
		[KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Shift)] File_MD5,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] File_SHA1,
		[KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift)] File_Svn,
		[KeyGesture(Key.Delete)] File_Delete,
		File_Exit,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[KeyGesture(Key.F, ModifierKeys.Control)] Edit_Find,
		Edit_BinaryFind,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] Edit_TextEdit,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)] Edit_HexEdit,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)] Select_None,
		[KeyGesture(Key.I, ModifierKeys.Control)] Select_Invert,
		Select_Directories,
		Select_Files,
		[KeyGesture(Key.OemPlus, ModifierKeys.Alt)] [KeyGesture(Key.OemPlus, ModifierKeys.Control, false)] Select_AddCopiedCut,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] [KeyGesture(Key.OemMinus, ModifierKeys.Control, false)] Select_Remove,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt | ModifierKeys.Shift)] [KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift, false)] Select_RemoveWithChildren,
		View_Tiles,
		[KeyGesture(Key.U, ModifierKeys.Control)] View_DiskUsage,
	}
}
