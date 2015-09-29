﻿using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tables
{
	public enum TablesCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control, false)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open_Open,
		[KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, false)] File_Open_CopiedCut,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save_Save,
		File_Save_SaveAs,
		File_Operations_Rename,
		File_Operations_Delete,
		File_Operations_Explore,
		File_Operations_OpenDisk,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		File_Revert,
		File_Copy_Path,
		File_Copy_Name,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Sort,
		[KeyGesture(Key.E, ModifierKeys.Control)] Expression_Expression,
		Expression_SelectByExpression,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Select_Cells,
		Select_Null,
		Select_NonNull,
		Select_Unique,
		Select_Duplicates,
		View_ActiveTabs,
	}
}
