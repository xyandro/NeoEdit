using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public class Tabs : Tabs<TextEditor> { }

	public partial class TextEditTabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> TextEditors { get { return UIHelper<TextEditTabs>.GetPropValue(() => this.TextEditors); } set { UIHelper<TextEditTabs>.SetPropValue(() => this.TextEditors, value); } }
		[DepProp]
		public TextEditor Active { get { return UIHelper<TextEditTabs>.GetPropValue(() => this.Active); } set { UIHelper<TextEditTabs>.SetPropValue(() => this.Active, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<TextEditTabs>.GetPropValue(() => this.View); } set { UIHelper<TextEditTabs>.SetPropValue(() => this.View, value); } }

		static TextEditTabs() { UIHelper<TextEditTabs>.Register(); }

		public static void Create(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = 1, int column = 1, bool createNew = false)
		{
			var textEditor = CreateTextEditor(filename, bytes, codePage, line, column);
			if (textEditor == null)
				return;

			((!createNew ? UIHelper<TextEditTabs>.GetNewest() : null) ?? new TextEditTabs()).Add(textEditor);
		}

		static TextEditor CreateTextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = -1, int column = -1)
		{
#if !DEBUG
			if (filename != null)
			{
				var fileInfo = new FileInfo(filename);
				if (fileInfo.Exists)
				{
					if (fileInfo.Length > 52428800) // 50 MB
					{
						switch (new Message
						{
							Title = "Confirm",
							Text = "The file you are trying to open is very large.  Would you like to open it in the text viewer instead?",
							Options = Message.OptionsEnum.YesNoCancel,
							DefaultAccept = Message.OptionsEnum.Yes,
							DefaultCancel = Message.OptionsEnum.Cancel,
						}.Show())
						{
							case Message.OptionsEnum.Yes: Launcher.Static.LaunchTextViewer(filename); return null;
							case Message.OptionsEnum.No: break;
							case Message.OptionsEnum.Cancel: return null;
						}
					}
				}
			}
#endif

			return new TextEditor(filename, bytes, codePage, line, column);
		}

		TextEditTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			TextEditors = new ObservableCollection<TextEditor>();
		}

		class OpenFileDialogResult
		{
			public List<string> files { get; set; }
		}

		OpenFileDialogResult Command_File_Open_Dialog()
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
				return null;

			return new OpenFileDialogResult { files = dialog.FileNames.ToList() };
		}

		void Command_File_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				Add(CreateTextEditor(filename));
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
				Add(CreateTextEditor(file));
		}

		const string quickMacroFilename = "Quick.xml";
		void Command_Macro_QuickRecord()
		{
			if (recordingMacro == null)
				Command_Macro_Record();
			else
				Command_Macro_StopRecording(quickMacroFilename);
		}

		void Command_Macro_QuickPlay()
		{
			Command_Macro_Play(quickMacroFilename);
		}

		void Command_Macro_Record()
		{
			if (recordingMacro != null)
			{
				new Message
				{
					Title = "Error",
					Text = String.Format("Cannot start recording; recording is already in progess."),
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			recordingMacro = new Macro();
		}

		string macroDirectory = Path.Combine(Path.GetDirectoryName(typeof(TextEditTabs).Assembly.Location), "Macro");
		void Command_Macro_StopRecording(string fileName = null)
		{
			if (recordingMacro == null)
			{
				new Message
				{
					Title = "Error",
					Text = String.Format("Cannot stop recording; recording not in progess."),
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var macro = recordingMacro;
			recordingMacro = null;

			Directory.CreateDirectory(macroDirectory);
			if (fileName == null)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "xml",
					Filter = "Macro files|*.xml|All files|*.*",
					FileName = "Macro.xml",
					InitialDirectory = macroDirectory,
				};
				if (dialog.ShowDialog() != true)
					return;

				fileName = dialog.FileName;
			}
			else
				fileName = Path.Combine(macroDirectory, fileName);

			XMLConverter.ToXML(macro).Save(fileName);
		}

		string ChooseMacro()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Macro files|*.xml|All files|*.*",
				InitialDirectory = macroDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.FileName;
		}

		void Command_Macro_Play(string macroFile = null)
		{
			if (macroFile == null)
			{
				macroFile = ChooseMacro();
				if (macroFile == null)
					return;
			}
			else
				macroFile = Path.Combine(macroDirectory, macroFile);

			XMLConverter.FromXML<Macro>(XElement.Load(macroFile)).Play(this, playing => macroPlaying = playing);
		}

		void Command_Macro_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(ClipboardWindow.GetStrings());
			var macroFile = ChooseMacro();
			if (macroFile == null)
				return;

			var macro = XMLConverter.FromXML<Macro>(XElement.Load(macroFile));
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				Add(CreateTextEditor(files.Dequeue()));
				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
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
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		Macro recordingMacro;
		internal bool macroPlaying = false;

		internal void RunCommand(TextEditCommand command)
		{
			if (macroPlaying)
				return;

			switch (command)
			{
				case TextEditCommand.Macro_QuickRecord: Command_Macro_QuickRecord(); return;
				case TextEditCommand.Macro_QuickPlay: Command_Macro_QuickPlay(); return;
				case TextEditCommand.Macro_Record: Command_Macro_Record(); return;
				case TextEditCommand.Macro_StopRecording: Command_Macro_StopRecording(); return;
				case TextEditCommand.Macro_Play: Command_Macro_Play(); return;
				case TextEditCommand.Macro_PlayOnCopiedFiles: Command_Macro_PlayOnCopiedFiles(); return;
			}

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			if (recordingMacro != null)
				recordingMacro.AddCommand(command, shiftDown, dialogResult);

			HandleCommand(command, shiftDown, dialogResult);
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			bool dialogResultSet = true;
			switch (command)
			{
				case TextEditCommand.File_Open: dialogResult = Command_File_Open_Dialog(); break;
				default: dialogResultSet = false; break;
			}

			if ((!dialogResultSet) && (Active != null))
			{
				dialogResultSet = true;
				switch (command)
				{
					case TextEditCommand.File_Encoding: dialogResult = Active.Command_File_Encoding_Dialog(); break;
					case TextEditCommand.File_ReopenWithEncoding: dialogResult = Active.Command_File_ReopenWithEncoding_Dialog(); break;
					case TextEditCommand.Edit_Find: dialogResult = Active.Command_Edit_FindReplace_Dialog(false); break;
					case TextEditCommand.Edit_Replace: dialogResult = Active.Command_Edit_FindReplace_Dialog(true); break;
					case TextEditCommand.Edit_GotoLine: dialogResult = Active.Command_Edit_Goto_Dialog(GotoDialog.GotoType.Line); break;
					case TextEditCommand.Edit_GotoColumn: dialogResult = Active.Command_Edit_Goto_Dialog(GotoDialog.GotoType.Column); break;
					case TextEditCommand.Edit_GotoPosition: dialogResult = Active.Command_Edit_Goto_Dialog(GotoDialog.GotoType.Position); break;
					case TextEditCommand.Files_Timestamp_Write:
					case TextEditCommand.Files_Timestamp_Access:
					case TextEditCommand.Files_Timestamp_Create:
					case TextEditCommand.Files_Timestamp_All:
						dialogResult = Active.Command_Files_Timestamp_Dialog();
						break;
					case TextEditCommand.Data_DateTime_Convert: dialogResult = Active.Command_Data_DateTime_Convert_Dialog(); break;
					case TextEditCommand.Data_Convert: dialogResult = Active.Command_Data_Convert_Dialog(); break;
					case TextEditCommand.Data_Width: dialogResult = Active.Command_Data_Width_Dialog(); break;
					case TextEditCommand.Data_Trim: dialogResult = Active.Command_Data_Trim_Dialog(); break;
					case TextEditCommand.Data_EvaluateExpression: dialogResult = Active.Command_Data_EvaluateExpression_Dialog(); break;
					case TextEditCommand.Data_Repeat: dialogResult = Active.Command_Data_Repeat_Dialog(); break;
					case TextEditCommand.Data_Hash_MD5: dialogResult = Active.Command_Data_Hash_Dialog(); break;
					case TextEditCommand.Data_Hash_SHA1: dialogResult = Active.Command_Data_Hash_Dialog(); break;
					case TextEditCommand.Data_Hash_SHA256: dialogResult = Active.Command_Data_Hash_Dialog(); break;
					case TextEditCommand.Data_Sort: dialogResult = Active.Command_Data_Sort_Dialog(); break;
					case TextEditCommand.Insert_RandomNumber: dialogResult = Active.Command_Insert_RandomNumber_Dialog(); break;
					case TextEditCommand.Insert_RandomData: dialogResult = Active.Command_Insert_RandomData_Dialog(); break;
					case TextEditCommand.Insert_MinMaxValues: dialogResult = Active.Command_Insert_MinMaxValues_Dialog(); break;
					case TextEditCommand.Select_Limit: dialogResult = Active.Command_Select_Limit_Dialog(); break;
					case TextEditCommand.Select_Width: dialogResult = Active.Command_Select_Width_Dialog(); break;
					case TextEditCommand.Select_Count: dialogResult = Active.Command_Select_Count_Dialog(); break;
					case TextEditCommand.Select_ExpressionMatches: dialogResult = Active.Command_Select_ExpressionMatches_Dialog(); break;
					default: dialogResultSet = false; break;
				}
			}

			return (!dialogResultSet) || (dialogResult != null);
		}

		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TextEditCommand.File_New: Add(CreateTextEditor()); break;
				case TextEditCommand.File_Open: Command_File_Open(dialogResult as OpenFileDialogResult); break;
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
				case TextEditCommand.File_Encoding: Active.Command_File_Encoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_ReopenWithEncoding: Active.Command_File_ReopenWithEncoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_HexEditor: if (Active.Command_File_HexEditor()) { TextEditors.Remove(Active); if (TextEditors.Count == 0) Close(); } break;
				case TextEditCommand.Edit_Undo: Active.Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Active.Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Cut: Active.Command_Edit_CutCopy(true); break;
				case TextEditCommand.Edit_Copy: Active.Command_Edit_CutCopy(false); break;
				case TextEditCommand.Edit_Paste: Active.Command_Edit_Paste(shiftDown); break;
				case TextEditCommand.Edit_ShowClipboard: Active.Command_Edit_ShowClipboard(); break;
				case TextEditCommand.Edit_Find: Active.Command_Edit_FindReplace(false, shiftDown, dialogResult as GetRegExDialog.Result); break;
				case TextEditCommand.Edit_FindNext: Active.Command_Edit_FindNextPrev(true, shiftDown); break;
				case TextEditCommand.Edit_FindPrev: Active.Command_Edit_FindNextPrev(false, shiftDown); break;
				case TextEditCommand.Edit_Replace: Active.Command_Edit_FindReplace(true, shiftDown, dialogResult as GetRegExDialog.Result); break;
				case TextEditCommand.Edit_GotoLine: Active.Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_GotoColumn: Active.Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_GotoPosition: Active.Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_ToggleBookmark: Active.Command_Edit_ToggleBookmark(); break;
				case TextEditCommand.Edit_NextBookmark: Active.Command_Edit_NextPreviousBookmark(true, shiftDown); break;
				case TextEditCommand.Edit_PreviousBookmark: Active.Command_Edit_NextPreviousBookmark(false, shiftDown); break;
				case TextEditCommand.Edit_ClearBookmarks: Active.Command_Edit_ClearBookmarks(); break;
				case TextEditCommand.Files_Copy: Active.Command_Files_CutCopy(false); break;
				case TextEditCommand.Files_Cut: Active.Command_Files_CutCopy(true); break;
				case TextEditCommand.Files_Open: Active.Command_Files_Open(); break;
				case TextEditCommand.Files_Delete: Active.Command_Files_Delete(); break;
				case TextEditCommand.Files_Timestamp_Write: Active.Command_Files_Timestamp(TextEditor.TimestampType.Write, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Timestamp_Access: Active.Command_Files_Timestamp(TextEditor.TimestampType.Access, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Timestamp_Create: Active.Command_Files_Timestamp(TextEditor.TimestampType.Create, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Timestamp_All: Active.Command_Files_Timestamp(TextEditor.TimestampType.All, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Simplify: Active.Command_Files_Simplify(); break;
				case TextEditCommand.Files_CreateDirectory: Active.Command_Files_CreateDirectory(); break;
				case TextEditCommand.Files_Information_Size: Active.Command_Files_Information_Size(); break;
				case TextEditCommand.Files_Information_WriteTime: Active.Command_Files_Information_WriteTime(); break;
				case TextEditCommand.Files_Information_AccessTime: Active.Command_Files_Information_AccessTime(); break;
				case TextEditCommand.Files_Information_CreateTime: Active.Command_Files_Information_CreateTime(); break;
				case TextEditCommand.Files_Information_Attributes: Active.Command_Files_Information_Attributes(); break;
				case TextEditCommand.Files_Information_ReadOnly: Active.Command_Files_Information_ReadOnly(); break;
				case TextEditCommand.Files_Hash_MD5: Active.Command_Files_Hash(Hash.Type.MD5); break;
				case TextEditCommand.Files_Hash_SHA1: Active.Command_Files_Hash(Hash.Type.SHA1); break;
				case TextEditCommand.Files_Hash_SHA256: Active.Command_Files_Hash(Hash.Type.SHA256); break;
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
				case TextEditCommand.Files_Operations_CreateFiles: Active.Command_Files_Files_Operations_CreateFiles(); break;
				case TextEditCommand.Files_Operations_CopyKeysToSelections: Active.Command_Files_Operations_CopyMoveKeysToSelections(false); break;
				case TextEditCommand.Files_Operations_MoveKeysToSelections: Active.Command_Files_Operations_CopyMoveKeysToSelections(true); break;
				case TextEditCommand.Data_Case_Upper: Active.Command_Data_Case_Upper(); break;
				case TextEditCommand.Data_Case_Lower: Active.Command_Data_Case_Lower(); break;
				case TextEditCommand.Data_Case_Proper: Active.Command_Data_Case_Proper(); break;
				case TextEditCommand.Data_Case_Toggle: Active.Command_Data_Case_Toggle(); break;
				case TextEditCommand.Data_Hex_ToHex: Active.Command_Data_Hex_ToHex(); break;
				case TextEditCommand.Data_Hex_FromHex: Active.Command_Data_Hex_FromHex(); break;
				case TextEditCommand.Data_DateTime_Insert: Active.Command_Data_DateTime_Insert(); break;
				case TextEditCommand.Data_DateTime_Convert: Active.Command_Data_DateTime_Convert(dialogResult as ConvertDateTimeDialog.Result); break;
				case TextEditCommand.Data_Convert: Active.Command_Data_Convert(dialogResult as ConvertDialog.Result); break;
				case TextEditCommand.Data_Length: Active.Command_Data_Length(); break;
				case TextEditCommand.Data_Width: Active.Command_Data_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Data_Trim: Active.Command_Data_Trim(dialogResult as TrimDialog.Result); break;
				case TextEditCommand.Data_SingleLine: Active.Command_Data_SingleLine(); break;
				case TextEditCommand.Data_Table_ToTable: Active.Command_Data_ToTable(); break;
				case TextEditCommand.Data_Table_FromTable: Active.Command_Data_FromTable(); break;
				case TextEditCommand.Data_EvaluateExpression: Active.Command_Data_EvaluateExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Data_EvaluateSelectedExpression: Active.Command_Data_EvaluateSelectedExpression(); break;
				case TextEditCommand.Data_Series: Active.Command_Data_Series(); break;
				case TextEditCommand.Data_CopyDown: Active.Command_Data_CopyDown(); break;
				case TextEditCommand.Data_Copy_Count: Active.Command_Data_Copy_Count(); break;
				case TextEditCommand.Data_Copy_Length: Active.Command_Data_Copy_Length(); break;
				case TextEditCommand.Data_Copy_Min_String: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Min_Numeric: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Min_Length: Active.Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Max_String: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Max_Numeric: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Max_Length: Active.Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Sum: Active.Command_Data_Copy_Sum(); break;
				case TextEditCommand.Data_Copy_Lines: Active.Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Line); break;
				case TextEditCommand.Data_Copy_Columns: Active.Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Column); break;
				case TextEditCommand.Data_Copy_Positions: Active.Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Position); break;
				case TextEditCommand.Data_Repeat: Active.Command_Data_Repeat(dialogResult as RepeatDialog.Result); break;
				case TextEditCommand.Data_Escape_XML: Active.Command_Data_Escape_XML(); break;
				case TextEditCommand.Data_Escape_Regex: Active.Command_Data_Escape_Regex(); break;
				case TextEditCommand.Data_Escape_URL: Active.Command_Data_Escape_URL(); break;
				case TextEditCommand.Data_Unescape_XML: Active.Command_Data_Unescape_XML(); break;
				case TextEditCommand.Data_Unescape_Regex: Active.Command_Data_Unescape_Regex(); break;
				case TextEditCommand.Data_Unescape_URL: Active.Command_Data_Unescape_URL(); break;
				case TextEditCommand.Data_Hash_MD5: Active.Command_Data_Hash(Hash.Type.MD5, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Hash_SHA1: Active.Command_Data_Hash(Hash.Type.SHA1, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Hash_SHA256: Active.Command_Data_Hash(Hash.Type.SHA256, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Sort: Active.Command_Data_Sort(dialogResult as SortDialog.Result); break;
				case TextEditCommand.Insert_GUID: Active.Command_Insert_GUID(); break;
				case TextEditCommand.Insert_RandomNumber: Active.Command_Insert_RandomNumber(dialogResult as RandomNumberDialog.Result); break;
				case TextEditCommand.Insert_RandomData: Active.Command_Insert_RandomData(dialogResult as RandomDataDialog.Result); break;
				case TextEditCommand.Insert_MinMaxValues: Active.Command_Insert_MinMaxValues(dialogResult as MinMaxValuesDialog.Result); break;
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
				case TextEditCommand.SelectRegion_Toggle: Active.Command_SelectRegion_Toggle(); break;
				case TextEditCommand.Select_All: Active.Command_Select_All(); break;
				case TextEditCommand.Select_Limit: Active.Command_Select_Limit(dialogResult as LimitDialog.Result); break;
				case TextEditCommand.Select_Lines: Active.Command_Select_Lines(); break;
				case TextEditCommand.Select_Empty: Active.Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Active.Command_Select_Empty(false); break;
				case TextEditCommand.Select_Trim: Active.Command_Select_Trim(); break;
				case TextEditCommand.Select_Width: Active.Command_Select_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Select_Unique: Active.Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Active.Command_Select_Duplicates(); break;
				case TextEditCommand.Select_Count: Active.Command_Select_Count(dialogResult as CountDialog.Result); break;
				case TextEditCommand.Select_Regions: Active.Command_Select_Regions(); break;
				case TextEditCommand.Select_FindResults: Active.Command_Select_FindResults(); break;
				case TextEditCommand.Select_Min_String: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Min_Numeric: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Min_Length: Active.Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_Max_String: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Max_Numeric: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Max_Length: Active.Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_ExpressionMatches: Active.Command_Select_ExpressionMatches(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Select_FirstSelection: Active.Command_Select_FirstSelection(); break;
				case TextEditCommand.Select_ShowCurrent: Active.Command_Select_ShowCurrent(); break;
				case TextEditCommand.Select_NextSelection: Active.Command_Select_NextSelection(); break;
				case TextEditCommand.Select_PrevSelection: Active.Command_Select_PrevSelection(); break;
				case TextEditCommand.Select_Single: Active.Command_Select_Single(); break;
				case TextEditCommand.Select_Remove: Active.Command_Select_Remove(); break;
				case TextEditCommand.Region_SetSelection: Active.Command_Region_SetSelection(); break;
				case TextEditCommand.Region_SetFindResults: Active.Command_Region_SetFindResults(); break;
				case TextEditCommand.Region_ClearRegions: Active.Command_Region_ClearRegions(); break;
				case TextEditCommand.Region_LimitToSelection: Active.Command_Region_LimitToSelection(); break;
				case TextEditCommand.View_Highlighting_None: Active.HighlightType = Highlighting.HighlightingType.None; break;
				case TextEditCommand.View_Highlighting_CSharp: Active.HighlightType = Highlighting.HighlightingType.CSharp; break;
				case TextEditCommand.View_Highlighting_CPlusPlus: Active.HighlightType = Highlighting.HighlightingType.CPlusPlus; break;
			}
		}

		void Add(TextEditor textEditor)
		{
			if (textEditor == null)
				return;

			if ((Active != null) && (Active.FileName == null) && (Active.Empty()))
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

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (macroPlaying)
				return;

			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddKey(e.Key, shiftDown, controlDown);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			if (Active == null)
				return false;
			return Active.HandleKey(key, shiftDown, controlDown);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if (macroPlaying)
				return;

			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			e.Handled = HandleText(e.Text);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddText(e.Text);
		}

		internal bool HandleText(string text)
		{
			if (Active == null)
				return false;
			return Active.HandleText(text);
		}
	}
}
