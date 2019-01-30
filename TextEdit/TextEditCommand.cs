﻿using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit
{
	public enum TextEditCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, 2)] File_New,
		File_NewFromSelections,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open_Open,
		[KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, 2)] File_Open_CopiedCut,
		File_Open_Selected,
		File_OpenWith_Disk,
		[KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)] File_OpenWith_HexEditor,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save_Save,
		File_Save_SaveAs,
		File_Save_SaveAsByExpression,
		File_Save_CopyTo,
		File_Save_CopyToByExpression,
		File_Operations_Rename,
		File_Operations_RenameByExpression,
		File_Operations_Delete,
		File_Operations_Explore,
		File_Operations_CommandPrompt,
		File_Operations_DragDrop,
		File_Operations_VCSDiff,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		[KeyGesture(Key.F5, ModifierKeys.Control)] File_AutoRefresh,
		File_Revert,
		File_Insert_Files,
		File_Insert_CopiedCut,
		File_Insert_Selected,
		[KeyGesture(Key.P, ModifierKeys.Control)] File_Copy_Path,
		File_Copy_Name,
		File_Copy_DisplayName,
		File_Encoding_Encoding,
		File_Encoding_ReopenWithEncoding,
		File_Encoding_LineEndings,
		File_Encryption,
		File_Compress,
		File_Shell_Integrate,
		File_Shell_Unintegrate,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy_Copy,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Copy_Cut,
		[KeyGesture(Key.V, ModifierKeys.Control)] [KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Paste_Paste,
		[KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Alt)] Edit_Paste_RotatePaste,
		[KeyGesture(Key.F, ModifierKeys.Control)] [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Find_Find,
		[KeyGesture(Key.F3)] [KeyGesture(Key.F3, ModifierKeys.Shift, 2)] Edit_Find_Next,
		[KeyGesture(Key.F4)] [KeyGesture(Key.F4, ModifierKeys.Shift, 2)] Edit_Find_Previous,
		[KeyGesture(Key.F3, ModifierKeys.Control)] [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Find_Selected,
		[KeyGesture(Key.M, ModifierKeys.Control)] Edit_Find_MassFind,
		Edit_Find_Replace,
		Edit_Find_ClearSearchResults,
		Edit_CopyDown,
		Edit_Rotate,
		[KeyGesture(Key.R, ModifierKeys.Control)] Edit_Repeat,
		Edit_Markup_Escape,
		Edit_Markup_Unescape,
		Edit_RegEx_Escape,
		Edit_RegEx_Unescape,
		Edit_URL_Escape,
		Edit_URL_Unescape,
		Edit_URL_Absolute,
		Edit_Data_Hash,
		Edit_Data_Compress,
		Edit_Data_Decompress,
		Edit_Data_Encrypt,
		Edit_Data_Decrypt,
		Edit_Data_Sign,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Sort,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Convert,
		[KeyGesture(Key.F2, ModifierKeys.Alt)] Edit_Bookmarks_Toggle,
		[KeyGesture(Key.F2)] [KeyGesture(Key.F2, ModifierKeys.Shift, 2)] Edit_Bookmarks_Next,
		[KeyGesture(Key.F2, ModifierKeys.Control)] [KeyGesture(Key.F2, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Bookmarks_Previous,
		Edit_Bookmarks_Clear,
		[KeyGesture(Key.Left, ModifierKeys.Control)] [KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Navigate_WordLeft,
		[KeyGesture(Key.Right, ModifierKeys.Control)] [KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Navigate_WordRight,
		Edit_Navigate_AllLeft,
		Edit_Navigate_AllRight,
		Edit_Navigate_JumpBy_Words,
		Edit_Navigate_JumpBy_Numbers,
		Edit_Navigate_JumpBy_Paths,
		[KeyGesture(Key.D, ModifierKeys.Control)] [KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift, 2)] Diff_Diff,
		Diff_Selections,
		Diff_SelectedFiles,
		Diff_VCSNormalFiles,
		Diff_Regions_Region1,
		Diff_Regions_Region2,
		Diff_Regions_Region3,
		Diff_Regions_Region4,
		Diff_Regions_Region5,
		Diff_Regions_Region6,
		Diff_Regions_Region7,
		Diff_Regions_Region8,
		Diff_Regions_Region9,
		Diff_Break,
		Diff_IgnoreWhitespace,
		Diff_IgnoreCase,
		Diff_IgnoreNumbers,
		Diff_IgnoreLineEndings,
		Diff_IgnoreCharacters,
		Diff_Reset,
		[KeyGesture(Key.Down, ModifierKeys.Alt)] [KeyGesture(Key.Down, ModifierKeys.Alt | ModifierKeys.Shift, 2)] Diff_Next,
		[KeyGesture(Key.Up, ModifierKeys.Alt)] [KeyGesture(Key.Up, ModifierKeys.Alt | ModifierKeys.Shift, 2)] Diff_Previous,
		[KeyGesture(Key.Left, ModifierKeys.Alt)] Diff_CopyLeft,
		[KeyGesture(Key.Right, ModifierKeys.Alt)] Diff_CopyRight,
		Diff_Fix_Whitespace,
		Diff_Fix_Case,
		Diff_Fix_Numbers,
		Diff_Fix_LineEndings,
		Diff_Select_Match,
		Diff_Select_Diff,
		Diff_Select_LeftTab,
		Diff_Select_RightTab,
		Diff_Select_BothTabs,
		Files_Name_Simplify,
		Files_Name_MakeAbsolute,
		Files_Name_MakeRelative,
		Files_Name_GetUnique,
		Files_Name_Sanitize,
		Files_Get_Size,
		Files_Get_Time_Write,
		Files_Get_Time_Access,
		Files_Get_Time_Create,
		Files_Get_Attributes,
		Files_Get_Version_File,
		Files_Get_Version_Product,
		Files_Get_Version_Assembly,
		Files_Get_Children,
		Files_Get_Descendants,
		Files_Get_VersionControlStatus,
		Files_Set_Size,
		Files_Set_Time_Write,
		Files_Set_Time_Access,
		Files_Set_Time_Create,
		Files_Set_Time_All,
		Files_Set_Attributes,
		Files_Find_Binary,
		Files_Find_Text,
		Files_Find_MassFind,
		Files_Insert,
		Files_Create_Files,
		Files_Create_Directories,
		Files_Create_FromExpressions,
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
		Files_Select_MatchDepth,
		Files_Select_CommonAncestor,
		Files_Select_ByVersionControlStatus,
		Files_Hash,
		Files_Sign,
		Files_Operations_Copy,
		Files_Operations_Move,
		Files_Operations_Delete,
		Files_Operations_DragDrop,
		Files_Operations_OpenDisk,
		Files_Operations_Explore,
		Files_Operations_CommandPrompt,
		Files_Operations_RunCommand_Parallel,
		Files_Operations_RunCommand_Sequential,
		Files_Operations_RunCommand_Shell,
		Files_Operations_Encoding,
		Files_Operations_SplitFile,
		Files_Operations_CombineFiles,
		[KeyGesture(Key.E, ModifierKeys.Control)] Expression_Expression,
		Expression_Copy,
		[KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Alt)] Expression_EvaluateSelected,
		[KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift)] Expression_SelectByExpression,
		Expression_InlineVariables_Add,
		[KeyGesture(Key.F9, ModifierKeys.Control)] Expression_InlineVariables_Calculate,
		Expression_InlineVariables_Solve,
		Expression_InlineVariables_IncludeInExpressions,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] Text_Select_Trim,
		Text_Select_ByWidth,
		[KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Alt)] Text_Select_WholeWord,
		[KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Alt)] Text_Select_BoundedWord,
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
		Text_Unicode,
		Text_GUID,
		Text_RandomText,
		Text_LoremIpsum,
		Text_ReverseRegEx,
		Text_FirstDistinct,
		Text_RepeatCount,
		Text_RepeatIndex,
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
		Numeric_Add_Sum,
		Numeric_Add_ForwardSum,
		Numeric_Add_ReverseSum,
		Numeric_Add_UndoForwardSum,
		Numeric_Add_UndoReverseSum,
		[KeyGesture(Key.Add, ModifierKeys.Control)] [KeyGesture(Key.OemPlus, ModifierKeys.Control, 2)] Numeric_Add_Increment,
		[KeyGesture(Key.Subtract, ModifierKeys.Control)] [KeyGesture(Key.OemMinus, ModifierKeys.Control, 2)] Numeric_Add_Decrement,
		[KeyGesture(Key.Add, ModifierKeys.Control | ModifierKeys.Shift)] [KeyGesture(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Shift, 2)] Numeric_Add_AddClipboard,
		[KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift)] [KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift, 2)] Numeric_Add_SubtractClipboard,
		Numeric_Whole,
		Numeric_Fraction,
		Numeric_Absolute,
		Numeric_Floor,
		Numeric_Ceiling,
		Numeric_Round,
		Numeric_Limit,
		Numeric_Cycle,
		Numeric_Trim,
		Numeric_Factor,
		Numeric_RandomNumber,
		Numeric_CombinationsPermutations,
		Numeric_MinMaxValues,
		DateTime_Now,
		DateTime_UtcNow,
		DateTime_Convert,
		Image_GrabColor,
		Image_GrabImage,
		Image_AdjustColor,
		Image_AddColor,
		Image_OverlayColor,
		Image_Size,
		Image_Crop,
		Image_FlipHorizontal,
		Image_FlipVertical,
		Image_Rotate,
		Image_GIF_Animate,
		Image_GIF_Split,
		Table_DetectType,
		Table_Convert,
		Table_TextToTable,
		Table_LineSelectionsToTable,
		Table_RegionSelectionsToTable_Region1,
		Table_RegionSelectionsToTable_Region2,
		Table_RegionSelectionsToTable_Region3,
		Table_RegionSelectionsToTable_Region4,
		Table_RegionSelectionsToTable_Region5,
		Table_RegionSelectionsToTable_Region6,
		Table_RegionSelectionsToTable_Region7,
		Table_RegionSelectionsToTable_Region8,
		Table_RegionSelectionsToTable_Region9,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift)] Table_EditTable,
		Table_AddHeaders,
		Table_AddRow,
		Table_AddColumn,
		Table_Select_RowsByExpression,
		[KeyGesture(Key.J, ModifierKeys.Control | ModifierKeys.Shift)] Table_SetJoinSource,
		[KeyGesture(Key.J, ModifierKeys.Control)] Table_Join,
		Table_Transpose,
		Table_Database_GenerateInserts,
		Table_Database_GenerateUpdates,
		Table_Database_GenerateDeletes,
		[KeyGesture(Key.G, ModifierKeys.Control)] [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift, 2)] Position_Goto_Lines,
		Position_Goto_Columns,
		Position_Goto_Indexes,
		Position_Goto_Positions,
		Position_Goto_FilesLines,
		Position_Copy_Lines,
		Position_Copy_Columns,
		Position_Copy_Indexes,
		Position_Copy_Positions,
		Content_Type_SetFromExtension,
		Content_Type_None,
		Content_Type_Balanced,
		Content_Type_Columns,
		Content_Type_CPlusPlus,
		Content_Type_CSharp,
		Content_Type_CSV,
		Content_Type_ExactColumns,
		Content_Type_HTML,
		Content_Type_JSON,
		Content_Type_SQL,
		Content_Type_TSV,
		Content_Type_XML,
		Content_HighlightSyntax,
		Content_Reformat,
		[KeyGesture(Key.OemQuestion, ModifierKeys.Control)] Content_Comment,
		[KeyGesture(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Shift)] Content_Uncomment,
		[KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 2)] Content_TogglePosition,
		[KeyGesture(Key.Enter, ModifierKeys.Control | ModifierKeys.Alt)] Content_Current,
		Content_Parent,
		Content_Ancestor,
		Content_Attributes,
		Content_WithAttribute,
		Content_Children_Children,
		Content_Children_SelfAndChildren,
		Content_Children_First,
		Content_Children_WithAttribute,
		Content_Descendants_Descendants,
		Content_Descendants_SelfAndDescendants,
		Content_Descendants_First,
		Content_Descendants_WithAttribute,
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
		Content_KeepSelections,
		Network_AbsoluteURL,
		Network_Fetch,
		Network_FetchHex,
		Network_FetchFile,
		Network_Lookup_IP,
		Network_Lookup_HostName,
		Network_AdaptersInfo,
		Network_Ping,
		Network_ScanPorts,
		Database_Connect,
		[KeyGesture(Key.Q, ModifierKeys.Control)] Database_ExecuteQuery,
		[KeyGesture(Key.Q, ModifierKeys.Control | ModifierKeys.Shift)] Database_QueryBuilder,
		Database_Examine,
		Database_GetSproc,
		[KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_KeysCaseSensitive,
		[KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Keys_Set_KeysCaseInsensitive,
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_Values5,
		Keys_Set_Values6,
		Keys_Set_Values7,
		Keys_Set_Values8,
		Keys_Set_Values9,
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
		Keys_Remove_Keys,
		Keys_Remove_Values1,
		Keys_Remove_Values2,
		Keys_Remove_Values3,
		Keys_Remove_Values4,
		Keys_Remove_Values5,
		Keys_Remove_Values6,
		Keys_Remove_Values7,
		Keys_Remove_Values8,
		Keys_Remove_Values9,
		[KeyGesture(Key.D1, ModifierKeys.Control)] Keys_Replace_Values1,
		[KeyGesture(Key.D2, ModifierKeys.Control)] Keys_Replace_Values2,
		[KeyGesture(Key.D3, ModifierKeys.Control)] Keys_Replace_Values3,
		[KeyGesture(Key.D4, ModifierKeys.Control)] Keys_Replace_Values4,
		[KeyGesture(Key.D5, ModifierKeys.Control)] Keys_Replace_Values5,
		Keys_Replace_Values6,
		Keys_Replace_Values7,
		Keys_Replace_Values8,
		Keys_Replace_Values9,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		Select_Nothing,
		Select_Limit,
		[KeyGesture(Key.L, ModifierKeys.Control)] Select_Lines,
		[KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Alt)] Select_WholeLines,
		Select_Rectangle,
		[KeyGesture(Key.I, ModifierKeys.Control)] Select_Invert,
		Select_Join,
		Select_Empty,
		Select_NonEmpty,
		[KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control)] [KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift, 2)] Select_ToggleOpenClose,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt)] Select_Repeats_Unique,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt)] Select_Repeats_Duplicates,
		Select_Repeats_MatchPrevious,
		Select_Repeats_NonMatchPrevious,
		Select_Repeats_RepeatedLines,
		[KeyGesture(Key.B, ModifierKeys.Control)] Select_Repeats_ByCount,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Select_Repeats_CaseInsensitive_Unique,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Select_Repeats_CaseInsensitive_Duplicates,
		Select_Repeats_CaseInsensitive_MatchPrevious,
		Select_Repeats_CaseInsensitive_NonMatchPrevious,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt)] Select_Split,
		[KeyGesture(Key.D0, ModifierKeys.Alt)] Select_Selection_First,
		[KeyGesture(Key.Space, ModifierKeys.Alt)] Select_Selection_CenterVertically,
		[KeyGesture(Key.Space, ModifierKeys.Alt | ModifierKeys.Shift)] Select_Selection_Center,
		[KeyGesture(Key.Space, ModifierKeys.Control)] Select_Selection_ToggleAnchor,
		[KeyGesture(Key.OemPlus, ModifierKeys.Alt)] Select_Selection_Next,
		[KeyGesture(Key.OemMinus, ModifierKeys.Alt)] Select_Selection_Previous,
		[KeyGesture(Key.Enter, ModifierKeys.Alt)] Select_Selection_Single,
		[KeyGesture(Key.Back, ModifierKeys.Alt)] Select_Selection_Remove,
		Select_Selection_RemoveBeforeCurrent,
		Select_Selection_RemoveAfterCurrent,
		[KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift)] Region_ModifyRegions,
		Region_SetSelections_Region1,
		Region_SetSelections_Region2,
		Region_SetSelections_Region3,
		Region_SetSelections_Region4,
		Region_SetSelections_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift)] Region_SetSelections_Region6,
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Shift)] Region_SetSelections_Region7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Shift)] Region_SetSelections_Region8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Shift)] Region_SetSelections_Region9,
		Region_SetSelections_All,
		Region_AddSelections_Region1,
		Region_AddSelections_Region2,
		Region_AddSelections_Region3,
		Region_AddSelections_Region4,
		Region_AddSelections_Region5,
		Region_AddSelections_Region6,
		Region_AddSelections_Region7,
		Region_AddSelections_Region8,
		Region_AddSelections_Region9,
		Region_AddSelections_All,
		Region_RemoveSelections_Region1,
		Region_RemoveSelections_Region2,
		Region_RemoveSelections_Region3,
		Region_RemoveSelections_Region4,
		Region_RemoveSelections_Region5,
		Region_RemoveSelections_Region6,
		Region_RemoveSelections_Region7,
		Region_RemoveSelections_Region8,
		Region_RemoveSelections_Region9,
		Region_RemoveSelections_All,
		Region_ReplaceSelections_Region1,
		Region_ReplaceSelections_Region2,
		Region_ReplaceSelections_Region3,
		Region_ReplaceSelections_Region4,
		Region_ReplaceSelections_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Region_ReplaceSelections_Region6,
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Region_ReplaceSelections_Region7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Region_ReplaceSelections_Region8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Region_ReplaceSelections_Region9,
		Region_ReplaceSelections_All,
		Region_LimitToSelections_Region1,
		Region_LimitToSelections_Region2,
		Region_LimitToSelections_Region3,
		Region_LimitToSelections_Region4,
		Region_LimitToSelections_Region5,
		Region_LimitToSelections_Region6,
		Region_LimitToSelections_Region7,
		Region_LimitToSelections_Region8,
		Region_LimitToSelections_Region9,
		Region_LimitToSelections_All,
		Region_Clear_Region1,
		Region_Clear_Region2,
		Region_Clear_Region3,
		Region_Clear_Region4,
		Region_Clear_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Alt)] Region_Clear_Region6,
		[KeyGesture(Key.D7, ModifierKeys.Alt)] Region_Clear_Region7,
		[KeyGesture(Key.D8, ModifierKeys.Alt)] Region_Clear_Region8,
		[KeyGesture(Key.D9, ModifierKeys.Alt)] Region_Clear_Region9,
		[KeyGesture(Key.D0, ModifierKeys.Control | ModifierKeys.Shift)] Region_Clear_All,
		Region_RepeatBySelections_Region1,
		Region_RepeatBySelections_Region2,
		Region_RepeatBySelections_Region3,
		Region_RepeatBySelections_Region4,
		Region_RepeatBySelections_Region5,
		Region_RepeatBySelections_Region6,
		Region_RepeatBySelections_Region7,
		Region_RepeatBySelections_Region8,
		Region_RepeatBySelections_Region9,
		Region_CopyEnclosingRegion_Region1,
		Region_CopyEnclosingRegion_Region2,
		Region_CopyEnclosingRegion_Region3,
		Region_CopyEnclosingRegion_Region4,
		Region_CopyEnclosingRegion_Region5,
		Region_CopyEnclosingRegion_Region6,
		Region_CopyEnclosingRegion_Region7,
		Region_CopyEnclosingRegion_Region8,
		Region_CopyEnclosingRegion_Region9,
		Region_CopyEnclosingRegionIndex_Region1,
		Region_CopyEnclosingRegionIndex_Region2,
		Region_CopyEnclosingRegionIndex_Region3,
		Region_CopyEnclosingRegionIndex_Region4,
		Region_CopyEnclosingRegionIndex_Region5,
		Region_CopyEnclosingRegionIndex_Region6,
		Region_CopyEnclosingRegionIndex_Region7,
		Region_CopyEnclosingRegionIndex_Region8,
		Region_CopyEnclosingRegionIndex_Region9,
		Region_TransformSelections_Flatten_Region1,
		Region_TransformSelections_Flatten_Region2,
		Region_TransformSelections_Flatten_Region3,
		Region_TransformSelections_Flatten_Region4,
		Region_TransformSelections_Flatten_Region5,
		Region_TransformSelections_Flatten_Region6,
		Region_TransformSelections_Flatten_Region7,
		Region_TransformSelections_Flatten_Region8,
		Region_TransformSelections_Flatten_Region9,
		Region_TransformSelections_Transpose_Region1,
		Region_TransformSelections_Transpose_Region2,
		Region_TransformSelections_Transpose_Region3,
		Region_TransformSelections_Transpose_Region4,
		Region_TransformSelections_Transpose_Region5,
		Region_TransformSelections_Transpose_Region6,
		Region_TransformSelections_Transpose_Region7,
		Region_TransformSelections_Transpose_Region8,
		Region_TransformSelections_Transpose_Region9,
		Region_TransformSelections_RotateLeft_Region1,
		Region_TransformSelections_RotateLeft_Region2,
		Region_TransformSelections_RotateLeft_Region3,
		Region_TransformSelections_RotateLeft_Region4,
		Region_TransformSelections_RotateLeft_Region5,
		Region_TransformSelections_RotateLeft_Region6,
		Region_TransformSelections_RotateLeft_Region7,
		Region_TransformSelections_RotateLeft_Region8,
		Region_TransformSelections_RotateLeft_Region9,
		Region_TransformSelections_RotateRight_Region1,
		Region_TransformSelections_RotateRight_Region2,
		Region_TransformSelections_RotateRight_Region3,
		Region_TransformSelections_RotateRight_Region4,
		Region_TransformSelections_RotateRight_Region5,
		Region_TransformSelections_RotateRight_Region6,
		Region_TransformSelections_RotateRight_Region7,
		Region_TransformSelections_RotateRight_Region8,
		Region_TransformSelections_RotateRight_Region9,
		Region_TransformSelections_Rotate180_Region1,
		Region_TransformSelections_Rotate180_Region2,
		Region_TransformSelections_Rotate180_Region3,
		Region_TransformSelections_Rotate180_Region4,
		Region_TransformSelections_Rotate180_Region5,
		Region_TransformSelections_Rotate180_Region6,
		Region_TransformSelections_Rotate180_Region7,
		Region_TransformSelections_Rotate180_Region8,
		Region_TransformSelections_Rotate180_Region9,
		Region_TransformSelections_MirrorHorizontal_Region1,
		Region_TransformSelections_MirrorHorizontal_Region2,
		Region_TransformSelections_MirrorHorizontal_Region3,
		Region_TransformSelections_MirrorHorizontal_Region4,
		Region_TransformSelections_MirrorHorizontal_Region5,
		Region_TransformSelections_MirrorHorizontal_Region6,
		Region_TransformSelections_MirrorHorizontal_Region7,
		Region_TransformSelections_MirrorHorizontal_Region8,
		Region_TransformSelections_MirrorHorizontal_Region9,
		Region_TransformSelections_MirrorVertical_Region1,
		Region_TransformSelections_MirrorVertical_Region2,
		Region_TransformSelections_MirrorVertical_Region3,
		Region_TransformSelections_MirrorVertical_Region4,
		Region_TransformSelections_MirrorVertical_Region5,
		Region_TransformSelections_MirrorVertical_Region6,
		Region_TransformSelections_MirrorVertical_Region7,
		Region_TransformSelections_MirrorVertical_Region8,
		Region_TransformSelections_MirrorVertical_Region9,
		Region_Select_Regions_Region1,
		Region_Select_Regions_Region2,
		Region_Select_Regions_Region3,
		Region_Select_Regions_Region4,
		Region_Select_Regions_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control)] Region_Select_Regions_Region6,
		[KeyGesture(Key.D7, ModifierKeys.Control)] Region_Select_Regions_Region7,
		[KeyGesture(Key.D8, ModifierKeys.Control)] Region_Select_Regions_Region8,
		[KeyGesture(Key.D9, ModifierKeys.Control)] Region_Select_Regions_Region9,
		Region_Select_Regions_All,
		Region_Select_EnclosingRegion_Region1,
		Region_Select_EnclosingRegion_Region2,
		Region_Select_EnclosingRegion_Region3,
		Region_Select_EnclosingRegion_Region4,
		Region_Select_EnclosingRegion_Region5,
		Region_Select_EnclosingRegion_Region6,
		Region_Select_EnclosingRegion_Region7,
		Region_Select_EnclosingRegion_Region8,
		Region_Select_EnclosingRegion_Region9,
		Region_Select_WithEnclosingRegion_Region1,
		Region_Select_WithEnclosingRegion_Region2,
		Region_Select_WithEnclosingRegion_Region3,
		Region_Select_WithEnclosingRegion_Region4,
		Region_Select_WithEnclosingRegion_Region5,
		Region_Select_WithEnclosingRegion_Region6,
		Region_Select_WithEnclosingRegion_Region7,
		Region_Select_WithEnclosingRegion_Region8,
		Region_Select_WithEnclosingRegion_Region9,
		Region_Select_WithoutEnclosingRegion_Region1,
		Region_Select_WithoutEnclosingRegion_Region2,
		Region_Select_WithoutEnclosingRegion_Region3,
		Region_Select_WithoutEnclosingRegion_Region4,
		Region_Select_WithoutEnclosingRegion_Region5,
		Region_Select_WithoutEnclosingRegion_Region6,
		Region_Select_WithoutEnclosingRegion_Region7,
		Region_Select_WithoutEnclosingRegion_Region8,
		Region_Select_WithoutEnclosingRegion_Region9,
		[KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt)] View_Full,
		[KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Alt)] View_Grid,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt)] View_CustomGrid,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt)] View_ActiveTabs,
		View_TabIndex,
		View_ActiveTabIndex,
		View_FontSize,
		View_Select_TabsWithSelections,
		View_Select_TabsWithoutSelections,
		View_Select_TabsWithSelectionsToTop,
		View_Close_TabsWithSelections,
		View_Close_TabsWithoutSelections,
		View_Close_ActiveTabs,
		View_Close_InactiveTabs,
		[KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Alt)] View_NewWindow,
		View_WordList,
		Macro_Record_Quick_1,
		Macro_Record_Quick_2,
		Macro_Record_Quick_3,
		Macro_Record_Quick_4,
		Macro_Record_Quick_5,
		[KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_6,
		[KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_7,
		[KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_8,
		[KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_9,
		[KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_10,
		[KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_11,
		[KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_12,
		Macro_Record_Record,
		Macro_Record_StopRecording,
		Macro_Append_Quick_1,
		Macro_Append_Quick_2,
		Macro_Append_Quick_3,
		Macro_Append_Quick_4,
		Macro_Append_Quick_5,
		Macro_Append_Quick_6,
		Macro_Append_Quick_7,
		Macro_Append_Quick_8,
		Macro_Append_Quick_9,
		Macro_Append_Quick_10,
		Macro_Append_Quick_11,
		Macro_Append_Quick_12,
		Macro_Append_Append,
		Macro_Play_Quick_1,
		Macro_Play_Quick_2,
		Macro_Play_Quick_3,
		Macro_Play_Quick_4,
		Macro_Play_Quick_5,
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
		Macro_Open_Quick_1,
		Macro_Open_Quick_2,
		Macro_Open_Quick_3,
		Macro_Open_Quick_4,
		Macro_Open_Quick_5,
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
