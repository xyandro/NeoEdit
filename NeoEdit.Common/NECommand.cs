﻿using System.Windows.Input;

namespace NeoEdit.Common
{
	public enum NECommand
	{
		None,
		[NoMacro] Internal_Activate,
		[NoMacro] Internal_AddTab,
		[NoMacro] Internal_MouseActivate,
		[NoMacro] Internal_CloseTab,
		Internal_Key,
		Internal_Text,
		Internal_SetBinaryValue,
		[NoMacro] Internal_Scroll,
		[NoMacro] Internal_Mouse,
		[NoMacro] Internal_Redraw,
		[NoMacro] Internal_SetupDiff,
		[NoMacro] Internal_GotoTab,
		[KeyGesture(Key.N, ModifierKeys.Control)] File_New_New,
		File_New_FromSelections,
		File_New_FromClipboards,
		File_New_FromClipboardSelections,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open_Open,
		[KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt)] [KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, 2)] File_Open_CopiedCut,
		File_Open_Selected,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] File_Save_Save,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save_SaveModified,
		File_Save_SaveAs,
		File_Save_SaveAsByExpression,
		File_Copy_CopyTo,
		File_Copy_CopyToByExpression,
		[KeyGesture(Key.P, ModifierKeys.Control)] File_Copy_Path,
		File_Copy_Name,
		File_Copy_DisplayName,
		File_Rename_Rename,
		File_Rename_RenameByExpression,
		File_Operations_Delete,
		File_Operations_Explore,
		File_Operations_CommandPrompt,
		File_Operations_DragDrop,
		File_Operations_VCSDiff,
		File_Operations_SetDisplayName,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		File_AutoRefresh,
		File_Revert,
		[KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Alt)] File_MoveToNewWindow,
		File_Insert_Files,
		File_Insert_CopiedCut,
		File_Insert_Selected,
		File_Encoding_Encoding,
		File_Encoding_ReopenWithEncoding,
		File_Encoding_LineEndings,
		File_Encrypt,
		File_Compress,
		File_Shell_Integrate,
		File_Shell_Unintegrate,
		File_DontExitOnClose,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy_Copy,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Copy_Cut,
		[KeyGesture(Key.V, ModifierKeys.Control)] [KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Paste_Paste,
		[KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Alt)] Edit_Paste_RotatePaste,
		[KeyGesture(Key.F, ModifierKeys.Control)] Edit_Find_Find,
		Edit_Find_RegexReplace,
		[KeyGesture(Key.E, ModifierKeys.Control)] Edit_Expression_Expression,
		[KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Alt)] Edit_Expression_EvaluateSelected,
		Edit_CopyDown,
		Edit_Rotate,
		[KeyGesture(Key.R, ModifierKeys.Control)] Edit_Repeat,
		Edit_Escape_Markup,
		Edit_Escape_RegEx,
		Edit_Escape_URL,
		Edit_Unescape_Markup,
		Edit_Unescape_RegEx,
		Edit_Unescape_URL,
		Edit_Data_Hash,
		Edit_Data_Compress,
		Edit_Data_Decompress,
		Edit_Data_Encrypt,
		Edit_Data_Decrypt,
		Edit_Data_Sign,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Sort,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Convert,
		[KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions,
		[KeyGesture(Key.D1, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control)] Edit_ModifyRegions_Select_Select_Region6,
		Edit_ModifyRegions_Select_Select_Region7,
		Edit_ModifyRegions_Select_Select_Region8,
		Edit_ModifyRegions_Select_Select_Region9,
		[KeyGesture(Key.D1, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Select_Previous_Region6,
		Edit_ModifyRegions_Select_Previous_Region7,
		Edit_ModifyRegions_Select_Previous_Region8,
		Edit_ModifyRegions_Select_Previous_Region9,
		[KeyGesture(Key.D1, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Alt)] Edit_ModifyRegions_Select_Next_Region6,
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
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Set_Region6,
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
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Alt)] Edit_ModifyRegions_Modify_Remove_Region6,
		Edit_ModifyRegions_Modify_Remove_Region7,
		Edit_ModifyRegions_Modify_Remove_Region8,
		Edit_ModifyRegions_Modify_Remove_Region9,
		[KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region1,
		[KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region2,
		[KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region3,
		[KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region4,
		[KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region5,
		[KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Edit_ModifyRegions_Modify_Add_Region6,
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
		[KeyGesture(Key.Left, ModifierKeys.Control)] [KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Navigate_WordLeft,
		[KeyGesture(Key.Right, ModifierKeys.Control)] [KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift, 2)] Edit_Navigate_WordRight,
		Edit_Navigate_AllLeft,
		Edit_Navigate_AllRight,
		Edit_Navigate_JumpBy_Words,
		Edit_Navigate_JumpBy_Numbers,
		Edit_Navigate_JumpBy_Paths,
		Edit_EscapeClearsSelections,
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
		Diff_Fix_Encoding,
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
		Files_Find,
		Files_Insert,
		Files_Create_Files,
		Files_Create_Directories,
		Files_Create_FromExpressions,
		Files_Select_Name_Directory,
		Files_Select_Name_Name,
		Files_Select_Name_FileNamewoExtension,
		Files_Select_Name_Extension,
		Files_Select_Name_Next,
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
		Files_Compress,
		Files_Decompress,
		Files_Encrypt,
		Files_Decrypt,
		Files_Sign,
		Files_Operations_Copy,
		Files_Operations_Move,
		Files_Operations_Delete,
		Files_Operations_DragDrop,
		Files_Operations_Explore,
		Files_Operations_CommandPrompt,
		Files_Operations_RunCommand_Parallel,
		Files_Operations_RunCommand_Sequential,
		Files_Operations_RunCommand_Shell,
		Files_Operations_Encoding,
		Files_Operations_SplitFile,
		Files_Operations_CombineFiles,
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
		Text_ReverseRegEx,
		Text_FirstDistinct,
		Text_RepeatCount,
		Text_RepeatIndex,
		Numeric_Select_Min,
		Numeric_Select_Max,
		Numeric_Select_Fraction_Whole,
		Numeric_Select_Fraction_Fraction,
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
		Numeric_Fraction_Whole,
		Numeric_Fraction_Fraction,
		Numeric_Fraction_Simplify,
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
		DateTime_Format,
		DateTime_ToUtc,
		DateTime_ToLocal,
		DateTime_ToTimeZone,
		DateTime_AddClipboard,
		DateTime_SubtractClipboard,
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
		Image_GetTakenDate,
		Image_SetTakenDate,
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
		Table_SetJoinSource,
		Table_Join,
		Table_Transpose,
		Table_Database_GenerateInserts,
		Table_Database_GenerateUpdates,
		Table_Database_GenerateDeletes,
		[KeyGesture(Key.G, ModifierKeys.Control)] Position_Goto_Lines,
		Position_Goto_Columns,
		Position_Goto_Indexes,
		Position_Goto_Positions,
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
		Content_StrictParsing,
		Content_Reformat,
		[KeyGesture(Key.OemQuestion, ModifierKeys.Control)] Content_Comment,
		[KeyGesture(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Shift)] Content_Uncomment,
		Content_Copy,
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
		Network_FetchStream,
		Network_FetchPlaylist,
		Network_Lookup_IP,
		Network_Lookup_HostName,
		Network_AdaptersInfo,
		Network_Ping,
		Network_ScanPorts,
		Network_WCF_GetConfig,
		[KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift)] Network_WCF_Execute,
		Network_WCF_InterceptCalls,
		Network_WCF_ResetClients,
		Database_Connect,
		[KeyGesture(Key.Q, ModifierKeys.Control)] Database_ExecuteQuery,
		Database_Examine,
		Database_GetSproc,
		[KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift)] Keys_Set_KeysCaseSensitive,
		[KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Keys_Set_KeysCaseInsensitive,
		Keys_Set_Values1,
		Keys_Set_Values2,
		Keys_Set_Values3,
		Keys_Set_Values4,
		Keys_Set_Values5,
		Keys_Set_Values6,
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
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Keys_Add_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Keys_Add_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Keys_Add_Values9,
		Keys_Remove_Keys,
		Keys_Remove_Values1,
		Keys_Remove_Values2,
		Keys_Remove_Values3,
		Keys_Remove_Values4,
		Keys_Remove_Values5,
		Keys_Remove_Values6,
		[KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Alt)] Keys_Remove_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Alt)] Keys_Remove_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Alt)] Keys_Remove_Values9,
		Keys_Replace_Values1,
		Keys_Replace_Values2,
		Keys_Replace_Values3,
		Keys_Replace_Values4,
		Keys_Replace_Values5,
		Keys_Replace_Values6,
		[KeyGesture(Key.D7, ModifierKeys.Control)] Keys_Replace_Values7,
		[KeyGesture(Key.D8, ModifierKeys.Control)] Keys_Replace_Values8,
		[KeyGesture(Key.D9, ModifierKeys.Control)] Keys_Replace_Values9,
		[KeyGesture(Key.A, ModifierKeys.Control)] Select_All,
		Select_Nothing,
		Select_Limit,
		[KeyGesture(Key.L, ModifierKeys.Control)] Select_Lines,
		[KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Alt)] Select_WholeLines,
		Select_Rectangle,
		[KeyGesture(Key.I, ModifierKeys.Control)] Select_Invert,
		[KeyGesture(Key.J, ModifierKeys.Control)] Select_Join,
		Select_Empty,
		Select_NonEmpty,
		[KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control)] [KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift, 2)] Select_ToggleOpenClose,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt)] Select_RepeatsCaseSensitive_Unique,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt)] Select_RepeatsCaseSensitive_Duplicates,
		Select_RepeatsCaseSensitive_MatchPrevious,
		Select_RepeatsCaseSensitive_NonMatchPrevious,
		Select_RepeatsCaseSensitive_RepeatedLines,
		Select_RepeatsCaseSensitive_ByCount,
		Select_RepeatsCaseSensitive_Tabs_Match,
		Select_RepeatsCaseSensitive_Tabs_Mismatch,
		Select_RepeatsCaseSensitive_Tabs_Common,
		Select_RepeatsCaseSensitive_Tabs_NonCommon,
		[KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Select_RepeatsCaseInsensitive_Unique,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Select_RepeatsCaseInsensitive_Duplicates,
		Select_RepeatsCaseInsensitive_MatchPrevious,
		Select_RepeatsCaseInsensitive_NonMatchPrevious,
		Select_RepeatsCaseInsensitive_RepeatedLines,
		Select_RepeatsCaseInsensitive_ByCount,
		Select_RepeatsCaseInsensitive_Tabs_Match,
		Select_RepeatsCaseInsensitive_Tabs_Mismatch,
		Select_RepeatsCaseInsensitive_Tabs_Common,
		Select_RepeatsCaseInsensitive_Tabs_NonCommon,
		[KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt)] Select_Split,
		[KeyGesture(Key.M, ModifierKeys.Control)] Select_SummarizeCaseSensitive_One,
		[KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Alt)] Select_SummarizeCaseSensitive_Many,
		[KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Shift)] Select_SummarizeCaseInsensitive_One,
		[KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Select_SummarizeCaseInsensitive_Many,
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
		[NoMacro] Macro_Record_Quick_1,
		[NoMacro] Macro_Record_Quick_2,
		[NoMacro] Macro_Record_Quick_3,
		[NoMacro] Macro_Record_Quick_4,
		[NoMacro] Macro_Record_Quick_5,
		[NoMacro] [KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_6,
		[NoMacro] [KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_7,
		[NoMacro] [KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_8,
		[NoMacro] [KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_9,
		[NoMacro] [KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_10,
		[NoMacro] [KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_11,
		[NoMacro] [KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Shift)] Macro_Record_Quick_12,
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
		[NoMacro] Macro_Play_Quick_1,
		[NoMacro] Macro_Play_Quick_2,
		[NoMacro] Macro_Play_Quick_3,
		[NoMacro] Macro_Play_Quick_4,
		[NoMacro] Macro_Play_Quick_5,
		[NoMacro] [KeyGesture(Key.F6)] Macro_Play_Quick_6,
		[NoMacro] [KeyGesture(Key.F7)] Macro_Play_Quick_7,
		[NoMacro] [KeyGesture(Key.F8)] Macro_Play_Quick_8,
		[NoMacro] [KeyGesture(Key.F9)] Macro_Play_Quick_9,
		[NoMacro] [KeyGesture(Key.F10)] Macro_Play_Quick_10,
		[NoMacro] [KeyGesture(Key.F11)] Macro_Play_Quick_11,
		[NoMacro] [KeyGesture(Key.F12)] Macro_Play_Quick_12,
		[NoMacro] Macro_Play_Play,
		[NoMacro] Macro_Play_Repeat,
		[NoMacro] Macro_Play_PlayOnCopiedFiles,
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
		Macro_Visualize,
		Window_New_NewWindow,
		Window_New_FromSelections,
		Window_New_FromClipboards,
		Window_New_FromClipboardSelections,
		[KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt)] Window_Full,
		[KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Alt)] Window_Grid,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt)] Window_CustomGrid,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt)] Window_ActiveTabs,
		Window_TabIndex,
		Window_ActiveTabIndex,
		Window_Font_Size,
		Window_Font_ShowSpecial,
		Window_ViewBinary,
		Window_ViewBinaryCodePages,
		[KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)] Window_Select_AllTabs,
		Window_Select_NoTabs,
		Window_Select_TabsWithSelections,
		Window_Select_TabsWithoutSelections,
		Window_Select_ModifiedTabs,
		Window_Select_UnmodifiedTabs,
		Window_Select_InactiveTabs,
		Window_Close_TabsWithSelections,
		Window_Close_TabsWithoutSelections,
		Window_Close_ModifiedTabs,
		Window_Close_UnmodifiedTabs,
		Window_Close_ActiveTabs,
		Window_Close_InactiveTabs,
		Window_WordList,
		Help_About,
		Help_Tutorial,
		Help_Update,
		Help_Extract,
		Help_RunGC,
		Help_CopyCommandLine,
	}
}
