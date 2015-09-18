﻿using System.Windows.Input;
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
		File_SHA256,
		File_QuickHash,
		[KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift)] File_VCS,
		[KeyGesture(Key.Delete)] File_Delete,
		File_Exit,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[KeyGesture(Key.F, ModifierKeys.Control)] Edit_Find,
		Edit_FindBinary,
		Edit_FindText,
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList6,
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ToList9,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] Edit_TextEdit,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)] Edit_HexEdit,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)] Select_None,
		[KeyGesture(Key.I, ModifierKeys.Control)] Select_Invert,
		Select_Directories,
		Select_Files,
		Select_Unique,
		Select_Duplicates,
		[KeyGesture(Key.OemPlus, ModifierKeys.Alt)] [KeyGesture(Key.OemPlus, ModifierKeys.Control, false)] Select_AddCopiedCut,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] [KeyGesture(Key.OemMinus, ModifierKeys.Control, false)] Select_Remove,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt | ModifierKeys.Shift)] [KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift, false)] Select_RemoveWithChildren,
		[KeyGesture(Key.U, ModifierKeys.Control)] View_DiskUsage,
		[KeyGesture(Key.D1, ModifierKeys.Control)] View_List1,
		[KeyGesture(Key.D2, ModifierKeys.Control)] View_List2,
		[KeyGesture(Key.D3, ModifierKeys.Control)] View_List3,
		[KeyGesture(Key.D4, ModifierKeys.Control)] View_List4,
		[KeyGesture(Key.D5, ModifierKeys.Control)] View_List5,
		[KeyGesture(Key.D6, ModifierKeys.Control)] View_List6,
		[KeyGesture(Key.D7, ModifierKeys.Control)] View_List7,
		[KeyGesture(Key.D8, ModifierKeys.Control)] View_List8,
		[KeyGesture(Key.D9, ModifierKeys.Control)] View_List9,
	}
}
