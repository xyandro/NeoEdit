﻿using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tables
{
	enum TablesCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control, false)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save,
		File_SaveAs,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Sort,
		[KeyGesture(Key.E, ModifierKeys.Control)] Expression_Expression,
	}
}
