using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
		public ObservableCollection<TextEditor> TextEditors { get { return uiHelper.GetPropValue<ObservableCollection<TextEditor>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<TextEditor> Selected { get { return uiHelper.GetPropValue<ObservableCollection<TextEditor>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public TextEditor Active { get { return uiHelper.GetPropValue<TextEditor>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Tabs.ViewType View { get { return uiHelper.GetPropValue<Tabs.ViewType>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorTabs()
		{
			UIHelper<TextEditorTabs>.Register();
		}

		readonly UIHelper<TextEditorTabs> uiHelper;
		public TextEditorTabs(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int line = 1, int column = 1)
		{
			uiHelper = new UIHelper<TextEditorTabs>(this);
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			TextEditors = new ObservableCollection<TextEditor>();
			Add(new TextEditor(filename, bytes, encoding, line, column));
			TextEditors.Add(new TextEditor(@"C:\Documents\Cpp\NeoEdit - Work\Test2.cs"));
			TextEditors.Add(new TextEditor(@"C:\Documents\Cpp\NeoEdit - Work\Test3.cs"));
			TextEditors.Add(new TextEditor(@"C:\Documents\Cpp\NeoEdit - Work\Test4.xaml"));
			Selected = new ObservableCollection<TextEditor>(TextEditors);

			View = Tabs<TextEditor>.ViewType.Tiles;
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

		void Command_File_OpenCopied()
		{
			var files = ClipboardWindow.GetFiles();
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
			var active = Active;
			foreach (var textEditor in TextEditors)
			{
				Active = textEditor;
				if (!textEditor.CanClose())
				{
					e.Cancel = true;
					return;
				}
			}
			Active = active;
			TextEditors.ToList().ForEach(textEditor => textEditor.Close());
			base.OnClosing(e);
		}

		void RunCommand(TextEditCommand command)
		{
			switch (command)
			{
				case TextEditCommand.File_New: Add(new TextEditor()); break;
				case TextEditCommand.File_Open: Command_File_Open(); break;
				case TextEditCommand.File_OpenCopied: Command_File_OpenCopied(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
				default: RunChildCommand(command); break;
			}
		}

		void RunChildCommand(TextEditCommand command)
		{
			foreach (var editor in Selected)
			{
				switch (command)
				{
					case TextEditCommand.File_Save: editor.Command_File_Save(); break;
					case TextEditCommand.File_SaveAs: editor.Command_File_SaveAs(); break;
					case TextEditCommand.File_Close: if (editor.CanClose()) { editor.Close(); TextEditors.Remove(editor); } break;
					case TextEditCommand.File_Refresh: editor.Command_File_Refresh(); break;
					case TextEditCommand.File_Revert: editor.Command_File_Revert(); break;
					case TextEditCommand.File_InsertFiles: editor.Command_File_InsertFiles(); break;
					case TextEditCommand.File_CopyPath: editor.Command_File_CopyPath(); break;
					case TextEditCommand.File_CopyName: editor.Command_File_CopyName(); break;
					case TextEditCommand.File_Encoding_UTF8: editor.CoderUsed = Coder.Type.UTF8; break;
					case TextEditCommand.File_Encoding_UTF7: editor.CoderUsed = Coder.Type.UTF7; break;
					case TextEditCommand.File_Encoding_UTF16LE: editor.CoderUsed = Coder.Type.UTF16LE; break;
					case TextEditCommand.File_Encoding_UTF16BE: editor.CoderUsed = Coder.Type.UTF16BE; break;
					case TextEditCommand.File_Encoding_UTF32LE: editor.CoderUsed = Coder.Type.UTF32LE; break;
					case TextEditCommand.File_Encoding_UTF32BE: editor.CoderUsed = Coder.Type.UTF32BE; break;
					case TextEditCommand.File_Encoding_Base64: editor.CoderUsed = Coder.Type.Base64; break;
					case TextEditCommand.File_BOM: editor.Command_File_BOM(); break;
					case TextEditCommand.File_Ending_CRLF: editor.Command_File_SetEndings("\r\n"); break;
					case TextEditCommand.File_Ending_LF: editor.Command_File_SetEndings("\n"); break;
					case TextEditCommand.File_Ending_CR: editor.Command_File_SetEndings("\r"); break;
					case TextEditCommand.File_BinaryEditor: editor.Command_File_BinaryEditor(); editor.Close(); TextEditors.Remove(editor); if (TextEditors.Count == 0) Close(); break;
					case TextEditCommand.Edit_Undo: editor.Command_Edit_Undo(); break;
					case TextEditCommand.Edit_Redo: editor.Command_Edit_Redo(); break;
					case TextEditCommand.Edit_Cut: editor.Command_Edit_CutCopy(true); break;
					case TextEditCommand.Edit_Copy: editor.Command_Edit_CutCopy(false); break;
					case TextEditCommand.Edit_Paste: editor.Command_Edit_Paste(); break;
					case TextEditCommand.Edit_ShowClipboard: editor.Command_Edit_ShowClipboard(); break;
					case TextEditCommand.Edit_Find: editor.Command_Edit_Find(); break;
					case TextEditCommand.Edit_FindNext: editor.Command_Edit_FindNextPrev(true); break;
					case TextEditCommand.Edit_FindPrev: editor.Command_Edit_FindNextPrev(false); break;
					case TextEditCommand.Edit_GotoLine: editor.Command_Edit_GotoLine(); break;
					case TextEditCommand.Edit_GotoIndex: editor.Command_Edit_GotoIndex(); break;
					case TextEditCommand.Files_Copy: editor.Command_Files_CutCopy(false); break;
					case TextEditCommand.Files_Cut: editor.Command_Files_CutCopy(true); break;
					case TextEditCommand.Files_Delete: editor.Command_Files_Delete(); break;
					case TextEditCommand.Files_Timestamp_Write: editor.Command_Files_Timestamp(TextEditor.TimestampType.Write); break;
					case TextEditCommand.Files_Timestamp_Access: editor.Command_Files_Timestamp(TextEditor.TimestampType.Access); break;
					case TextEditCommand.Files_Timestamp_Create: editor.Command_Files_Timestamp(TextEditor.TimestampType.Create); break;
					case TextEditCommand.Files_Timestamp_All: editor.Command_Files_Timestamp(TextEditor.TimestampType.All); break;
					case TextEditCommand.Files_Path_Simplify: editor.Command_Files_Path_Simplify(); break;
					case TextEditCommand.Files_Path_GetFileName: editor.Command_Files_Path_GetFilePath(TextEditor.GetPathType.FileName); break;
					case TextEditCommand.Files_Path_GetFileNameWoExtension: editor.Command_Files_Path_GetFilePath(TextEditor.GetPathType.FileNameWoExtension); break;
					case TextEditCommand.Files_Path_GetDirectory: editor.Command_Files_Path_GetFilePath(TextEditor.GetPathType.Directory); break;
					case TextEditCommand.Files_Path_GetExtension: editor.Command_Files_Path_GetFilePath(TextEditor.GetPathType.Extension); break;
					case TextEditCommand.Files_CreateDirectory: editor.Command_Files_CreateDirectory(); break;
					case TextEditCommand.Files_Information_Size: editor.Command_Files_Information_Size(); break;
					case TextEditCommand.Files_Information_WriteTime: editor.Command_Files_Information_WriteTime(); break;
					case TextEditCommand.Files_Information_AccessTime: editor.Command_Files_Information_AccessTime(); break;
					case TextEditCommand.Files_Information_CreateTime: editor.Command_Files_Information_CreateTime(); break;
					case TextEditCommand.Files_Information_Attributes: editor.Command_Files_Information_Attributes(); break;
					case TextEditCommand.Files_Information_ReadOnly: editor.Command_Files_Information_ReadOnly(); break;
					case TextEditCommand.Files_Select_Existing: editor.Command_Files_Select_Existing(TextEditMenuItem.LastClick == MouseButton.Left); break;
					case TextEditCommand.Files_Select_Files: editor.Command_Files_Select_Files(); break;
					case TextEditCommand.Files_Select_Directories: editor.Command_Files_Select_Directories(); break;
					case TextEditCommand.Files_Select_Roots: editor.Command_Files_Select_Roots(TextEditMenuItem.LastClick == MouseButton.Left); break;
					case TextEditCommand.Files_RenameKeysToSelections: editor.Command_Files_RenameKeysToSelections(); break;
					case TextEditCommand.Data_Case_Upper: editor.Command_Data_Case_Upper(); break;
					case TextEditCommand.Data_Case_Lower: editor.Command_Data_Case_Lower(); break;
					case TextEditCommand.Data_Case_Proper: editor.Command_Data_Case_Proper(); break;
					case TextEditCommand.Data_Case_Toggle: editor.Command_Data_Case_Toggle(); break;
					case TextEditCommand.Data_Hex_ToHex: editor.Command_Data_Hex_ToHex(); break;
					case TextEditCommand.Data_Hex_FromHex: editor.Command_Data_Hex_FromHex(); break;
					case TextEditCommand.Data_Char_ToChar: editor.Command_Data_Char_ToChar(); break;
					case TextEditCommand.Data_Char_FromChar: editor.Command_Data_Char_FromChar(); break;
					case TextEditCommand.Data_DateTime_Insert: editor.Command_Data_DateTime_Insert(); break;
					case TextEditCommand.Data_DateTime_Convert: editor.Command_Data_DateTime_Convert(); break;
					case TextEditCommand.Data_Length: editor.Command_Data_Length(); break;
					case TextEditCommand.Data_Width: editor.Command_Data_Width(); break;
					case TextEditCommand.Data_Trim: editor.Command_Data_Trim(); break;
					case TextEditCommand.Data_EvaluateExpression: editor.Command_Data_EvaluateExpression(); break;
					case TextEditCommand.Data_Series: editor.Command_Data_Series(); break;
					case TextEditCommand.Data_Repeat: editor.Command_Data_Repeat(); break;
					case TextEditCommand.Data_GUID: editor.Command_Data_GUID(); break;
					case TextEditCommand.Data_Random: editor.Command_Data_Random(); break;
					case TextEditCommand.Data_Escape_XML: editor.Command_Data_Escape_XML(); break;
					case TextEditCommand.Data_Escape_Regex: editor.Command_Data_Escape_Regex(); break;
					case TextEditCommand.Data_Unescape_XML: editor.Command_Data_Unescape_XML(); break;
					case TextEditCommand.Data_Unescape_Regex: editor.Command_Data_Unescape_Regex(); break;
					case TextEditCommand.Data_MD5_UTF8: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF8); break;
					case TextEditCommand.Data_MD5_UTF7: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF7); break;
					case TextEditCommand.Data_MD5_UTF16LE: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF16LE); break;
					case TextEditCommand.Data_MD5_UTF16BE: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF16BE); break;
					case TextEditCommand.Data_MD5_UTF32LE: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF32LE); break;
					case TextEditCommand.Data_MD5_UTF32BE: editor.Command_Data_Checksum(Checksum.Type.MD5, Coder.Type.UTF32BE); break;
					case TextEditCommand.Data_SHA1_UTF8: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF8); break;
					case TextEditCommand.Data_SHA1_UTF7: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF7); break;
					case TextEditCommand.Data_SHA1_UTF16LE: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF16LE); break;
					case TextEditCommand.Data_SHA1_UTF16BE: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF16BE); break;
					case TextEditCommand.Data_SHA1_UTF32LE: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF32LE); break;
					case TextEditCommand.Data_SHA1_UTF32BE: editor.Command_Data_Checksum(Checksum.Type.SHA1, Coder.Type.UTF32BE); break;
					case TextEditCommand.Data_SHA256_UTF8: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF8); break;
					case TextEditCommand.Data_SHA256_UTF7: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF7); break;
					case TextEditCommand.Data_SHA256_UTF16LE: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF16LE); break;
					case TextEditCommand.Data_SHA256_UTF16BE: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF16BE); break;
					case TextEditCommand.Data_SHA256_UTF32LE: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF32LE); break;
					case TextEditCommand.Data_SHA256_UTF32BE: editor.Command_Data_Checksum(Checksum.Type.SHA256, Coder.Type.UTF32BE); break;
					case TextEditCommand.Sort_String: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.String); break;
					case TextEditCommand.Sort_Numeric: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Numeric); break;
					case TextEditCommand.Sort_Keys: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Keys); break;
					case TextEditCommand.Sort_Reverse: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Reverse); break;
					case TextEditCommand.Sort_Randomize: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Randomize); break;
					case TextEditCommand.Sort_Length: editor.Command_Sort(TextEditor.SortScope.Selections, TextEditor.SortType.Length); break;
					case TextEditCommand.Sort_Lines_String: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.String); break;
					case TextEditCommand.Sort_Lines_Numeric: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Numeric); break;
					case TextEditCommand.Sort_Lines_Keys: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Keys); break;
					case TextEditCommand.Sort_Lines_Reverse: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Reverse); break;
					case TextEditCommand.Sort_Lines_Randomize: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Randomize); break;
					case TextEditCommand.Sort_Lines_Length: editor.Command_Sort(TextEditor.SortScope.Lines, TextEditor.SortType.Length); break;
					case TextEditCommand.Sort_Regions_String: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.String); break;
					case TextEditCommand.Sort_Regions_Numeric: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Numeric); break;
					case TextEditCommand.Sort_Regions_Keys: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Keys); break;
					case TextEditCommand.Sort_Regions_Reverse: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Reverse); break;
					case TextEditCommand.Sort_Regions_Randomize: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Randomize); break;
					case TextEditCommand.Sort_Regions_Length: editor.Command_Sort(TextEditor.SortScope.Regions, TextEditor.SortType.Length); break;
					case TextEditCommand.Keys_SetKeys: editor.Command_Keys_SetValues(0); break;
					case TextEditCommand.Keys_SetValues1: editor.Command_Keys_SetValues(1); break;
					case TextEditCommand.Keys_SetValues2: editor.Command_Keys_SetValues(2); break;
					case TextEditCommand.Keys_SetValues3: editor.Command_Keys_SetValues(3); break;
					case TextEditCommand.Keys_SetValues4: editor.Command_Keys_SetValues(4); break;
					case TextEditCommand.Keys_SetValues5: editor.Command_Keys_SetValues(5); break;
					case TextEditCommand.Keys_SetValues6: editor.Command_Keys_SetValues(6); break;
					case TextEditCommand.Keys_SetValues7: editor.Command_Keys_SetValues(7); break;
					case TextEditCommand.Keys_SetValues8: editor.Command_Keys_SetValues(8); break;
					case TextEditCommand.Keys_SetValues9: editor.Command_Keys_SetValues(9); break;
					case TextEditCommand.Keys_SelectionReplace1: editor.Command_Keys_SelectionReplace(1); break;
					case TextEditCommand.Keys_SelectionReplace2: editor.Command_Keys_SelectionReplace(2); break;
					case TextEditCommand.Keys_SelectionReplace3: editor.Command_Keys_SelectionReplace(3); break;
					case TextEditCommand.Keys_SelectionReplace4: editor.Command_Keys_SelectionReplace(4); break;
					case TextEditCommand.Keys_SelectionReplace5: editor.Command_Keys_SelectionReplace(5); break;
					case TextEditCommand.Keys_SelectionReplace6: editor.Command_Keys_SelectionReplace(6); break;
					case TextEditCommand.Keys_SelectionReplace7: editor.Command_Keys_SelectionReplace(7); break;
					case TextEditCommand.Keys_SelectionReplace8: editor.Command_Keys_SelectionReplace(8); break;
					case TextEditCommand.Keys_SelectionReplace9: editor.Command_Keys_SelectionReplace(9); break;
					case TextEditCommand.Keys_GlobalFindKeys: editor.Command_Keys_GlobalFind(0); break;
					case TextEditCommand.Keys_GlobalFind1: editor.Command_Keys_GlobalFind(1); break;
					case TextEditCommand.Keys_GlobalFind2: editor.Command_Keys_GlobalFind(2); break;
					case TextEditCommand.Keys_GlobalFind3: editor.Command_Keys_GlobalFind(3); break;
					case TextEditCommand.Keys_GlobalFind4: editor.Command_Keys_GlobalFind(4); break;
					case TextEditCommand.Keys_GlobalFind5: editor.Command_Keys_GlobalFind(5); break;
					case TextEditCommand.Keys_GlobalFind6: editor.Command_Keys_GlobalFind(6); break;
					case TextEditCommand.Keys_GlobalFind7: editor.Command_Keys_GlobalFind(7); break;
					case TextEditCommand.Keys_GlobalFind8: editor.Command_Keys_GlobalFind(8); break;
					case TextEditCommand.Keys_GlobalFind9: editor.Command_Keys_GlobalFind(9); break;
					case TextEditCommand.Keys_GlobalReplace1: editor.Command_Keys_GlobalReplace(1); break;
					case TextEditCommand.Keys_GlobalReplace2: editor.Command_Keys_GlobalReplace(2); break;
					case TextEditCommand.Keys_GlobalReplace3: editor.Command_Keys_GlobalReplace(3); break;
					case TextEditCommand.Keys_GlobalReplace4: editor.Command_Keys_GlobalReplace(4); break;
					case TextEditCommand.Keys_GlobalReplace5: editor.Command_Keys_GlobalReplace(5); break;
					case TextEditCommand.Keys_GlobalReplace6: editor.Command_Keys_GlobalReplace(6); break;
					case TextEditCommand.Keys_GlobalReplace7: editor.Command_Keys_GlobalReplace(7); break;
					case TextEditCommand.Keys_GlobalReplace8: editor.Command_Keys_GlobalReplace(8); break;
					case TextEditCommand.Keys_GlobalReplace9: editor.Command_Keys_GlobalReplace(9); break;
					case TextEditCommand.Keys_CopyKeys: editor.Command_Keys_CopyValues(0); break;
					case TextEditCommand.Keys_CopyValues1: editor.Command_Keys_CopyValues(1); break;
					case TextEditCommand.Keys_CopyValues2: editor.Command_Keys_CopyValues(2); break;
					case TextEditCommand.Keys_CopyValues3: editor.Command_Keys_CopyValues(3); break;
					case TextEditCommand.Keys_CopyValues4: editor.Command_Keys_CopyValues(4); break;
					case TextEditCommand.Keys_CopyValues5: editor.Command_Keys_CopyValues(5); break;
					case TextEditCommand.Keys_CopyValues6: editor.Command_Keys_CopyValues(6); break;
					case TextEditCommand.Keys_CopyValues7: editor.Command_Keys_CopyValues(7); break;
					case TextEditCommand.Keys_CopyValues8: editor.Command_Keys_CopyValues(8); break;
					case TextEditCommand.Keys_CopyValues9: editor.Command_Keys_CopyValues(9); break;
					case TextEditCommand.Keys_HitsKeys: editor.Command_Keys_HitsMisses(0, true); break;
					case TextEditCommand.Keys_HitsValues1: editor.Command_Keys_HitsMisses(1, true); break;
					case TextEditCommand.Keys_HitsValues2: editor.Command_Keys_HitsMisses(2, true); break;
					case TextEditCommand.Keys_HitsValues3: editor.Command_Keys_HitsMisses(3, true); break;
					case TextEditCommand.Keys_HitsValues4: editor.Command_Keys_HitsMisses(4, true); break;
					case TextEditCommand.Keys_HitsValues5: editor.Command_Keys_HitsMisses(5, true); break;
					case TextEditCommand.Keys_HitsValues6: editor.Command_Keys_HitsMisses(6, true); break;
					case TextEditCommand.Keys_HitsValues7: editor.Command_Keys_HitsMisses(7, true); break;
					case TextEditCommand.Keys_HitsValues8: editor.Command_Keys_HitsMisses(8, true); break;
					case TextEditCommand.Keys_HitsValues9: editor.Command_Keys_HitsMisses(9, true); break;
					case TextEditCommand.Keys_MissesKeys: editor.Command_Keys_HitsMisses(0, false); break;
					case TextEditCommand.Keys_MissesValues1: editor.Command_Keys_HitsMisses(1, false); break;
					case TextEditCommand.Keys_MissesValues2: editor.Command_Keys_HitsMisses(2, false); break;
					case TextEditCommand.Keys_MissesValues3: editor.Command_Keys_HitsMisses(3, false); break;
					case TextEditCommand.Keys_MissesValues4: editor.Command_Keys_HitsMisses(4, false); break;
					case TextEditCommand.Keys_MissesValues5: editor.Command_Keys_HitsMisses(5, false); break;
					case TextEditCommand.Keys_MissesValues6: editor.Command_Keys_HitsMisses(6, false); break;
					case TextEditCommand.Keys_MissesValues7: editor.Command_Keys_HitsMisses(7, false); break;
					case TextEditCommand.Keys_MissesValues8: editor.Command_Keys_HitsMisses(8, false); break;
					case TextEditCommand.Keys_MissesValues9: editor.Command_Keys_HitsMisses(9, false); break;
					case TextEditCommand.Keys_Counts: editor.Command_Keys_Counts(); break;
					case TextEditCommand.SelectMark_Toggle: editor.Command_SelectMark_Toggle(); break;
					case TextEditCommand.Select_All: editor.Command_Select_All(); break;
					case TextEditCommand.Select_Limit: editor.Command_Select_Limit(); break;
					case TextEditCommand.Select_Lines: editor.Command_Select_Lines(); break;
					case TextEditCommand.Select_NonEmpty: editor.Command_Select_NonEmpty(TextEditMenuItem.LastClick == MouseButton.Left); break;
					case TextEditCommand.Select_Unique: editor.Command_Select_Unique(); break;
					case TextEditCommand.Select_Duplicates: editor.Command_Select_Duplicates(); break;
					case TextEditCommand.Select_Marks: editor.Command_Select_Marks(); break;
					case TextEditCommand.Select_Find: editor.Command_Select_Find(); break;
					case TextEditCommand.Select_Min_String: editor.Command_Select_Min_String(); break;
					case TextEditCommand.Select_Min_Numeric: editor.Command_Select_Min_Numeric(); break;
					case TextEditCommand.Select_Max_String: editor.Command_Select_Max_String(); break;
					case TextEditCommand.Select_Max_Numeric: editor.Command_Select_Max_Numeric(); break;
					case TextEditCommand.Select_ExpressionMatches: editor.Command_Select_ExpressionMatches(TextEditMenuItem.LastClick == MouseButton.Left); break;
					case TextEditCommand.Select_RegExMatches: editor.Command_Select_RegExMatches(TextEditMenuItem.LastClick == MouseButton.Left); break;
					case TextEditCommand.Select_ShowFirst: editor.Command_Select_ShowFirst(); break;
					case TextEditCommand.Select_ShowCurrent: editor.Command_Select_ShowCurrent(); break;
					case TextEditCommand.Select_NextSelection: editor.Command_Select_NextSelection(); break;
					case TextEditCommand.Select_PrevSelection: editor.Command_Select_PrevSelection(); break;
					case TextEditCommand.Select_Single: editor.Command_Select_Single(); break;
					case TextEditCommand.Select_Remove: editor.Command_Select_Remove(); break;
					case TextEditCommand.Mark_Selection: editor.Command_Mark_Selection(); break;
					case TextEditCommand.Mark_Find: editor.Command_Mark_Find(); break;
					case TextEditCommand.Mark_Clear: editor.Command_Mark_Clear(); break;
					case TextEditCommand.Mark_LimitToSelection: editor.Command_Mark_LimitToSelection(); break;
					case TextEditCommand.View_Highlighting_None: editor.HighlightType = Highlighting.HighlightingType.None; break;
					case TextEditCommand.View_Highlighting_CSharp: editor.HighlightType = Highlighting.HighlightingType.CSharp; break;
					case TextEditCommand.View_Highlighting_CPlusPlus: editor.HighlightType = Highlighting.HighlightingType.CPlusPlus; break;
				}

				if (editor.SelectionsInvalidated())
					editor.EnsureVisible();

				editor.InvalidateRender();
			}
		}

		void Add(TextEditor textEditor)
		{
			TextEditors.Add(textEditor);
			Active = textEditor;
		}

		Label GetLabel(TextEditor textEditor)
		{
			return textEditor.GetLabel();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			foreach (var item in Selected)
				if (item.HandleKey(e.Key))
					e.Handled = true;
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);

			if (e.OriginalSource is MenuItem)
				return;

			foreach (var item in Selected)
				item.HandleText(e.Text);
		}
	}
}
