﻿using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	class TextEditMenuItem : NEMenuItem<TextEditCommand>
	{
		public TextEditMenuItem()
		{
			// Allow right-click
			SetValue(typeof(MenuItem).GetField("InsideContextMenuProperty", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as DependencyProperty, true);
		}

		MouseButton last = MouseButton.Left;
		static public MouseButton LastClick { get; private set; }

		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			last = MouseButton.Right;
			base.OnMouseRightButtonUp(e);
			last = MouseButton.Left;
		}

		protected override void OnClick()
		{
			LastClick = last;
			base.OnClick();
		}
	}

	enum TextEditCommand
	{
		None,
		[Header("_New")] [KeyGesture(Key.N, ModifierKeys.Control)] File_New,
		[Header("_Open")] [KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		[Header("_Save")] [KeyGesture(Key.S, ModifierKeys.Control)] File_Save,
		[Header("Save _As")] File_SaveAs,
		[Header("_Close")] [KeyGesture(Key.W, ModifierKeys.Control)] [KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[Header("Re_vert")] File_Revert,
		[Header("Check _Updates")] File_CheckUpdates,
		[Header("_Insert File(s)")] File_InsertFiles,
		[Header("Copy _Path")] File_CopyPath,
		[Header("Copy Name")] File_CopyName,
		[Header("Binary Editor")] File_BinaryEditor,
		[Header("_BOM")] File_BOM,
		[Header("E_xit")] File_Exit,
		[Header("_Undo")] [KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[Header("_Redo")] [KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[Header("C_ut")] [KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[Header("_Copy")] [KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[Header("_Paste")] [KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[Header("_Show Clipboard")] [KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ShowClipboard,
		[Header("_Find")] [KeyGesture(Key.F, ModifierKeys.Control)]  [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Find,
		[Header("Find _Next")] [KeyGesture(Key.F3)]  [KeyGesture(Key.F3, ModifierKeys.Shift)] Edit_FindNext,
		[Header("Find _Prev")] [KeyGesture(Key.F3, ModifierKeys.Control)]  [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift)] Edit_FindPrev,
		[Header("Goto _Line")] [KeyGesture(Key.G, ModifierKeys.Control)]  [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift)] Edit_GotoLine,
		[Header("Goto C_olumn")] Edit_GotoIndex,
		[Header("_Copy")] Files_Copy,
		[Header("C_ut")] Files_Cut,
		[Header("_Delete")] Files_Delete,
		[Header("_Write")] Files_Timestamp_Write,
		[Header("_Access")] Files_Timestamp_Access,
		[Header("_Create")] Files_Timestamp_Create,
		[Header("A_ll")] Files_Timestamp_All,
		[Header("_Simplify")] Files_Path_Simplify,
		[Header("_File Name")] Files_Path_GetFileName,
		[Header("File Name w/o Extension")] Files_Path_GetFileNameWoExtension,
		[Header("_Directory Name")] Files_Path_GetDirectory,
		[Header("_Extension")] Files_Path_GetExtension,
		[Header("Create Directory")] Files_CreateDirectory,
		[Header("_Size")] Files_Information_Size,
		[Header("_Write Time")] Files_Information_WriteTime,
		[Header("_Access Time")] Files_Information_AccessTime,
		[Header("_Create Time")] Files_Information_CreateTime,
		[Header("A_ttributes")] Files_Information_Attributes,
		[Header("_Read Only")] Files_Information_ReadOnly,
		[Header("_Existing")] Files_Select_Existing,
		[Header("_Files")] Files_Select_Files,
		[Header("_Directories")] Files_Select_Directories,
		[Header("_Roots")] Files_Select_Roots,
		[Header("_Rename Keys To Selections")] Files_RenameKeysToSelections,
		[Header("_Upper")] [KeyGesture(Key.U, ModifierKeys.Control)] Data_Case_Upper,
		[Header("_Lower")] [KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift)] Data_Case_Lower,
		[Header("_Proper")] Data_Case_Proper,
		[Header("_Toggle")] Data_Case_Toggle,
		[Header("_To Hex")] [KeyGesture(Key.H, ModifierKeys.Control)] Data_Hex_ToHex,
		[Header("_From Hex")] [KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift)] Data_Hex_FromHex,
		[Header("_To Char")] Data_Char_ToChar,
		[Header("_From Char")] Data_Char_FromChar,
		[Header("_Insert")] Data_DateTime_Insert,
		[Header("_Convert")] Data_DateTime_Convert,
		[Header("_Length")] Data_Length,
		[Header("_Width")] Data_Width,
		[Header("_Trim")] Data_Trim,
		[Header("Evaluate _Expression")] [KeyGesture(Key.E, ModifierKeys.Control)] Data_EvaluateExpression,
		[Header("_Series")] Data_Series,
		[Header("_Repeat")] [KeyGesture(Key.R, ModifierKeys.Control)] Data_Repeat,
		[Header("Insert _GUID")] Data_GUID,
		[Header("Insert Random Number")] Data_Random,
		[Header("XML")] Data_Escape_XML,
		[Header("Regex")] Data_Escape_Regex,
		[Header("XML")] Data_Unescape_XML,
		[Header("Regex")] Data_Unescape_Regex,
		[Header("UTF8")] Data_MD5_UTF8,
		[Header("UTF7")] Data_MD5_UTF7,
		[Header("UTF16LE")] Data_MD5_UTF16LE,
		[Header("UTF16BE")] Data_MD5_UTF16BE,
		[Header("UTF32LE")] Data_MD5_UTF32LE,
		[Header("UTF32BE")] Data_MD5_UTF32BE,
		[Header("UTF8")] Data_SHA1_UTF8,
		[Header("UTF7")] Data_SHA1_UTF7,
		[Header("UTF16LE")] Data_SHA1_UTF16LE,
		[Header("UTF16BE")] Data_SHA1_UTF16BE,
		[Header("UTF32LE")] Data_SHA1_UTF32LE,
		[Header("UTF32BE")] Data_SHA1_UTF32BE,
		[Header("_String")] Sort_String,
		[Header("_Numeric")] Sort_Numeric,
		[Header("_Keys")] Sort_Keys,
		[Header("R_everse")] Sort_Reverse,
		[Header("R_andomize")] Sort_Randomize,
		[Header("Sort By Len_gth")] Sort_Length,
		[Header("_String")] Sort_Lines_String,
		[Header("_Numeric")] Sort_Lines_Numeric,
		[Header("_Keys")] Sort_Lines_Keys,
		[Header("R_everse")] Sort_Lines_Reverse,
		[Header("R_andomize")] Sort_Lines_Randomize,
		[Header("Sort By Len_gth")] Sort_Lines_Length,
		[Header("_String")] Sort_Regions_String,
		[Header("_Numeric")] Sort_Regions_Numeric,
		[Header("_Keys")] Sort_Regions_Keys,
		[Header("R_everse")] Sort_Regions_Reverse,
		[Header("R_andomize")] Sort_Regions_Randomize,
		[Header("Sort By Len_gth")] Sort_Regions_Length,
		[Header("_Keys")] [KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetKeys,
		[Header("Values _1")] [KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues1,
		[Header("Values _2")] [KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues2,
		[Header("Values _3")] [KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues3,
		[Header("Values _4")] [KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues4,
		[Header("Values _5")] [KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues5,
		[Header("Values _6")] [KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues6,
		[Header("Values _7")] [KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues7,
		[Header("Values _8")] [KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues8,
		[Header("Values _9")] [KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Shift)] Keys_SetValues9,
		[Header("Values _1")] [KeyGesture(Key.D1, ModifierKeys.Control)] Keys_SelectionReplace1,
		[Header("Values _2")] [KeyGesture(Key.D2, ModifierKeys.Control)] Keys_SelectionReplace2,
		[Header("Values _3")] [KeyGesture(Key.D3, ModifierKeys.Control)] Keys_SelectionReplace3,
		[Header("Values _4")] [KeyGesture(Key.D4, ModifierKeys.Control)] Keys_SelectionReplace4,
		[Header("Values _5")] [KeyGesture(Key.D5, ModifierKeys.Control)] Keys_SelectionReplace5,
		[Header("Values _6")] [KeyGesture(Key.D6, ModifierKeys.Control)] Keys_SelectionReplace6,
		[Header("Values _7")] [KeyGesture(Key.D7, ModifierKeys.Control)] Keys_SelectionReplace7,
		[Header("Values _8")] [KeyGesture(Key.D8, ModifierKeys.Control)] Keys_SelectionReplace8,
		[Header("Values _9")] [KeyGesture(Key.D9, ModifierKeys.Control)] Keys_SelectionReplace9,
		[Header("_Keys")] Keys_GlobalFindKeys,
		[Header("Values _1")] Keys_GlobalFind1,
		[Header("Values _2")] Keys_GlobalFind2,
		[Header("Values _3")] Keys_GlobalFind3,
		[Header("Values _4")] Keys_GlobalFind4,
		[Header("Values _5")] Keys_GlobalFind5,
		[Header("Values _6")] Keys_GlobalFind6,
		[Header("Values _7")] Keys_GlobalFind7,
		[Header("Values _8")] Keys_GlobalFind8,
		[Header("Values _9")] Keys_GlobalFind9,
		[Header("Values _1")] Keys_GlobalReplace1,
		[Header("Values _2")] Keys_GlobalReplace2,
		[Header("Values _3")] Keys_GlobalReplace3,
		[Header("Values _4")] Keys_GlobalReplace4,
		[Header("Values _5")] Keys_GlobalReplace5,
		[Header("Values _6")] Keys_GlobalReplace6,
		[Header("Values _7")] Keys_GlobalReplace7,
		[Header("Values _8")] Keys_GlobalReplace8,
		[Header("Values _9")] Keys_GlobalReplace9,
		[Header("_Keys")] Keys_CopyKeys,
		[Header("Values _1")] Keys_CopyValues1,
		[Header("Values _2")] Keys_CopyValues2,
		[Header("Values _3")] Keys_CopyValues3,
		[Header("Values _4")] Keys_CopyValues4,
		[Header("Values _5")] Keys_CopyValues5,
		[Header("Values _6")] Keys_CopyValues6,
		[Header("Values _7")] Keys_CopyValues7,
		[Header("Values _8")] Keys_CopyValues8,
		[Header("Values _9")] Keys_CopyValues9,
		[Header("_Keys")] Keys_HitsKeys,
		[Header("Values _1")] Keys_HitsValues1,
		[Header("Values _2")] Keys_HitsValues2,
		[Header("Values _3")] Keys_HitsValues3,
		[Header("Values _4")] Keys_HitsValues4,
		[Header("Values _5")] Keys_HitsValues5,
		[Header("Values _6")] Keys_HitsValues6,
		[Header("Values _7")] Keys_HitsValues7,
		[Header("Values _8")] Keys_HitsValues8,
		[Header("Values _9")] Keys_HitsValues9,
		[Header("_Keys")] Keys_MissesKeys,
		[Header("Values _1")] Keys_MissesValues1,
		[Header("Values _2")] Keys_MissesValues2,
		[Header("Values _3")] Keys_MissesValues3,
		[Header("Values _4")] Keys_MissesValues4,
		[Header("Values _5")] Keys_MissesValues5,
		[Header("Values _6")] Keys_MissesValues6,
		[Header("Values _7")] Keys_MissesValues7,
		[Header("Values _8")] Keys_MissesValues8,
		[Header("Values _9")] Keys_MissesValues9,
		[Header("Counts to Keys/Values 1")] Keys_Counts,
		[Header("Toggle Marks/Selection")] [KeyGesture(Key.M, ModifierKeys.Control)] SelectMark_Toggle,
		[Header("_All")] [KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		[Header("Limit")] Select_Limit,
		[Header("All Lines")] [KeyGesture(Key.L, ModifierKeys.Control)] Select_AllLines,
		[Header("_Lines")] Select_Lines,
		[Header("_Marks")] Select_Marks,
		[Header("_Find Results")] Select_Find,
		[Header("Remove Empty Selections")] Select_RemoveEmpty,
		[Header("_Unique")] Select_Unique,
		[Header("_Duplicates")] Select_Duplicates,
		[Header("_String")] Select_Min_String,
		[Header("_Numeric")] Select_Min_Numeric,
		[Header("_String")] Select_Max_String,
		[Header("_Numeric")] Select_Max_Numeric,
		[Header("_Expression Matches")] Select_ExpressionMatches,
		[Header("_RegEx Matches")] Select_RegExMatches,
		[Header("First Selection")] [KeyGesture(Key.D0, ModifierKeys.Alt)] Select_ShowFirst,
		[Header("Show Current")] [KeyGesture(Key.Space, ModifierKeys.Alt)]  [KeyGesture(Key.Space, ModifierKeys.Control)] Select_ShowCurrent,
		[Header("Next Selection")] [KeyGesture(Key.Down, ModifierKeys.Alt)] Select_NextSelection,
		[Header("Prev Selection")] [KeyGesture(Key.Up, ModifierKeys.Alt)] Select_PrevSelection,
		[Header("S_ingle")] [KeyGesture(Key.Enter, ModifierKeys.Alt)] Select_Single,
		[Header("S_ingle")] [KeyGesture(Key.OemMinus, ModifierKeys.Alt)] Select_Remove,
		[Header("_Selection")] Mark_Selection,
		[Header("_Find Results")] Mark_Find,
		[Header("_Clear Marks")] Mark_Clear,
		[Header("_Limit To Selection")] Mark_LimitToSelection,
		[Header("_Tiles")] [KeyGesture(Key.T, ModifierKeys.Control)] View_Tiles,
	}
}
