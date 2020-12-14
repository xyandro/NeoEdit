﻿namespace NeoEdit.Common
{
	public enum NECommand
	{
		None,
		[NoMacro] Internal_CommandLine,
		[NoMacro] Internal_Activate,
		[NoMacro] Internal_CloseFile,
		Internal_Key,
		Internal_Text,
		Internal_SetBinaryValue,
		[NoMacro] Internal_Scroll,
		[NoMacro] Internal_Mouse,
		[NoMacro] Internal_Redraw,
		[KeyGesture("A", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] File_Select_All,
		File_Select_None,
		File_Select_WithSelections,
		File_Select_WithoutSelections,
		File_Select_Modified,
		File_Select_Unmodified,
		File_Select_ExternalModified,
		File_Select_ExternalUnmodified,
		File_Select_Inactive,
		[NoMacro] [KeyGesture("A", Modifiers.Control | Modifiers.Alt)] File_Select_Choose,
		[KeyGesture("N", Modifiers.Control)] File_New_New,
		File_New_FromSelections_All,
		File_New_FromSelections_Files,
		File_New_FromSelections_Selections,
		File_New_FromClipboard_All,
		File_New_FromClipboard_Files,
		File_New_FromClipboard_Selections,
		File_New_WordList,
		[KeyGesture("O", Modifiers.Control)] File_Open_Open,
		[KeyGesture("O", Modifiers.Control | Modifiers.Alt)] File_Open_CopiedCut,
		File_Open_ReopenWithEncoding,
		[KeyGesture("F5")] File_Refresh,
		File_AutoRefresh,
		File_Revert,
		[KeyGesture("S", Modifiers.Control)] File_Save_SaveModified,
		[KeyGesture("S", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] File_Save_SaveAll,
		File_Save_SaveAs,
		File_Save_SaveAsByExpression,
		File_Move_Move,
		File_Move_MoveByExpression,
		File_Copy_Copy,
		File_Copy_CopyByExpression,
		[KeyGesture("P", Modifiers.Control)] File_Copy_Path,
		File_Copy_Name,
		File_Copy_DisplayName,
		File_Delete,
		File_Encoding,
		File_LineEndings,
		File_FileIndex,
		File_ActiveFileIndex,
		File_Advanced_Compress,
		File_Advanced_Encrypt,
		File_Advanced_Explore,
		File_Advanced_CommandPrompt,
		File_Advanced_DragDrop,
		File_Advanced_SetDisplayName,
		File_Advanced_DontExitOnClose,
		[KeyGesture("F4", Modifiers.Control)] File_Close_Active,
		File_Close_Inactive,
		File_Close_WithSelections,
		File_Close_WithoutSelections,
		File_Close_Modified,
		File_Close_Unmodified,
		File_Close_ExternalModified,
		File_Close_ExternalUnmodified,
		File_Exit,
		[KeyGesture("A", Modifiers.Control)] Edit_Select_All,
		Edit_Select_Nothing,
		[KeyGesture("J", Modifiers.Control)] Edit_Select_Join,
		[KeyGesture("I", Modifiers.Control)] Edit_Select_Invert,
		Edit_Select_Limit,
		[KeyGesture("L", Modifiers.Control)] Edit_Select_Lines,
		[KeyGesture("L", Modifiers.Control | Modifiers.Alt)] Edit_Select_WholeLines,
		Edit_Select_Empty,
		Edit_Select_NonEmpty,
		Edit_Select_Overlap_DeOverlap,
		Edit_Select_Overlap_AllowOverlappingSelections,
		[KeyGesture("Space", Modifiers.Control)] Edit_Select_ToggleAnchor,
		[KeyGesture("0", Modifiers.Alt)] Edit_Select_Focused_First,
		[KeyGesture("+", Modifiers.Alt)] Edit_Select_Focused_Next,
		[KeyGesture("-", Modifiers.Alt)] Edit_Select_Focused_Previous,
		[KeyGesture("Enter", Modifiers.Alt)] Edit_Select_Focused_Single,
		[KeyGesture("Back", Modifiers.Alt)] Edit_Select_Focused_Remove,
		Edit_Select_Focused_RemoveBeforeCurrent,
		Edit_Select_Focused_RemoveAfterCurrent,
		[KeyGesture("Space", Modifiers.Alt)] Edit_Select_Focused_CenterVertically,
		[KeyGesture("Space", Modifiers.Alt | Modifiers.Shift)] Edit_Select_Focused_Center,
		[KeyGesture("C", Modifiers.Control)] Edit_Copy,
		[KeyGesture("X", Modifiers.Control)] Edit_Cut,
		[KeyGesture("V", Modifiers.Control)] [KeyGesture("V", Modifiers.Control | Modifiers.Shift, 2)] Edit_Paste_Paste,
		[KeyGesture("V", Modifiers.Control | Modifiers.Alt)] Edit_Paste_RotatePaste,
		Edit_Undo_Text,
		Edit_Undo_Step,
		[KeyGesture("Z", Modifiers.Control)] Edit_Undo_BetweenFiles_Text,
		[KeyGesture("Z", Modifiers.Control | Modifiers.Alt)] Edit_Undo_BetweenFiles_Step,
		Edit_Undo_BetweenFiles_Sync,
		Edit_Undo_Global,
		Edit_Redo_Text,
		Edit_Redo_Step,
		[KeyGesture("Z", Modifiers.Control | Modifiers.Shift)] Edit_Redo_BetweenFiles_Text,
		[KeyGesture("Z", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_Redo_BetweenFiles_Step,
		Edit_Redo_BetweenFiles_Sync,
		[KeyGesture("R", Modifiers.Control)] Edit_Repeat,
		Edit_Rotate,
		[KeyGesture("E", Modifiers.Control)] Edit_Expression_Expression,
		[KeyGesture("E", Modifiers.Control | Modifiers.Alt)] Edit_Expression_EvaluateSelected,
		[KeyGesture("R", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions,
		[KeyGesture("1", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region1,
		[KeyGesture("2", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region2,
		[KeyGesture("3", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region3,
		[KeyGesture("4", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region4,
		[KeyGesture("5", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region5,
		[KeyGesture("6", Modifiers.Control)] Edit_ModifyRegions_Select_Select_Region6,
		Edit_ModifyRegions_Select_Select_Region7,
		Edit_ModifyRegions_Select_Select_Region8,
		Edit_ModifyRegions_Select_Select_Region9,
		[KeyGesture("1", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region1,
		[KeyGesture("2", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region2,
		[KeyGesture("3", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region3,
		[KeyGesture("4", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region4,
		[KeyGesture("5", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region5,
		[KeyGesture("6", Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Select_Previous_Region6,
		Edit_ModifyRegions_Select_Previous_Region7,
		Edit_ModifyRegions_Select_Previous_Region8,
		Edit_ModifyRegions_Select_Previous_Region9,
		[KeyGesture("1", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region1,
		[KeyGesture("2", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region2,
		[KeyGesture("3", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region3,
		[KeyGesture("4", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region4,
		[KeyGesture("5", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region5,
		[KeyGesture("6", Modifiers.Alt)] Edit_ModifyRegions_Select_Next_Region6,
		Edit_ModifyRegions_Select_Next_Region7,
		Edit_ModifyRegions_Select_Next_Region8,
		Edit_ModifyRegions_Select_Next_Region9,
		Edit_ModifyRegions_Select_Enclosing_Region1,
		Edit_ModifyRegions_Select_Enclosing_Region2,
		Edit_ModifyRegions_Select_Enclosing_Region3,
		Edit_ModifyRegions_Select_Enclosing_Region4,
		Edit_ModifyRegions_Select_Enclosing_Region5,
		Edit_ModifyRegions_Select_Enclosing_Region6,
		Edit_ModifyRegions_Select_Enclosing_Region7,
		Edit_ModifyRegions_Select_Enclosing_Region8,
		Edit_ModifyRegions_Select_Enclosing_Region9,
		Edit_ModifyRegions_Select_WithEnclosing_Region1,
		Edit_ModifyRegions_Select_WithEnclosing_Region2,
		Edit_ModifyRegions_Select_WithEnclosing_Region3,
		Edit_ModifyRegions_Select_WithEnclosing_Region4,
		Edit_ModifyRegions_Select_WithEnclosing_Region5,
		Edit_ModifyRegions_Select_WithEnclosing_Region6,
		Edit_ModifyRegions_Select_WithEnclosing_Region7,
		Edit_ModifyRegions_Select_WithEnclosing_Region8,
		Edit_ModifyRegions_Select_WithEnclosing_Region9,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region1,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region2,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region3,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region4,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region5,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region6,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region7,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region8,
		Edit_ModifyRegions_Select_WithoutEnclosing_Region9,
		[KeyGesture("1", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region1,
		[KeyGesture("2", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region2,
		[KeyGesture("3", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region3,
		[KeyGesture("4", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region4,
		[KeyGesture("5", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region5,
		[KeyGesture("6", Modifiers.Control | Modifiers.Shift)] Edit_ModifyRegions_Modify_Set_Region6,
		Edit_ModifyRegions_Modify_Set_Region7,
		Edit_ModifyRegions_Modify_Set_Region8,
		Edit_ModifyRegions_Modify_Set_Region9,
		Edit_ModifyRegions_Modify_Clear_Region1,
		Edit_ModifyRegions_Modify_Clear_Region2,
		Edit_ModifyRegions_Modify_Clear_Region3,
		Edit_ModifyRegions_Modify_Clear_Region4,
		Edit_ModifyRegions_Modify_Clear_Region5,
		Edit_ModifyRegions_Modify_Clear_Region6,
		Edit_ModifyRegions_Modify_Clear_Region7,
		Edit_ModifyRegions_Modify_Clear_Region8,
		Edit_ModifyRegions_Modify_Clear_Region9,
		[KeyGesture("1", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region1,
		[KeyGesture("2", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region2,
		[KeyGesture("3", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region3,
		[KeyGesture("4", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region4,
		[KeyGesture("5", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region5,
		[KeyGesture("6", Modifiers.Control | Modifiers.Alt)] Edit_ModifyRegions_Modify_Remove_Region6,
		Edit_ModifyRegions_Modify_Remove_Region7,
		Edit_ModifyRegions_Modify_Remove_Region8,
		Edit_ModifyRegions_Modify_Remove_Region9,
		[KeyGesture("1", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region1,
		[KeyGesture("2", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region2,
		[KeyGesture("3", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region3,
		[KeyGesture("4", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region4,
		[KeyGesture("5", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region5,
		[KeyGesture("6", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Edit_ModifyRegions_Modify_Add_Region6,
		Edit_ModifyRegions_Modify_Add_Region7,
		Edit_ModifyRegions_Modify_Add_Region8,
		Edit_ModifyRegions_Modify_Add_Region9,
		Edit_ModifyRegions_Modify_Unite_Region1,
		Edit_ModifyRegions_Modify_Unite_Region2,
		Edit_ModifyRegions_Modify_Unite_Region3,
		Edit_ModifyRegions_Modify_Unite_Region4,
		Edit_ModifyRegions_Modify_Unite_Region5,
		Edit_ModifyRegions_Modify_Unite_Region6,
		Edit_ModifyRegions_Modify_Unite_Region7,
		Edit_ModifyRegions_Modify_Unite_Region8,
		Edit_ModifyRegions_Modify_Unite_Region9,
		Edit_ModifyRegions_Modify_Intersect_Region1,
		Edit_ModifyRegions_Modify_Intersect_Region2,
		Edit_ModifyRegions_Modify_Intersect_Region3,
		Edit_ModifyRegions_Modify_Intersect_Region4,
		Edit_ModifyRegions_Modify_Intersect_Region5,
		Edit_ModifyRegions_Modify_Intersect_Region6,
		Edit_ModifyRegions_Modify_Intersect_Region7,
		Edit_ModifyRegions_Modify_Intersect_Region8,
		Edit_ModifyRegions_Modify_Intersect_Region9,
		Edit_ModifyRegions_Modify_Exclude_Region1,
		Edit_ModifyRegions_Modify_Exclude_Region2,
		Edit_ModifyRegions_Modify_Exclude_Region3,
		Edit_ModifyRegions_Modify_Exclude_Region4,
		Edit_ModifyRegions_Modify_Exclude_Region5,
		Edit_ModifyRegions_Modify_Exclude_Region6,
		Edit_ModifyRegions_Modify_Exclude_Region7,
		Edit_ModifyRegions_Modify_Exclude_Region8,
		Edit_ModifyRegions_Modify_Exclude_Region9,
		Edit_ModifyRegions_Modify_Repeat_Region1,
		Edit_ModifyRegions_Modify_Repeat_Region2,
		Edit_ModifyRegions_Modify_Repeat_Region3,
		Edit_ModifyRegions_Modify_Repeat_Region4,
		Edit_ModifyRegions_Modify_Repeat_Region5,
		Edit_ModifyRegions_Modify_Repeat_Region6,
		Edit_ModifyRegions_Modify_Repeat_Region7,
		Edit_ModifyRegions_Modify_Repeat_Region8,
		Edit_ModifyRegions_Modify_Repeat_Region9,
		Edit_ModifyRegions_Copy_Enclosing_Region1,
		Edit_ModifyRegions_Copy_Enclosing_Region2,
		Edit_ModifyRegions_Copy_Enclosing_Region3,
		Edit_ModifyRegions_Copy_Enclosing_Region4,
		Edit_ModifyRegions_Copy_Enclosing_Region5,
		Edit_ModifyRegions_Copy_Enclosing_Region6,
		Edit_ModifyRegions_Copy_Enclosing_Region7,
		Edit_ModifyRegions_Copy_Enclosing_Region8,
		Edit_ModifyRegions_Copy_Enclosing_Region9,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region1,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region2,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region3,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region4,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region5,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region6,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region7,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region8,
		Edit_ModifyRegions_Copy_EnclosingIndex_Region9,
		Edit_ModifyRegions_Transform_Flatten_Region1,
		Edit_ModifyRegions_Transform_Flatten_Region2,
		Edit_ModifyRegions_Transform_Flatten_Region3,
		Edit_ModifyRegions_Transform_Flatten_Region4,
		Edit_ModifyRegions_Transform_Flatten_Region5,
		Edit_ModifyRegions_Transform_Flatten_Region6,
		Edit_ModifyRegions_Transform_Flatten_Region7,
		Edit_ModifyRegions_Transform_Flatten_Region8,
		Edit_ModifyRegions_Transform_Flatten_Region9,
		Edit_ModifyRegions_Transform_Transpose_Region1,
		Edit_ModifyRegions_Transform_Transpose_Region2,
		Edit_ModifyRegions_Transform_Transpose_Region3,
		Edit_ModifyRegions_Transform_Transpose_Region4,
		Edit_ModifyRegions_Transform_Transpose_Region5,
		Edit_ModifyRegions_Transform_Transpose_Region6,
		Edit_ModifyRegions_Transform_Transpose_Region7,
		Edit_ModifyRegions_Transform_Transpose_Region8,
		Edit_ModifyRegions_Transform_Transpose_Region9,
		Edit_ModifyRegions_Transform_RotateLeft_Region1,
		Edit_ModifyRegions_Transform_RotateLeft_Region2,
		Edit_ModifyRegions_Transform_RotateLeft_Region3,
		Edit_ModifyRegions_Transform_RotateLeft_Region4,
		Edit_ModifyRegions_Transform_RotateLeft_Region5,
		Edit_ModifyRegions_Transform_RotateLeft_Region6,
		Edit_ModifyRegions_Transform_RotateLeft_Region7,
		Edit_ModifyRegions_Transform_RotateLeft_Region8,
		Edit_ModifyRegions_Transform_RotateLeft_Region9,
		Edit_ModifyRegions_Transform_RotateRight_Region1,
		Edit_ModifyRegions_Transform_RotateRight_Region2,
		Edit_ModifyRegions_Transform_RotateRight_Region3,
		Edit_ModifyRegions_Transform_RotateRight_Region4,
		Edit_ModifyRegions_Transform_RotateRight_Region5,
		Edit_ModifyRegions_Transform_RotateRight_Region6,
		Edit_ModifyRegions_Transform_RotateRight_Region7,
		Edit_ModifyRegions_Transform_RotateRight_Region8,
		Edit_ModifyRegions_Transform_RotateRight_Region9,
		Edit_ModifyRegions_Transform_Rotate180_Region1,
		Edit_ModifyRegions_Transform_Rotate180_Region2,
		Edit_ModifyRegions_Transform_Rotate180_Region3,
		Edit_ModifyRegions_Transform_Rotate180_Region4,
		Edit_ModifyRegions_Transform_Rotate180_Region5,
		Edit_ModifyRegions_Transform_Rotate180_Region6,
		Edit_ModifyRegions_Transform_Rotate180_Region7,
		Edit_ModifyRegions_Transform_Rotate180_Region8,
		Edit_ModifyRegions_Transform_Rotate180_Region9,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region1,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region2,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region3,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region4,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region5,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region6,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region7,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region8,
		Edit_ModifyRegions_Transform_MirrorHorizontal_Region9,
		Edit_ModifyRegions_Transform_MirrorVertical_Region1,
		Edit_ModifyRegions_Transform_MirrorVertical_Region2,
		Edit_ModifyRegions_Transform_MirrorVertical_Region3,
		Edit_ModifyRegions_Transform_MirrorVertical_Region4,
		Edit_ModifyRegions_Transform_MirrorVertical_Region5,
		Edit_ModifyRegions_Transform_MirrorVertical_Region6,
		Edit_ModifyRegions_Transform_MirrorVertical_Region7,
		Edit_ModifyRegions_Transform_MirrorVertical_Region8,
		Edit_ModifyRegions_Transform_MirrorVertical_Region9,
		[KeyGesture("Left", Modifiers.Control)] [KeyGesture("Left", Modifiers.Control | Modifiers.Shift, 2)] Edit_Navigate_WordLeft,
		[KeyGesture("Right", Modifiers.Control)] [KeyGesture("Right", Modifiers.Control | Modifiers.Shift, 2)] Edit_Navigate_WordRight,
		Edit_Navigate_AllLeft,
		Edit_Navigate_AllRight,
		Edit_Navigate_JumpBy_Words,
		Edit_Navigate_JumpBy_Numbers,
		Edit_Navigate_JumpBy_Paths,
		Edit_RepeatCount,
		Edit_RepeatIndex,
		[KeyGesture("C", Modifiers.Control | Modifiers.Shift)] Edit_Advanced_Convert,
		Edit_Advanced_Hash,
		Edit_Advanced_Compress,
		Edit_Advanced_Decompress,
		Edit_Advanced_Encrypt,
		Edit_Advanced_Decrypt,
		Edit_Advanced_Sign,
		Edit_Advanced_RunCommand_Parallel,
		Edit_Advanced_RunCommand_Sequential,
		Edit_Advanced_RunCommand_Shell,
		Edit_Advanced_EscapeClearsSelections,
		[KeyGesture("W", Modifiers.Control | Modifiers.Alt)] Text_Select_WholeWord,
		[KeyGesture("B", Modifiers.Control | Modifiers.Alt)] Text_Select_BoundedWord,
		[KeyGesture("T", Modifiers.Control | Modifiers.Alt)] Text_Select_Trim,
		[KeyGesture("S", Modifiers.Control | Modifiers.Alt)] Text_Select_Split,
		[KeyGesture("U", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Text_Select_Repeats_Unique_IgnoreCase,
		[KeyGesture("U", Modifiers.Control | Modifiers.Alt)] Text_Select_Repeats_Unique_MatchCase,
		[KeyGesture("D", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Text_Select_Repeats_Duplicates_IgnoreCase,
		[KeyGesture("D", Modifiers.Control | Modifiers.Alt)] Text_Select_Repeats_Duplicates_MatchCase,
		Text_Select_Repeats_NonMatchPrevious_IgnoreCase,
		Text_Select_Repeats_NonMatchPrevious_MatchCase,
		Text_Select_Repeats_MatchPrevious_IgnoreCase,
		Text_Select_Repeats_MatchPrevious_MatchCase,
		Text_Select_Repeats_ByCount_IgnoreCase,
		Text_Select_Repeats_ByCount_MatchCase,
		Text_Select_Repeats_BetweenFiles_Ordered_Match_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Ordered_Match_MatchCase,
		Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_MatchCase,
		Text_Select_Repeats_BetweenFiles_Unordered_Match_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Unordered_Match_MatchCase,
		Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_IgnoreCase,
		Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_MatchCase,
		Text_Select_ByWidth,
		Text_Select_Min_Text,
		Text_Select_Min_Length,
		Text_Select_Max_Text,
		Text_Select_Max_Length,
		[KeyGesture("]", Modifiers.Control)] [KeyGesture("]", Modifiers.Control | Modifiers.Shift, 2)] Text_Select_ToggleOpenClose,
		[KeyGesture("F", Modifiers.Control)] Text_Find_Find,
		Text_Find_RegexReplace,
		[KeyGesture("T", Modifiers.Control)] Text_Trim,
		[KeyGesture("W", Modifiers.Control)] Text_Width,
		Text_SingleLine,
		[KeyGesture("U", Modifiers.Control | Modifiers.Shift)] Text_Case_Upper,
		[KeyGesture("U", Modifiers.Control)] Text_Case_Lower,
		Text_Case_Proper,
		Text_Case_Invert,
		[KeyGesture("S", Modifiers.Control | Modifiers.Shift)] Text_Sort,
		Text_Escape_Markup,
		Text_Escape_Regex,
		Text_Escape_URL,
		Text_Unescape_Markup,
		Text_Unescape_Regex,
		Text_Unescape_URL,
		Text_Random,
		Text_Advanced_Unicode,
		Text_Advanced_FirstDistinct,
		Text_Advanced_GUID,
		Text_Advanced_ReverseRegex,
		Numeric_Select_Min,
		Numeric_Select_Max,
		Numeric_Select_Limit,
		Numeric_Round,
		Numeric_Floor,
		Numeric_Ceiling,
		Numeric_Sum_Sum,
		[KeyGesture("+", Modifiers.Control)] Numeric_Sum_Increment,
		[KeyGesture("-", Modifiers.Control)] Numeric_Sum_Decrement,
		[KeyGesture("+", Modifiers.Control | Modifiers.Shift)] Numeric_Sum_AddClipboard,
		[KeyGesture("-", Modifiers.Control | Modifiers.Shift)] Numeric_Sum_SubtractClipboard,
		Numeric_Sum_ForwardSum,
		Numeric_Sum_UndoForwardSum,
		Numeric_Sum_ReverseSum,
		Numeric_Sum_UndoReverseSum,
		Numeric_AbsoluteValue,
		Numeric_Scale,
		Numeric_Cycle,
		Numeric_Trim,
		Numeric_Fraction,
		Numeric_Factor,
		Numeric_Series_ZeroBased,
		Numeric_Series_OneBased,
		Numeric_Series_Linear,
		Numeric_Series_Geometric,
		[KeyGesture("H", Modifiers.Control)] Numeric_ConvertBase_ToHex,
		[KeyGesture("H", Modifiers.Control | Modifiers.Shift)] Numeric_ConvertBase_FromHex,
		Numeric_ConvertBase_ConvertBase,
		Numeric_RandomNumber,
		Numeric_CombinationsPermutations,
		Numeric_MinMaxValues,
		Files_Select_Files,
		Files_Select_Directories,
		Files_Select_Existing,
		Files_Select_NonExisting,
		Files_Select_Name_Directory,
		Files_Select_Name_Name,
		Files_Select_Name_NameWOExtension,
		Files_Select_Name_Extension,
		Files_Select_Name_Next,
		Files_Select_Name_CommonAncestor,
		Files_Select_Name_MatchDepth,
		Files_Select_Roots,
		Files_Select_NonRoots,
		Files_Select_ByContent,
		Files_Select_BySourceControlStatus,
		Files_Copy,
		Files_Move,
		Files_Delete,
		Files_Name_MakeAbsolute,
		Files_Name_MakeRelative,
		Files_Name_Simplify,
		Files_Name_Sanitize,
		Files_Get_Size,
		Files_Get_Time_Write,
		Files_Get_Time_Access,
		Files_Get_Time_Create,
		Files_Get_Attributes,
		Files_Get_Version_File,
		Files_Get_Version_Product,
		Files_Get_Version_Assembly,
		Files_Get_Hash,
		Files_Get_SourceControlStatus,
		Files_Get_Children,
		Files_Get_Descendants,
		Files_Get_Content,
		Files_Set_Size,
		Files_Set_Time_Write,
		Files_Set_Time_Access,
		Files_Set_Time_Create,
		Files_Set_Time_All,
		Files_Set_Attributes,
		Files_Set_Content,
		Files_Set_Encoding,
		Files_Create_Files,
		Files_Create_Directories,
		Files_Compress,
		Files_Decompress,
		Files_Encrypt,
		Files_Decrypt,
		Files_Sign,
		Files_Advanced_Explore,
		Files_Advanced_CommandPrompt,
		Files_Advanced_DragDrop,
		Files_Advanced_SplitFiles,
		Files_Advanced_CombineFiles,
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
		Content_StrictParsing,
		Content_Reformat,
		[KeyGesture("/", Modifiers.Control)] Content_Comment,
		[KeyGesture("/", Modifiers.Control | Modifiers.Shift)] Content_Uncomment,
		Content_Copy,
		[KeyGesture("]", Modifiers.Control | Modifiers.Alt)] [KeyGesture("]", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_TogglePosition,
		[KeyGesture("Enter", Modifiers.Control | Modifiers.Alt)] Content_Current,
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
		[KeyGesture("Up", Modifiers.Control | Modifiers.Alt)] [KeyGesture("Up", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Up,
		[KeyGesture("Down", Modifiers.Control | Modifiers.Alt)] [KeyGesture("Down", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Down,
		[KeyGesture("Left", Modifiers.Control | Modifiers.Alt)] [KeyGesture("Left", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Left,
		[KeyGesture("Right", Modifiers.Control | Modifiers.Alt)] [KeyGesture("Right", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Right,
		[KeyGesture("Home", Modifiers.Control | Modifiers.Alt)] [KeyGesture("Home", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Home,
		[KeyGesture("End", Modifiers.Control | Modifiers.Alt)] [KeyGesture("End", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_End,
		[KeyGesture("PageUp", Modifiers.Control | Modifiers.Alt)] [KeyGesture("PageUp", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Pgup,
		[KeyGesture("PageDown", Modifiers.Control | Modifiers.Alt)] [KeyGesture("PageDown", Modifiers.Control | Modifiers.Alt | Modifiers.Shift, 2)] Content_Navigate_Pgdn,
		[KeyGesture("Space", Modifiers.Control | Modifiers.Alt)] Content_Navigate_Row,
		[KeyGesture("Space", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Content_Navigate_Column,
		Content_KeepSelections,
		DateTime_Now,
		DateTime_UTCNow,
		DateTime_ToUTC,
		DateTime_ToLocal,
		DateTime_ToTimeZone,
		DateTime_Format,
		DateTime_AddClipboard,
		DateTime_SubtractClipboard,
		Table_Select_RowsByExpression,
		Table_New_FromSelection,
		Table_New_FromLineSelections,
		Table_New_FromRegionSelections_Region1,
		Table_New_FromRegionSelections_Region2,
		Table_New_FromRegionSelections_Region3,
		Table_New_FromRegionSelections_Region4,
		Table_New_FromRegionSelections_Region5,
		Table_New_FromRegionSelections_Region6,
		Table_New_FromRegionSelections_Region7,
		Table_New_FromRegionSelections_Region8,
		Table_New_FromRegionSelections_Region9,
		[KeyGesture("T", Modifiers.Control | Modifiers.Shift)] Table_Edit,
		Table_DetectType,
		Table_Convert,
		Table_SetJoinSource,
		Table_Join,
		Table_Transpose,
		Table_Database_GenerateInserts,
		Table_Database_GenerateUpdates,
		Table_Database_GenerateDeletes,
		Image_Resize,
		Image_Crop,
		Image_GrabColor,
		Image_GrabImage,
		Image_AddColor,
		Image_AdjustColor,
		Image_OverlayColor,
		Image_FlipHorizontal,
		Image_FlipVertical,
		Image_Rotate,
		Image_GIF_Animate,
		Image_GIF_Split,
		Image_GetTakenDate,
		Image_SetTakenDate,
		[KeyGesture("G", Modifiers.Control)] Position_Goto_Lines,
		Position_Goto_Columns,
		Position_Goto_Indexes,
		Position_Goto_Positions,
		Position_Copy_Lines,
		Position_Copy_Columns,
		Position_Copy_Indexes,
		Position_Copy_Positions,
		Diff_Select_Matches,
		Diff_Select_Diffs,
		Diff_Select_LeftFile,
		Diff_Select_RightFile,
		Diff_Select_BothFiles,
		[KeyGesture("D", Modifiers.Control)] [KeyGesture("D", Modifiers.Control | Modifiers.Shift, 2)] Diff_Diff,
		Diff_Break,
		Diff_SourceControl,
		Diff_IgnoreWhitespace,
		Diff_IgnoreCase,
		Diff_IgnoreNumbers,
		Diff_IgnoreLineEndings,
		Diff_IgnoreCharacters,
		Diff_Reset,
		[KeyGesture("Down", Modifiers.Alt)] [KeyGesture("Down", Modifiers.Alt | Modifiers.Shift, 2)] Diff_Next,
		[KeyGesture("Up", Modifiers.Alt)] [KeyGesture("Up", Modifiers.Alt | Modifiers.Shift, 2)] Diff_Previous,
		[KeyGesture("Left", Modifiers.Alt)] Diff_CopyLeft,
		[KeyGesture("Right", Modifiers.Alt)] Diff_CopyRight,
		Diff_Fix_Whitespace,
		Diff_Fix_Case,
		Diff_Fix_Numbers,
		Diff_Fix_LineEndings,
		Diff_Fix_Encoding,
		Network_AbsoluteURL,
		Network_Fetch_Fetch,
		Network_Fetch_Hex,
		Network_Fetch_File,
		Network_Fetch_Stream,
		Network_Fetch_Playlist,
		Network_Lookup_IP,
		Network_Lookup_Hostname,
		Network_AdaptersInfo,
		Network_Ping,
		Network_ScanPorts,
		Network_WCF_GetConfig,
		[KeyGesture("W", Modifiers.Control | Modifiers.Shift)] Network_WCF_Execute,
		Network_WCF_InterceptCalls,
		Network_WCF_ResetClients,
		Database_Connect,
		[KeyGesture("Q", Modifiers.Control)] Database_ExecuteQuery,
		Database_Examine,
		Database_GetSproc,
		[KeyGesture("K", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] KeyValue_Set_Keys_IgnoreCase,
		[KeyGesture("K", Modifiers.Control | Modifiers.Shift)] KeyValue_Set_Keys_MatchCase,
		KeyValue_Set_Values1,
		KeyValue_Set_Values2,
		KeyValue_Set_Values3,
		KeyValue_Set_Values4,
		KeyValue_Set_Values5,
		KeyValue_Set_Values6,
		[KeyGesture("7", Modifiers.Control | Modifiers.Shift)] KeyValue_Set_Values7,
		[KeyGesture("8", Modifiers.Control | Modifiers.Shift)] KeyValue_Set_Values8,
		[KeyGesture("9", Modifiers.Control | Modifiers.Shift)] KeyValue_Set_Values9,
		KeyValue_Add_Keys,
		KeyValue_Add_Values1,
		KeyValue_Add_Values2,
		KeyValue_Add_Values3,
		KeyValue_Add_Values4,
		KeyValue_Add_Values5,
		KeyValue_Add_Values6,
		[KeyGesture("7", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] KeyValue_Add_Values7,
		[KeyGesture("8", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] KeyValue_Add_Values8,
		[KeyGesture("9", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] KeyValue_Add_Values9,
		KeyValue_Remove_Keys,
		KeyValue_Remove_Values1,
		KeyValue_Remove_Values2,
		KeyValue_Remove_Values3,
		KeyValue_Remove_Values4,
		KeyValue_Remove_Values5,
		KeyValue_Remove_Values6,
		[KeyGesture("7", Modifiers.Control | Modifiers.Alt)] KeyValue_Remove_Values7,
		[KeyGesture("8", Modifiers.Control | Modifiers.Alt)] KeyValue_Remove_Values8,
		[KeyGesture("9", Modifiers.Control | Modifiers.Alt)] KeyValue_Remove_Values9,
		KeyValue_Replace_Values1,
		KeyValue_Replace_Values2,
		KeyValue_Replace_Values3,
		KeyValue_Replace_Values4,
		KeyValue_Replace_Values5,
		KeyValue_Replace_Values6,
		[KeyGesture("7", Modifiers.Control)] KeyValue_Replace_Values7,
		[KeyGesture("8", Modifiers.Control)] KeyValue_Replace_Values8,
		[KeyGesture("9", Modifiers.Control)] KeyValue_Replace_Values9,
		[NoMacro] Macro_Play_Quick_1,
		[NoMacro] Macro_Play_Quick_2,
		[NoMacro] Macro_Play_Quick_3,
		[NoMacro] Macro_Play_Quick_4,
		[NoMacro] Macro_Play_Quick_5,
		[NoMacro] [KeyGesture("F6")] Macro_Play_Quick_6,
		[NoMacro] [KeyGesture("F7")] Macro_Play_Quick_7,
		[NoMacro] [KeyGesture("F8")] Macro_Play_Quick_8,
		[NoMacro] [KeyGesture("F9")] Macro_Play_Quick_9,
		[NoMacro] [KeyGesture("F10")] Macro_Play_Quick_10,
		[NoMacro] [KeyGesture("F11")] Macro_Play_Quick_11,
		[NoMacro] [KeyGesture("F12")] Macro_Play_Quick_12,
		[NoMacro] Macro_Play_Play,
		[NoMacro] Macro_Play_Repeat,
		[NoMacro] Macro_Play_PlayOnCopiedFiles,
		[NoMacro] Macro_Record_Quick_1,
		[NoMacro] Macro_Record_Quick_2,
		[NoMacro] Macro_Record_Quick_3,
		[NoMacro] Macro_Record_Quick_4,
		[NoMacro] Macro_Record_Quick_5,
		[NoMacro] [KeyGesture("F6", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_6,
		[NoMacro] [KeyGesture("F7", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_7,
		[NoMacro] [KeyGesture("F8", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_8,
		[NoMacro] [KeyGesture("F9", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_9,
		[NoMacro] [KeyGesture("F10", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_10,
		[NoMacro] [KeyGesture("F11", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_11,
		[NoMacro] [KeyGesture("F12", Modifiers.Control | Modifiers.Shift)] Macro_Record_Quick_12,
		[NoMacro] Macro_Record_Record,
		[NoMacro] Macro_Record_StopRecording,
		[NoMacro] Macro_Append_Quick_1,
		[NoMacro] Macro_Append_Quick_2,
		[NoMacro] Macro_Append_Quick_3,
		[NoMacro] Macro_Append_Quick_4,
		[NoMacro] Macro_Append_Quick_5,
		[NoMacro] Macro_Append_Quick_6,
		[NoMacro] Macro_Append_Quick_7,
		[NoMacro] Macro_Append_Quick_8,
		[NoMacro] Macro_Append_Quick_9,
		[NoMacro] Macro_Append_Quick_10,
		[NoMacro] Macro_Append_Quick_11,
		[NoMacro] Macro_Append_Quick_12,
		[NoMacro] Macro_Append_Append,
		Macro_Open_Quick_1,
		Macro_Open_Quick_2,
		Macro_Open_Quick_3,
		Macro_Open_Quick_4,
		Macro_Open_Quick_5,
		[KeyGesture("F6", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_6,
		[KeyGesture("F7", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_7,
		[KeyGesture("F8", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_8,
		[KeyGesture("F9", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_9,
		[KeyGesture("F10", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_10,
		[KeyGesture("F11", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_11,
		[KeyGesture("F12", Modifiers.Control | Modifiers.Alt)] Macro_Open_Quick_12,
		Macro_Open_Open,
		[KeyGesture(".", Modifiers.Control)] Macro_RepeatLastAction,
		Macro_Visualize,
		Window_New_New,
		Window_New_FromSelections_All,
		Window_New_FromSelections_Files,
		Window_New_FromSelections_Selections,
		[KeyGesture("M", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Window_New_SummarizeSelections_Files_IgnoreCase,
		[KeyGesture("M", Modifiers.Control | Modifiers.Alt)] Window_New_SummarizeSelections_Files_MatchCase,
		[KeyGesture("M", Modifiers.Control | Modifiers.Shift)] Window_New_SummarizeSelections_Selections_IgnoreCase,
		[KeyGesture("M", Modifiers.Control)] Window_New_SummarizeSelections_Selections_MatchCase,
		Window_New_FromClipboard_All,
		Window_New_FromClipboard_Files,
		Window_New_FromClipboard_Selections,
		[KeyGesture("N", Modifiers.Control | Modifiers.Alt)] Window_New_FromFiles_Active,
		[KeyGesture("O", Modifiers.Control | Modifiers.Alt | Modifiers.Shift)] Window_New_FromFiles_CopiedCut,
		[KeyGesture("F", Modifiers.Control | Modifiers.Alt)] Window_Full,
		[KeyGesture("G", Modifiers.Control | Modifiers.Alt)] Window_Grid,
		[KeyGesture("C", Modifiers.Control | Modifiers.Alt)] Window_CustomGrid,
		[KeyGesture("F3")] Window_ActiveFirst,
		Window_Font_Size,
		Window_Font_ShowSpecial,
		Window_ViewBinary,
		Window_BinaryCodePages,
		Help_Tutorial,
		Help_Update,
		Help_Advanced_TimeNextAction,
		Help_Advanced_Shell_Integrate,
		Help_Advanced_Shell_Unintegrate,
		Help_Advanced_CopyCommandLine,
		Help_Advanced_RunGC,
		Help_About,
	}
}
