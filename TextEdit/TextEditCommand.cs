﻿using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit
{
	public enum TextEditCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, 2)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open_Open,
		[KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, 2)] File_Open_CopiedCut,
		File_Open_Selected,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt)] File_OpenWith_Disk,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)] File_OpenWith_HexEditor,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save_Save,
		File_Save_SaveAs,
		File_Operations_Rename,
		File_Operations_Delete,
		File_Operations_Explore,
		File_Operations_CommandPrompt,
		File_Operations_DragDrop,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		File_Revert,
		File_Insert_Files,
		File_Insert_CopiedCut,
		File_Insert_Selected,
		File_Copy_Path,
		File_Copy_Name,
		File_Copy_AllPaths,
		File_Copy_Count,
		File_Encoding_Encoding,
		File_Encoding_ReopenWithEncoding,
		File_Encryption,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy_Copy,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_Copy_AllFiles,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Copy_Cut,
		[KeyGesture(Key.V, ModifierKeys.Control)] [KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Paste_Paste,
		[KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_Paste_AllFiles,
		[KeyGesture(Key.F, ModifierKeys.Control)] [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Find_Find,
		[KeyGesture(Key.F3)] [KeyGesture(Key.F3, ModifierKeys.Shift, 2)] Edit_Find_Next,
		[KeyGesture(Key.F3, ModifierKeys.Control)] [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Find_Previous,
		Edit_Find_Replace,
		Edit_CopyDown,
		[KeyGesture(Key.R, ModifierKeys.Control)] Edit_Repeat,
		Edit_Markup_Escape,
		Edit_Markup_Unescape,
		Edit_RegEx_Escape,
		Edit_RegEx_Unescape,
		Edit_URL_Escape,
		Edit_URL_Unescape,
		Edit_URL_Absolute,
		Edit_Color,
		Edit_Hash,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Sort,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Convert,
		[KeyGesture(Key.F2, ModifierKeys.Alt)] Edit_Bookmarks_Toggle,
		[KeyGesture(Key.F2)] [KeyGesture(Key.F2, ModifierKeys.Shift, 2)] Edit_Bookmarks_Next,
		[KeyGesture(Key.F2, ModifierKeys.Control)] [KeyGesture(Key.F2, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Bookmarks_Previous,
		Edit_Bookmarks_Clear,
		[KeyGesture(Key.D, ModifierKeys.Control)] [KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift)] Diff_Diff,
		Diff_Selections,
		Diff_SelectedFiles,
		Diff_Break,
		Diff_IgnoreWhitespace,
		Diff_IgnoreCase,
		Diff_IgnoreNumbers,
		Diff_IgnoreLineEndings,
		[KeyGesture(Key.Down, ModifierKeys.Alt)] Diff_Next,
		[KeyGesture(Key.Up, ModifierKeys.Alt)] Diff_Previous,
		[KeyGesture(Key.Left, ModifierKeys.Alt)] Diff_CopyLeft,
		[KeyGesture(Key.Right, ModifierKeys.Alt)] Diff_CopyRight,
		Diff_SelectMatch,
		Diff_SelectNonMatch,
		Files_Names_Simplify,
		Files_Names_MakeAbsolute,
		Files_Names_GetUnique,
		Files_Names_Sanitize,
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
		Files_Directory_GetChildren,
		Files_Directory_GetDescendants,
		Files_Insert,
		Files_Select_Name_Directory,
		Files_Select_Name_Name,
		Files_Select_Name_FileNamewoExtension,
		Files_Select_Name_Extension,
		Files_Select_Files,
		Files_Select_Directories,
		Files_Select_Existing,
		Files_Select_NonExisting,
		Files_Select_Roots,
		Files_Select_NonRoots,
		Files_Hash,
		Files_Operations_Copy,
		Files_Operations_Move,
		Files_Operations_Delete,
		Files_Operations_SaveClipboards,
		Files_Operations_DragDrop,
		Files_Operations_OpenDisk,
		Files_Operations_Explore,
		Files_Operations_CommandPrompt,
		Files_Operations_Create_Files,
		Files_Operations_Create_Directories,
		Files_Operations_Create_FromExpressions,
		Files_Operations_RunCommand_Parallel,
		Files_Operations_RunCommand_Sequential,
		[KeyGesture(Key.E, ModifierKeys.Control)] Expression_Expression,
		Expression_Copy,
		[KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift)] Expression_EvaluateSelected,
		Expression_SelectByExpression,
		Expression_ClearVariables,
		Expression_SetVariables,
		Text_Copy_Length,
		Text_Copy_Min_Text,
		Text_Copy_Min_Length,
		Text_Copy_Max_Text,
		Text_Copy_Max_Length,
		Text_Select_Trim,
		Text_Select_ByWidth,
		Text_Select_Min_Text,
		Text_Select_Min_Length,
		Text_Select_Max_Text,
		Text_Select_Max_Length,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift)] Text_Case_Upper,
		[KeyGesture(Key.U, ModifierKeys.Control)] Text_Case_Lower,
		Text_Case_Proper,
		Text_Case_Toggle,
		Text_Length,
		[KeyGesture(Key.W, ModifierKeys.Control)] Text_Width,
		[KeyGesture(Key.T, ModifierKeys.Control)] Text_Trim,
		Text_SingleLine,
		Text_GUID,
		Text_RandomText,
		Text_LoremIpsum,
		Text_ReverseRegEx,
		Text_FirstDistinct,
		Numeric_Copy_Min,
		Numeric_Copy_Max,
		Numeric_Copy_Sum,
		Numeric_Copy_GCF,
		Numeric_Copy_LCM,
		Numeric_Select_Min,
		Numeric_Select_Max,
		Numeric_Select_Whole,
		Numeric_Select_Fraction,
		[KeyGesture(Key.H, ModifierKeys.Control)] Numeric_Hex_ToHex,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift)] Numeric_Hex_FromHex,
		Numeric_ConvertBase,
		Numeric_Series_ZeroBased,
		Numeric_Series_OneBased,
		Numeric_Series_Linear,
		Numeric_Series_Geometric,
		Numeric_Scale,
		Numeric_ForwardSum,
		Numeric_ReverseSum,
		Numeric_Whole,
		Numeric_Fraction,
		Numeric_Floor,
		Numeric_Round,
		Numeric_Ceiling,
		Numeric_Factor,
		Numeric_RandomNumber,
		Numeric_CombinationsPermutations,
		Numeric_MinMaxValues,
		DateTime_Now,
		DateTime_Convert,
		Table_DetectType,
		Table_Convert,
		Table_AddHeaders,
		Table_LineSelectionsToTable,
		Table_RegionSelectionsToTable,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift)] Table_EditTable,
		Table_AddColumn,
		Table_Select_RowsByExpression,
		[KeyGesture(Key.J, ModifierKeys.Control | ModifierKeys.Shift)] Table_SetJoinSource,
		[KeyGesture(Key.J, ModifierKeys.Control)] Table_Join,
		Table_Transpose,
		Table_SetVariables,
		Table_Database_GenerateInserts,
		Table_Database_GenerateUpdates,
		Table_Database_GenerateDeletes,
		[KeyGesture(Key.G, ModifierKeys.Control)] [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift, 2)] Position_Goto_Lines,
		Position_Goto_Columns,
		Position_Goto_Positions,
		Position_Goto_FilesLines,
		Position_Copy_Lines,
		Position_Copy_Columns,
		Position_Copy_Positions,
		Content_Type_SetFromExtension,
		Content_Type_None,
		Content_Type_Balanced,
		Content_Type_Columns,
		Content_Type_CSharp,
		Content_Type_CSV,
		Content_Type_HTML,
		Content_Type_JSON,
		Content_Type_TSV,
		Content_Type_XML,
		Content_Reformat,
		Content_Comment,
		Content_Uncomment,
		[KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Content_TogglePosition,
		[KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control | ModifierKeys.Alt)] Content_Current,
		Content_Parent,
		Content_Ancestor,
		Content_Attributes,
		Content_Children_Children,
		Content_Children_SelfAndChildren,
		Content_Children_First,
		Content_Children_Attribute,
		Content_Descendants_Descendants,
		Content_Descendants_SelfAndDescendants,
		Content_Descendants_First,
		Content_Descendants_Attribute,
		[KeyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_Up,
		[KeyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_Down,
		[KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_Left,
		[KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_Right,
		[KeyGesture(Key.Home, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.Home, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_Home,
		[KeyGesture(Key.End, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.End, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_End,
		[KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_PgUp,
		[KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_Navigate_PgDn,
		[KeyGesture(Key.Space, ModifierKeys.Control | ModifierKeys.Alt)] Content_Navigate_Row,
		[KeyGesture(Key.Space, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Content_Navigate_Column,
		Content_Select_Attribute,
		Network_Fetch,
		Network_Fetch_Hex,
		Network_Lookup_IP,
		Network_Lookup_HostName,
		Network_AdaptersInfo,
		Network_Ping,
		Network_ScanPorts,
		Database_Connect,
		[KeyGesture(Key.Q, ModifierKeys.Control)] Database_ExecuteQuery,
		Database_UseCurrentWindow,
		[KeyGesture(Key.Q, ModifierKeys.Control | ModifierKeys.Shift)] Database_QueryTable,
		Database_Examine,
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
		Keys_Add_Keys,
		Keys_Add_Values1,
		Keys_Add_Values2,
		Keys_Add_Values3,
		Keys_Add_Values4,
		Keys_Add_Values5,
		Keys_Add_Values6,
		Keys_Add_Values7,
		Keys_Add_Values8,
		Keys_Add_Values9,
		[KeyGesture(Key.D1, ModifierKeys.Control)] Keys_Replace_Values1,
		[KeyGesture(Key.D2, ModifierKeys.Control)] Keys_Replace_Values2,
		[KeyGesture(Key.D3, ModifierKeys.Control)] Keys_Replace_Values3,
		[KeyGesture(Key.D4, ModifierKeys.Control)] Keys_Replace_Values4,
		[KeyGesture(Key.D5, ModifierKeys.Control)] Keys_Replace_Values5,
		[KeyGesture(Key.D6, ModifierKeys.Control)] Keys_Replace_Values6,
		[KeyGesture(Key.D7, ModifierKeys.Control)] Keys_Replace_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control)] Keys_Replace_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control)] Keys_Replace_Values9,
		Keys_Find_Keys,
		Keys_Find_Values1,
		Keys_Find_Values2,
		Keys_Find_Values3,
		Keys_Find_Values4,
		Keys_Find_Values5,
		Keys_Find_Values6,
		Keys_Find_Values7,
		Keys_Find_Values8,
		Keys_Find_Values9,
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
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		Select_Nothing,
		Select_Limit,
		[KeyGesture(Key.L, ModifierKeys.Control)] Select_Lines,
		Select_Rectangle,
		Select_Rotate,
		Select_Invert,
		Select_Join,
		Select_Empty,
		Select_NonEmpty,
		Select_Unique,
		Select_Duplicates,
		Select_RepeatedLines,
		Select_ByCount,
		Select_Split,
		Select_Regions,
		Select_FindResults,
		[KeyGesture(Key.D0, ModifierKeys.Alt)] Select_Selection_First,
		[KeyGesture(Key.Space, ModifierKeys.Alt)] Select_Selection_CenterVertically,
		[KeyGesture(Key.Space, ModifierKeys.Alt | ModifierKeys.Shift)] Select_Selection_Center,
		[KeyGesture(Key.OemPlus, ModifierKeys.Alt)] Select_Selection_Next,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] Select_Selection_Previous,
		[KeyGesture(Key.Enter, ModifierKeys.Alt)] Select_Selection_Single,
		[KeyGesture(Key.Back, ModifierKeys.Alt)] Select_Selection_Remove,
		[KeyGesture(Key.M, ModifierKeys.Control)] Region_ToggleRegionsSelections,
		Region_SetSelection,
		Region_SetFindResults,
		Region_ClearRegions,
		Region_LimitToSelection,
		Region_WithEnclosingRegion,
		Region_WithoutEnclosingRegion,
		Region_SelectEnclosingRegion,
		Region_CopyEnclosingRegion,
		View_Highlighting_None,
		View_Highlighting_CSharp,
		View_Highlighting_CPlusPlus,
		View_Full,
		View_Grid,
		View_CustomGrid,
		View_ActiveTabs,
		View_SelectTabsWithSelections,
		View_SelectTabsWithoutSelections,
		View_WordList,
		[KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_6,
		[KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_7,
		[KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_8,
		[KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_9,
		[KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_10,
		[KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_11,
		[KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_12,
		Macro_Record_Record,
		Macro_Record_StopRecording,
		Macro_Append_Quick_6,
		Macro_Append_Quick_7,
		Macro_Append_Quick_8,
		Macro_Append_Quick_9,
		Macro_Append_Quick_10,
		Macro_Append_Quick_11,
		Macro_Append_Quick_12,
		Macro_Append_Append,
		[KeyGesture(Key.F6)] Macro_Play_Quick_6,
		[KeyGesture(Key.F7)] Macro_Play_Quick_7,
		[KeyGesture(Key.F8)] Macro_Play_Quick_8,
		[KeyGesture(Key.F9)] Macro_Play_Quick_9,
		[KeyGesture(Key.F10)] Macro_Play_Quick_10,
		[KeyGesture(Key.F11)] Macro_Play_Quick_11,
		[KeyGesture(Key.F12)] Macro_Play_Quick_12,
		Macro_Play_Play,
		Macro_Play_Repeat,
		Macro_Play_PlayOnCopiedFiles,
		[KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_6,
		[KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_7,
		[KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_8,
		[KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_9,
		[KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_10,
		[KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_11,
		[KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Alt)] Macro_Open_Quick_12,
		Macro_Open_Open,
		[KeyGesture(Key.OemPeriod, ModifierKeys.Control)] Macro_RepeatLastAction,
		Macro_TimeNextAction,
	}
}
