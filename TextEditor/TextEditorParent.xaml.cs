using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorParent
	{
		[DepProp]
		string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		int ModifiedSteps { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		bool CheckUpdates { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorParent() { UIHelper<TextEditorParent>.Register(); }

		readonly UIHelper<TextEditorParent> uiHelper;
		public TextEditorParent(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int line = 1, int column = 1)
		{
			uiHelper = new UIHelper<TextEditorParent>(this);
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			canvas.OpenFile(filename, bytes, encoding);

			KeyDown += (s, e) => uiHelper.RaiseEvent(canvas, e);
			MouseWheel += (s, e) => uiHelper.RaiseEvent(canvas, e);
		}

		void RunCommand(TextEditCommand command)
		{
			canvas.InvalidateRender();

			var shiftDown = this.shiftDown;
			shiftOverride = shiftDown;

			switch (command)
			{
				case TextEditCommand.File_New: canvas.Command_File_New(); break;
				case TextEditCommand.File_Open: canvas.Command_File_Open(); break;
				case TextEditCommand.File_Save: canvas.Command_File_Save(); break;
				case TextEditCommand.File_SaveAs: canvas.Command_File_SaveAs(); break;
				case TextEditCommand.File_Revert: canvas.Command_File_Revert(); break;
				case TextEditCommand.File_CheckUpdates: canvas.Command_File_CheckUpdates(); break;
				case TextEditCommand.File_InsertFiles: canvas.Command_File_InsertFiles(); break;
				case TextEditCommand.File_CopyPath: canvas.Command_File_CopyPath(); break;
				case TextEditCommand.File_CopyName: canvas.Command_File_CopyName(); break;
				case TextEditCommand.File_BinaryEditor: canvas.Command_File_BinaryEditor(); Close(); break;
				case TextEditCommand.File_BOM: canvas.Command_File_BOM(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.Edit_Undo: canvas.Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: canvas.Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Cut: canvas.Command_Edit_CutCopy(true); break;
				case TextEditCommand.Edit_Copy: canvas.Command_Edit_CutCopy(false); break;
				case TextEditCommand.Edit_Paste: canvas.Command_Edit_Paste(); break;
				case TextEditCommand.Edit_ShowClipboard: canvas.Command_Edit_ShowClipboard(); break;
				case TextEditCommand.Edit_Find: canvas.Command_Edit_Find(); break;
				case TextEditCommand.Edit_FindNext: canvas.Command_Edit_FindNextPrev(true); break;
				case TextEditCommand.Edit_FindPrev: canvas.Command_Edit_FindNextPrev(false); break;
				case TextEditCommand.Edit_GotoLine: canvas.Command_Edit_GotoLine(); break;
				case TextEditCommand.Edit_GotoIndex: canvas.Command_Edit_GotoIndex(); break;
				case TextEditCommand.Files_Copy: canvas.Command_Files_CutCopy(false); break;
				case TextEditCommand.Files_Cut: canvas.Command_Files_CutCopy(true); break;
				case TextEditCommand.Files_Delete: canvas.Command_Files_Delete(); break;
				case TextEditCommand.Files_Timestamp_Write: canvas.Command_Files_Timestamp(TextEditor.TimestampType.Write); break;
				case TextEditCommand.Files_Timestamp_Access: canvas.Command_Files_Timestamp(TextEditor.TimestampType.Access); break;
				case TextEditCommand.Files_Timestamp_Create: canvas.Command_Files_Timestamp(TextEditor.TimestampType.Create); break;
				case TextEditCommand.Files_Timestamp_All: canvas.Command_Files_Timestamp(TextEditor.TimestampType.All); break;
				case TextEditCommand.Files_Path_Simplify: canvas.Command_Files_Path_Simplify(); break;
				case TextEditCommand.Files_Path_GetFileName: canvas.Command_Files_Path_GetFilePath(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Path_GetFileNameWoExtension: canvas.Command_Files_Path_GetFilePath(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Path_GetDirectory: canvas.Command_Files_Path_GetFilePath(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Path_GetExtension: canvas.Command_Files_Path_GetFilePath(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_CreateDirectory: canvas.Command_Files_CreateDirectory(); break;
				case TextEditCommand.Files_Information_Size: canvas.Command_Files_Information_Size(); break;
				case TextEditCommand.Files_Information_WriteTime: canvas.Command_Files_Information_WriteTime(); break;
				case TextEditCommand.Files_Information_AccessTime: canvas.Command_Files_Information_AccessTime(); break;
				case TextEditCommand.Files_Information_CreateTime: canvas.Command_Files_Information_CreateTime(); break;
				case TextEditCommand.Files_Information_Attributes: canvas.Command_Files_Information_Attributes(); break;
				case TextEditCommand.Files_Information_ReadOnly: canvas.Command_Files_Information_ReadOnly(); break;
				case TextEditCommand.Data_Case_Upper: canvas.Command_Data_Case_Upper(); break;
				case TextEditCommand.Data_Case_Lower: canvas.Command_Data_Case_Lower(); break;
				case TextEditCommand.Data_Case_Proper: canvas.Command_Data_Case_Proper(); break;
				case TextEditCommand.Data_Case_Toggle: canvas.Command_Data_Case_Toggle(); break;
				case TextEditCommand.Data_Hex_ToHex: canvas.Command_Data_Hex_ToHex(); break;
				case TextEditCommand.Data_Hex_FromHex: canvas.Command_Data_Hex_FromHex(); break;
				case TextEditCommand.Data_Char_ToChar: canvas.Command_Data_Char_ToChar(); break;
				case TextEditCommand.Data_Char_FromChar: canvas.Command_Data_Char_FromChar(); break;
				case TextEditCommand.Data_DateTime_Insert: canvas.Command_Data_DateTime_Insert(); break;
				case TextEditCommand.Data_DateTime_Convert: canvas.Command_Data_DateTime_Convert(); break;
				case TextEditCommand.Data_Length: canvas.Command_Data_Length(); break;
				case TextEditCommand.Data_Width: canvas.Command_Data_Width(); break;
				case TextEditCommand.Data_Trim: canvas.Command_Data_Trim(); break;
				case TextEditCommand.Data_EvaluateExpression: canvas.Command_Data_EvaluateExpression(); break;
				case TextEditCommand.Data_Series: canvas.Command_Data_Series(); break;
				case TextEditCommand.Data_Repeat: canvas.Command_Data_Repeat(); break;
				case TextEditCommand.Data_GUID: canvas.Command_Data_GUID(); break;
				case TextEditCommand.Data_Random: canvas.Command_Data_Random(); break;
				case TextEditCommand.Data_Escape_XML: canvas.Command_Data_Escape_XML(); break;
				case TextEditCommand.Data_Escape_Regex: canvas.Command_Data_Escape_Regex(); break;
				case TextEditCommand.Data_Unescape_XML: canvas.Command_Data_Unescape_XML(); break;
				case TextEditCommand.Data_Unescape_Regex: canvas.Command_Data_Unescape_Regex(); break;
				case TextEditCommand.Data_MD5_UTF8: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF8); break;
				case TextEditCommand.Data_MD5_UTF7: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF7); break;
				case TextEditCommand.Data_MD5_UTF16LE: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF16LE); break;
				case TextEditCommand.Data_MD5_UTF16BE: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF16BE); break;
				case TextEditCommand.Data_MD5_UTF32LE: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF32LE); break;
				case TextEditCommand.Data_MD5_UTF32BE: canvas.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF32BE); break;
				case TextEditCommand.Data_SHA1_UTF8: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF8); break;
				case TextEditCommand.Data_SHA1_UTF7: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF7); break;
				case TextEditCommand.Data_SHA1_UTF16LE: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF16LE); break;
				case TextEditCommand.Data_SHA1_UTF16BE: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF16BE); break;
				case TextEditCommand.Data_SHA1_UTF32LE: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF32LE); break;
				case TextEditCommand.Data_SHA1_UTF32BE: canvas.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF32BE); break;
				case TextEditCommand.Sort_String: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.String); break;
				case TextEditCommand.Sort_Numeric: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Numeric); break;
				case TextEditCommand.Sort_Keys: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Keys); break;
				case TextEditCommand.Sort_Reverse: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Reverse); break;
				case TextEditCommand.Sort_Randomize: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Randomize); break;
				case TextEditCommand.Sort_Length: canvas.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Length); break;
				case TextEditCommand.Sort_Lines_String: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.String); break;
				case TextEditCommand.Sort_Lines_Numeric: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Numeric); break;
				case TextEditCommand.Sort_Lines_Keys: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Keys); break;
				case TextEditCommand.Sort_Lines_Reverse: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Reverse); break;
				case TextEditCommand.Sort_Lines_Randomize: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Randomize); break;
				case TextEditCommand.Sort_Lines_Length: canvas.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Length); break;
				case TextEditCommand.Sort_Regions_String: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.String); break;
				case TextEditCommand.Sort_Regions_Numeric: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Numeric); break;
				case TextEditCommand.Sort_Regions_Keys: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Keys); break;
				case TextEditCommand.Sort_Regions_Reverse: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Reverse); break;
				case TextEditCommand.Sort_Regions_Randomize: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Randomize); break;
				case TextEditCommand.Sort_Regions_Length: canvas.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Length); break;
				case TextEditCommand.Keys_SetKeys: canvas.Command_Keys_SetValues(0); break;
				case TextEditCommand.Keys_SetValues1: canvas.Command_Keys_SetValues(1); break;
				case TextEditCommand.Keys_SetValues2: canvas.Command_Keys_SetValues(2); break;
				case TextEditCommand.Keys_SetValues3: canvas.Command_Keys_SetValues(3); break;
				case TextEditCommand.Keys_SetValues4: canvas.Command_Keys_SetValues(4); break;
				case TextEditCommand.Keys_SetValues5: canvas.Command_Keys_SetValues(5); break;
				case TextEditCommand.Keys_SetValues6: canvas.Command_Keys_SetValues(6); break;
				case TextEditCommand.Keys_SetValues7: canvas.Command_Keys_SetValues(7); break;
				case TextEditCommand.Keys_SetValues8: canvas.Command_Keys_SetValues(8); break;
				case TextEditCommand.Keys_SetValues9: canvas.Command_Keys_SetValues(9); break;
				case TextEditCommand.Keys_SelectionReplace1: canvas.Command_Keys_SelectionReplace(1); break;
				case TextEditCommand.Keys_SelectionReplace2: canvas.Command_Keys_SelectionReplace(2); break;
				case TextEditCommand.Keys_SelectionReplace3: canvas.Command_Keys_SelectionReplace(3); break;
				case TextEditCommand.Keys_SelectionReplace4: canvas.Command_Keys_SelectionReplace(4); break;
				case TextEditCommand.Keys_SelectionReplace5: canvas.Command_Keys_SelectionReplace(5); break;
				case TextEditCommand.Keys_SelectionReplace6: canvas.Command_Keys_SelectionReplace(6); break;
				case TextEditCommand.Keys_SelectionReplace7: canvas.Command_Keys_SelectionReplace(7); break;
				case TextEditCommand.Keys_SelectionReplace8: canvas.Command_Keys_SelectionReplace(8); break;
				case TextEditCommand.Keys_SelectionReplace9: canvas.Command_Keys_SelectionReplace(9); break;
				case TextEditCommand.Keys_GlobalFindKeys: canvas.Command_Keys_GlobalFind(0); break;
				case TextEditCommand.Keys_GlobalFind1: canvas.Command_Keys_GlobalFind(1); break;
				case TextEditCommand.Keys_GlobalFind2: canvas.Command_Keys_GlobalFind(2); break;
				case TextEditCommand.Keys_GlobalFind3: canvas.Command_Keys_GlobalFind(3); break;
				case TextEditCommand.Keys_GlobalFind4: canvas.Command_Keys_GlobalFind(4); break;
				case TextEditCommand.Keys_GlobalFind5: canvas.Command_Keys_GlobalFind(5); break;
				case TextEditCommand.Keys_GlobalFind6: canvas.Command_Keys_GlobalFind(6); break;
				case TextEditCommand.Keys_GlobalFind7: canvas.Command_Keys_GlobalFind(7); break;
				case TextEditCommand.Keys_GlobalFind8: canvas.Command_Keys_GlobalFind(8); break;
				case TextEditCommand.Keys_GlobalFind9: canvas.Command_Keys_GlobalFind(9); break;
				case TextEditCommand.Keys_GlobalReplace1: canvas.Command_Keys_GlobalReplace(1); break;
				case TextEditCommand.Keys_GlobalReplace2: canvas.Command_Keys_GlobalReplace(2); break;
				case TextEditCommand.Keys_GlobalReplace3: canvas.Command_Keys_GlobalReplace(3); break;
				case TextEditCommand.Keys_GlobalReplace4: canvas.Command_Keys_GlobalReplace(4); break;
				case TextEditCommand.Keys_GlobalReplace5: canvas.Command_Keys_GlobalReplace(5); break;
				case TextEditCommand.Keys_GlobalReplace6: canvas.Command_Keys_GlobalReplace(6); break;
				case TextEditCommand.Keys_GlobalReplace7: canvas.Command_Keys_GlobalReplace(7); break;
				case TextEditCommand.Keys_GlobalReplace8: canvas.Command_Keys_GlobalReplace(8); break;
				case TextEditCommand.Keys_GlobalReplace9: canvas.Command_Keys_GlobalReplace(9); break;
				case TextEditCommand.Keys_CopyKeys: canvas.Command_Keys_CopyValues(0); break;
				case TextEditCommand.Keys_CopyValues1: canvas.Command_Keys_CopyValues(1); break;
				case TextEditCommand.Keys_CopyValues2: canvas.Command_Keys_CopyValues(2); break;
				case TextEditCommand.Keys_CopyValues3: canvas.Command_Keys_CopyValues(3); break;
				case TextEditCommand.Keys_CopyValues4: canvas.Command_Keys_CopyValues(4); break;
				case TextEditCommand.Keys_CopyValues5: canvas.Command_Keys_CopyValues(5); break;
				case TextEditCommand.Keys_CopyValues6: canvas.Command_Keys_CopyValues(6); break;
				case TextEditCommand.Keys_CopyValues7: canvas.Command_Keys_CopyValues(7); break;
				case TextEditCommand.Keys_CopyValues8: canvas.Command_Keys_CopyValues(8); break;
				case TextEditCommand.Keys_CopyValues9: canvas.Command_Keys_CopyValues(9); break;
				case TextEditCommand.Keys_HitsKeys: canvas.Command_Keys_HitsValues(0); break;
				case TextEditCommand.Keys_HitsValues1: canvas.Command_Keys_HitsValues(1); break;
				case TextEditCommand.Keys_HitsValues2: canvas.Command_Keys_HitsValues(2); break;
				case TextEditCommand.Keys_HitsValues3: canvas.Command_Keys_HitsValues(3); break;
				case TextEditCommand.Keys_HitsValues4: canvas.Command_Keys_HitsValues(4); break;
				case TextEditCommand.Keys_HitsValues5: canvas.Command_Keys_HitsValues(5); break;
				case TextEditCommand.Keys_HitsValues6: canvas.Command_Keys_HitsValues(6); break;
				case TextEditCommand.Keys_HitsValues7: canvas.Command_Keys_HitsValues(7); break;
				case TextEditCommand.Keys_HitsValues8: canvas.Command_Keys_HitsValues(8); break;
				case TextEditCommand.Keys_HitsValues9: canvas.Command_Keys_HitsValues(9); break;
				case TextEditCommand.Keys_MissesKeys: canvas.Command_Keys_MissesValues(0); break;
				case TextEditCommand.Keys_MissesValues1: canvas.Command_Keys_MissesValues(1); break;
				case TextEditCommand.Keys_MissesValues2: canvas.Command_Keys_MissesValues(2); break;
				case TextEditCommand.Keys_MissesValues3: canvas.Command_Keys_MissesValues(3); break;
				case TextEditCommand.Keys_MissesValues4: canvas.Command_Keys_MissesValues(4); break;
				case TextEditCommand.Keys_MissesValues5: canvas.Command_Keys_MissesValues(5); break;
				case TextEditCommand.Keys_MissesValues6: canvas.Command_Keys_MissesValues(6); break;
				case TextEditCommand.Keys_MissesValues7: canvas.Command_Keys_MissesValues(7); break;
				case TextEditCommand.Keys_MissesValues8: canvas.Command_Keys_MissesValues(8); break;
				case TextEditCommand.Keys_MissesValues9: canvas.Command_Keys_MissesValues(9); break;
				case TextEditCommand.SelectMark_Toggle: canvas.Command_SelectMark_Toggle(); break;
				case TextEditCommand.Select_All: canvas.Command_Select_All(); break;
				case TextEditCommand.Select_Limit: canvas.Command_Select_Limit(); break;
				case TextEditCommand.Select_AllLines: canvas.Command_Select_AllLines(); break;
				case TextEditCommand.Select_Lines: canvas.Command_Select_Lines(); break;
				case TextEditCommand.Select_Marks: canvas.Command_Select_Marks(); break;
				case TextEditCommand.Select_Find: canvas.Command_Select_Find(); break;
				case TextEditCommand.Select_RemoveEmpty: canvas.Command_Select_RemoveEmpty(); break;
				case TextEditCommand.Select_Unique: canvas.Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: canvas.Command_Select_Duplicates(); break;
				case TextEditCommand.Select_Min_String: canvas.Command_Select_Min_String(); break;
				case TextEditCommand.Select_Min_Numeric: canvas.Command_Select_Min_Numeric(); break;
				case TextEditCommand.Select_Max_String: canvas.Command_Select_Max_String(); break;
				case TextEditCommand.Select_Max_Numeric: canvas.Command_Select_Max_Numeric(); break;
				case TextEditCommand.Select_ExpressionMatches: canvas.Command_Select_ExpressionMatches(); break;
				case TextEditCommand.Select_RegExMatches: canvas.Command_Select_RegExMatches(); break;
				case TextEditCommand.Select_RegExNonMatches: canvas.Command_Select_RegExNonMatches(); break;
				case TextEditCommand.Select_ShowFirst: canvas.Command_Select_ShowFirst(); break;
				case TextEditCommand.Select_ShowCurrent: canvas.Command_Select_ShowCurrent(); break;
				case TextEditCommand.Select_NextSelection: canvas.Command_Select_NextSelection(); break;
				case TextEditCommand.Select_PrevSelection: canvas.Command_Select_PrevSelection(); break;
				case TextEditCommand.Select_Single: canvas.Command_Select_Single(); break;
				case TextEditCommand.Select_Remove: canvas.Command_Select_Remove(); break;
				case TextEditCommand.Mark_Selection: canvas.Command_Mark_Selection(); break;
				case TextEditCommand.Mark_Find: canvas.Command_Mark_Find(); break;
				case TextEditCommand.Mark_Clear: canvas.Command_Mark_Clear(); break;
				case TextEditCommand.Mark_LimitToSelection: canvas.Command_Mark_LimitToSelection(); break;
			}

			shiftOverride = null;

			if (canvas.SelectionsInvalidated())
				canvas.EnsureVisible();
		}

		void EncodingClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			CoderUsed = Helpers.ParseEnum<Coder.Type>(header);
		}

		void HighlightingClicked(object sender, RoutedEventArgs e)
		{
			var header = (sender as MenuItem).Header.ToString();
			HighlightType = Helpers.ParseEnum<Highlighting.HighlightingType>(header);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			uiHelper.RaiseEvent(this, e);
		}

		internal bool? shiftOverride;
		internal bool shiftDown { get { return shiftOverride.HasValue ? shiftOverride.Value : (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		internal bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			shiftOverride = shiftDown;
			e.Handled = canvas.OnKeyDown(e.Key);
			shiftOverride = null;
		}
	}
}
