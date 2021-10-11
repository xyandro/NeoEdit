using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;

namespace NeoEdit.UI.Controls
{
	public class NEMenuItem : MenuItem
	{
		static List<(KeyGesture KeyGesture, NECommand Command, int Index, IConfiguration Configuration)> shortcuts = new List<(KeyGesture KeyGesture, NECommand Command, int Index, IConfiguration Configuration)>
		{
			(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.File_Select_All, 0, null),
			(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt), NECommand.File_Select_Choose, 0, null),
			(new KeyGesture(Key.N, ModifierKeys.Control), NECommand.File_New_New, 0, null),
			(new KeyGesture(Key.O, ModifierKeys.Control), NECommand.File_Open_Open, 0, null),
			(new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt), NECommand.File_Open_CopiedCut, 0, null),
			(new KeyGesture(Key.F5, ModifierKeys.None), NECommand.File_Open_Refresh, 0, null),
			(new KeyGesture(Key.S, ModifierKeys.Control), NECommand.File_Save_SaveModified, 0, null),
			(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.File_Save_SaveAll, 0, null),
			(new KeyGesture(Key.P, ModifierKeys.Control), NECommand.File_Copy_Path, 0, null),
			(new KeyGesture(Key.F4, ModifierKeys.Control), NECommand.File_Close_Active, 0, null),
			(new KeyGesture(Key.A, ModifierKeys.Control), NECommand.Edit_Select_All, 0, null),
			(new KeyGesture(Key.J, ModifierKeys.Control), NECommand.Edit_Select_Join, 0, null),
			(new KeyGesture(Key.I, ModifierKeys.Control), NECommand.Edit_Select_Invert, 0, null),
			(new KeyGesture(Key.L, ModifierKeys.Control), NECommand.Edit_Select_Lines, 0, null),
			(new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_Select_WholeLines, 0, null),
			(new KeyGesture(Key.F2, ModifierKeys.None), NECommand.Edit_Select_AllowOverlappingSelections, 0, null),
			(new KeyGesture(Key.Space, ModifierKeys.Control), NECommand.Edit_Select_ToggleAnchor, 0, null),
			(new KeyGesture(Key.D0, ModifierKeys.Alt), NECommand.Edit_Select_Focused_First, 0, null),
			(new KeyGesture(Key.Add, ModifierKeys.Alt), NECommand.Edit_Select_Focused_Next, 0, null),
			(new KeyGesture(Key.Subtract, ModifierKeys.Alt), NECommand.Edit_Select_Focused_Previous, 0, null),
			(new KeyGesture(Key.Enter, ModifierKeys.Alt), NECommand.Edit_Select_Focused_Single, 0, null),
			(new KeyGesture(Key.Back, ModifierKeys.Alt), NECommand.Edit_Select_Focused_Remove, 0, null),
			(new KeyGesture(Key.Space, ModifierKeys.Alt), NECommand.Edit_Select_Focused_CenterVertically, 0, null),
			(new KeyGesture(Key.Space, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_Select_Focused_Center, 0, null),
			(new KeyGesture(Key.C, ModifierKeys.Control), NECommand.Edit_Copy, 0, null),
			(new KeyGesture(Key.X, ModifierKeys.Control), NECommand.Edit_Cut, 0, null),
			(new KeyGesture(Key.V, ModifierKeys.Control), NECommand.Edit_Paste_Paste, 0, null),
			(new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_Paste_Paste, 1, null),
			(new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_Paste_RotatePaste, 0, null),
			(new KeyGesture(Key.Z, ModifierKeys.Control), NECommand.Edit_Undo_BetweenFiles_Text, 0, null),
			(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_Undo_BetweenFiles_Step, 0, null),
			(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_Redo_BetweenFiles_Text, 0, null),
			(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_Redo_BetweenFiles_Step, 0, null),
			(new KeyGesture(Key.R, ModifierKeys.Control), NECommand.Edit_Repeat, 0, null),
			(new KeyGesture(Key.E, ModifierKeys.Control), NECommand.Edit_Expression_Expression, 0, null),
			(new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_Expression_EvaluateSelected, 0, null),
			(new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, null),
			(new KeyGesture(Key.D1, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Control), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Select, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.D1, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Previous, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.D1, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Select_Next, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Set, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Remove, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 1 } }),
			(new KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 2 } }),
			(new KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 3 } }),
			(new KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 4 } }),
			(new KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 5 } }),
			(new KeyGesture(Key.D6, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Edit_ModifyRegions, 0, new Configuration_Edit_ModifyRegions { Action = Configuration_Edit_ModifyRegions.Actions.Modify_Add, Regions = new List<int> { 6 } }),
			(new KeyGesture(Key.Left, ModifierKeys.Control), NECommand.Edit_Navigate_WordLeft, 0, null),
			(new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_Navigate_WordLeft, 1, null),
			(new KeyGesture(Key.Right, ModifierKeys.Control), NECommand.Edit_Navigate_WordRight, 0, null),
			(new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_Navigate_WordRight, 1, null),
			(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Edit_Advanced_Convert, 0, null),
			(new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_WholeWord, 0, null),
			(new KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_BoundedWord, 0, null),
			(new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_Trim, 0, null),
			(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_Split, 0, null),
			(new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Text_Select_Repeats_Unique_IgnoreCase, 0, null),
			(new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_Repeats_Unique_MatchCase, 0, null),
			(new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Text_Select_Repeats_Duplicates_IgnoreCase, 0, null),
			(new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Text_Select_Repeats_Duplicates_MatchCase, 0, null),
			(new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control), NECommand.Text_Select_ToggleOpenClose, 0, null),
			(new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Text_Select_ToggleOpenClose, 1, null),
			(new KeyGesture(Key.F, ModifierKeys.Control), NECommand.Text_Find_Find, 0, null),
			(new KeyGesture(Key.T, ModifierKeys.Control), NECommand.Text_Trim, 0, null),
			(new KeyGesture(Key.W, ModifierKeys.Control), NECommand.Text_Width, 0, null),
			(new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Text_Case_Upper, 0, null),
			(new KeyGesture(Key.U, ModifierKeys.Control), NECommand.Text_Case_Lower, 0, null),
			(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Text_Sort, 0, null),
			(new KeyGesture(Key.Add, ModifierKeys.Control), NECommand.Numeric_Sum_Increment, 0, null),
			(new KeyGesture(Key.Subtract, ModifierKeys.Control), NECommand.Numeric_Sum_Decrement, 0, null),
			(new KeyGesture(Key.Add, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Numeric_Sum_AddClipboard, 0, null),
			(new KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Numeric_Sum_SubtractClipboard, 0, null),
			(new KeyGesture(Key.H, ModifierKeys.Control), NECommand.Numeric_ConvertBase_ToHex, 0, null),
			(new KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Numeric_ConvertBase_FromHex, 0, null),
			(new KeyGesture(Key.OemQuestion, ModifierKeys.Control), NECommand.Content_Comment, 0, null),
			(new KeyGesture(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Content_Uncomment, 0, null),
			(new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_TogglePosition, 0, null),
			(new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_TogglePosition, 1, null),
			(new KeyGesture(Key.Enter, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Current, 0, null),
			(new KeyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Up, 0, null),
			(new KeyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Up, 1, null),
			(new KeyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Down, 0, null),
			(new KeyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Down, 1, null),
			(new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Left, 0, null),
			(new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Left, 1, null),
			(new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Right, 0, null),
			(new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Right, 1, null),
			(new KeyGesture(Key.Home, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Home, 0, null),
			(new KeyGesture(Key.Home, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Home, 1, null),
			(new KeyGesture(Key.End, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_End, 0, null),
			(new KeyGesture(Key.End, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_End, 1, null),
			(new KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Pgup, 0, null),
			(new KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Pgup, 1, null),
			(new KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Pgdn, 0, null),
			(new KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Pgdn, 1, null),
			(new KeyGesture(Key.Space, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Content_Navigate_Row, 0, null),
			(new KeyGesture(Key.Space, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Content_Navigate_Column, 0, null),
			(new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Table_Edit, 0, null),
			(new KeyGesture(Key.G, ModifierKeys.Control), NECommand.Position_Goto_Lines, 0, null),
			(new KeyGesture(Key.D, ModifierKeys.Control), NECommand.Diff_Diff, 0, null),
			(new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Diff_Diff, 1, null),
			(new KeyGesture(Key.Down, ModifierKeys.Alt), NECommand.Diff_Next, 0, null),
			(new KeyGesture(Key.Down, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Diff_Next, 1, null),
			(new KeyGesture(Key.Up, ModifierKeys.Alt), NECommand.Diff_Previous, 0, null),
			(new KeyGesture(Key.Up, ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Diff_Previous, 1, null),
			(new KeyGesture(Key.Left, ModifierKeys.Alt), NECommand.Diff_CopyLeft, 0, null),
			(new KeyGesture(Key.Right, ModifierKeys.Alt), NECommand.Diff_CopyRight, 0, null),
			(new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Network_WCF_Execute, 0, null),
			(new KeyGesture(Key.Q, ModifierKeys.Control), NECommand.Database_ExecuteQuery, 0, null),
			(new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.KeyValue_Set_Keys_IgnoreCase, 0, null),
			(new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift), NECommand.KeyValue_Set_Keys_MatchCase, 0, null),
			(new KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Shift), NECommand.KeyValue_Set_Values7, 0, null),
			(new KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Shift), NECommand.KeyValue_Set_Values8, 0, null),
			(new KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Shift), NECommand.KeyValue_Set_Values9, 0, null),
			(new KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.KeyValue_Add_Values7, 0, null),
			(new KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.KeyValue_Add_Values8, 0, null),
			(new KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.KeyValue_Add_Values9, 0, null),
			(new KeyGesture(Key.D7, ModifierKeys.Control | ModifierKeys.Alt), NECommand.KeyValue_Remove_Values7, 0, null),
			(new KeyGesture(Key.D8, ModifierKeys.Control | ModifierKeys.Alt), NECommand.KeyValue_Remove_Values8, 0, null),
			(new KeyGesture(Key.D9, ModifierKeys.Control | ModifierKeys.Alt), NECommand.KeyValue_Remove_Values9, 0, null),
			(new KeyGesture(Key.D7, ModifierKeys.Control), NECommand.KeyValue_Replace_Values7, 0, null),
			(new KeyGesture(Key.D8, ModifierKeys.Control), NECommand.KeyValue_Replace_Values8, 0, null),
			(new KeyGesture(Key.D9, ModifierKeys.Control), NECommand.KeyValue_Replace_Values9, 0, null),
			(new KeyGesture(Key.F6, ModifierKeys.None), NECommand.Macro_Play_Quick_6, 0, null),
			(new KeyGesture(Key.F7, ModifierKeys.None), NECommand.Macro_Play_Quick_7, 0, null),
			(new KeyGesture(Key.F8, ModifierKeys.None), NECommand.Macro_Play_Quick_8, 0, null),
			(new KeyGesture(Key.F9, ModifierKeys.None), NECommand.Macro_Play_Quick_9, 0, null),
			(new KeyGesture(Key.F10, ModifierKeys.None), NECommand.Macro_Play_Quick_10, 0, null),
			(new KeyGesture(Key.F11, ModifierKeys.None), NECommand.Macro_Play_Quick_11, 0, null),
			(new KeyGesture(Key.F12, ModifierKeys.None), NECommand.Macro_Play_Quick_12, 0, null),
			(new KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_6, 0, null),
			(new KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_7, 0, null),
			(new KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_8, 0, null),
			(new KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_9, 0, null),
			(new KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_10, 0, null),
			(new KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_11, 0, null),
			(new KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Macro_Record_Quick_12, 0, null),
			(new KeyGesture(Key.F6, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_6, 0, null),
			(new KeyGesture(Key.F7, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_7, 0, null),
			(new KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_8, 0, null),
			(new KeyGesture(Key.F9, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_9, 0, null),
			(new KeyGesture(Key.F10, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_10, 0, null),
			(new KeyGesture(Key.F11, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_11, 0, null),
			(new KeyGesture(Key.F12, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Macro_Open_Quick_12, 0, null),
			(new KeyGesture(Key.OemPeriod, ModifierKeys.Control), NECommand.Macro_RepeatLastAction, 0, null),
			(new KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Window_New_SummarizeSelections_Files_IgnoreCase, 0, null),
			(new KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Window_New_SummarizeSelections_Files_MatchCase, 0, null),
			(new KeyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Shift), NECommand.Window_New_SummarizeSelections_Selections_IgnoreCase, 0, null),
			(new KeyGesture(Key.M, ModifierKeys.Control), NECommand.Window_New_SummarizeSelections_Selections_MatchCase, 0, null),
			(new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Window_New_FromFiles_Active, 0, null),
			(new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift), NECommand.Window_New_FromFiles_CopiedCut, 0, null),
			(new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Window_Full, 0, null),
			(new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Window_Grid, 0, null),
			(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt), NECommand.Window_CustomGrid, 0, null),
			(new KeyGesture(Key.F3, ModifierKeys.None), NECommand.Window_WorkMode, 0, null),
		};

		static NEMenuItem()
		{
			var hotKeyDups = shortcuts.Select(x => x.KeyGesture.ToText()).GroupBy(x => x).Where(group => group.Skip(1).Any()).Select(group => group.Key).ToList();
			if (hotKeyDups.Any())
				throw new Exception($"Duplicate hotkeys: {string.Join(", ", hotKeyDups)}");
		}

		static string GetInputGestureText(NECommand command) => string.Join(", ", shortcuts.Where(x => (x.Command == command) && (x.Configuration == null)).Select(x => x.KeyGesture.ToText()));

		public NEMenuItem() => Command = new RoutedCommand();

		bool? multiStatus;
		public bool? MultiStatus
		{
			get => multiStatus;
			set
			{
				multiStatus = value;
				Icon = value switch
				{
					true => new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Checked.png")) },
					false => new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Unchecked.png")) },
					null => new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Indeterminate.png")) },
				};
			}
		}

		NECommand commandEnum;
		public NECommand CommandEnum
		{
			get => commandEnum;
			set
			{
				commandEnum = value;
				InputGestureText = GetInputGestureText(value);
			}
		}

		public static void RegisterCommands(UIElement window, ItemsControl menu, Action<NECommand, int, IConfiguration, bool?> handler)
		{
			foreach (var item in menu.Items)
				switch (item)
				{
					case NEMenuItem menuItem: menuItem.RegisterCommand(window, handler); break;
					case ItemsControl itemsControl: RegisterCommands(window, itemsControl, handler); break;
				}
		}

		void RegisterCommand(UIElement window, Action<NECommand, int, IConfiguration, bool?> handler)
		{
			window.CommandBindings.Add(new CommandBinding(Command, (s, e) => handler(commandEnum, 0, null, MultiStatus)));
			foreach (var shortcut in shortcuts.Where(x => x.Command == commandEnum))
			{
				var command = new RoutedCommand();
				window.CommandBindings.Add(new CommandBinding(command, (s, e) => handler(shortcut.Command, shortcut.Index, shortcut.Configuration, MultiStatus)));
				foreach (var gesture in GetGestures(shortcut.KeyGesture))
					window.InputBindings.Add(new KeyBinding(command, gesture));
			}
		}

		static IEnumerable<KeyGesture> GetGestures(KeyGesture keyGesture)
		{
			if (keyGesture == null)
				yield break;

			yield return keyGesture;
			if (keyGesture.Key == Key.Add)
				yield return new KeyGesture(Key.OemPlus, keyGesture.Modifiers);
			if (keyGesture.Key == Key.Subtract)
				yield return new KeyGesture(Key.OemMinus, keyGesture.Modifiers);
		}
	}
}
