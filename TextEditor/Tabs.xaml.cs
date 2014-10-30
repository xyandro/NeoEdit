using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor
{
	public class Tabs : Tabs<TextEditor> { }

	public partial class TextEditorTabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> TextEditors { get { return UIHelper<TextEditorTabs>.GetPropValue<ObservableCollection<TextEditor>>(this); } set { UIHelper<TextEditorTabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor Active { get { return UIHelper<TextEditorTabs>.GetPropValue<TextEditor>(this); } set { UIHelper<TextEditorTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<TextEditorTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<TextEditorTabs>.SetPropValue(this, value); } }

		static TextEditorTabs() { UIHelper<TextEditorTabs>.Register(); }

		public static TextEditorTabs Create(string filename = null, byte[] bytes = null, StrCoder.CodePage codePage = StrCoder.CodePage.AutoByBOM, int line = 1, int column = 1, bool createNew = false)
		{
			var textEditorTabs = (!createNew ? UIHelper<TextEditorTabs>.GetNewest() : null) ?? new TextEditorTabs();
			textEditorTabs.Add(new TextEditor(filename, bytes, codePage, line, column));
			return textEditorTabs;
		}

		TextEditorTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			TextEditors = new ObservableCollection<TextEditor>();
		}

		void Command_File_Open()
		{
			var dir = Active != null ? Path.GetDirectoryName(Active.FileName) : null;
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
				InitialDirectory = dir,
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var filename in dialog.FileNames)
				Add(new TextEditor(filename));
		}

		void Command_File_OpenCopiedCutFiles()
		{
			var files = ClipboardWindow.GetStrings();
			if ((files == null) || (files.Count < 0))
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to open these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			foreach (var file in files)
				Add(new TextEditor(file));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = Message.OptionsEnum.None;
			var active = Active;
			foreach (var textEditor in TextEditors)
			{
				Active = textEditor;
				if (!textEditor.CanClose(ref answer))
				{
					e.Cancel = true;
					return;
				}
			}
			Active = active;
			base.OnClosing(e);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		void RunCommand(TextEditCommand command)
		{
			switch (command)
			{
				case TextEditCommand.File_New: Add(new TextEditor()); break;
				case TextEditCommand.File_Open: Command_File_Open(); break;
				case TextEditCommand.File_OpenCopiedCutFiles: Command_File_OpenCopiedCutFiles(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case TextEditCommand.File_Save: Active.Command_File_Save(); break;
				case TextEditCommand.File_SaveAs: Active.Command_File_SaveAs(); break;
				case TextEditCommand.File_Close: if (Active.CanClose()) { TextEditors.Remove(Active); } break;
				case TextEditCommand.File_Refresh: Active.Command_File_Refresh(); break;
				case TextEditCommand.File_Revert: Active.Command_File_Revert(); break;
				case TextEditCommand.File_InsertFiles: Active.Command_File_InsertFiles(); break;
				case TextEditCommand.File_CopyPath: Active.Command_File_CopyPath(); break;
				case TextEditCommand.File_CopyName: Active.Command_File_CopyName(); break;
				case TextEditCommand.File_Encoding: Active.Command_File_Encoding(); break;
				case TextEditCommand.File_BinaryEditor: if (Active.Command_File_BinaryEditor()) { TextEditors.Remove(Active); if (TextEditors.Count == 0) Close(); } break;
				case TextEditCommand.Edit_Undo: Active.Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Active.Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Cut: Active.Command_Edit_CutCopy(true); break;
				case TextEditCommand.Edit_Copy: Active.Command_Edit_CutCopy(false); break;
				case TextEditCommand.Edit_Paste: Active.Command_Edit_Paste(); break;
				case TextEditCommand.Edit_ShowClipboard: Active.Command_Edit_ShowClipboard(); break;
				case TextEditCommand.Edit_Find: Active.Command_Edit_Find(); break;
				case TextEditCommand.Edit_FindNext: Active.Command_Edit_FindNextPrev(true); break;
				case TextEditCommand.Edit_FindPrev: Active.Command_Edit_FindNextPrev(false); break;
				case TextEditCommand.Edit_GotoLine: Active.Command_Edit_Goto(true); break;
				case TextEditCommand.Edit_GotoColumn: Active.Command_Edit_Goto(false); break;
				case TextEditCommand.Files_Copy: Active.Command_Files_CutCopy(false); break;
				case TextEditCommand.Files_Cut: Active.Command_Files_CutCopy(true); break;
				case TextEditCommand.Files_Open: Active.Command_Files_Open(); break;
				case TextEditCommand.Files_Delete: Active.Command_Files_Delete(); break;
				case TextEditCommand.Files_Timestamp_Write: Active.Command_Files_Timestamp(TextEditor.TimestampType.Write); break;
				case TextEditCommand.Files_Timestamp_Access: Active.Command_Files_Timestamp(TextEditor.TimestampType.Access); break;
				case TextEditCommand.Files_Timestamp_Create: Active.Command_Files_Timestamp(TextEditor.TimestampType.Create); break;
				case TextEditCommand.Files_Timestamp_All: Active.Command_Files_Timestamp(TextEditor.TimestampType.All); break;
				case TextEditCommand.Files_Simplify: Active.Command_Files_Simplify(); break;
				case TextEditCommand.Files_CreateDirectory: Active.Command_Files_CreateDirectory(); break;
				case TextEditCommand.Files_Information_Size: Active.Command_Files_Information_Size(); break;
				case TextEditCommand.Files_Information_WriteTime: Active.Command_Files_Information_WriteTime(); break;
				case TextEditCommand.Files_Information_AccessTime: Active.Command_Files_Information_AccessTime(); break;
				case TextEditCommand.Files_Information_CreateTime: Active.Command_Files_Information_CreateTime(); break;
				case TextEditCommand.Files_Information_Attributes: Active.Command_Files_Information_Attributes(); break;
				case TextEditCommand.Files_Information_ReadOnly: Active.Command_Files_Information_ReadOnly(); break;
				case TextEditCommand.Files_Checksum_MD5: Active.Command_Files_Checksum(Checksum.Type.MD5); break;
				case TextEditCommand.Files_Checksum_SHA1: Active.Command_Files_Checksum(Checksum.Type.SHA1); break;
				case TextEditCommand.Files_Checksum_SHA256: Active.Command_Files_Checksum(Checksum.Type.SHA256); break;
				case TextEditCommand.Files_Select_FileName: Active.Command_Files_Select_GetFilePath(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Select_FileNamewoExtension: Active.Command_Files_Select_GetFilePath(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Select_DirectoryName: Active.Command_Files_Select_GetFilePath(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Select_Extension: Active.Command_Files_Select_GetFilePath(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_Select_Existing: Active.Command_Files_Select_Existing(true); break;
				case TextEditCommand.Files_Select_NonExisting: Active.Command_Files_Select_Existing(false); break;
				case TextEditCommand.Files_Select_Files: Active.Command_Files_Select_Files(); break;
				case TextEditCommand.Files_Select_Directories: Active.Command_Files_Select_Directories(); break;
				case TextEditCommand.Files_Select_Roots: Active.Command_Files_Select_Roots(true); break;
				case TextEditCommand.Files_Select_NonRoots: Active.Command_Files_Select_Roots(false); break;
				case TextEditCommand.Files_RenameKeysToSelections: Active.Command_Files_RenameKeysToSelections(); break;
				case TextEditCommand.Data_Case_Upper: Active.Command_Data_Case_Upper(); break;
				case TextEditCommand.Data_Case_Lower: Active.Command_Data_Case_Lower(); break;
				case TextEditCommand.Data_Case_Proper: Active.Command_Data_Case_Proper(); break;
				case TextEditCommand.Data_Case_Toggle: Active.Command_Data_Case_Toggle(); break;
				case TextEditCommand.Data_Hex_ToHex: Active.Command_Data_Hex_ToHex(); break;
				case TextEditCommand.Data_Hex_FromHex: Active.Command_Data_Hex_FromHex(); break;
				case TextEditCommand.Data_Binary_ToBinary_String: Active.Command_Data_Binary_ToBinary(); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntLE_UInt8LE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt8LE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntLE_UInt16LE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt16LE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntLE_UInt32LE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt32LE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntLE_UInt64LE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt64LE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntLE_Int8LE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int8LE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntLE_Int16LE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int16LE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntLE_Int32LE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int32LE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntLE_Int64LE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int64LE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntBE_UInt8BE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt8BE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntBE_UInt16BE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt16BE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntBE_UInt32BE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt32BE); break;
				case TextEditCommand.Data_Binary_ToBinary_UIntBE_UInt64BE: Active.Command_Data_Binary_ToBinary(Coder.Type.UInt64BE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntBE_Int8BE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int8BE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntBE_Int16BE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int16BE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntBE_Int32BE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int32BE); break;
				case TextEditCommand.Data_Binary_ToBinary_IntBE_Int64BE: Active.Command_Data_Binary_ToBinary(Coder.Type.Int64BE); break;
				case TextEditCommand.Data_Binary_ToBinary_Float_Single: Active.Command_Data_Binary_ToBinary(Coder.Type.Single); break;
				case TextEditCommand.Data_Binary_ToBinary_Float_Double: Active.Command_Data_Binary_ToBinary(Coder.Type.Double); break;
				case TextEditCommand.Data_Binary_FromBinary_String: Active.Command_Data_Binary_FromBinary(); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntLE_UInt8LE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt8LE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntLE_UInt16LE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt16LE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntLE_UInt32LE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt32LE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntLE_UInt64LE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt64LE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntLE_Int8LE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int8LE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntLE_Int16LE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int16LE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntLE_Int32LE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int32LE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntLE_Int64LE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int64LE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntBE_UInt8BE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt8BE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntBE_UInt16BE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt16BE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntBE_UInt32BE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt32BE); break;
				case TextEditCommand.Data_Binary_FromBinary_UIntBE_UInt64BE: Active.Command_Data_Binary_FromBinary(Coder.Type.UInt64BE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntBE_Int8BE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int8BE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntBE_Int16BE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int16BE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntBE_Int32BE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int32BE); break;
				case TextEditCommand.Data_Binary_FromBinary_IntBE_Int64BE: Active.Command_Data_Binary_FromBinary(Coder.Type.Int64BE); break;
				case TextEditCommand.Data_Binary_FromBinary_Float_Single: Active.Command_Data_Binary_FromBinary(Coder.Type.Single); break;
				case TextEditCommand.Data_Binary_FromBinary_Float_Double: Active.Command_Data_Binary_FromBinary(Coder.Type.Double); break;
				case TextEditCommand.Data_Base64_ToBase64: Active.Command_Data_Base64_ToBase64(); break;
				case TextEditCommand.Data_Base64_FromBase64: Active.Command_Data_Base64_FromBase64(); break;
				case TextEditCommand.Data_DateTime_Insert: Active.Command_Data_DateTime_Insert(); break;
				case TextEditCommand.Data_DateTime_Convert: Active.Command_Data_DateTime_Convert(); break;
				case TextEditCommand.Data_Length: Active.Command_Data_Length(); break;
				case TextEditCommand.Data_Width: Active.Command_Data_Width(); break;
				case TextEditCommand.Data_Trim: Active.Command_Data_Trim(); break;
				case TextEditCommand.Data_SingleLine: Active.Command_Data_SingleLine(); break;
				case TextEditCommand.Data_Table_ToTable: Active.Command_Data_ToTable(); break;
				case TextEditCommand.Data_Table_FromTable: Active.Command_Data_FromTable(); break;
				case TextEditCommand.Data_EvaluateExpression: Active.Command_Data_EvaluateExpression(); break;
				case TextEditCommand.Data_Series: Active.Command_Data_Series(); break;
				case TextEditCommand.Data_CopyDown: Active.Command_Data_CopyDown(); break;
				case TextEditCommand.Data_Copy_Count: Active.Command_Data_Copy_Count(); break;
				case TextEditCommand.Data_Copy_Min_String: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Min_Numeric: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Min_Length: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Max_String: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Max_Numeric: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Max_Length: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Sum: Active.Command_Data_Copy_Sum(); break;
				case TextEditCommand.Data_Repeat: Active.Command_Data_Repeat(); break;
				case TextEditCommand.Data_InsertGUID: Active.Command_Data_InsertGUID(); break;
				case TextEditCommand.Data_InsertRandomNumber: Active.Command_Data_InsertRandomNumber(); break;
				case TextEditCommand.Data_Escape_XML: Active.Command_Data_Escape_XML(); break;
				case TextEditCommand.Data_Escape_Regex: Active.Command_Data_Escape_Regex(); break;
				case TextEditCommand.Data_Escape_URL: Active.Command_Data_Escape_URL(); break;
				case TextEditCommand.Data_Unescape_XML: Active.Command_Data_Unescape_XML(); break;
				case TextEditCommand.Data_Unescape_Regex: Active.Command_Data_Unescape_Regex(); break;
				case TextEditCommand.Data_Unescape_URL: Active.Command_Data_Unescape_URL(); break;
				case TextEditCommand.Data_Checksum_MD5: Active.Command_Data_Checksum(Checksum.Type.MD5); break;
				case TextEditCommand.Data_Checksum_SHA1: Active.Command_Data_Checksum(Checksum.Type.SHA1); break;
				case TextEditCommand.Data_Checksum_SHA256: Active.Command_Data_Checksum(Checksum.Type.SHA256); break;
				case TextEditCommand.Data_Sort: Active.Command_Data_Sort(); break;
				case TextEditCommand.Keys_Set_Keys: Active.Command_Keys_Set(0); break;
				case TextEditCommand.Keys_Set_Values1: Active.Command_Keys_Set(1); break;
				case TextEditCommand.Keys_Set_Values2: Active.Command_Keys_Set(2); break;
				case TextEditCommand.Keys_Set_Values3: Active.Command_Keys_Set(3); break;
				case TextEditCommand.Keys_Set_Values4: Active.Command_Keys_Set(4); break;
				case TextEditCommand.Keys_Set_Values5: Active.Command_Keys_Set(5); break;
				case TextEditCommand.Keys_Set_Values6: Active.Command_Keys_Set(6); break;
				case TextEditCommand.Keys_Set_Values7: Active.Command_Keys_Set(7); break;
				case TextEditCommand.Keys_Set_Values8: Active.Command_Keys_Set(8); break;
				case TextEditCommand.Keys_Set_Values9: Active.Command_Keys_Set(9); break;
				case TextEditCommand.Keys_SelectionReplace_Values1: Active.Command_Keys_SelectionReplace(1); break;
				case TextEditCommand.Keys_SelectionReplace_Values2: Active.Command_Keys_SelectionReplace(2); break;
				case TextEditCommand.Keys_SelectionReplace_Values3: Active.Command_Keys_SelectionReplace(3); break;
				case TextEditCommand.Keys_SelectionReplace_Values4: Active.Command_Keys_SelectionReplace(4); break;
				case TextEditCommand.Keys_SelectionReplace_Values5: Active.Command_Keys_SelectionReplace(5); break;
				case TextEditCommand.Keys_SelectionReplace_Values6: Active.Command_Keys_SelectionReplace(6); break;
				case TextEditCommand.Keys_SelectionReplace_Values7: Active.Command_Keys_SelectionReplace(7); break;
				case TextEditCommand.Keys_SelectionReplace_Values8: Active.Command_Keys_SelectionReplace(8); break;
				case TextEditCommand.Keys_SelectionReplace_Values9: Active.Command_Keys_SelectionReplace(9); break;
				case TextEditCommand.Keys_GlobalFind_Keys: Active.Command_Keys_GlobalFind(0); break;
				case TextEditCommand.Keys_GlobalFind_Values1: Active.Command_Keys_GlobalFind(1); break;
				case TextEditCommand.Keys_GlobalFind_Values2: Active.Command_Keys_GlobalFind(2); break;
				case TextEditCommand.Keys_GlobalFind_Values3: Active.Command_Keys_GlobalFind(3); break;
				case TextEditCommand.Keys_GlobalFind_Values4: Active.Command_Keys_GlobalFind(4); break;
				case TextEditCommand.Keys_GlobalFind_Values5: Active.Command_Keys_GlobalFind(5); break;
				case TextEditCommand.Keys_GlobalFind_Values6: Active.Command_Keys_GlobalFind(6); break;
				case TextEditCommand.Keys_GlobalFind_Values7: Active.Command_Keys_GlobalFind(7); break;
				case TextEditCommand.Keys_GlobalFind_Values8: Active.Command_Keys_GlobalFind(8); break;
				case TextEditCommand.Keys_GlobalFind_Values9: Active.Command_Keys_GlobalFind(9); break;
				case TextEditCommand.Keys_GlobalReplace_Values1: Active.Command_Keys_GlobalReplace(1); break;
				case TextEditCommand.Keys_GlobalReplace_Values2: Active.Command_Keys_GlobalReplace(2); break;
				case TextEditCommand.Keys_GlobalReplace_Values3: Active.Command_Keys_GlobalReplace(3); break;
				case TextEditCommand.Keys_GlobalReplace_Values4: Active.Command_Keys_GlobalReplace(4); break;
				case TextEditCommand.Keys_GlobalReplace_Values5: Active.Command_Keys_GlobalReplace(5); break;
				case TextEditCommand.Keys_GlobalReplace_Values6: Active.Command_Keys_GlobalReplace(6); break;
				case TextEditCommand.Keys_GlobalReplace_Values7: Active.Command_Keys_GlobalReplace(7); break;
				case TextEditCommand.Keys_GlobalReplace_Values8: Active.Command_Keys_GlobalReplace(8); break;
				case TextEditCommand.Keys_GlobalReplace_Values9: Active.Command_Keys_GlobalReplace(9); break;
				case TextEditCommand.Keys_Copy_Keys: Active.Command_Keys_Copy(0); break;
				case TextEditCommand.Keys_Copy_Values1: Active.Command_Keys_Copy(1); break;
				case TextEditCommand.Keys_Copy_Values2: Active.Command_Keys_Copy(2); break;
				case TextEditCommand.Keys_Copy_Values3: Active.Command_Keys_Copy(3); break;
				case TextEditCommand.Keys_Copy_Values4: Active.Command_Keys_Copy(4); break;
				case TextEditCommand.Keys_Copy_Values5: Active.Command_Keys_Copy(5); break;
				case TextEditCommand.Keys_Copy_Values6: Active.Command_Keys_Copy(6); break;
				case TextEditCommand.Keys_Copy_Values7: Active.Command_Keys_Copy(7); break;
				case TextEditCommand.Keys_Copy_Values8: Active.Command_Keys_Copy(8); break;
				case TextEditCommand.Keys_Copy_Values9: Active.Command_Keys_Copy(9); break;
				case TextEditCommand.Keys_Hits_Keys: Active.Command_Keys_HitsMisses(0, true); break;
				case TextEditCommand.Keys_Hits_Values1: Active.Command_Keys_HitsMisses(1, true); break;
				case TextEditCommand.Keys_Hits_Values2: Active.Command_Keys_HitsMisses(2, true); break;
				case TextEditCommand.Keys_Hits_Values3: Active.Command_Keys_HitsMisses(3, true); break;
				case TextEditCommand.Keys_Hits_Values4: Active.Command_Keys_HitsMisses(4, true); break;
				case TextEditCommand.Keys_Hits_Values5: Active.Command_Keys_HitsMisses(5, true); break;
				case TextEditCommand.Keys_Hits_Values6: Active.Command_Keys_HitsMisses(6, true); break;
				case TextEditCommand.Keys_Hits_Values7: Active.Command_Keys_HitsMisses(7, true); break;
				case TextEditCommand.Keys_Hits_Values8: Active.Command_Keys_HitsMisses(8, true); break;
				case TextEditCommand.Keys_Hits_Values9: Active.Command_Keys_HitsMisses(9, true); break;
				case TextEditCommand.Keys_Misses_Keys: Active.Command_Keys_HitsMisses(0, false); break;
				case TextEditCommand.Keys_Misses_Values1: Active.Command_Keys_HitsMisses(1, false); break;
				case TextEditCommand.Keys_Misses_Values2: Active.Command_Keys_HitsMisses(2, false); break;
				case TextEditCommand.Keys_Misses_Values3: Active.Command_Keys_HitsMisses(3, false); break;
				case TextEditCommand.Keys_Misses_Values4: Active.Command_Keys_HitsMisses(4, false); break;
				case TextEditCommand.Keys_Misses_Values5: Active.Command_Keys_HitsMisses(5, false); break;
				case TextEditCommand.Keys_Misses_Values6: Active.Command_Keys_HitsMisses(6, false); break;
				case TextEditCommand.Keys_Misses_Values7: Active.Command_Keys_HitsMisses(7, false); break;
				case TextEditCommand.Keys_Misses_Values8: Active.Command_Keys_HitsMisses(8, false); break;
				case TextEditCommand.Keys_Misses_Values9: Active.Command_Keys_HitsMisses(9, false); break;
				case TextEditCommand.Keys_CountstoKeysValues1: Active.Command_Keys_CountstoKeysValues1(); break;
				case TextEditCommand.SelectMark_Toggle: Active.Command_SelectMark_Toggle(); break;
				case TextEditCommand.Select_All: Active.Command_Select_All(); break;
				case TextEditCommand.Select_Limit: Active.Command_Select_Limit(); break;
				case TextEditCommand.Select_Lines: Active.Command_Select_Lines(); break;
				case TextEditCommand.Select_Empty: Active.Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Active.Command_Select_Empty(false); break;
				case TextEditCommand.Select_Trim: Active.Command_Select_Trim(); break;
				case TextEditCommand.Select_Unique: Active.Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Active.Command_Select_Duplicates(); break;
				case TextEditCommand.Select_Marks: Active.Command_Select_Marks(); break;
				case TextEditCommand.Select_FindResults: Active.Command_Select_FindResults(); break;
				case TextEditCommand.Select_Min_String: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Min_Numeric: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Min_Length: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_Max_String: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Max_Numeric: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Max_Length: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_ExpressionMatches: Active.Command_Select_ExpressionMatches(); break;
				case TextEditCommand.Select_RegExMatches: Active.Command_Select_RegExMatches(); break;
				case TextEditCommand.Select_FirstSelection: Active.Command_Select_FirstSelection(); break;
				case TextEditCommand.Select_ShowCurrent: Active.Command_Select_ShowCurrent(); break;
				case TextEditCommand.Select_NextSelection: Active.Command_Select_NextSelection(); break;
				case TextEditCommand.Select_PrevSelection: Active.Command_Select_PrevSelection(); break;
				case TextEditCommand.Select_Single: Active.Command_Select_Single(); break;
				case TextEditCommand.Select_Remove: Active.Command_Select_Remove(); break;
				case TextEditCommand.Mark_Selection: Active.Command_Mark_Selection(); break;
				case TextEditCommand.Mark_FindResults: Active.Command_Mark_FindResults(); break;
				case TextEditCommand.Mark_ClearMarks: Active.Command_Mark_ClearMarks(); break;
				case TextEditCommand.Mark_LimitToSelection: Active.Command_Mark_LimitToSelection(); break;
				case TextEditCommand.View_Highlighting_None: Active.HighlightType = Highlighting.HighlightingType.None; break;
				case TextEditCommand.View_Highlighting_CSharp: Active.HighlightType = Highlighting.HighlightingType.CSharp; break;
				case TextEditCommand.View_Highlighting_CPlusPlus: Active.HighlightType = Highlighting.HighlightingType.CPlusPlus; break;
			}
		}

		void Add(TextEditor textEditor)
		{
			if ((Active != null) && (Active.Empty()))
			{
				var index = TextEditors.IndexOf(Active);
				Active = TextEditors[index] = textEditor;
				return;
			}

			TextEditors.Add(textEditor);
			Active = textEditor;
		}

		Label GetLabel(TextEditor textEditor)
		{
			return textEditor.GetLabel();
		}
	}
}
