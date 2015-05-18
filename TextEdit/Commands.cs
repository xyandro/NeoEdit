﻿using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit
{
	enum TextEditCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, false)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		File_OpenCopiedCutFiles,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save,
		File_SaveAs,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		File_Revert,
		File_InsertFiles,
		File_InsertCopiedCutFiles,
		File_CopyPath,
		File_CopyName,
		File_Encoding,
		File_ReopenWithEncoding,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)] File_HexEditor,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.OemPeriod, ModifierKeys.Control)] Edit_RepeatLastAction,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] [KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_Paste,
		[KeyGesture(Key.F, ModifierKeys.Control)] [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_Find,
		[KeyGesture(Key.F3)] [KeyGesture(Key.F3, ModifierKeys.Shift, false)] Edit_FindNext,
		[KeyGesture(Key.F3, ModifierKeys.Control)] [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_FindPrev,
		Edit_Replace,
		[KeyGesture(Key.G, ModifierKeys.Control)] [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_GotoLine,
		Edit_GotoColumn,
		Edit_GotoPosition,
		[KeyGesture(Key.F2, ModifierKeys.Alt)] Edit_ToggleBookmark,
		[KeyGesture(Key.F2)] [KeyGesture(Key.F2, ModifierKeys.Shift, false)] Edit_NextBookmark,
		[KeyGesture(Key.F2, ModifierKeys.Control)] [KeyGesture(Key.F2, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_PreviousBookmark,
		Edit_ClearBookmarks,
		Files_Open,
		Files_Insert,
		Files_SaveClipboards,
		Files_CreateFiles,
		Files_CreateDirectories,
		Files_Delete,
		Files_Simplify,
		Files_GetUniqueNames,
		Files_SanitizeNames,
		Files_Get_Size,
		Files_Get_WriteTime,
		Files_Get_AccessTime,
		Files_Get_CreateTime,
		Files_Get_Attributes,
		Files_Set_Size,
		Files_Set_WriteTime,
		Files_Set_AccessTime,
		Files_Set_CreateTime,
		Files_Set_AllTimes,
		Files_Set_Attributes,
		Files_Select_DirectoryName,
		Files_Select_FileName,
		Files_Select_FileNamewoExtension,
		Files_Select_Extension,
		Files_Select_Existing,
		Files_Select_NonExisting,
		Files_Select_Files,
		Files_Select_Directories,
		Files_Select_Roots,
		Files_Select_NonRoots,
		Files_Hash_MD5,
		Files_Hash_SHA1,
		Files_Hash_SHA256,
		Files_Operations_Copy,
		Files_Operations_Move,
		[KeyGesture(Key.U, ModifierKeys.Control)] Data_Case_Upper,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift)] Data_Case_Lower,
		Data_Case_Proper,
		Data_Case_Toggle,
		[KeyGesture(Key.H, ModifierKeys.Control)] Data_Hex_ToHex,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift)] Data_Hex_FromHex,
		Data_DateTime_Insert,
		Data_DateTime_Convert,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Data_Convert,
		Data_Length,
		[KeyGesture(Key.W, ModifierKeys.Control)] Data_Width,
		[KeyGesture(Key.T, ModifierKeys.Control)] Data_Trim,
		Data_SingleLine,
		Data_Table_ToTable,
		Data_Table_FromTable,
		[KeyGesture(Key.E, ModifierKeys.Control)] Data_EvaluateExpression,
		[KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift)] Data_EvaluateSelectedExpression,
		Data_Series,
		[KeyGesture(Key.D, ModifierKeys.Control)] Data_CopyDown,
		Data_Copy_Count,
		Data_Copy_Length,
		Data_Copy_Min_String,
		Data_Copy_Min_Numeric,
		Data_Copy_Min_Length,
		Data_Copy_Max_String,
		Data_Copy_Max_Numeric,
		Data_Copy_Max_Length,
		Data_Copy_Sum,
		Data_Copy_Lines,
		Data_Copy_Columns,
		Data_Copy_Positions,
		[KeyGesture(Key.R, ModifierKeys.Control)] Data_Repeat,
		Data_Escape_XML,
		Data_Escape_Regex,
		Data_Escape_URL,
		Data_Unescape_XML,
		Data_Unescape_Regex,
		Data_Unescape_URL,
		Data_Hash_MD5,
		Data_Hash_SHA1,
		Data_Hash_SHA256,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Data_Sort,
		Markup_FetchURL,
		Markup_Tidy,
		Markup_Validate,
		Markup_Parent,
		Markup_Children,
		Markup_AllChildren,
		Markup_Text,
		Markup_AllText,
		Markup_OuterTag,
		Markup_InnerTag,
		Markup_Select_Elements,
		Markup_Select_Text,
		Markup_Select_Comments,
		Insert_GUID,
		Insert_RandomNumber,
		Insert_RandomData,
		Insert_MinMaxValues,
		[KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Keys,
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values6,
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values9,
		[KeyGesture(Key.D1, ModifierKeys.Control)] Keys_SelectionReplace_Values1,
		[KeyGesture(Key.D2, ModifierKeys.Control)] Keys_SelectionReplace_Values2,
		[KeyGesture(Key.D3, ModifierKeys.Control)] Keys_SelectionReplace_Values3,
		[KeyGesture(Key.D4, ModifierKeys.Control)] Keys_SelectionReplace_Values4,
		[KeyGesture(Key.D5, ModifierKeys.Control)] Keys_SelectionReplace_Values5,
		[KeyGesture(Key.D6, ModifierKeys.Control)] Keys_SelectionReplace_Values6,
		[KeyGesture(Key.D7, ModifierKeys.Control)] Keys_SelectionReplace_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control)] Keys_SelectionReplace_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control)] Keys_SelectionReplace_Values9,
		Keys_GlobalFind_Keys,
		Keys_GlobalFind_Values1,
		Keys_GlobalFind_Values2,
		Keys_GlobalFind_Values3,
		Keys_GlobalFind_Values4,
		Keys_GlobalFind_Values5,
		Keys_GlobalFind_Values6,
		Keys_GlobalFind_Values7,
		Keys_GlobalFind_Values8,
		Keys_GlobalFind_Values9,
		Keys_GlobalReplace_Values1,
		Keys_GlobalReplace_Values2,
		Keys_GlobalReplace_Values3,
		Keys_GlobalReplace_Values4,
		Keys_GlobalReplace_Values5,
		Keys_GlobalReplace_Values6,
		Keys_GlobalReplace_Values7,
		Keys_GlobalReplace_Values8,
		Keys_GlobalReplace_Values9,
		Keys_Copy_Keys,
		Keys_Copy_Values1,
		Keys_Copy_Values2,
		Keys_Copy_Values3,
		Keys_Copy_Values4,
		Keys_Copy_Values5,
		Keys_Copy_Values6,
		Keys_Copy_Values7,
		Keys_Copy_Values8,
		Keys_Copy_Values9,
		Keys_Hits_Keys,
		Keys_Hits_Values1,
		Keys_Hits_Values2,
		Keys_Hits_Values3,
		Keys_Hits_Values4,
		Keys_Hits_Values5,
		Keys_Hits_Values6,
		Keys_Hits_Values7,
		Keys_Hits_Values8,
		Keys_Hits_Values9,
		Keys_Misses_Keys,
		Keys_Misses_Values1,
		Keys_Misses_Values2,
		Keys_Misses_Values3,
		Keys_Misses_Values4,
		Keys_Misses_Values5,
		Keys_Misses_Values6,
		Keys_Misses_Values7,
		Keys_Misses_Values8,
		Keys_Misses_Values9,
		Keys_CountstoKeysValues1,
		[KeyGesture(Key.M, ModifierKeys.Control)] SelectRegion_Toggle,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		Select_Limit,
		[KeyGesture(Key.L, ModifierKeys.Control)] Select_Lines,
		Select_Empty,
		Select_NonEmpty,
		Select_Trim,
		Select_Width,
		Select_Unique,
		Select_Duplicates,
		Select_Count,
		Select_Regions,
		Select_FindResults,
		Select_Min_String,
		Select_Min_Numeric,
		Select_Min_Length,
		Select_Max_String,
		Select_Max_Numeric,
		Select_Max_Length,
		Select_ExpressionMatches,
		[KeyGesture(Key.D0, ModifierKeys.Alt)] [KeyGesture(Key.D0, ModifierKeys.Control, false)] Select_FirstSelection,
		[KeyGesture(Key.Space, ModifierKeys.Alt)] [KeyGesture(Key.Space, ModifierKeys.Control, false)] Select_ShowCurrent,
		[KeyGesture(Key.Down, ModifierKeys.Alt)] Select_NextSelection,
		[KeyGesture(Key.Up, ModifierKeys.Alt)] Select_PrevSelection,
		[KeyGesture(Key.Enter, ModifierKeys.Alt)] [KeyGesture(Key.Enter, ModifierKeys.Control, false)] Select_Single,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] [KeyGesture(Key.OemMinus, ModifierKeys.Control, false)] Select_Remove,
		Region_SetSelection,
		Region_SetFindResults,
		Region_ClearRegions,
		Region_LimitToSelection,
		View_Highlighting_None,
		View_Highlighting_CSharp,
		View_Highlighting_CPlusPlus,
		View_Tiles,
		[KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift)] Macro_QuickRecord,
		[KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)] Macro_QuickPlay,
		Macro_Record,
		Macro_StopRecording,
		Macro_Play,
		Macro_PlayOnCopiedFiles,
	}
}
